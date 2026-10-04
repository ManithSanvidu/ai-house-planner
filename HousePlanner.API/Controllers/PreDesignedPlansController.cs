using System.Text.Json;
using System.Net.Http.Json;
using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Controllers;

[ApiController]
[Route("api/v1/pre-designed-plans")]
public class PreDesignedPlansController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserContextService _users;
    private readonly IPlanImageStorage _images;
    private readonly IPricingService? _pricing;
    private readonly HttpClient? _costClient;
    public PreDesignedPlansController(ApplicationDbContext db, ICurrentUserContextService users,
        IPlanImageStorage images, IPricingService? pricing = null, IHttpClientFactory? clients = null)
    { _db = db; _users = users; _images = images; _pricing = pricing; _costClient = clients?.CreateClient("AgenticService"); }

    private async Task<bool> Authenticated() => await _users.GetAsync(HttpContext) is not null;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? bedrooms, [FromQuery] int? bathrooms,
        [FromQuery] int? floors, [FromQuery] string? style, [FromQuery] string? terrain,
        [FromQuery] decimal? minimumLandSizePerches, [FromQuery] decimal? maximumBuiltUpArea,
        [FromQuery] bool? parking, [FromQuery] bool? office, [FromQuery] bool? balcony,
        [FromQuery] bool? accessible, [FromQuery] string? category, [FromQuery] string? search)
    {
        var query = _db.PreDesignedHousePlans.AsNoTracking().Where(x => x.IsActive);
        if (bedrooms.HasValue) query = query.Where(x => x.Bedrooms == bedrooms);
        if (bathrooms.HasValue) query = query.Where(x => x.Bathrooms == bathrooms);
        if (floors.HasValue) query = query.Where(x => x.FloorCount == floors);
        if (!string.IsNullOrWhiteSpace(style)) query = query.Where(x => x.Style.ToLower() == style.ToLower());
        if (!string.IsNullOrWhiteSpace(terrain)) query = query.Where(x => x.SuitableTerrain.ToLower() == terrain.ToLower());
        if (minimumLandSizePerches.HasValue) query = query.Where(x => x.MinimumLandSizePerches <= minimumLandSizePerches);
        if (maximumBuiltUpArea.HasValue) query = query.Where(x => x.TotalBuiltUpAreaSqft <= maximumBuiltUpArea);
        if (parking == true) query = query.Where(x => x.ParkingSpaces > 0);
        if (office == true) query = query.Where(x => x.HasOffice);
        if (balcony == true) query = query.Where(x => x.HasBalcony);
        if (accessible == true) query = query.Where(x => x.IsAccessibleFriendly);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Category != null && x.Category.ToLower() == category.ToLower());
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.ToLower(); query = query.Where(x => x.Name.ToLower().Contains(term) || x.Style.ToLower().Contains(term) || (x.Category != null && x.Category.ToLower().Contains(term)) || x.TagsJson.ToLower().Contains(term)); }
        var plans = await query.OrderBy(x => x.Bedrooms).ThenBy(x => x.Name).ToListAsync();
        return Ok(plans.Select(x => MapSummary(x, _images)));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        if (!await Authenticated()) return Unauthorized();
        var plan = await _db.PreDesignedHousePlans.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (plan is null) return NotFound();

        var cost = await PreviewCostAsync(plan);
        return Ok(MapDetail(plan, _images, cost));
    }

    [HttpPost("{id:guid}/check-compatibility")]
    public async Task<IActionResult> CheckCompatibility(Guid id, CompatibilityRequest request)
    {
        if (!await Authenticated()) return Unauthorized();
        var plan = await _db.PreDesignedHousePlans.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (plan is null) return NotFound();
        var issues = new List<string>(); var warnings = new List<string>();
        if (request.LandSizePerches < plan.MinimumLandSizePerches) issues.Add($"Requires at least {plan.MinimumLandSizePerches:g} perches.");
        if (plan.MinimumPlotWidthFt.HasValue && request.PlotWidthFt.HasValue && request.PlotWidthFt < plan.MinimumPlotWidthFt) issues.Add($"Plot width must be at least {plan.MinimumPlotWidthFt:g} ft.");
        if (plan.MinimumPlotLengthFt.HasValue && request.PlotLengthFt.HasValue && request.PlotLengthFt < plan.MinimumPlotLengthFt) issues.Add($"Plot length must be at least {plan.MinimumPlotLengthFt:g} ft.");
        if (!string.Equals(plan.SuitableTerrain, "all", StringComparison.OrdinalIgnoreCase) && !string.Equals(plan.SuitableTerrain, request.Terrain, StringComparison.OrdinalIgnoreCase)) issues.Add($"Designed for {plan.SuitableTerrain} terrain.");
        if (request.PreferredBedrooms.HasValue && request.PreferredBedrooms != plan.Bedrooms) warnings.Add($"Plan contains {plan.Bedrooms} bedrooms.");
        if (request.PreferredFloors.HasValue && request.PreferredFloors != plan.FloorCount) warnings.Add($"Plan contains {plan.FloorCount} floor(s).");
        return Ok(new CompatibilityResponse(issues.Count == 0, issues, warnings));
    }

    [HttpGet("{id:guid}/check-current-project")]
    public async Task<IActionResult> CheckCurrentProject(Guid id)
    {
        var user = await _users.GetAsync(HttpContext);
        if (user?.Id is null) return Unauthorized();
        if (!string.Equals(user.Role, "Customer", StringComparison.OrdinalIgnoreCase)) return Forbid();
        var plan = await _db.PreDesignedHousePlans.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (plan is null) return NotFound();
        var project = await _db.LandSubmissions.AsNoTracking()
            .Where(x => x.ClientId == user.Id.Value).OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefaultAsync();
        if (project is null)
            return NotFound(new { code = "project_details_required", message = "Add your project details to check whether this plan fits your land and requirements." });
        var issues = new List<string>(); var warnings = new List<string>();
        if (project.LandSizePerches < plan.MinimumLandSizePerches)
            issues.Add($"Requires at least {plan.MinimumLandSizePerches:g} perches; your project has {project.LandSizePerches:g}.");
        if (!string.Equals(plan.SuitableTerrain, "all", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(plan.SuitableTerrain, project.ManualTerrainType, StringComparison.OrdinalIgnoreCase))
            issues.Add($"Designed for {plan.SuitableTerrain} terrain.");
        if (project.PreferredBedrooms != plan.Bedrooms) warnings.Add($"Plan contains {plan.Bedrooms} bedrooms; your project requests {project.PreferredBedrooms}.");
        if (project.PreferredFloors != plan.FloorCount) warnings.Add($"Plan contains {plan.FloorCount} floor(s); your project requests {project.PreferredFloors}.");
        return Ok(new CurrentProjectCompatibilityResponse(issues.Count == 0 && warnings.Count == 0, issues, warnings,
            project.LandSizePerches, plan.MinimumLandSizePerches));
    }

    internal static PreDesignedPlanSummaryDto MapSummary(PreDesignedHousePlan x, IPlanImageStorage images) => new(x.Id, x.Name, x.Slug, x.DesignCode, x.Style, x.Bedrooms, x.Bathrooms, x.FloorCount, x.TotalBuiltUpAreaSqft, x.MinimumLandSizePerches, x.SuitableTerrain, x.ParkingSpaces, x.HasBalcony, x.HasVeranda, x.HasOffice, x.IsAccessibleFriendly, x.Category, ParseTags(x.TagsJson), images.GetPublicUrl(x.ThumbnailUrl), x.IsActive, x.UpdatedAt, x.EstimatedConstructionCost);
    internal static PreDesignedPlanDetailDto MapDetail(PreDesignedHousePlan x, IPlanImageStorage images, CostSummaryDto? cost = null)
    {
        using var document = JsonDocument.Parse(x.LayoutJson);
        return new(x.Id, x.Name, x.Slug, x.DesignCode, x.Description, x.Style, x.Bedrooms, x.Bathrooms, x.FloorCount, x.TotalBuiltUpAreaSqft, x.MinimumLandSizePerches, x.MinimumPlotWidthFt, x.MinimumPlotLengthFt, x.SuitableTerrain, x.ParkingSpaces, x.HasBalcony, x.HasVeranda, x.HasOffice, x.HasUtilityRoom, x.IsAccessibleFriendly, x.Category, ParseTags(x.TagsJson), images.GetPublicUrl(x.ThumbnailUrl), document.RootElement.Clone(), x.IsActive, x.CreatedAt, x.UpdatedAt, "Architect-validated design. Construction estimates may vary based on site conditions, materials, and final contractor pricing.", cost, x.EstimatedConstructionCost);
    }
    internal static IReadOnlyList<string> ParseTags(string json) { try { return JsonSerializer.Deserialize<string[]>(json) ?? []; } catch { return []; } }

    private async Task<CostSummaryDto?> PreviewCostAsync(PreDesignedHousePlan plan)
    {
        if (_pricing is null || _costClient is null || plan.TotalBuiltUpAreaSqft <= 0)
            return null;
        try
        {
            var prices = (await _pricing.GetActivePricingAsync("Sri Lanka", "Standard")).ToList();
            if (prices.Count == 0) return null;
            var terrain = plan.SuitableTerrain?.Trim().ToLowerInvariant() is "hillside" or "coastal"
                ? plan.SuitableTerrain.Trim().ToLowerInvariant() : "flat";
            using var response = await _costClient.PostAsJsonAsync("cost/preview", new
            {
                areaSqft = plan.TotalBuiltUpAreaSqft,
                terrainType = terrain,
                pricingItems = prices
            });
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<CostSummaryDto>()
                : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            // A catalogue preview must not prevent browsing an approved plan.
            return null;
        }
    }
}
