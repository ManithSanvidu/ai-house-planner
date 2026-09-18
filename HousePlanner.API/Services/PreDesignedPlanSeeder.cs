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
    private sealed record SeedPlan(
        string Name, string Slug, string DesignCode, string? Description, string Style,
        int Bedrooms, int Bathrooms, int FloorCount, decimal TotalBuiltUpAreaSqft,
        decimal MinimumLandSizePerches, decimal? MinimumPlotWidthFt, decimal? MinimumPlotLengthFt,
        string SuitableTerrain, int ParkingSpaces, bool HasBalcony, bool HasVeranda,
        bool HasOffice, bool HasUtilityRoom, bool IsAccessibleFriendly, string? Category,
        string[] Tags, JsonElement Layout, bool IsActive);

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
        var existingCodes = (await db.PreDesignedHousePlans
            .Select(plan => plan.DesignCode)
            .ToListAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var seed in plans.Where(plan => !existingCodes.Contains(plan.DesignCode)))
        {
            var actualBathrooms = LayoutRoomCounts.Bathrooms(seed.Layout);
            if (seed.Bathrooms != actualBathrooms)
            {
                logger.LogWarning("Skipping catalogue plan {Code}: bathroom count mismatch (declared {Declared}, actual {Actual}).",
                    seed.DesignCode, seed.Bathrooms, actualBathrooms);
                continue;
            }
            var errors = layoutValidator.Validate(seed.Layout, seed.Bedrooms, seed.FloorCount);
            if (errors.Count > 0)
                throw new InvalidDataException($"Seed plan {seed.DesignCode} is invalid: {string.Join(" ", errors)}");

            db.PreDesignedHousePlans.Add(new PreDesignedHousePlan
            {
                Name = seed.Name,
                Slug = seed.Slug,
                DesignCode = seed.DesignCode,
                Description = seed.Description,
                Style = seed.Style,
                Bedrooms = seed.Bedrooms,
                Bathrooms = seed.Bathrooms,
                FloorCount = seed.FloorCount,
                TotalBuiltUpAreaSqft = seed.TotalBuiltUpAreaSqft,
                MinimumLandSizePerches = seed.MinimumLandSizePerches,
                MinimumPlotWidthFt = seed.MinimumPlotWidthFt,
                MinimumPlotLengthFt = seed.MinimumPlotLengthFt,
                SuitableTerrain = seed.SuitableTerrain,
                ParkingSpaces = seed.ParkingSpaces,
                HasBalcony = seed.HasBalcony,
                HasVeranda = seed.HasVeranda,
                HasOffice = seed.HasOffice,
                HasUtilityRoom = seed.HasUtilityRoom,
                IsAccessibleFriendly = seed.IsAccessibleFriendly,
                Category = seed.Category,
                TagsJson = JsonSerializer.Serialize(seed.Tags),
                LayoutJson = seed.Layout.GetRawText(),
                IsActive = seed.IsActive
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Pre-designed plan catalog ready with {Count} entries",
            await db.PreDesignedHousePlans.CountAsync(cancellationToken));
    }
}
