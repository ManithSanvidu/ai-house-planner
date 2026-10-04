using System.Text.Json;
using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HousePlanner.API.Tests.Services;

public class PricingPostgresIntegrationTests
{
    [Fact]
    public async Task Migrations_PricingConstraintsAndTerrainHistory_WorkOnPostgres()
    {
        var configured = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(configured)) return;

        var adminBuilder = new NpgsqlConnectionStringBuilder(configured);
        if (adminBuilder.Host is not ("localhost" or "127.0.0.1"))
            throw new InvalidOperationException("Integration tests require a local disposable PostgreSQL server.");

        var databaseName = $"cost_test_{Guid.NewGuid():N}";
        await using var admin = new NpgsqlConnection(adminBuilder.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin))
            await create.ExecuteNonQueryAsync();

        try
        {
            var testBuilder = new NpgsqlConnectionStringBuilder(adminBuilder.ConnectionString) { Database = databaseName };
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(testBuilder.ConnectionString).Options;
            await using (var db = new ApplicationDbContext(options))
            {
                await db.Database.MigrateAsync();
                Assert.Empty(await db.Database.GetPendingMigrationsAsync());

                db.PricingItems.Add(new PricingData
                {
                    ItemName = "Foundation Materials", Category = "material", DisplayGroup = "Foundation",
                    Unit = "per_sqft", UnitCostLkr = 3000m, Region = "Sri Lanka",
                    QualityLevel = "Standard", IsActive = true,
                    TerrainMultiplier = new TerrainMultiplierData { Flat = 1m, Hillside = 1.15m, Coastal = 1.1m }
                });
                await db.SaveChangesAsync();
            }

            await using (var db = new ApplicationDbContext(options))
            {
                var service = new PricingService(db, TimeProvider.System);
                var record = await db.PricingItems.SingleAsync();
                await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreatePricingAsync(new CreatePricingDto
                {
                    ItemName = "Foundation Tiles", Category = "material", DisplayGroup = "Foundation",
                    UnitCostLkr = 100m, Region = "Sri Lanka", QualityLevel = "Standard",
                    TerrainMultiplier = new TerrainMultiplierData { Flat = 1m, Hillside = 1m, Coastal = 1m }
                }));
                await service.UpdatePricingAsync(record.Id, new UpdatePricingDto
                {
                    UnitCostLkr = 3200m,
                    TerrainMultiplier = new TerrainMultiplierData { Flat = 1m, Hillside = 1.2m, Coastal = 1.1m },
                    Reason = "New quotation"
                });
                var history = await db.PricingHistory.SingleAsync();
                Assert.Equal(1.15m, JsonSerializer.Deserialize<TerrainMultiplierData>(history.PreviousTerrainMultipliersJson!)!.Hillside);
                Assert.Equal(1.2m, JsonSerializer.Deserialize<TerrainMultiplierData>(history.NewTerrainMultipliersJson!)!.Hillside);
            }

            await using (var db = new ApplicationDbContext(options))
            {
                db.PricingItems.Add(new PricingData
                {
                    ItemName = "Invalid Material", Category = "invalid", Unit = "per_sqft",
                    UnitCostLkr = 100m, Region = "Sri Lanka", QualityLevel = "Standard",
                    TerrainMultiplier = new TerrainMultiplierData()
                });
                await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            }
        }
        finally
        {
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{databaseName}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
