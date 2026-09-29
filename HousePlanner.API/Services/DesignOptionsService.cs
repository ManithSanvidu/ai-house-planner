using HousePlanner.API.Data;
using HousePlanner.API.Entities;
using HousePlanner.API.Models;
using HousePlanner.API.Controllers;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HousePlanner.API.Services;

public class DesignOptionsService : IDesignOptionsService
{
    private readonly ApplicationDbContext _context;

    public DesignOptionsService(ApplicationDbContext context)
    {
        _context = context;
    }

    private static readonly List<LandRangeDto> LandRanges = new()
    {
        new LandRangeDto("LAND_5_8", "5–8 perches", 5, 8, "1,361–2,178 sq ft"),
        new LandRangeDto("LAND_8_12", "8–12 perches", 8, 12, "2,178–3,267 sq ft"),
        new LandRangeDto("LAND_12_20", "12–20 perches", 12, 20, "3,267–5,445 sq ft"),
        new LandRangeDto("LAND_20_30", "20–30 perches", 20, 30, "5,445–8,167 sq ft"),
        new LandRangeDto("LAND_30_PLUS", "30+ perches", 30, 9999, "8,167+ sq ft")
    };

    public async Task<DesignOptionsResponseDto> GetAvailableOptionsAsync(DesignOptionsRequestDto request, CancellationToken cancellationToken = default)
    {
        var query = _context.PreDesignedHousePlans.Where(p => p.IsActive);
        var plans = await query.ToListAsync(cancellationToken);

        // Filter based on currently requested bounds
        if (!string.IsNullOrEmpty(request.LandRangeId))
        {
            var range = LandRanges.FirstOrDefault(r => r.Id == request.LandRangeId);
            if (range != null)
            {
                plans = plans.Where(p => p.MinimumLandSizePerches <= range.MaxPerches).ToList();
            }
        }

        if (request.Floors.HasValue)
            plans = plans.Where(p => p.FloorCount == request.Floors.Value).ToList();

        if (request.Bedrooms.HasValue)
            plans = plans.Where(p => p.Bedrooms == request.Bedrooms.Value).ToList();

        if (request.Bathrooms.HasValue)
            plans = plans.Where(p => p.Bathrooms == request.Bathrooms.Value).ToList();

        if (!string.IsNullOrEmpty(request.ArchitecturalStyle))
            plans = plans.Where(p => p.Style == request.ArchitecturalStyle).ToList();

        if (request.OpenPlan == true) plans = plans.Where(p => p.HasOpenPlan).ToList();
        if (request.MasterEnsuite == true) plans = plans.Where(p => p.HasMasterEnsuite).ToList();
        if (request.SeparateDining == true) plans = plans.Where(p => p.HasSeparateDining).ToList();
        if (request.HomeOffice == true) plans = plans.Where(p => p.HasOffice).ToList();
        if (request.Balcony == true) plans = plans.Where(p => p.HasBalcony && p.FloorCount >= 2).ToList();
        if (request.Veranda == true) plans = plans.Where(p => p.HasVeranda).ToList();
        if (request.UtilityRoom == true) plans = plans.Where(p => p.HasUtilityRoom).ToList();
        if (request.ParkingRequired == true) plans = plans.Where(p => p.ParkingSpaces > 0).ToList();
        if (request.Accessibility == true) plans = plans.Where(p => p.IsAccessibleFriendly).ToList();

        // Extract available options from remaining plans
        var bedrooms = plans.Select(p => p.Bedrooms).Distinct().OrderBy(x => x).ToList();
        var bathrooms = plans.Select(p => p.Bathrooms).Distinct().OrderBy(x => x).ToList();
        var floors = plans.Select(p => p.FloorCount).Distinct().OrderBy(x => x).ToList();
        var styles = plans.Select(p => p.Style).Distinct().OrderBy(x => x).ToList();

        var features = new Dictionary<string, FeatureAvailabilityDto>
        {
            ["open_plan"] = GetFeatureAvailability(plans, p => p.HasOpenPlan),
            ["master_ensuite"] = GetFeatureAvailability(plans, p => p.HasMasterEnsuite),
            ["separate_dining"] = GetFeatureAvailability(plans, p => p.HasSeparateDining),
            ["home_office"] = GetFeatureAvailability(plans, p => p.HasOffice),
            ["balcony"] = request.Floors == 1
                ? new FeatureAvailabilityDto(false, "Balconies require a validated multi-floor design.")
                : GetFeatureAvailability(plans, p => p.HasBalcony && p.FloorCount >= 2),
            ["veranda"] = GetFeatureAvailability(plans, p => p.HasVeranda),
            ["utility_room"] = GetFeatureAvailability(plans, p => p.HasUtilityRoom),
            ["parking"] = GetFeatureAvailability(plans, p => p.ParkingSpaces > 0),
            ["accessibility"] = GetFeatureAvailability(plans, p => p.IsAccessibleFriendly)
        };

        return new DesignOptionsResponseDto(
            LandRanges,
            new List<string> { "NARROW_DEEP", "BALANCED", "WIDE_SHALLOW", "CUSTOM_DIMENSIONS" },
            bedrooms,
            bathrooms,
            floors,
            styles,
            features,
            plans.Count
        );
    }

    private FeatureAvailabilityDto GetFeatureAvailability(List<PreDesignedHousePlan> plans, Func<PreDesignedHousePlan, bool> predicate)
    {
        return new FeatureAvailabilityDto(
            true,
            "Your requirements will be matched against available architectural plans."
        );
    }

    public Task<DesignOptionsValidationResult> ValidateFinalSelectionAsync(AiGenerationRequest request, CancellationToken cancellationToken = default)
    {
        var conflicts = new List<string>();
        
        if (request.Preferences != null)
        {
            if (request.Preferences.Bedrooms <= 0) conflicts.Add("bedrooms");
            if (request.Preferences.Bathrooms <= 0) conflicts.Add("bathrooms");
            if (request.Preferences.Floors <= 0) conflicts.Add("floors");
        }
        else
        {
            conflicts.Add("preferences");
        }

        if (request.LandSizePerches <= 0) conflicts.Add("landSize");

        if (conflicts.Count > 0)
        {
            return Task.FromResult(new DesignOptionsValidationResult
            {
                IsValid = false,
                ErrorCode = "INVALID_BASIC_REQUIREMENTS",
                Message = "Please provide valid basic requirements (bedrooms, bathrooms, floors, and land size).",
                Conflicts = conflicts,
                Suggestions = new List<SuggestionDto>()
            });
        }

        return Task.FromResult(new DesignOptionsValidationResult { IsValid = true, Message = "Your requirements will be matched against available architectural plans." });
    }

    public Task<DesignOptionsValidationResult> ValidateSpecificPlanAsync(PreDesignedHousePlan plan,
        AiGenerationRequest request, CancellationToken cancellationToken = default)
    {
        return ValidateFinalSelectionAsync(request, cancellationToken);
    }
}
