using System.Text.Json;
using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using HousePlanner.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Services;

public class PricingService : IPricingService
{
    private readonly ApplicationDbContext _context;
    private readonly IExternalPricingProvider _provider;
    private readonly IPricingNormalizationService _normalizer;
    private readonly TimeProvider _timeProvider;

    public PricingService(ApplicationDbContext context, IExternalPricingProvider provider,
        IPricingNormalizationService normalizer, TimeProvider timeProvider)
    {
        _context = context;
        _provider = provider;
        _normalizer = normalizer;
        _timeProvider = timeProvider;
    }

    public async Task<IEnumerable<PricingDto>> GetAllPricingAsync()
    {
        var items = await _context.PricingItems.AsNoTracking().ToListAsync();
        return items.Select(MapToDto);
    }

    public async Task<PricingDto?> GetPricingByIdAsync(int id)
    {
        var item = await _context.PricingItems.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        return item is null ? null : MapToDto(item);
    }

    public async Task<PricingDto?> UpdatePricingAsync(int id, UpdatePricingDto updateDto)
    {
        var item = await _context.PricingItems.FindAsync(id);
        if (item == null) return null;

        item.UnitCostLkr = updateDto.UnitCostLkr;
        if (updateDto.TerrainMultiplier != null)
        {
            item.TerrainMultiplier.Flat = updateDto.TerrainMultiplier.Flat;
            item.TerrainMultiplier.Hillside = updateDto.TerrainMultiplier.Hillside;
            item.TerrainMultiplier.Coastal = updateDto.TerrainMultiplier.Coastal;
        }

        item.UpdatedAt = _timeProvider.GetUtcNow();
        await _context.SaveChangesAsync();
        return MapToDto(item);
    }

    public async Task<PricingSyncResultDto> SyncExternalPricingAsync(CancellationToken cancellationToken = default)
    {
        var audit = new PricingImportAudit
        {
            Id = Guid.NewGuid(),
            Provider = _provider.Name,
            Status = "running",
            StartedAt = _timeProvider.GetUtcNow()
        };
        _context.PricingImportAudits.Add(audit);

        IReadOnlyList<ExternalPriceRecord> records;
        try
        {
            records = await _provider.GetPricesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var issue = new PricingSyncIssueDto(null, ex.Message, "failed");
            audit.Status = "failed";
            audit.FailedCount = 1;
            audit.CompletedAt = _timeProvider.GetUtcNow();
            audit.FailureMessage = ex.Message;
            audit.DetailsJson = JsonSerializer.Serialize(new[] { issue });
            await _context.SaveChangesAsync(cancellationToken);
            throw new PricingSyncException("The external pricing provider failed.", ToResult(audit, [issue]), ex);
        }

        var existingItems = await _context.PricingItems
            .Where(item => item.Provider == _provider.Name && item.ExternalItemId != null)
            .ToListAsync(cancellationToken);
        var byExternalId = existingItems.ToDictionary(item => item.ExternalItemId!, StringComparer.OrdinalIgnoreCase);
        var issues = new List<PricingSyncIssueDto>();

        foreach (var record in records)
        {
            if (record is null)
            {
                audit.FailedCount++;
                issues.Add(new(null, "The provider returned a null record.", "failed"));
                continue;
            }

            var result = _normalizer.Normalize(record);
            if (result.Status != PricingNormalizationStatus.Normalized || result.Value is null)
            {
                var outcome = result.Status == PricingNormalizationStatus.Skipped ? "skipped" : "failed";
                if (result.Status == PricingNormalizationStatus.Skipped) audit.SkippedCount++;
                else audit.FailedCount++;
                issues.Add(new(record.ExternalItemId, result.Reason ?? "Record could not be normalized.", outcome));
                continue;
            }

            var externalItemId = record.ExternalItemId.Trim();
            if (!byExternalId.TryGetValue(externalItemId, out var item))
            {
                item = new PricingData
                {
                    Provider = _provider.Name,
                    ExternalItemId = externalItemId,
                    TerrainMultiplier = new TerrainMultiplierData()
                };
                _context.PricingItems.Add(item);
                byExternalId[externalItemId] = item;
            }

            ApplyNormalizedRecord(item, record, result.Value, _provider.Name, _timeProvider.GetUtcNow());
            audit.ImportedCount++;
        }

        audit.Status = "succeeded";
        audit.CompletedAt = _timeProvider.GetUtcNow();
        audit.DetailsJson = JsonSerializer.Serialize(issues);
        await _context.SaveChangesAsync(cancellationToken);
        return ToResult(audit, issues);
    }

    private static void ApplyNormalizedRecord(PricingData item, ExternalPriceRecord record,
        NormalizedExternalPrice normalized, string provider, DateTimeOffset importedAt)
    {
        item.ItemName = normalized.ItemName;
        item.Category = normalized.Category;
        item.UnitCostLkr = normalized.UnitCostLkr;
        item.Unit = normalized.Unit;
        item.DisplayGroup = normalized.DisplayGroup;
        item.TerrainMultiplier.Flat = normalized.FlatMultiplier;
        item.TerrainMultiplier.Hillside = normalized.HillsideMultiplier;
        item.TerrainMultiplier.Coastal = normalized.CoastalMultiplier;
        item.Provider = provider;
        item.ExternalItemId = record.ExternalItemId.Trim();
        item.ExternalItemName = record.Name.Trim();
        item.OriginalUnit = record.Unit.Trim();
        item.OriginalPrice = record.Price;
        item.OriginalCurrency = record.Currency.Trim().ToUpperInvariant();
        item.Region = record.Region?.Trim();
        item.ObservedAt = record.ObservedAt;
        item.EffectiveAt = record.EffectiveAt;
        item.SourceUrl = record.SourceUrl?.Trim();
        item.SourceReference = record.SourceReference?.Trim();
        item.ImportedAt = importedAt;
        item.UpdatedAt = importedAt;
    }

    private static PricingSyncResultDto ToResult(PricingImportAudit audit, IReadOnlyList<PricingSyncIssueDto> issues) => new()
    {
        AuditId = audit.Id,
        Provider = audit.Provider,
        ImportedCount = audit.ImportedCount,
        SkippedCount = audit.SkippedCount,
        FailedCount = audit.FailedCount,
        StartedAt = audit.StartedAt,
        CompletedAt = audit.CompletedAt ?? audit.StartedAt,
        Issues = issues
    };

    private static PricingDto MapToDto(PricingData entity) => new()
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
        ObservedAt = entity.ObservedAt,
        EffectiveAt = entity.EffectiveAt,
        SourceUrl = entity.SourceUrl,
        SourceReference = entity.SourceReference,
        ImportedAt = entity.ImportedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
