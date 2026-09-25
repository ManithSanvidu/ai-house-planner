using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Services;

public class PricingService : IPricingService
{
    private readonly ApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;

    public PricingService(ApplicationDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<IEnumerable<PricingDto>> GetAllPricingAsync()
    {
        var items = await _context.PricingItems.AsNoTracking()
            .OrderByDescending(item => item.IsActive)
            .ThenBy(item => item.DisplayGroup)
            .ThenBy(item => item.ItemName)
            .ThenBy(item => item.Region)
            .ThenBy(item => item.QualityLevel)
            .ToListAsync();
        var userNames = await LoadUserNamesAsync(items.Select(item => item.UpdatedByUserId));
        return items.Select(item => MapToDto(item, GetUserName(userNames, item.UpdatedByUserId)));
    }

    public async Task<IEnumerable<PricingDto>> GetActivePricingAsync(string? region, string? qualityLevel)
    {
        var requestedRegion = NormalizeRegion(region);
        var requestedQuality = NormalizeQualityLevel(qualityLevel);
        var regionKey = requestedRegion.ToLower();
        var defaultRegionKey = "sri lanka";

        var candidates = await _context.PricingItems.AsNoTracking()
            .Where(item => item.IsActive && item.QualityLevel == requestedQuality &&
                (item.Region.ToLower() == regionKey || item.Region.ToLower() == defaultRegionKey))
            .ToListAsync();

        var selected = candidates
            .GroupBy(item => item.ItemName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(item => item.Region.Equals(requestedRegion, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(item => item.Region.Equals("Sri Lanka", StringComparison.OrdinalIgnoreCase))
                .First())
            .OrderBy(item => item.DisplayGroup)
            .ThenBy(item => item.ItemName)
            .ToList();

        var userNames = await LoadUserNamesAsync(selected.Select(item => item.UpdatedByUserId));
        return selected.Select(item => MapToDto(item, GetUserName(userNames, item.UpdatedByUserId)));
    }

    public async Task<PricingDto?> GetPricingByIdAsync(int id)
    {
        var item = await _context.PricingItems.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        return item is null ? null : MapToDto(item, await GetUserNameAsync(item.UpdatedByUserId));
    }

    public async Task<PricingDto> CreatePricingAsync(CreatePricingDto createDto, string? updatedByUserId = null)
    {
        if (createDto is null)
            throw new ArgumentNullException(nameof(createDto));

        var itemName = createDto.ItemName?.Trim();
        if (string.IsNullOrWhiteSpace(itemName))
            throw new ArgumentException("ItemName is required.", nameof(createDto));

        var category = createDto.Category?.Trim().ToLowerInvariant();
        if (category is not ("material" or "labour"))
            throw new ArgumentException("Category must be 'material' or 'labour'.", nameof(createDto));

        ValidatePrice(category, createDto.UnitCostLkr);

        if (createDto.TerrainMultiplier is null ||
            createDto.TerrainMultiplier.Flat <= 0 ||
            createDto.TerrainMultiplier.Hillside <= 0 ||
            createDto.TerrainMultiplier.Coastal <= 0)
            throw new ArgumentException("TerrainMultiplier values must be provided and greater than zero.", nameof(createDto));

        var region = NormalizeRegion(createDto.Region);
        var qualityLevel = NormalizeQualityLevel(createDto.QualityLevel);

        // One active labour factor is allowed for each regional quality catalogue.
        if (category == "labour")
        {
            var hasLabourFactor = await _context.PricingItems
                .AnyAsync(p => p.IsActive && p.Category == "labour" &&
                    p.Region.ToLower() == region.ToLower() && p.QualityLevel == qualityLevel);

            if (hasLabourFactor)
            {
                throw new InvalidOperationException($"Only one active labour factor is allowed for region '{region}' and quality '{qualityLevel}'.");
            }
        }

        // Optional duplicate protection
        var duplicateExists = await _context.PricingItems.AnyAsync(p => p.IsActive &&
            p.ItemName.ToLower() == itemName.ToLower() &&
            p.Region.ToLower() == region.ToLower() && p.QualityLevel == qualityLevel);

        if (duplicateExists)
        {
            throw new InvalidOperationException($"An active pricing item named '{itemName}' already exists for region '{region}' and quality '{qualityLevel}'.");
        }

        // Canonical unit derivation
        var unit = category == "material" ? "per_sqft" : "factor";

        var displayGroup = string.IsNullOrWhiteSpace(createDto.DisplayGroup)
            ? (category == "labour" ? "Labour" : null)
            : createDto.DisplayGroup.Trim();

        var now = _timeProvider.GetUtcNow();
        var item = new PricingData
        {
            ItemName = itemName,
            Category = category,
            Unit = unit,
            UnitCostLkr = createDto.UnitCostLkr,
            DisplayGroup = displayGroup,
            TerrainMultiplier = new TerrainMultiplierData
            {
                Flat = createDto.TerrainMultiplier.Flat,
                Hillside = createDto.TerrainMultiplier.Hillside,
                Coastal = createDto.TerrainMultiplier.Coastal
            },
            Provider = "Manual",
            Region = region,
            QualityLevel = qualityLevel,
            IsActive = true,
            SourceReference = string.IsNullOrWhiteSpace(createDto.SourceReference) ? null : createDto.SourceReference.Trim(),
            ImportedAt = null,
            CreatedAt = now,
            UpdatedByUserId = NormalizeUserId(updatedByUserId),
            UpdatedAt = now
        };

        _context.PricingItems.Add(item);
        await _context.SaveChangesAsync();

        return MapToDto(item, await GetUserNameAsync(item.UpdatedByUserId));
    }

    public async Task<PricingDto?> UpdatePricingAsync(int id, UpdatePricingDto updateDto, string? updatedByUserId = null)
    {
        var item = await _context.PricingItems.FindAsync(id);
        if (item == null) return null;
        if (!item.IsActive)
            throw new InvalidOperationException("Inactive pricing records cannot be edited. Create a new active price instead.");

        ValidatePrice(item.Category, updateDto.UnitCostLkr);
        ValidateTerrainMultipliers(updateDto.TerrainMultiplier);
        var now = _timeProvider.GetUtcNow();

        _context.PricingHistory.Add(new PricingHistory
        {
            PricingDataId = item.Id,
            PreviousValue = item.UnitCostLkr,
            NewValue = updateDto.UnitCostLkr,
            ChangedByUserId = NormalizeUserId(updatedByUserId),
            ChangedAt = now,
            Reason = NormalizeReason(updateDto.Reason)
        });

        item.UnitCostLkr = updateDto.UnitCostLkr;
        if (updateDto.TerrainMultiplier != null)
        {
            item.TerrainMultiplier.Flat = updateDto.TerrainMultiplier.Flat;
            item.TerrainMultiplier.Hillside = updateDto.TerrainMultiplier.Hillside;
            item.TerrainMultiplier.Coastal = updateDto.TerrainMultiplier.Coastal;
        }

        item.UpdatedAt = now;
        item.UpdatedByUserId = NormalizeUserId(updatedByUserId);
        await _context.SaveChangesAsync();
        return MapToDto(item, await GetUserNameAsync(item.UpdatedByUserId));
    }

    public async Task<PricingDto?> DeactivatePricingAsync(int id, string? reason, string? updatedByUserId = null)
    {
        var item = await _context.PricingItems.FindAsync(id);
        if (item is null) return null;
        if (!item.IsActive) return MapToDto(item, await GetUserNameAsync(item.UpdatedByUserId));

        var now = _timeProvider.GetUtcNow();
        item.IsActive = false;
        item.UpdatedAt = now;
        item.UpdatedByUserId = NormalizeUserId(updatedByUserId);
        _context.PricingHistory.Add(new PricingHistory
        {
            PricingDataId = item.Id,
            PreviousValue = item.UnitCostLkr,
            NewValue = item.UnitCostLkr,
            ChangedByUserId = NormalizeUserId(updatedByUserId),
            ChangedAt = now,
            Reason = string.IsNullOrWhiteSpace(reason) ? "Pricing record deactivated." : $"Deactivated: {reason.Trim()}"
        });
        await _context.SaveChangesAsync();
        return MapToDto(item, await GetUserNameAsync(item.UpdatedByUserId));
    }

    public async Task<IReadOnlyList<PricingHistoryDto>> GetPricingHistoryAsync(int id)
    {
        var historyItems = await _context.PricingHistory.AsNoTracking()
            .Where(history => history.PricingDataId == id)
            .OrderByDescending(history => history.ChangedAt)
            .ToListAsync();
        var userNames = await LoadUserNamesAsync(historyItems.Select(history => history.ChangedByUserId));

        return historyItems.Select(history => new PricingHistoryDto
        {
            Id = history.Id,
            PricingDataId = history.PricingDataId,
            PreviousValue = history.PreviousValue,
            NewValue = history.NewValue,
            ChangedByUserId = history.ChangedByUserId,
            ChangedByName = GetUserName(userNames, history.ChangedByUserId),
            ChangedAt = history.ChangedAt,
            Reason = history.Reason
        })
            .ToList();
    }

    private static void ValidatePrice(string category, decimal value)
    {
        if (value <= 0)
            throw new ArgumentException("UnitCostLkr must be greater than zero.");
        if (category == "labour" && value > 1)
            throw new ArgumentException("A labour factor must be greater than zero and no more than 1.");
    }

    private static void ValidateTerrainMultipliers(TerrainMultiplierData? multipliers)
    {
        if (multipliers is null || multipliers.Flat <= 0 || multipliers.Hillside <= 0 || multipliers.Coastal <= 0)
            throw new ArgumentException("TerrainMultiplier values must be provided and greater than zero.");
    }

    private static string NormalizeRegion(string? region) =>
        string.IsNullOrWhiteSpace(region) ? "Sri Lanka" : region.Trim();

    private static string NormalizeQualityLevel(string? qualityLevel)
    {
        var normalized = string.IsNullOrWhiteSpace(qualityLevel) ? "Standard" : qualityLevel.Trim();
        return normalized.ToLowerInvariant() switch
        {
            "basic" => "Basic",
            "standard" => "Standard",
            "premium" => "Premium",
            "luxury" => "Luxury",
            _ => throw new ArgumentException("QualityLevel must be Basic, Standard, Premium, or Luxury.")
        };
    }

    private static string? NormalizeUserId(string? userId) =>
        string.IsNullOrWhiteSpace(userId) ? null : userId.Trim()[..Math.Min(userId.Trim().Length, 100)];

    private static string? NormalizeReason(string? reason) =>
        string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()[..Math.Min(reason.Trim().Length, 500)];

    private async Task<string?> GetUserNameAsync(string? userId)
    {
        var userNames = await LoadUserNamesAsync([userId]);
        return GetUserName(userNames, userId);
    }

    private async Task<IReadOnlyDictionary<string, string>> LoadUserNamesAsync(IEnumerable<string?> userIds)
    {
        var ids = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (ids.Count == 0)
            return new Dictionary<string, string>(StringComparer.Ordinal);

        var databaseIds = ids
            .Select(id => Guid.TryParse(id, out var parsed) ? parsed : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        var users = await _context.Users.AsNoTracking()
            .Where(user =>
                (user.SupabaseUid != null && ids.Contains(user.SupabaseUid)) ||
                databaseIds.Contains(user.Id))
            .Select(user => new { user.Id, user.SupabaseUid, user.FullName })
            .ToListAsync();

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var user in users)
        {
            result[user.Id.ToString()] = user.FullName;
            if (!string.IsNullOrWhiteSpace(user.SupabaseUid))
                result[user.SupabaseUid] = user.FullName;
        }

        return result;
    }

    private static string? GetUserName(IReadOnlyDictionary<string, string> userNames, string? userId) =>
        !string.IsNullOrWhiteSpace(userId) && userNames.TryGetValue(userId.Trim(), out var name)
            ? name
            : null;

    private static PricingDto MapToDto(PricingData entity, string? updatedByName = null) => new()
    {
        Id = entity.Id,
        ItemName = entity.ItemName,
        Category = entity.Category,
        UnitCostLkr = entity.UnitCostLkr,
        Unit = entity.Unit,
        TerrainMultiplier = new TerrainMultiplierData
        {
            Flat = entity.TerrainMultiplier.Flat,
            Hillside = entity.TerrainMultiplier.Hillside,
            Coastal = entity.TerrainMultiplier.Coastal
        },
        DisplayGroup = entity.DisplayGroup,
        Provider = entity.Provider,
        ExternalItemId = entity.ExternalItemId,
        ExternalItemName = entity.ExternalItemName,
        OriginalUnit = entity.OriginalUnit,
        OriginalPrice = entity.OriginalPrice,
        OriginalCurrency = entity.OriginalCurrency,
        Region = entity.Region,
        QualityLevel = entity.QualityLevel,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt,
        UpdatedByUserId = entity.UpdatedByUserId,
        UpdatedByName = updatedByName,
        ObservedAt = entity.ObservedAt,
        EffectiveAt = entity.EffectiveAt,
        SourceUrl = entity.SourceUrl,
        SourceReference = entity.SourceReference,
        ImportedAt = entity.ImportedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
