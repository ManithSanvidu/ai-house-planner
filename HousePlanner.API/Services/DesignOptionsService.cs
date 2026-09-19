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
        bool available = plans.Any(predicate);
        return new FeatureAvailabilityDto(
            available, 
            available ? null : "No validated design is available for this configuration."
        );
    }

    public async Task<DesignOptionsValidationResult> ValidateFinalSelectionAsync(AiGenerationRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Preferences?.Floors == 1 && request.Preferences.Balcony == true)
            return new DesignOptionsValidationResult
            {
                IsValid = false, ErrorCode = "UNSUPPORTED_DESIGN_CONFIGURATION",
                Message = "Balconies require a validated multi-floor design.", Conflicts = ["balcony"],
                Suggestions = [new SuggestionDto("balcony", false, "Continue without balcony")]
            };
        IQueryable<PreDesignedHousePlan> BuildQuery(string? skipConstraint = null)
        {
            var query = _context.PreDesignedHousePlans.Where(p => p.IsActive);
            if (request.Preferences != null)
            {
                if (request.Preferences.Floors > 0 && skipConstraint != "floors")
                    query = query.Where(p => p.FloorCount == request.Preferences.Floors);
                if (request.Preferences.Bedrooms > 0 && skipConstraint != "bedrooms")
                    query = query.Where(p => p.Bedrooms == request.Preferences.Bedrooms);
                if (request.Preferences.Bathrooms.HasValue && request.Preferences.Bathrooms.Value > 0 && skipConstraint != "bathrooms")
                    query = query.Where(p => p.Bathrooms == request.Preferences.Bathrooms.Value);
                if (!string.IsNullOrEmpty(request.Preferences.ArchitecturalStyle) && skipConstraint != "style")
                    query = query.Where(p => p.Style == request.Preferences.ArchitecturalStyle);
                
                if (request.Preferences.OpenPlan == true && skipConstraint != "openPlan") query = query.Where(p => p.HasOpenPlan);
                if (request.Preferences.MasterEnsuite == true && skipConstraint != "masterEnsuite") query = query.Where(p => p.HasMasterEnsuite);
                if (request.Preferences.SeparateDining == true && skipConstraint != "separateDining") query = query.Where(p => p.HasSeparateDining);
                if (request.Preferences.HomeOffice == true && skipConstraint != "homeOffice") query = query.Where(p => p.HasOffice);
                if (request.Preferences.Balcony == true && skipConstraint != "balcony") query = query.Where(p => p.HasBalcony);
                if (request.Preferences.Veranda == true && skipConstraint != "veranda") query = query.Where(p => p.HasVeranda);
                if (request.Preferences.UtilityRoom == true && skipConstraint != "utilityRoom") query = query.Where(p => p.HasUtilityRoom);
                if (request.Preferences.ParkingRequired == true && skipConstraint != "parkingRequired") query = query.Where(p => p.ParkingSpaces > 0);
                if (request.Preferences.Accessibility == true && skipConstraint != "accessibility") query = query.Where(p => p.IsAccessibleFriendly);
            }
            query = query.Where(p => p.MinimumLandSizePerches <= request.LandSizePerches);
            return query;
        }

        bool exists = await BuildQuery().AnyAsync(cancellationToken);

        if (!exists)
        {
            var conflicts = new List<string>();
            var suggestions = new List<SuggestionDto>();

            async Task CheckRelaxation(string field, string label)
            {
                var relaxedQuery = BuildQuery(field);
                if (await relaxedQuery.AnyAsync(cancellationToken))
                {
                    conflicts.Add(field == "parkingRequired" ? "parking" : field);
                    suggestions.Add(new SuggestionDto(field, false, label));
                }
            }

            if (request.Preferences?.ParkingRequired == true) await CheckRelaxation("parkingRequired", "Continue without parking");
            if (request.Preferences?.Balcony == true) await CheckRelaxation("balcony", "Continue without balcony");
            if (request.Preferences?.Veranda == true) await CheckRelaxation("veranda", "Continue without veranda");
            if (request.Preferences?.UtilityRoom == true) await CheckRelaxation("utilityRoom", "Continue without utility room");
            if (request.Preferences?.HomeOffice == true) await CheckRelaxation("homeOffice", "Continue without home office");
            if (request.Preferences?.SeparateDining == true) await CheckRelaxation("separateDining", "Continue without separate dining");
            if (request.Preferences?.MasterEnsuite == true) await CheckRelaxation("masterEnsuite", "Continue without master ensuite");

            async Task CheckIntegerRelaxation(string field, string labelPrefix)
            {
                var relaxedQuery = BuildQuery(field);
                var availableValues = await relaxedQuery
                    .Select(p => field == "bedrooms" ? p.Bedrooms : field == "bathrooms" ? p.Bathrooms : p.FloorCount)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                if (availableValues.Any())
                {
                    conflicts.Add(field);
                    int currentVal = field == "bedrooms" ? request.Preferences.Bedrooms : field == "bathrooms" ? (request.Preferences.Bathrooms ?? 1) : request.Preferences.Floors;
                    int closest = availableValues.OrderBy(x => Math.Abs(x - currentVal)).First();
                    suggestions.Add(new SuggestionDto(field, closest, $"{labelPrefix} {closest}"));
                }
            }

            // Only suggest integer changes if we haven't already found boolean suggestions.
            if (suggestions.Count == 0 && request.Preferences != null)
            {
                if (request.Preferences.Bathrooms > 0) await CheckIntegerRelaxation("bathrooms", "Change bathrooms to");
                if (request.Preferences.Bedrooms > 0) await CheckIntegerRelaxation("bedrooms", "Change bedrooms to");
                if (request.Preferences.Floors > 0) await CheckIntegerRelaxation("floors", "Change floors to");
            }

            return new DesignOptionsValidationResult
            {
                IsValid = false,
                ErrorCode = "UNSUPPORTED_DESIGN_CONFIGURATION",
                Message = "No validated design currently supports this exact configuration.",
                Conflicts = conflicts,
                Suggestions = suggestions
            };
        }

        return new DesignOptionsValidationResult { IsValid = true };
    }
}
