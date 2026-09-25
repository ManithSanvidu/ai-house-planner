using HousePlanner.API.Controllers;
using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HousePlanner.API.Tests.Services;

public sealed class PricingDataSeederTests
{
    [Fact]
    public async Task SeedAsync_InsertsSixAgentCompatibleManualRates_AndIsIdempotent()
    {
        await using var db = CreateContext();
        var seeder = CreateSeeder(db);

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var items = await db.PricingItems.OrderBy(item => item.Id).ToListAsync();
        Assert.Equal(6, items.Count);
        Assert.Equal(5, items.Count(item => item.Category == "material" && item.Unit == "per_sqft"));

        var labour = Assert.Single(items, item => item.Category == "labour");
        Assert.Equal("Construction Labour", labour.ItemName);
        Assert.Equal("factor", labour.Unit);
        Assert.Equal(0.35m, labour.UnitCostLkr);

        Assert.All(items, item =>
        {
            Assert.Equal("Manual", item.Provider);
            Assert.Equal("Sri Lanka", item.Region);
            Assert.Equal("Initial contractor benchmark - Sri Lanka construction rates 2026", item.SourceReference);
            Assert.Equal(1.0m, item.TerrainMultiplier.Flat);
            Assert.Equal(1.15m, item.TerrainMultiplier.Hillside);
            Assert.Equal(1.10m, item.TerrainMultiplier.Coastal);
            Assert.NotEqual(default, item.UpdatedAt);
            Assert.Null(item.ImportedAt);
        });
    }

    [Fact]
    public async Task SeedAsync_DoesNotOverwriteConstructorUpdatedPrice()
    {
        await using var db = CreateContext();
        var seeder = CreateSeeder(db);
        await seeder.SeedAsync();

        var foundation = await db.PricingItems.SingleAsync(item => item.ItemName == "Foundation Materials");
        foundation.UnitCostLkr = 3250m;
        foundation.UpdatedAt = DateTimeOffset.Parse("2026-09-24T10:00:00+05:30");
        await db.SaveChangesAsync();

        await seeder.SeedAsync();

        foundation = await db.PricingItems.SingleAsync(item => item.ItemName == "Foundation Materials");
        Assert.Equal(3250m, foundation.UnitCostLkr);
        Assert.Equal(DateTimeOffset.Parse("2026-09-24T10:00:00+05:30"), foundation.UpdatedAt);
        Assert.Equal(6, await db.PricingItems.CountAsync());
    }

    [Fact]
    public async Task InternalPricingEndpoint_ReturnsAllSeedRecords()
    {
        await using var db = CreateContext();
        await CreateSeeder(db).SeedAsync();
        var controller = new InternalPricingController(new PricingService(db, TimeProvider.System));

        var result = await controller.GetPricing();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var prices = Assert.IsAssignableFrom<IEnumerable<PricingDto>>(ok.Value).ToList();
        Assert.Equal(6, prices.Count);
        Assert.Contains(prices, item => item.ItemName == "Foundation Materials" && item.UnitCostLkr == 3000m);
        Assert.Contains(prices, item => item.ItemName == "Construction Labour" && item.Unit == "factor");
    }

    private static PricingDataSeeder CreateSeeder(ApplicationDbContext db)
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(value => value.ContentRootPath).Returns(AppContext.BaseDirectory);
        return new PricingDataSeeder(db, environment.Object, TimeProvider.System,
            NullLogger<PricingDataSeeder>.Instance);
    }

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
