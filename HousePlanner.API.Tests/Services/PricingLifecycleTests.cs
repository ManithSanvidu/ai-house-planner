using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Tests.Services;

public sealed class PricingLifecycleTests
{
    [Fact]
    public async Task GetAll_ResolvesUpdaterNameFromSupabaseIdentity()
    {
        await using var db = CreateContext();
        var authId = Guid.NewGuid().ToString();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "constructor@example.com",
            SupabaseUid = authId,
            FullName = "Nimal Perera",
            RoleId = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var item = Material("Structural Materials", 4000m);
        item.UpdatedByUserId = authId;
        db.Users.Add(user);
        db.PricingItems.Add(item);
        await db.SaveChangesAsync();
        var service = new PricingService(db, TimeProvider.System);

        var result = Assert.Single(await service.GetAllPricingAsync());

        Assert.Equal(authId, result.UpdatedByUserId);
        Assert.Equal("Nimal Perera", result.UpdatedByName);
    }

    [Fact]
    public async Task Update_CreatesHistoryWithActorAndReason()
    {
        await using var db = CreateContext();
        var item = Material("Structural Materials", 4000m);
        db.PricingItems.Add(item);
        await db.SaveChangesAsync();
        var service = new PricingService(db, TimeProvider.System);

        await service.UpdatePricingAsync(item.Id, new UpdatePricingDto
        {
            UnitCostLkr = 4500m,
            TerrainMultiplier = Multipliers(),
            Reason = "October contractor review"
        }, "constructor-123");

        var history = await db.PricingHistory.SingleAsync();
        Assert.Equal(4000m, history.PreviousValue);
        Assert.Equal(4500m, history.NewValue);
        Assert.Equal("constructor-123", history.ChangedByUserId);
        Assert.Equal("October contractor review", history.Reason);
    }

    [Fact]
    public async Task Deactivate_PreservesRecordAndExcludesItFromAgentCatalogue()
    {
        await using var db = CreateContext();
        var item = Material("Roofing Materials", 2500m);
        db.PricingItems.Add(item);
        await db.SaveChangesAsync();
        var service = new PricingService(db, TimeProvider.System);

        await service.DeactivatePricingAsync(item.Id, "Superseded", "constructor-123");

        Assert.False((await db.PricingItems.FindAsync(item.Id))!.IsActive);
        Assert.Empty(await service.GetActivePricingAsync("Sri Lanka", "Standard"));
        Assert.Single(await service.GetAllPricingAsync());
        Assert.Single(await service.GetPricingHistoryAsync(item.Id));
    }

    [Fact]
    public async Task ActiveCatalogue_UsesRegionalOverrideAndSriLankaFallback()
    {
        await using var db = CreateContext();
        db.PricingItems.AddRange(
            Material("Foundation Materials", 3000m),
            Material("Structural Materials", 4000m),
            Material("Structural Materials", 4500m, "Colombo"),
            Labour(0.35m));
        await db.SaveChangesAsync();
        var service = new PricingService(db, TimeProvider.System);

        var prices = (await service.GetActivePricingAsync("Colombo", null)).ToList();

        Assert.Equal(3, prices.Count);
        Assert.Contains(prices, item => item.ItemName == "Structural Materials" && item.Region == "Colombo" && item.UnitCostLkr == 4500m);
        Assert.Contains(prices, item => item.ItemName == "Foundation Materials" && item.Region == "Sri Lanka");
        Assert.Contains(prices, item => item.Category == "labour" && item.Region == "Sri Lanka");
    }

    [Fact]
    public async Task Create_RejectsSecondActiveLabourFactorForSameRegionAndQuality()
    {
        await using var db = CreateContext();
        db.PricingItems.Add(Labour(0.35m));
        await db.SaveChangesAsync();
        var service = new PricingService(db, TimeProvider.System);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreatePricingAsync(new CreatePricingDto
        {
            ItemName = "New Labour",
            Category = "labour",
            UnitCostLkr = 0.4m,
            Region = "Sri Lanka",
            QualityLevel = "Standard",
            TerrainMultiplier = Multipliers()
        }));

        Assert.Contains("Only one active labour factor", error.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1.01)]
    public async Task Create_RejectsInvalidLabourFactor(decimal factor)
    {
        await using var db = CreateContext();
        var service = new PricingService(db, TimeProvider.System);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreatePricingAsync(new CreatePricingDto
        {
            ItemName = "Construction Labour",
            Category = "labour",
            UnitCostLkr = factor,
            TerrainMultiplier = Multipliers()
        }));
    }

    [Fact]
    public async Task ExistingCostEstimateSnapshot_DoesNotChangeWhenPriceChanges()
    {
        await using var db = CreateContext();
        var price = Material("Structural Materials", 4000m);
        var estimate = new CostEstimate
        {
            HouseDesignId = Guid.NewGuid(),
            MaterialCostLkr = 12_000_000m,
            LabourCostLkr = 4_200_000m,
            TotalCostLkr = 16_200_000m,
            BudgetDeltaPercent = 81m,
            PricingSnapshotJson = "[{\"itemName\":\"Structural Materials\",\"unitCostLkr\":4000}]"
        };
        db.PricingItems.Add(price);
        db.CostEstimates.Add(estimate);
        await db.SaveChangesAsync();
        var service = new PricingService(db, TimeProvider.System);

        await service.UpdatePricingAsync(price.Id, new UpdatePricingDto
        {
            UnitCostLkr = 4500m,
            TerrainMultiplier = Multipliers(),
            Reason = "New month"
        }, "constructor-123");

        Assert.Contains("4000", (await db.CostEstimates.FindAsync(estimate.Id))!.PricingSnapshotJson);
        Assert.Equal(12_000_000m, estimate.MaterialCostLkr);
    }

    private static PricingData Material(string name, decimal value, string region = "Sri Lanka") => new()
    {
        ItemName = name,
        Category = "material",
        Unit = "per_sqft",
        UnitCostLkr = value,
        DisplayGroup = "Structural",
        Region = region,
        QualityLevel = "Standard",
        IsActive = true,
        Provider = "Manual",
        TerrainMultiplier = Multipliers(),
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static PricingData Labour(decimal value) => new()
    {
        ItemName = "Construction Labour",
        Category = "labour",
        Unit = "factor",
        UnitCostLkr = value,
        DisplayGroup = "Labour",
        Region = "Sri Lanka",
        QualityLevel = "Standard",
        IsActive = true,
        Provider = "Manual",
        TerrainMultiplier = Multipliers(),
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private static TerrainMultiplierData Multipliers() => new() { Flat = 1m, Hillside = 1.15m, Coastal = 1.1m };

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
