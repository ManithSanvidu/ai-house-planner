using HousePlanner.API.Controllers;
using HousePlanner.API.Data;
using HousePlanner.API.Entities;
using HousePlanner.API.Models;
using HousePlanner.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HousePlanner.API.Tests.Services;

public class DesignOptionsServiceTests
{
    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private async Task SeedData(ApplicationDbContext context)
    {
        context.PreDesignedHousePlans.AddRange(
            new PreDesignedHousePlan
            {
                Name = "Plan 1",
                Slug = "p1",
                DesignCode = "P1",
                Style = "Modern",
                MinimumLandSizePerches = 6,
                Bedrooms = 2,
                Bathrooms = 1,
                FloorCount = 1,
                HasBalcony = false,
                HasOpenPlan = true,
                IsActive = true
            },
            new PreDesignedHousePlan
            {
                Name = "Plan 2",
                Slug = "p2",
                DesignCode = "P2",
                Style = "Modern",
                MinimumLandSizePerches = 15,
                Bedrooms = 4,
                Bathrooms = 3,
                FloorCount = 2,
                HasBalcony = true,
                HasOpenPlan = true,
                IsActive = true
            }
        );
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAvailableOptions_UnsupportedFeature_IsUnavailable()
    {
        var context = GetDbContext();
        await SeedData(context);
        var service = new DesignOptionsService(context);

        // Neither plan has ParkingSpaces > 0
        var result = await service.GetAvailableOptionsAsync(new DesignOptionsRequestDto());

        Assert.False(result.Features["parking"].Available);
        Assert.NotNull(result.Features["parking"].Reason);
        Assert.True(result.Features["open_plan"].Available);
    }

    [Fact]
    public async Task GetAvailableOptions_ChangingLandRange_ChangesBedroomOptions()
    {
        var context = GetDbContext();
        await SeedData(context);
        var service = new DesignOptionsService(context);

        // For land 5-8 perches (max 8), only Plan 1 (min 6) matches. Plan 2 (min 15) is filtered out.
        var result = await service.GetAvailableOptionsAsync(new DesignOptionsRequestDto { LandRangeId = "LAND_5_8" });

        Assert.Single(result.Bedrooms);
        Assert.Equal(2, result.Bedrooms[0]);
    }

    [Fact]
    public async Task GetAvailableOptions_ChangingFloors_ChangesFeatureAvailability()
    {
        var context = GetDbContext();
        await SeedData(context);
        var service = new DesignOptionsService(context);

        // Floors = 1 restricts to Plan 1, which has NO balcony.
        var result = await service.GetAvailableOptionsAsync(new DesignOptionsRequestDto { Floors = 1 });
        Assert.False(result.Features["balcony"].Available);

        // Floors = 2 restricts to Plan 2, which HAS balcony.
        var result2 = await service.GetAvailableOptionsAsync(new DesignOptionsRequestDto { Floors = 2 });
        Assert.True(result2.Features["balcony"].Available);
    }

    [Fact]
    public async Task ValidateFinalSelection_InvalidSelection_IsRejected()
    {
        var context = GetDbContext();
        await SeedData(context);
        var service = new DesignOptionsService(context);

        var req = new AiGenerationRequest
        {
            LandSizePerches = 7, // Maps to 5-8 range -> Plan 1
            Preferences = new PreferencesDto
            {
                Balcony = true // Invalid because Plan 1 has no balcony
            }
        };

        var validation = await service.ValidateFinalSelectionAsync(req);
        Assert.False(validation.IsValid);
        Assert.Equal("UNSUPPORTED_DESIGN_CONFIGURATION", validation.ErrorCode);
    }

    [Fact]
    public async Task ValidateFinalSelection_ValidSelection_Passes()
    {
        var context = GetDbContext();
        await SeedData(context);
        var service = new DesignOptionsService(context);

        var req = new AiGenerationRequest
        {
            LandSizePerches = 16, // Maps to 12-20 range -> Plan 1 & Plan 2.
            Preferences = new PreferencesDto
            {
                Floors = 2, // -> Plan 2
                Bedrooms = 4,
                Bathrooms = 3,
                Balcony = true, // Plan 2 has balcony
                OpenPlan = true
            }
        };

        var validation = await service.ValidateFinalSelectionAsync(req);
        Assert.True(validation.IsValid, "Validation failed: " + string.Join(", ", validation.Suggestions));
    }

    [Fact]
    public async Task ValidateFinalSelection_ExactUserFailure_IsRejected()
    {
        var context = GetDbContext();
        await SeedData(context); // Plan 2 has parking=false, floors=2, beds=4, baths=3
        var service = new DesignOptionsService(context);

        var req = new AiGenerationRequest
        {
            LandSizePerches = 25,
            Preferences = new PreferencesDto
            {
                Floors = 2,
                Bedrooms = 4,
                Bathrooms = 2, // Plan 2 has 3 bathrooms, so this will fail
                ParkingRequired = true // Plan 2 doesn't have parking
            }
        };

        var validation = await service.ValidateFinalSelectionAsync(req);
        Assert.False(validation.IsValid);
        Assert.Equal("UNSUPPORTED_DESIGN_CONFIGURATION", validation.ErrorCode);
    }

    [Fact]
    public async Task ValidateFinalSelection_ConflictingSelection_GeneratesSuggestions()
    {
        var context = GetDbContext();
        await SeedData(context);
        var service = new DesignOptionsService(context);

        var req = new AiGenerationRequest
        {
            LandSizePerches = 25,
            Preferences = new PreferencesDto
            {
                Floors = 2,
                Bedrooms = 4,
                Bathrooms = 3, // Valid for Plan 2
                ParkingRequired = true // Invalid for Plan 2
            }
        };

        var validation = await service.ValidateFinalSelectionAsync(req);
        Assert.False(validation.IsValid);
        Assert.Contains("parking", validation.Conflicts);
        Assert.Contains(validation.Suggestions, s => s.Field == "parkingRequired" && (bool)s.Value == false);
    }

    [Fact]
    public async Task ValidateSpecificPlan_RejectsMismatchEvenWhenAnotherCataloguePlanMatches()
    {
        var context = GetDbContext(); await SeedData(context);
        var selected = await context.PreDesignedHousePlans.SingleAsync(x => x.DesignCode == "P1");
        var request = new AiGenerationRequest
        {
            LandSizePerches = 20,
            Preferences = new PreferencesDto { Bedrooms = 4, Bathrooms = 3, Floors = 2, Balcony = true }
        };
        var result = await new DesignOptionsService(context).ValidateSpecificPlanAsync(selected, request);
        Assert.False(result.IsValid);
        Assert.Equal("SELECTED_PLAN_INCOMPATIBLE", result.ErrorCode);
        Assert.Contains("bedrooms", result.Conflicts);
        Assert.Contains("balcony", result.Conflicts);
    }
}
