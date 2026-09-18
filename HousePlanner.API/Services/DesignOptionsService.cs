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
            ["balcony"] = GetFeatureAvailability(plans, p => p.HasBalcony),
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
            features
        );
    }

    private FeatureAvailabilityDto GetFeatureAvailability(List<PreDesignedHousePlan> plans, Func<PreDesignedHousePlan, bool> predicate)
    {
        bool available = plans.Any(predicate);
        return new FeatureAvailabilityDto(
            available, 
            available ? null : "No validated design is available for this configuration."
        );
    }

    public async Task<DesignOptionsValidationResult> ValidateFinalSelectionAsync(AiGenerationRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.PreDesignedHousePlans.Where(p => p.IsActive);
        
        if (request.Preferences != null)
        {
            if (request.Preferences.Floors > 0)
                query = query.Where(p => p.FloorCount == request.Preferences.Floors);
            
            if (request.Preferences.Bedrooms > 0)
                query = query.Where(p => p.Bedrooms == request.Preferences.Bedrooms);
                
            if (request.Preferences.Bathrooms.HasValue && request.Preferences.Bathrooms.Value > 0)
                query = query.Where(p => p.Bathrooms == request.Preferences.Bathrooms.Value);

            if (!string.IsNullOrEmpty(request.Preferences.ArchitecturalStyle))
                query = query.Where(p => p.Style == request.Preferences.ArchitecturalStyle);

            if (request.Preferences.OpenPlan == true) query = query.Where(p => p.HasOpenPlan);
            if (request.Preferences.MasterEnsuite == true) query = query.Where(p => p.HasMasterEnsuite);
            if (request.Preferences.SeparateDining == true) query = query.Where(p => p.HasSeparateDining);
            if (request.Preferences.HomeOffice == true) query = query.Where(p => p.HasOffice);
            if (request.Preferences.Balcony == true) query = query.Where(p => p.HasBalcony);
            if (request.Preferences.Veranda == true) query = query.Where(p => p.HasVeranda);
            if (request.Preferences.UtilityRoom == true) query = query.Where(p => p.HasUtilityRoom);
            if (request.Preferences.ParkingRequired == true) query = query.Where(p => p.ParkingSpaces > 0);
            if (request.Preferences.Accessibility == true) query = query.Where(p => p.IsAccessibleFriendly);
        }

        query = query.Where(p => p.MinimumLandSizePerches <= request.LandSizePerches);

        bool exists = await query.AnyAsync(cancellationToken);

        if (!exists)
        {
            return new DesignOptionsValidationResult
            {
                IsValid = false,
                ErrorCode = "UNSUPPORTED_DESIGN_CONFIGURATION",
                Message = "No validated design currently fits this exact configuration.",
                Suggestions = new List<string> { "Please adjust your feature requirements or increase land size." }
            };
        }

        return new DesignOptionsValidationResult { IsValid = true };
    }
}
