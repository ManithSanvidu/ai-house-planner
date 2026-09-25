using System.Text.Json;
using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Controllers;

[ApiController]
[Route("api/v1/admin/pre-designed-plans")]
public class AdminPreDesignedPlansController : ControllerBase
{
    private static readonly HashSet<string> Terrains = new(StringComparer.OrdinalIgnoreCase) { "flat", "urban", "hillside", "coastal", "all" };
    private readonly ApplicationDbContext _db; private readonly ICurrentUserContextService _users; private readonly IPreDesignedPlanLayoutValidator _layouts;
    public AdminPreDesignedPlansController(ApplicationDbContext db, ICurrentUserContextService users, IPreDesignedPlanLayoutValidator layouts)
    { _db = db; _users = users; _layouts = layouts; }
    private async Task<IActionResult?> RequireAdmin() { var user = await _users.GetAsync(HttpContext); return user is null ? Unauthorized() : !string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase) ? Forbid() : null; }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string status = "all")
    {
        if (await RequireAdmin() is { } denied) return denied;
        var query = _db.PreDesignedHousePlans.AsNoTracking();
        if (status == "active") query = query.Where(x => x.IsActive); else if (status == "inactive") query = query.Where(x => !x.IsActive);
        return Ok((await query.OrderBy(x => x.DesignCode).ToListAsync()).Select(PreDesignedPlansController.MapSummary));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id)
    { if (await RequireAdmin() is { } denied) return denied; var plan = await _db.PreDesignedHousePlans.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id); return plan is null ? NotFound() : Ok(PreDesignedPlansController.MapDetail(plan)); }

    [HttpPost]
    public async Task<IActionResult> Create(SavePreDesignedPlanDto input)
    {
        if (await RequireAdmin() is { } denied) return denied;
        var invalid = await Validate(input); if (invalid is not null) return invalid;
        var plan = new PreDesignedHousePlan { CreatedAt = DateTimeOffset.UtcNow }; Apply(plan, input);
        _db.Add(plan); await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Detail), new { id = plan.Id }, PreDesignedPlansController.MapDetail(plan));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, SavePreDesignedPlanDto input)
    {
        if (await RequireAdmin() is { } denied) return denied;
        var plan = await _db.PreDesignedHousePlans.FindAsync(id); if (plan is null) return NotFound();
        var invalid = await Validate(input, id); if (invalid is not null) return invalid;
        Apply(plan, input); await _db.SaveChangesAsync(); return Ok(PreDesignedPlansController.MapDetail(plan));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id)
    { if (await RequireAdmin() is { } denied) return denied; var plan = await _db.PreDesignedHousePlans.FindAsync(id); if (plan is null) return NotFound(); plan.IsActive = false; plan.UpdatedAt = DateTimeOffset.UtcNow; await _db.SaveChangesAsync(); return NoContent(); }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id, [FromBody] bool isActive)
    { if (await RequireAdmin() is { } denied) return denied; var plan = await _db.PreDesignedHousePlans.FindAsync(id); if (plan is null) return NotFound(); plan.IsActive = isActive; plan.UpdatedAt = DateTimeOffset.UtcNow; await _db.SaveChangesAsync(); return Ok(new { plan.Id, plan.IsActive }); }

    private async Task<IActionResult?> Validate(SavePreDesignedPlanDto x, Guid? current = null)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(x.Name) || string.IsNullOrWhiteSpace(x.Slug) || string.IsNullOrWhiteSpace(x.DesignCode)) errors.Add("Name, slug and design code are required.");
        if (x.Bedrooms <= 0 || x.Bathrooms <= 0 || x.FloorCount <= 0 || x.TotalBuiltUpAreaSqft <= 0 || x.MinimumLandSizePerches <= 0) errors.Add("Room counts, area and minimum land size must be positive.");
        if (!Terrains.Contains(x.SuitableTerrain)) errors.Add("Suitable terrain must be flat, urban, hillside, coastal or all.");
        if (await _db.PreDesignedHousePlans.AnyAsync(p => p.Id != current && (p.Slug == x.Slug || p.DesignCode == x.DesignCode))) errors.Add("Slug and design code must be unique.");
        errors.AddRange(_layouts.Validate(x.Layout, x.Bedrooms, x.FloorCount));
        return errors.Count == 0 ? null : BadRequest(new { errors });
    }
    private static void Apply(PreDesignedHousePlan p, SavePreDesignedPlanDto x)
    {
        p.Name = x.Name.Trim(); p.Slug = x.Slug.Trim().ToLowerInvariant(); p.DesignCode = x.DesignCode.Trim().ToUpperInvariant(); p.Description = x.Description; p.Style = x.Style.Trim(); p.Bedrooms = x.Bedrooms; p.Bathrooms = x.Bathrooms; p.FloorCount = x.FloorCount; p.TotalBuiltUpAreaSqft = x.TotalBuiltUpAreaSqft; p.MinimumLandSizePerches = x.MinimumLandSizePerches; p.MinimumPlotWidthFt = x.MinimumPlotWidthFt; p.MinimumPlotLengthFt = x.MinimumPlotLengthFt; p.SuitableTerrain = x.SuitableTerrain.ToLowerInvariant(); p.ParkingSpaces = x.ParkingSpaces; p.HasBalcony = x.HasBalcony; p.HasVeranda = x.HasVeranda; p.HasOffice = x.HasOffice; p.HasUtilityRoom = x.HasUtilityRoom; p.IsAccessibleFriendly = x.IsAccessibleFriendly; p.Category = x.Category; p.TagsJson = JsonSerializer.Serialize(x.Tags); p.ThumbnailUrl = x.ThumbnailUrl; p.LayoutJson = x.Layout.GetRawText(); p.IsActive = x.IsActive; p.UpdatedAt = DateTimeOffset.UtcNow;
    }
}
