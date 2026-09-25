using System.Text.Json;
using HousePlanner.API.Data;
using HousePlanner.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Services;

/// <summary>
/// Inserts the initial manually maintained pricing catalogue without overwriting
/// rates subsequently updated by a Constructor.
/// </summary>
public sealed class PricingDataSeeder(
    ApplicationDbContext db,
    IWebHostEnvironment environment,
    TimeProvider timeProvider,
    ILogger<PricingDataSeeder> logger)
{
    private const string ManualProvider = "Manual";
    private const string SeedRelativePath = "Data/Pricing/approved-pricing-feed.json";

    private sealed record PricingSeedFeed(List<PricingSeedRecord> Records);

    private sealed record PricingSeedRecord(
        string ItemName,
        string Category,
        string DisplayGroup,
        string Unit,
        decimal UnitCostLkr,
        TerrainMultiplierData TerrainMultiplier,
        string Provider,
        string Region,
        string QualityLevel,
        bool IsActive,
        string SourceReference);

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(environment.ContentRootPath, "Data", "Pricing", "approved-pricing-feed.json");
        if (!File.Exists(path))
            throw new FileNotFoundException($"The pricing seed file '{SeedRelativePath}' was not found.", path);

        await using var stream = File.OpenRead(path);
        var feed = await JsonSerializer.DeserializeAsync<PricingSeedFeed>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken)
            ?? throw new InvalidDataException("The pricing seed file is empty.");

        Validate(feed.Records);

        var existingItems = await db.PricingItems
            .AsNoTracking()
            .Select(item => new { item.ItemName, item.Region, item.QualityLevel })
            .ToListAsync(cancellationToken);
        var knownKeys = existingItems
            .Select(item => Key(item.ItemName, item.Region, item.QualityLevel))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var now = timeProvider.GetUtcNow();
        var inserted = 0;

        foreach (var seed in feed.Records)
        {
            // Constructor edits are authoritative. Existing rows are deliberately
            // left untouched whenever the application starts again.
            if (!knownKeys.Add(Key(seed.ItemName, seed.Region, seed.QualityLevel)))
                continue;

            db.PricingItems.Add(new PricingData
            {
                ItemName = seed.ItemName,
                Category = seed.Category,
                DisplayGroup = seed.DisplayGroup,
                Unit = seed.Unit,
                UnitCostLkr = seed.UnitCostLkr,
                TerrainMultiplier = new TerrainMultiplierData
                {
                    Flat = seed.TerrainMultiplier.Flat,
                    Hillside = seed.TerrainMultiplier.Hillside,
                    Coastal = seed.TerrainMultiplier.Coastal
                },
                Provider = ManualProvider,
                Region = seed.Region,
                QualityLevel = seed.QualityLevel,
                IsActive = seed.IsActive,
                SourceReference = seed.SourceReference,
                ImportedAt = null,
                CreatedAt = now,
                UpdatedAt = now
            });
            inserted++;
        }

        if (inserted > 0)
            await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Manual pricing catalogue ready. Inserted {InsertedCount}; {ExistingCount} seed records already existed.",
            inserted, feed.Records.Count - inserted);
    }

    private static void Validate(IReadOnlyList<PricingSeedRecord>? records)
    {
        if (records is null || records.Count == 0)
            throw new InvalidDataException("The pricing seed must contain at least one record.");

        var duplicateName = records
            .GroupBy(record => Key(record.ItemName, record.Region, record.QualityLevel), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicateName is not null)
            throw new InvalidDataException($"The pricing seed contains duplicate catalogue key '{duplicateName}'.");

        foreach (var record in records)
        {
            if (string.IsNullOrWhiteSpace(record.ItemName) || string.IsNullOrWhiteSpace(record.DisplayGroup))
                throw new InvalidDataException("Every pricing seed record requires an item name and display group.");
            if (record.Category is not ("material" or "labour"))
                throw new InvalidDataException($"Pricing seed item '{record.ItemName}' has an unsupported category.");
            if (record.Category == "material" && record.Unit != "per_sqft")
                throw new InvalidDataException($"Material seed item '{record.ItemName}' must use unit 'per_sqft'.");
            if (record.Category == "labour" && record.Unit != "factor")
                throw new InvalidDataException($"Labour seed item '{record.ItemName}' must use unit 'factor'.");
            if (!string.Equals(record.Provider, ManualProvider, StringComparison.Ordinal))
                throw new InvalidDataException($"Pricing seed item '{record.ItemName}' must use provider 'Manual'.");
            if (record.QualityLevel is not ("Basic" or "Standard" or "Premium" or "Luxury"))
                throw new InvalidDataException($"Pricing seed item '{record.ItemName}' has an unsupported quality level.");
            if (!record.IsActive)
                throw new InvalidDataException($"Initial pricing seed item '{record.ItemName}' must be active.");
            if (record.UnitCostLkr <= 0 || record.TerrainMultiplier is null ||
                record.TerrainMultiplier.Flat <= 0 || record.TerrainMultiplier.Hillside <= 0 ||
                record.TerrainMultiplier.Coastal <= 0)
                throw new InvalidDataException($"Pricing seed item '{record.ItemName}' has a non-positive price or terrain multiplier.");
        }

        var labour = records.Where(record => record.Category == "labour").ToList();
        if (labour.Count == 0 || labour.GroupBy(record => $"{record.Region}|{record.QualityLevel}", StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            throw new InvalidDataException("The pricing seed must contain one labour factor per regional quality catalogue.");
    }

    private static string Key(string itemName, string region, string qualityLevel) =>
        $"{itemName.Trim()}|{region.Trim()}|{qualityLevel.Trim()}";
}
