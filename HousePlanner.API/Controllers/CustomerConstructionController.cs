using System.Text.Json;
using HousePlanner.API.Data;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Controllers;

[ApiController]
[Route("api/v1/customer/construction")]
[Authorize(Roles = "Customer")]
public class CustomerConstructionController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserContextService _currentUser;
    private readonly IConstructorWorkflowService _workflow;

    public CustomerConstructionController(ApplicationDbContext db, ICurrentUserContextService currentUser, IConstructorWorkflowService workflow)
    {
        _db = db; _currentUser = currentUser; _workflow = workflow;
    }

    private async Task<Guid?> CustomerId()
    {
        var user = await _currentUser.GetAsync(HttpContext);
        return user?.Role == "Customer" ? user.Id : null;
    }

    [HttpGet("approved-designs")]
    public async Task<IActionResult> ApprovedDesigns()
    {
        var customerId = await CustomerId(); if (customerId is null) return Unauthorized();
        var items = await _db.ValidationRequests.AsNoTracking()
            .Where(v => v.ClientId == customerId && v.Status == "Approved" && v.HouseDesignId != null)
            .Include(v => v.HouseDesign).ThenInclude(d => d!.WorkflowState)
            .OrderByDescending(v => v.DecisionAt)
            .Select(v => new
            {
                designId = v.HouseDesignId, workflowId = v.WorkflowStateId,
                version = v.HouseDesign!.Version, floorCount = v.HouseDesign.FloorCount,
                area = v.HouseDesign.TotalBuiltUpAreaSqft, layoutJson = v.HouseDesign.LayoutJson,
                approvedAt = v.DecisionAt
            }).ToListAsync();
        return Ok(items.Select(x => new {
            x.designId, x.workflowId, x.version, x.floorCount, x.area, x.approvedAt,
            title = DesignTitle(x.layoutJson, x.version), bedrooms = CountRooms(x.layoutJson, "bedroom"),
            bathrooms = CountRooms(x.layoutJson, "bathroom")
        }));
    }

    [HttpGet("constructors")]
    public async Task<IActionResult> Constructors()
    {
        if (await CustomerId() is null) return Unauthorized();
        return Ok(await _db.Users.AsNoTracking().Where(u => u.Role.Name == "Constructor")
            .OrderBy(u => u.FullName).Select(u => new { id = u.Id, name = u.FullName }).ToListAsync());
    }

    [HttpPost("requests")]
    public async Task<IActionResult> CreateRequest([FromBody] CreateConstructionRequest dto)
    {
        var customerId = await CustomerId(); if (customerId is null) return Unauthorized();
        var approved = await _db.ValidationRequests
            .Include(v => v.WorkflowState).ThenInclude(w => w.LandSubmission)
            .FirstOrDefaultAsync(v => v.HouseDesignId == dto.HouseDesignId && v.Status == "Approved");
        if (approved == null) return BadRequest(new { message = "Construction can only be requested for an architect-approved design." });
        if (approved.ClientId != customerId || approved.WorkflowState.LandSubmission.ClientId != customerId) return NotFound();

        var constructor = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == dto.ConstructorId);
        if (constructor?.Role.Name != "Constructor") return BadRequest(new { message = "The selected account is not an available constructor." });

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.WorkflowStateId == approved.WorkflowStateId);
        if (project == null)
        {
            project = NewProject(approved.WorkflowStateId, dto.HouseDesignId);
            _db.Projects.Add(project);
        }
        if (project.HouseDesignId != null && project.HouseDesignId != dto.HouseDesignId)
            return Conflict(new { message = "The construction project is linked to a different approved design." });
        if (project.ContractorId != null) return Conflict(new { message = "This design already has an active construction project." });
        project.HouseDesignId = dto.HouseDesignId;

        if (await _db.ConstructorProjectRequests.AnyAsync(r => r.HouseDesignId == dto.HouseDesignId && r.ConstructorId == dto.ConstructorId && r.Status == "Pending"))
            return Conflict(new { message = "A pending request already exists for this constructor." });

        var request = new ConstructorProjectRequest
        {
            Id = Guid.NewGuid(), Project = project, ProjectId = project.Id, CustomerId = customerId.Value,
            ConstructorId = dto.ConstructorId, HouseDesignId = dto.HouseDesignId, Status = "Pending"
        };
        _db.ConstructorProjectRequests.Add(request);
        try { await _db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            _db.Entry(request).State = EntityState.Detached;
            if (await _db.ConstructorProjectRequests.AsNoTracking().AnyAsync(r => r.HouseDesignId == dto.HouseDesignId && r.ConstructorId == dto.ConstructorId && r.Status == "Pending"))
                return Conflict(new { message = "A pending request already exists for this constructor." });
            throw;
        }
        return CreatedAtAction(nameof(GetConstruction), new { }, new { request.Id, request.Status });
    }

    [HttpGet]
    public async Task<IActionResult> GetConstruction()
    {
        var customerId = await CustomerId(); if (customerId is null) return Unauthorized();
        var requests = await _db.ConstructorProjectRequests.AsNoTracking()
            .Where(r => r.CustomerId == customerId).Include(r => r.Constructor).Include(r => r.HouseDesign)
            .OrderByDescending(r => r.CreatedAt).Select(r => new {
                r.Id, r.ProjectId, r.HouseDesignId, constructorName = r.Constructor!.FullName,
                r.Status, r.DeclineReason, requestedAt = r.CreatedAt, r.RespondedAt,
                designVersion = r.HouseDesign!.Version
            }).ToListAsync();
        var projects = await _db.Projects.AsNoTracking()
            .Where(p => p.WorkflowState!.LandSubmission.ClientId == customerId && p.ContractorId != null)
            .Include(p => p.Contractor).Include(p => p.HouseDesign).Include(p => p.ConstructionPhases)
            .OrderByDescending(p => p.UpdatedAt).ToListAsync();
        return Ok(new {
            pendingRequests = requests.Where(r => r.Status == "Pending"),
            declinedRequests = requests.Where(r => r.Status == "Declined"),
            activeProjects = projects.Where(p => !p.Status.Equals("completed", StringComparison.OrdinalIgnoreCase)).Select(ProjectSummary),
            completedProjects = projects.Where(p => p.Status.Equals("completed", StringComparison.OrdinalIgnoreCase)).Select(ProjectSummary)
        });
    }

    [HttpGet("projects/{projectId:guid}")]
    public async Task<IActionResult> Project(Guid projectId)
    {
        var customerId = await CustomerId(); if (customerId is null) return Unauthorized();
        var project = await _db.Projects.AsNoTracking()
            .Include(p => p.Contractor).Include(p => p.HouseDesign).Include(p => p.ConstructionPhases)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.WorkflowState!.LandSubmission.ClientId == customerId);
        if (project == null || project.ContractorId == null) return NotFound();
        var logs = await _db.ConstructorWorkflowLogs.AsNoTracking().Include(l => l.ConstructionPhase)
            .Where(l => l.ProjectId == projectId).OrderByDescending(l => l.Date).ThenByDescending(l => l.CreatedAt).ToListAsync();
        var progress = await _workflow.GetProjectProgressAsync(projectId, Guid.Empty, "Admin");
        return Ok(new {
            project = ProjectSummary(project), progress,
            phases = project.ConstructionPhases.OrderBy(p => p.SequenceOrder).Select(p => new { p.Id, p.PhaseName, p.Status, p.SequenceOrder, p.EstimatedDurationDays }),
            logs = logs.Select(l => new { l.Id, l.Date, l.CompletedWork, l.ProgressPercentage, l.Status, l.Challenges, l.Issues, l.Resolution, l.TomorrowPlan, l.AdditionalNotes, phase = l.ConstructionPhase == null ? null : l.ConstructionPhase.PhaseName }),
            activity = logs.GroupBy(l => DateOnly.FromDateTime(l.Date.UtcDateTime)).Select(g => new { date = g.Key, count = g.Count(), intensity = Math.Min(3, g.Count()) })
        });
    }

    private static object ProjectSummary(Project p) => new {
        p.Id, p.Status, p.CreatedAt, p.UpdatedAt, p.HouseDesignId,
        constructorName = p.Contractor?.FullName, designVersion = p.HouseDesign?.Version,
        currentPhase = p.ConstructionPhases.OrderBy(x => x.SequenceOrder).FirstOrDefault(x => x.Status == "in_progress")?.PhaseName
            ?? p.ConstructionPhases.OrderBy(x => x.SequenceOrder).FirstOrDefault(x => x.Status != "completed")?.PhaseName
    };

    private static Project NewProject(Guid workflowId, Guid designId) => new()
    {
        Id = Guid.NewGuid(), WorkflowStateId = workflowId, HouseDesignId = designId, Status = "awaiting_constructor",
        ConstructionPhases = new List<ConstructionPhase> {
            new() { Id=Guid.NewGuid(), PhaseName="Site Preparation", Status="pending", SequenceOrder=1 },
            new() { Id=Guid.NewGuid(), PhaseName="Foundation", Status="pending", SequenceOrder=2 },
            new() { Id=Guid.NewGuid(), PhaseName="Framing", Status="pending", SequenceOrder=3 },
            new() { Id=Guid.NewGuid(), PhaseName="Roofing", Status="pending", SequenceOrder=4 },
            new() { Id=Guid.NewGuid(), PhaseName="Interior & Finish", Status="pending", SequenceOrder=5 }
        }
    };

    private static int CountRooms(string json, string type)
    {
        try { using var doc = JsonDocument.Parse(json); return doc.RootElement.GetProperty("rooms").EnumerateArray().Count(r => r.TryGetProperty("room_type", out var t) && t.GetString()?.Contains(type, StringComparison.OrdinalIgnoreCase) == true); }
        catch { return 0; }
    }
    private static string DesignTitle(string json, int version)
    {
        try { using var doc = JsonDocument.Parse(json); if (doc.RootElement.TryGetProperty("topology", out var t)) return $"{t.GetString()?.Replace('_', ' ')} Home"; }
        catch { }
        return $"Approved Design v{version}";
    }
}

public record CreateConstructionRequest(Guid HouseDesignId, Guid ConstructorId);
