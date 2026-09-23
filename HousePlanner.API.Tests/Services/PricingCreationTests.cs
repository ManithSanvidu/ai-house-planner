using HousePlanner.API.Controllers;
using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using HousePlanner.API.Options;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace HousePlanner.API.Tests.Services;

public class PricingCreationTests
{
    [Fact]
    public async Task CreatePricing_ValidMaterialItem_DerivesCanonicalUnitAndProvider()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var dto = new CreatePricingDto
        {
            ItemName = "Substructure Materials",
            Category = "MATERIAL", // test case-normalization
            DisplayGroup = "Foundation",
            UnitCostLkr = 8500m,
            TerrainMultiplier = new TerrainMultiplierData { Flat = 1.0m, Hillside = 1.25m, Coastal = 1.35m },
            SourceReference = "Manual contractor rate"
        };

        var result = await service.CreatePricingAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("Substructure Materials", result.ItemName);
        Assert.Equal("material", result.Category);
        Assert.Equal("per_sqft", result.Unit);
        Assert.Equal("Foundation", result.DisplayGroup);
        Assert.Equal(8500m, result.UnitCostLkr);
        Assert.Equal("Manual", result.Provider);
        Assert.Equal("Manual contractor rate", result.SourceReference);
        Assert.Null(result.ImportedAt);

        var persisted = await context.PricingItems.SingleAsync();
        Assert.Equal("material", persisted.Category);
        Assert.Equal("per_sqft", persisted.Unit);
        Assert.Equal("Manual", persisted.Provider);
    }

    [Fact]
    public async Task CreatePricing_ValidLabourFactor_DerivesFactorUnit()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var dto = new CreatePricingDto
        {
            ItemName = "Standard Construction Labour",
            Category = "labour",
            UnitCostLkr = 0.35m,
            TerrainMultiplier = new TerrainMultiplierData { Flat = 1.0m, Hillside = 1.0m, Coastal = 1.0m }
        };

        var result = await service.CreatePricingAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("Standard Construction Labour", result.ItemName);
        Assert.Equal("labour", result.Category);
        Assert.Equal("factor", result.Unit);
        Assert.Equal("Labour", result.DisplayGroup); // defaults to Labour
        Assert.Equal(0.35m, result.UnitCostLkr);
        Assert.Equal("Manual", result.Provider);
    }

    [Fact]
    public async Task CreatePricing_SecondLabourFactor_IsRejected()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        // Add first labour factor
        await service.CreatePricingAsync(new CreatePricingDto
        {
            ItemName = "Standard Labour",
            Category = "labour",
            UnitCostLkr = 0.30m,
            TerrainMultiplier = new TerrainMultiplierData { Flat = 1.0m, Hillside = 1.0m, Coastal = 1.0m }
        });

        // Try adding a second labour factor
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreatePricingAsync(new CreatePricingDto
        {
            ItemName = "Specialized Labour",
            Category = "labour",
            UnitCostLkr = 0.40m,
            TerrainMultiplier = new TerrainMultiplierData { Flat = 1.0m, Hillside = 1.0m, Coastal = 1.0m }
        }));

        Assert.Contains("Only one labour factor pricing record is supported", ex.Message);
    }

    [Fact]
    public async Task CreatePricing_DuplicateItemName_IsRejected()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        await service.CreatePricingAsync(new CreatePricingDto
        {
            ItemName = "Concrete Block",
            Category = "material",
            UnitCostLkr = 5000m,
            TerrainMultiplier = new TerrainMultiplierData { Flat = 1.0m, Hillside = 1.25m, Coastal = 1.35m }
        });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreatePricingAsync(new CreatePricingDto
        {
            ItemName = "concrete block", // case-insensitive duplicate
            Category = "material",
            UnitCostLkr = 6000m,
            TerrainMultiplier = new TerrainMultiplierData { Flat = 1.0m, Hillside = 1.25m, Coastal = 1.35m }
        }));

        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task CreatePricing_InvalidCategory_ThrowsArgumentException()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreatePricingAsync(new CreatePricingDto
        {
            ItemName = "Invalid Item",
            Category = "Structural", // should be rejected as machine category
            UnitCostLkr = 1000m,
            TerrainMultiplier = new TerrainMultiplierData { Flat = 1.0m, Hillside = 1.25m, Coastal = 1.35m }
        }));

        Assert.Contains("Category must be 'material' or 'labour'", ex.Message);
    }

    [Fact]
    public async Task CreatePricing_NonPositiveUnitCost_ThrowsArgumentException()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreatePricingAsync(new CreatePricingDto
        {
            ItemName = "Free Item",
            Category = "material",
            UnitCostLkr = 0m,
            TerrainMultiplier = new TerrainMultiplierData { Flat = 1.0m, Hillside = 1.25m, Coastal = 1.35m }
        }));

        Assert.Contains("UnitCostLkr must be greater than zero", ex.Message);
    }

    [Fact]
    public async Task CreatePricing_InvalidTerrainMultipliers_ThrowsArgumentException()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreatePricingAsync(new CreatePricingDto
        {
            ItemName = "Bad Multipliers",
            Category = "material",
            UnitCostLkr = 1000m,
            TerrainMultiplier = new TerrainMultiplierData { Flat = 0m, Hillside = 1.25m, Coastal = 1.35m }
        }));

        Assert.Contains("TerrainMultiplier values must be provided and greater than zero", ex.Message);
    }

    [Fact]
    public async Task CreatePricing_BlankItemName_ThrowsArgumentException()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreatePricingAsync(new CreatePricingDto
        {
            ItemName = "   ",
            Category = "material",
            UnitCostLkr = 1000m,
            TerrainMultiplier = new TerrainMultiplierData { Flat = 1.0m, Hillside = 1.25m, Coastal = 1.35m }
        }));

        Assert.Contains("ItemName is required", ex.Message);
    }

    [Fact]
    public async Task ExistingUpdatePricing_StillUpdatesCostAndMultipliers()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var created = await service.CreatePricingAsync(new CreatePricingDto
        {
            ItemName = "Roofing Sheets",
            Category = "material",
            DisplayGroup = "Roofing",
            UnitCostLkr = 4000m,
            TerrainMultiplier = new TerrainMultiplierData { Flat = 1.0m, Hillside = 1.2m, Coastal = 1.3m }
        });

        var updated = await service.UpdatePricingAsync(created.Id, new UpdatePricingDto
        {
            UnitCostLkr = 4500m,
            TerrainMultiplier = new TerrainMultiplierData { Flat = 1.0m, Hillside = 1.3m, Coastal = 1.4m }
        });

        Assert.NotNull(updated);
        Assert.Equal(4500m, updated.UnitCostLkr);
        Assert.Equal(1.3m, updated.TerrainMultiplier.Hillside);
        Assert.Equal("material", updated.Category); // Category unchanged
        Assert.Equal("per_sqft", updated.Unit); // Unit unchanged
    }

    [Fact]
    public async Task Controller_CreatePricing_Returns201Created_OnSuccess()
    {
        var mockService = new Mock<IPricingService>();
        var controller = new PricingController(mockService.Object);

        var createDto = new CreatePricingDto
        {
            ItemName = "Foundation Concrete",
            Category = "material",
            UnitCostLkr = 7000m,
            TerrainMultiplier = new TerrainMultiplierData { Flat = 1.0m, Hillside = 1.25m, Coastal = 1.35m }
        };

        mockService.Setup(s => s.CreatePricingAsync(It.IsAny<CreatePricingDto>()))
            .ReturnsAsync(new PricingDto
            {
                Id = 42,
                ItemName = createDto.ItemName,
                Category = "material",
                Unit = "per_sqft",
                UnitCostLkr = 7000m,
                TerrainMultiplier = createDto.TerrainMultiplier,
                Provider = "Manual"
            });

        var result = await controller.CreatePricing(createDto);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, statusResult.StatusCode);
        var value = Assert.IsType<PricingDto>(statusResult.Value);
        Assert.Equal(42, value.Id);
        Assert.Equal("Foundation Concrete", value.ItemName);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static PricingService CreateService(ApplicationDbContext context)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new ExternalPricingOptions());
        var stubProvider = new Mock<IExternalPricingProvider>();
        return new PricingService(context, stubProvider.Object, new PricingNormalizationService(options), TimeProvider.System);
    }
}
