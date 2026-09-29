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
    public async Task GetAvailableOptions_FeaturesAreAlwaysAvailable_ToAllowAgentMatching()
    {
        var context = GetDbContext();
        await SeedData(context);
        var service = new DesignOptionsService(context);

        // Even though neither plan has parking, the feature should be 'available' 
        // to let the user submit the requirement and let the Python agent find the closest match.
        var result = await service.GetAvailableOptionsAsync(new DesignOptionsRequestDto());

        Assert.True(result.Features["parking"].Available);
        Assert.True(result.Features["open_plan"].Available);
        Assert.True(result.Features["balcony"].Available);
    }

    [Fact]
    public async Task ValidateFinalSelection_BasicRequirementsInvalid_IsRejected()
    {
        var context = GetDbContext();
        var service = new DesignOptionsService(context);

        var req = new AiGenerationRequest
        {
            LandSizePerches = 0, // Invalid land size
            Preferences = new PreferencesDto
            {
                Bedrooms = 0, // Invalid
                Bathrooms = 0, // Invalid
                Floors = 0 // Invalid
            }
        };

        var validation = await service.ValidateFinalSelectionAsync(req);
        Assert.False(validation.IsValid);
        Assert.Equal("INVALID_BASIC_REQUIREMENTS", validation.ErrorCode);
        Assert.Contains("landSize", validation.Conflicts);
        Assert.Contains("bedrooms", validation.Conflicts);
    }

    [Fact]
    public async Task ValidateFinalSelection_ValidSelection_PassesAndRecommendsMatching()
    {
        var context = GetDbContext();
        await SeedData(context);
        var service = new DesignOptionsService(context);

        var req = new AiGenerationRequest
        {
            LandSizePerches = 16,
            Preferences = new PreferencesDto
            {
                Floors = 2,
                Bedrooms = 4,
                Bathrooms = 3,
                Balcony = true,
                OpenPlan = true
            }
        };

        var validation = await service.ValidateFinalSelectionAsync(req);
        Assert.True(validation.IsValid);
        Assert.Equal("Your requirements will be matched against available architectural plans.", validation.Message);
    }

    [Fact]
    public async Task ValidateFinalSelection_UnsupportedConfiguration_PassesToAgent()
    {
        var context = GetDbContext();
        await SeedData(context);
        var service = new DesignOptionsService(context);

        // A configuration that doesn't perfectly match the DB catalogue:
        // Land 25, 2 floors, 4 beds, 2 baths, requires parking.
        var req = new AiGenerationRequest
        {
            LandSizePerches = 25,
            Preferences = new PreferencesDto
            {
                Floors = 2,
                Bedrooms = 4,
                Bathrooms = 2,
                ParkingRequired = true
            }
        };

        // It should NOT be rejected by the backend API anymore, because 
        // the python agentic service handles compatibility matches now.
        var validation = await service.ValidateFinalSelectionAsync(req);
        Assert.True(validation.IsValid);
    }
}
