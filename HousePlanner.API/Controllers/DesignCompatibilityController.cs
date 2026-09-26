using Microsoft.AspNetCore.Mvc;
using HousePlanner.API.Models;
using HousePlanner.API.Services;

namespace HousePlanner.API.Controllers;

public class DesignCompatibilityRequestDto
{
    public decimal? LandSize { get; set; }
    public string? LandUnit { get; set; }
    public decimal? PlotWidthFt { get; set; }
    public decimal? PlotLengthFt { get; set; }
    public string? TerrainType { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public int? Floors { get; set; }
    public string? Style { get; set; }
    public Dictionary<string, bool> Features { get; set; } = new();
}

[ApiController]
[Route("api/v1/design-compatibility")]
public class DesignCompatibilityController : ControllerBase
{
    private readonly IDesignOptionsService _designOptionsService;

    public DesignCompatibilityController(IDesignOptionsService designOptionsService)
    {
        _designOptionsService = designOptionsService;
    }

    [HttpPost("options")]
    public async Task<IActionResult> GetOptions([FromBody] DesignCompatibilityRequestDto request, CancellationToken cancellationToken)
    {
        var baseReq = new DesignOptionsRequestDto();
        
        if (request.LandSize.HasValue && !string.IsNullOrEmpty(request.LandUnit))
        {
            decimal perches = request.LandUnit.ToLower() == "sqft" ? request.LandSize.Value / 272.25m : request.LandSize.Value;
            
            if (perches < 8) baseReq.LandRangeId = "LAND_5_8";
            else if (perches < 12) baseReq.LandRangeId = "LAND_8_12";
            else if (perches < 20) baseReq.LandRangeId = "LAND_12_20";
            else if (perches < 30) baseReq.LandRangeId = "LAND_20_30";
            else baseReq.LandRangeId = "LAND_30_PLUS";
        }

        var options_land = await _designOptionsService.GetAvailableOptionsAsync(baseReq, cancellationToken);
        
        var currentFloors = request.Floors.HasValue && options_land.Floors.Contains(request.Floors.Value) ? request.Floors : null;
        var req_floors = new DesignOptionsRequestDto { LandRangeId = baseReq.LandRangeId, Floors = currentFloors };
        var options_floors = await _designOptionsService.GetAvailableOptionsAsync(req_floors, cancellationToken);

        var currentBedrooms = request.Bedrooms.HasValue && options_floors.Bedrooms.Contains(request.Bedrooms.Value) ? request.Bedrooms : null;
        var req_bedrooms = new DesignOptionsRequestDto { LandRangeId = baseReq.LandRangeId, Floors = currentFloors, Bedrooms = currentBedrooms };
        var options_bedrooms = await _designOptionsService.GetAvailableOptionsAsync(req_bedrooms, cancellationToken);

        var currentBathrooms = request.Bathrooms.HasValue && options_bedrooms.Bathrooms.Contains(request.Bathrooms.Value) ? request.Bathrooms : null;
        var currentStyle = !string.IsNullOrEmpty(request.Style) && options_bedrooms.ArchitecturalStyles.Contains(request.Style) ? request.Style : null;
        
        var req_features = new DesignOptionsRequestDto 
        { 
            LandRangeId = baseReq.LandRangeId, 
            Floors = currentFloors, 
            Bedrooms = currentBedrooms, 
            Bathrooms = currentBathrooms, 
            ArchitecturalStyle = currentStyle 
        };
        var options_features = await _designOptionsService.GetAvailableOptionsAsync(req_features, cancellationToken);

        var result = new
        {
            supported = new
            {
                floors = options_land.Floors,
                bedrooms = options_floors.Bedrooms,
                bathrooms = options_bedrooms.Bathrooms,
                styles = options_bedrooms.ArchitecturalStyles,
                features = options_features.Features.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Available)
            },
            reasons = options_features.Features
                .Where(kvp => !kvp.Value.Available && !string.IsNullOrEmpty(kvp.Value.Reason))
                .ToDictionary(kvp => $"feature.{kvp.Key}", kvp => kvp.Value.Reason),
            compatiblePlanCount = options_features.ValidatedDesignCount
        };

        return Ok(result);
    }
}
