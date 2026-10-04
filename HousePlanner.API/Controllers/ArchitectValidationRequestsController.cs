using System.Text.Json;
using HousePlanner.API.Data;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Controllers;

[ApiController]
[Route("api/v1/architect/validation-requests")]
[Authorize(Roles = "Architect")]
public sealed class ArchitectValidationRequestsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserContextService _currentUser;

    public ArchitectValidationRequestsController(
        ApplicationDbContext context,
        ICurrentUserContextService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var architect = await RequireArchitectAsync();
        if (architect.Result is not null) return architect.Result;
        var counts = await _context.ValidationRequests.AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(x => new { Status = x.Key, Count = x.Count() })
            .ToListAsync();
        int Count(string status) => counts.FirstOrDefault(x => x.Status == status)?.Count ?? 0;
        return Ok(new
        {
            pending = Count("Pending"),
            underReview = Count("Under Review"),
            approved = Count("Approved"),
            rejected = Count("Rejected")
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetRequests(
        [FromQuery] string[]? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var architect = await RequireArchitectAsync();
        if (architect.Result is not null) return architect.Result;
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _context.ValidationRequests.AsNoTracking();
        if (status is { Length: > 0 }) query = query.Where(x => status.Contains(x.Status));
        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                id = x.Id,
                clientName = x.Client.FullName,
                submissionDate = x.CreatedAt,
                status = x.Status,
                budget = x.WorkflowState.LandSubmission.BudgetLkr,
                landSize = x.WorkflowState.LandSubmission.LandSizePerches,
                bedrooms = x.WorkflowState.LandSubmission.PreferredBedrooms,
                floors = x.WorkflowState.LandSubmission.PreferredFloors,
                style = x.WorkflowState.LandSubmission.StylePreference,
                designVersion = x.HouseDesign != null ? x.HouseDesign.Version : (int?)null,
                bathrooms = x.HouseDesign != null
                    ? x.HouseDesign.Rooms.Count(r => r.RoomType.Contains("bathroom")) : 0,
                area = x.HouseDesign != null ? x.HouseDesign.TotalBuiltUpAreaSqft : (decimal?)null
            }).ToListAsync();
        return Ok(new
        {
            items,
            page,
            pageSize,
            totalCount,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRequestDetails(Guid id)
    {
        var architect = await RequireArchitectAsync();
        if (architect.Result is not null) return architect.Result;
        var request = await DetailsQuery().FirstOrDefaultAsync(x => x.Id == id);
        if (request is null) return NotFound();
        if (request.ArchitectId.HasValue && request.ArchitectId != architect.Id) return Forbid();
        if (request.Status == "Pending")
        {
            request.Status = "Under Review";
            request.ArchitectId = architect.Id;
            request.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
        }
        return Ok(MapDetails(request));
    }

    [HttpPatch("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ArchitectReviewDto dto) =>
        await Decide(id, dto, approve: true, revisionRequested: false);

    [HttpPatch("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ArchitectReviewDto dto) =>
        await Decide(id, dto, approve: false, revisionRequested: false);

    [HttpPatch("{id:guid}/request-revision")]
    public async Task<IActionResult> RequestRevision(Guid id, [FromBody] ArchitectReviewDto dto) =>
        await Decide(id, dto, approve: false, revisionRequested: true);

    private async Task<IActionResult> Decide(
        Guid id, ArchitectReviewDto dto, bool approve, bool revisionRequested)
    {
        var architect = await RequireArchitectAsync();
        if (architect.Result is not null) return architect.Result;
        var request = await _context.ValidationRequests
            .Include(x => x.WorkflowState)
            .Include(x => x.HouseDesign)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (request is null) return NotFound();
        if (request.Status is not ("Pending" or "Under Review"))
            return Conflict(new { message = "This request has already been finalized." });
        if (request.ArchitectId.HasValue && request.ArchitectId != architect.Id) return Forbid();
        if (!approve && string.IsNullOrWhiteSpace(dto.Review))
            return BadRequest(new { message = "A review note is required." });
        if (approve && request.HouseDesign is null)
            return BadRequest(new { message = "A selected design is required before approval." });

        var now = DateTimeOffset.UtcNow;
        request.Status = approve ? "Approved" : "Rejected";
        request.ArchitectReview = dto.Review;
        request.ArchitectId = architect.Id;
        request.DecisionAt = now;
        request.UpdatedAt = now;
        request.WorkflowState.Status = approve ? "approved" : revisionRequested ? "revision_requested" : "rejected";
        request.WorkflowState.ApprovalStatus = approve ? "approved" : revisionRequested ? "revision_requested" : "rejected";
        request.WorkflowState.UpdatedAt = now;

        if (approve)
        {
            request.WorkflowState.ApprovedByUserId = architect.Id;
            request.WorkflowState.ApprovedAt = now;
            var project = await _context.Projects.FirstOrDefaultAsync(
                x => x.WorkflowStateId == request.WorkflowStateId);
            if (project is null)
            {
                _context.Projects.Add(new Project
                {
                    Id = Guid.NewGuid(), WorkflowStateId = request.WorkflowStateId,
                    HouseDesignId = request.HouseDesignId, Status = "awaiting_constructor"
                });
            }
            else
            {
                project.HouseDesignId = request.HouseDesignId;
                if (!project.ContractorId.HasValue) project.Status = "awaiting_constructor";
                project.UpdatedAt = now;
            }
        }
        await _context.SaveChangesAsync();
        return Ok(new { message = approve ? "Request approved." : revisionRequested
            ? "Revision requested." : "Request rejected." });
    }

    private async Task<(Guid? Id, IActionResult? Result)> RequireArchitectAsync()
    {
        var user = await _currentUser.GetAsync(HttpContext);
        if (user?.Id is null) return (null, Unauthorized());
        return string.Equals(user.Role, "Architect", StringComparison.OrdinalIgnoreCase)
            ? (user.Id, null)
            : (null, Forbid());
    }

    private IQueryable<ValidationRequest> DetailsQuery() => _context.ValidationRequests
        .Include(x => x.Client)
        .Include(x => x.WorkflowState).ThenInclude(x => x.LandSubmission)
        .Include(x => x.HouseDesign).ThenInclude(x => x!.Rooms)
        .Include(x => x.HouseDesign).ThenInclude(x => x!.CostEstimates);

    private static object MapDetails(ValidationRequest request)
    {
        var design = request.HouseDesign;
        var cost = design?.CostEstimates.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
        return new
        {
            id = request.Id,
            clientName = request.Client?.FullName,
            clientEmail = request.Client?.Email,
            submissionDate = request.CreatedAt,
            status = request.Status,
            budget = request.WorkflowState.LandSubmission.BudgetLkr,
            landSize = request.WorkflowState.LandSubmission.LandSizePerches,
            bedrooms = request.WorkflowState.LandSubmission.PreferredBedrooms,
            bathrooms = request.WorkflowState.LandSubmission.PreferredBathrooms,
            floors = request.WorkflowState.LandSubmission.PreferredFloors,
            style = request.WorkflowState.LandSubmission.StylePreference,
            terrainType = request.WorkflowState.TerrainType,
            foundationType = design?.FoundationType,
            architectReview = request.ArchitectReview,
            decisionAt = request.DecisionAt,
            validationResult = ParseValidation(request.WorkflowState.ValidationResultJson),
            approvalEligibility = new
            {
                canApprove = design is not null && request.Status is ("Pending" or "Under Review"),
                reason = design is null ? "A selected design is required before approval."
                    : request.Status is not ("Pending" or "Under Review")
                        ? "This request has already been finalized." : null,
                budgetStatus = cost?.BudgetDeltaPercent is decimal percent
                    ? percent < 100m ? "within_budget" : percent == 100m ? "at_budget" : "over_budget"
                    : "unavailable"
            },
            cost = cost is null ? null : new
            {
                materialCostLkr = cost.MaterialCostLkr,
                labourCostLkr = cost.LabourCostLkr,
                totalCostLkr = cost.TotalCostLkr,
                budgetDeltaPercent = cost.BudgetDeltaPercent,
                breakdown = CostBreakdownBuilder.Build(cost, design?.TerrainType),
                formulaVersion = cost.FormulaVersion,
                appliedAreaSqft = cost.AppliedAreaSqft,
                terrainType = cost.TerrainType,
                estimatedAt = cost.CreatedAt
            },
            design = design is null ? null : new
            {
                designId = design.Id,
                version = design.Version,
                floorCount = design.FloorCount,
                totalBuiltUpAreaSqft = design.TotalBuiltUpAreaSqft,
                layoutJson = design.LayoutJson,
                rooms = design.Rooms.OrderBy(x => x.FloorNumber).ThenBy(x => x.RoomType).Select(x => new
                {
                    roomId = x.Id, roomType = x.RoomType, name = x.Name, floorNumber = x.FloorNumber,
                    x = x.X, y = x.Y, width = x.Width, length = x.Length, wallHeight = x.WallHeight,
                    doors = Array.Empty<object>(), windows = Array.Empty<object>()
                })
            }
        };
    }

    private static object? ParseValidation(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            return new
            {
                passed = root.TryGetProperty("passed", out var passed) && passed.GetBoolean(),
                rules = root.TryGetProperty("rules", out var rules) && rules.ValueKind == JsonValueKind.Array
                    ? rules.EnumerateArray().Select(rule => new
                    {
                        ruleName = Text(rule, "rule_name"),
                        passed = rule.TryGetProperty("passed", out var rp) && rp.GetBoolean(),
                        status = Text(rule, "status"),
                        reason = Text(rule, "reason"),
                        expected = Value(rule, "expected"),
                        actual = Value(rule, "actual")
                    }).ToList() : [],
                errors = root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array
                    ? errors.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String)
                        .Select(x => x.GetString()).ToList() : [],
                summary = Text(root, "summary"),
                revisionReason = Text(root, "revision_reason")
            };
        }
        catch (JsonException) { return null; }
    }

    private static string? Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() : null;

    private static object? Value(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null) return null;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => value.GetRawText()
        };
    }
}

public sealed record ArchitectReviewDto(string? Review = null);
