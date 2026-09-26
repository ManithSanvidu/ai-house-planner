using System.Text.Json;
using HousePlanner.API.Data;
using HousePlanner.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Services;

public sealed class PreDesignedPlanSeeder(
    ApplicationDbContext db,
    IPreDesignedPlanLayoutValidator layoutValidator,
    IWebHostEnvironment environment,
    ILogger<PreDesignedPlanSeeder> logger)
{
    private sealed record SeedCapabilities(
        bool? balcony, bool? veranda, bool? home_office, bool? utility_room,
        bool? parking_required, bool? accessibility);

    private sealed record SeedPlan(
        string designCode, string name,
        int bedrooms, int bathrooms, int floors,
        decimal minimumLandSizePerches, decimal? minimumPlotWidthFt, decimal? minimumPlotLengthFt,
        List<string>? supportedTerrains, List<string>? supportedStyles,
        SeedCapabilities? capabilities,
        JsonElement layout);

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(environment.ContentRootPath, "Data", "Seed", "pre-designed-plans.json");
        if (!File.Exists(path))
        {
            logger.LogWarning("Pre-designed plan catalog was not found at {Path}", path);
            return;
        }

        await using var stream = File.OpenRead(path);
        var plans = await JsonSerializer.DeserializeAsync<List<SeedPlan>>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken) ?? [];
        var existingPlans = (await db.PreDesignedHousePlans.ToListAsync(cancellationToken))
            .ToDictionary(plan => plan.DesignCode, StringComparer.OrdinalIgnoreCase);

        foreach (var seed in plans)
        {
            var actualBathrooms = LayoutRoomCounts.Bathrooms(seed.layout);
            if (seed.bathrooms != actualBathrooms)
            {
                logger.LogWarning("Skipping catalogue plan {Code}: bathroom count mismatch (declared {Declared}, actual {Actual}).",
                    seed.designCode, seed.bathrooms, actualBathrooms);
                continue;
            }
            var errors = layoutValidator.Validate(seed.layout, seed.bedrooms, seed.floors);
            if (errors.Count > 0)
                throw new InvalidDataException($"Seed plan {seed.designCode} is invalid: {string.Join(" ", errors)}");

            decimal totalArea = 0;
            if (seed.layout.TryGetProperty("rooms", out var roomsJson) && roomsJson.ValueKind == JsonValueKind.Array)
            {
                foreach (var r in roomsJson.EnumerateArray())
                {
                    totalArea += r.GetProperty("width").GetDecimal() * r.GetProperty("length").GetDecimal();
                }
            }

            var entity = existingPlans.GetValueOrDefault(seed.designCode);
            if (entity is null)
            {
                entity = new PreDesignedHousePlan
                {
                    Name = seed.name,
                    Slug = seed.designCode.ToLowerInvariant(),
                    DesignCode = seed.designCode,
                    Description = "AI Generated Plan",
                    Style = seed.supportedStyles?.FirstOrDefault() ?? "Modern",
                    Bedrooms = seed.bedrooms,
                    Bathrooms = seed.bathrooms,
                    FloorCount = seed.floors,
                    TotalBuiltUpAreaSqft = totalArea,
                    MinimumLandSizePerches = seed.minimumLandSizePerches,
                    MinimumPlotWidthFt = seed.minimumPlotWidthFt,
                    MinimumPlotLengthFt = seed.minimumPlotLengthFt,
                    SuitableTerrain = seed.supportedTerrains?.FirstOrDefault() ?? "Flat",
                    Category = "Standard",
                    TagsJson = "[]",
                    LayoutJson = seed.layout.GetRawText(),
                    IsActive = true
                };
                db.PreDesignedHousePlans.Add(entity);
            }

            // Capabilities are always re-derived from geometry so an existing database
            // cannot retain stale feature flags after program rules change.
            entity.ParkingSpaces = LayoutRoomCounts.HasParking(seed.layout) ? 1 : 0;
            entity.HasBalcony = LayoutRoomCounts.HasBalcony(seed.layout);
            entity.HasVeranda = LayoutRoomCounts.HasVeranda(seed.layout);
            entity.HasOffice = LayoutRoomCounts.HasOffice(seed.layout);
            entity.HasUtilityRoom = LayoutRoomCounts.HasUtilityRoom(seed.layout);
            entity.HasOpenPlan = LayoutRoomCounts.HasOpenPlan(seed.layout);
            entity.HasMasterEnsuite = LayoutRoomCounts.HasMasterEnsuite(seed.layout);
            entity.HasSeparateDining = LayoutRoomCounts.HasSeparateDining(seed.layout);
            entity.IsAccessibleFriendly = LayoutRoomCounts.IsAccessibleFriendly(seed.layout);
            entity.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var seedCodes = plans.Select(p => p.designCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var toRemove = existingPlans.Values.Where(p => !seedCodes.Contains(p.DesignCode)).ToList();
        if (toRemove.Count > 0)
        {
            logger.LogInformation("Removing {Count} stale pre-designed plans from database", toRemove.Count);
            db.PreDesignedHousePlans.RemoveRange(toRemove);
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Pre-designed plan catalog ready with {Count} entries",
            await db.PreDesignedHousePlans.CountAsync(cancellationToken));
    }
}
