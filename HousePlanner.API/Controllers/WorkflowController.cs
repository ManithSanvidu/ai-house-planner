using System.Text;
using System.Text.Json;
using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Controllers;

[ApiController]
[Route("api/v1/workflows")]
public class WorkflowController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WorkflowController> _logger;
    private readonly HttpClient _agenticServiceClient;
    private readonly IWorkflowService _workflowService;
    private readonly ICurrentUserContextService _currentUserService;

    public WorkflowController(
        ApplicationDbContext context,
        ILogger<WorkflowController> logger,
        IHttpClientFactory httpClientFactory,
        IWorkflowService workflowService,
        ICurrentUserContextService currentUserService)
    {
        _context = context;
        _logger = logger;
        _agenticServiceClient = httpClientFactory.CreateClient("AgenticService");
        _workflowService = workflowService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Returns the current workflow state and latest design summary (including rooms with doors/windows).
    /// </summary>
    /// <param name="id">The WorkflowState unique ID</param>
    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(typeof(WorkflowStatusResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WorkflowStatusResponseDto>> GetWorkflowStatus(Guid id, [FromQuery] Guid? designId = null)
    {
        try
        {
            var workflow = await _context.WorkflowStates
                .AsNoTracking()
                .Where(w => w.Id == id)
                .Select(w => new
                {
                    w.Id,
                    w.Status,
                    w.TerrainType,
                    w.SlopeEstimate,
                    w.ApprovalStatus,
                    w.FailureReason,
                    w.ConstructionPlan,
                    // Pick the current (or latest) design version
                    LatestDesign = w.HouseDesigns
                        .Where(d => !d.IsArchived && (designId == null || d.Id == designId))
                        .OrderByDescending(d => d.IsCurrent)
                        .ThenByDescending(d => d.Version)
                        .Select(d => new
                        {
                            d.Id,
                            d.Version,
                            d.FloorCount,
                            d.TotalBuiltUpAreaSqft,
                            d.FoundationType,
                            d.TemplateId,
                            d.TerrainType,
                            d.IsCurrent,
                            d.LayoutJson,
                            Rooms = d.Rooms
                                .OrderBy(r => r.FloorNumber)
                                .ThenBy(r => r.RoomType)
                                .Select(r => new
                                {
                                    r.Id,
                                    r.RoomType,
                                    r.Name,
                                    r.FloorNumber,
                                    r.X,
                                    r.Y,
                                    r.Width,
                                    r.Length,
                                    r.AreaSqft,
                                    r.WallHeight
                                })
                                .ToList()
                        })
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (workflow is null)
            {
                _logger.LogWarning("Workflow with ID {WorkflowId} not found.", id);
                return NotFound(new { message = $"Workflow {id} not found." });
            }

            HouseDesignSummaryDto? designDto = null;
            if (workflow.LatestDesign is not null)
            {
                // Extract doors/windows from LayoutJson for each room
                var doorsWindowsMap = ExtractDoorsWindows(workflow.LatestDesign.LayoutJson);

                var roomDtos = workflow.LatestDesign.Rooms.Select(r =>
                {
                    var roomKey = (r.RoomType, r.FloorNumber, r.X, r.Y);
                    doorsWindowsMap.TryGetValue(roomKey, out var openings);

                    return new RoomSummaryDto(
                        RoomId: openings?.SourceId ?? r.Id,
                        RoomType: r.RoomType,
                        Name: r.Name,
                        FloorNumber: r.FloorNumber,
                        X: r.X,
                        Y: r.Y,
                        Width: r.Width,
                        Length: r.Length,
                        AreaSqft: r.AreaSqft,
                        WallHeight: r.WallHeight,
                        Doors: openings?.Doors,
                        Windows: openings?.Windows
                    );
                }).ToList();

                using var metadata = ParseLayout(workflow.LatestDesign.LayoutJson);
                var root = metadata.RootElement;
                designDto = new HouseDesignSummaryDto(
                    DesignId: workflow.LatestDesign.Id,
                    Version: workflow.LatestDesign.Version,
                    FloorCount: workflow.LatestDesign.FloorCount,
                    TotalBuiltUpAreaSqft: workflow.LatestDesign.TotalBuiltUpAreaSqft,
                    FoundationType: workflow.LatestDesign.FoundationType,
                    TemplateId: workflow.LatestDesign.TemplateId,
                    TerrainType: workflow.LatestDesign.TerrainType,
                    IsCurrent: workflow.LatestDesign.IsCurrent,
                    Rooms: roomDtos,
                    TemplateFamily: root.TryGetProperty("template_family", out var tf) ? tf.GetString() : null,
                    DesignSeed: root.TryGetProperty("design_seed", out var ds) && ds.ValueKind != JsonValueKind.Null ? ds.GetInt64() : null,
                    DesignScore: root.TryGetProperty("design_score", out var sc) && sc.ValueKind != JsonValueKind.Null ? sc.GetDecimal() : null,
                    GeometryFingerprint: root.TryGetProperty("geometry_fingerprint", out var fp) && fp.ValueKind != JsonValueKind.Null ? fp.GetString() : null,
                    GroundFootprintSqft: root.TryGetProperty("ground_footprint_sqft", out var gf) && gf.ValueKind != JsonValueKind.Null ? gf.GetDecimal() : null,
                    Connections: GetMetadata(root, "room_connections"),
                    Entrances: GetMetadata(root, "entrances"),
                    PlotConstraints: GetMetadata(root, "plot_constraints"),
                    CandidateSummary: GetMetadata(root, "candidate_summary")
                );
            }

            JsonElement? parsedConstructionPlan = null;
            if (!string.IsNullOrEmpty(workflow.ConstructionPlan))
            {
                try
                {
                    using var doc = JsonDocument.Parse(workflow.ConstructionPlan);
                    parsedConstructionPlan = doc.RootElement.Clone();
                }
                catch (JsonException)
                {
                    // Ignore JSON parsing failure
                }
            }

            var responseDto = new WorkflowStatusResponseDto(
                WorkflowId: workflow.Id,
                Status: workflow.Status,
                TerrainType: workflow.TerrainType,
                SlopeEstimate: workflow.SlopeEstimate,
                Design: designDto,
                Cost: null,
                ConstructionPlan: parsedConstructionPlan,
                ApprovalStatus: workflow.ApprovalStatus,
                FailureReason: workflow.FailureReason
            );

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving status for workflow {WorkflowId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred retrieving workflow status." });
        }
    }

    /// <summary>
    /// Extract doors and windows from the LayoutJson for each room (keyed by room_type).
    /// </summary>
    private static Dictionary<(string, int, decimal, decimal), RoomOpenings> ExtractDoorsWindows(string layoutJson)
    {
        var result = new Dictionary<(string, int, decimal, decimal), RoomOpenings>();

        try
        {
            using var doc = JsonDocument.Parse(layoutJson);
            if (doc.RootElement.TryGetProperty("rooms", out var roomsElement) && roomsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var roomEl in roomsElement.EnumerateArray())
                {
                    var roomType = roomEl.TryGetProperty("room_type", out var rt) ? rt.GetString() ?? "" : "";

                    var doors = new List<OpeningDto>();
                    var windows = new List<OpeningDto>();

                    if (roomEl.TryGetProperty("doors", out var doorsEl) && doorsEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var d in doorsEl.EnumerateArray())
                        {
                            doors.Add(new OpeningDto(
                                Wall: d.TryGetProperty("wall", out var w) ? w.GetString() ?? "" : "",
                                Offset: d.TryGetProperty("offset", out var o) ? o.GetDecimal() : 0m,
                                Width: d.TryGetProperty("width", out var wd) ? wd.GetDecimal() : 0m
                            ));
                        }
                    }

                    if (roomEl.TryGetProperty("windows", out var winsEl) && winsEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var win in winsEl.EnumerateArray())
                        {
                            windows.Add(new OpeningDto(
                                Wall: win.TryGetProperty("wall", out var w) ? w.GetString() ?? "" : "",
                                Offset: win.TryGetProperty("offset", out var o) ? o.GetDecimal() : 0m,
                                Width: win.TryGetProperty("width", out var wd) ? wd.GetDecimal() : 0m
                            ));
                        }
                    }

                    var floor = roomEl.TryGetProperty("floor", out var f) ? f.GetInt32() : 1;
                    var x = roomEl.TryGetProperty("x", out var xp) ? xp.GetDecimal() : 0;
                    var y = roomEl.TryGetProperty("y", out var yp) ? yp.GetDecimal() : 0;
                    Guid? sourceId = roomEl.TryGetProperty("room_id", out var id) && id.TryGetGuid(out var guid) ? guid : null;
                    result[(roomType, floor, x, y)] = new RoomOpenings { SourceId = sourceId, Doors = doors, Windows = windows };
                }
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Warning: Could not parse LayoutJson for doors/windows: {ex.Message}");
        }

        return result;
    }

    private static JsonDocument ParseLayout(string json)
    {
        try { return JsonDocument.Parse(json); }
        catch (JsonException) { return JsonDocument.Parse("{}"); }
    }

    private static JsonElement? GetMetadata(JsonElement root, string key) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty(key, out var value)
        && value.ValueKind != JsonValueKind.Null ? value.Clone() : null;

    private class RoomOpenings
    {
        public Guid? SourceId { get; set; }
        public List<OpeningDto> Doors { get; set; } = new();
        public List<OpeningDto> Windows { get; set; } = new();
    }

    [HttpGet("{id:guid}/designs")]
    public async Task<IActionResult> GetDesigns(Guid id, [FromQuery] bool includeArchived = true)
    {
        var workflow = await _context.WorkflowStates.AsNoTracking()
            .Include(w => w.HouseDesigns).ThenInclude(d => d.Rooms)
            .FirstOrDefaultAsync(w => w.Id == id);
        if (workflow is null) return NotFound(new { message = $"Workflow {id} not found." });
        return Ok(ToHistory(workflow, includeArchived));
    }

    [HttpGet("designs")]
    public async Task<IActionResult> GetMyDesigns()
    {
        var user = await _currentUserService.GetAsync(HttpContext);
        if (user?.Id is null) return Unauthorized(new { message = "User not identified." });
        var workflows = await _context.WorkflowStates.AsNoTracking()
            .Where(w => w.LandSubmission.ClientId == user.Id.Value)
            .Include(w => w.HouseDesigns).ThenInclude(d => d.Rooms)
            .OrderByDescending(w => w.UpdatedAt)
            .ToListAsync();
        return Ok(workflows.Select(w => ToHistory(w, false)).Where(w => w.Designs.Count > 0));
    }

    [HttpPost("{id:guid}/designs/{designId:guid}/select")]
    public async Task<IActionResult> SelectDesign(Guid id, Guid designId)
    {
        var workflow = await _context.WorkflowStates.Include(w => w.HouseDesigns)
            .FirstOrDefaultAsync(w => w.Id == id);
        if (workflow is null) return NotFound(new { message = $"Workflow {id} not found." });
        if (!workflow.HouseDesigns.Any(d => d.Id == designId && !d.IsArchived))
            return BadRequest(new { message = "The selected design does not belong to this workflow." });
        workflow.PreferredHouseDesignId = designId;
        workflow.Status = "selected_by_client";
        workflow.ApprovalStatus = "selected_by_client";
        workflow.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { workflowId = id, preferredHouseDesignId = designId, status = workflow.Status });
    }

    [HttpDelete("{id:guid}/design-selection")]
    public async Task<IActionResult> ClearDesignSelection(Guid id)
    {
        var workflow = await _context.WorkflowStates.FirstOrDefaultAsync(w => w.Id == id);
        if (workflow is null) return NotFound(new { message = $"Workflow {id} not found." });
        workflow.PreferredHouseDesignId = null;
        if (workflow.Status == "selected_by_client") workflow.Status = "design_generated";
        if (workflow.ApprovalStatus == "selected_by_client") workflow.ApprovalStatus = "client_review";
        workflow.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { workflowId = id, preferredHouseDesignId = (Guid?)null, status = workflow.Status });
    }

    [HttpDelete("{id:guid}/designs/{designId:guid}")]
    public async Task<IActionResult> RemoveDesign(Guid id, Guid designId)
    {
        var workflow = await _context.WorkflowStates.Include(w => w.HouseDesigns)
            .FirstOrDefaultAsync(w => w.Id == id);
        if (workflow is null) return NotFound(new { message = $"Workflow {id} not found." });
        var design = workflow.HouseDesigns.FirstOrDefault(d => d.Id == designId && !d.IsArchived);
        if (design is null) return NotFound(new { message = "Design version not found." });
        if (workflow.Status == "approved" || workflow.ApprovalStatus == "approved")
            return Conflict(new { message = "Approved designs cannot be deleted or archived." });

        var submitted = workflow.Status == "awaiting_architect_review" ||
            workflow.ApprovalStatus == "awaiting_architect_review" ||
            await _context.ValidationRequests.AnyAsync(r => r.WorkflowStateId == id &&
                (r.Status == "Pending" || r.Status == "Under Review"));
        design.IsArchived = true;
        design.IsCurrent = false;
        if (workflow.PreferredHouseDesignId == designId) workflow.PreferredHouseDesignId = null;
        var newestRemaining = workflow.HouseDesigns.Where(d => !d.IsArchived && d.Id != designId)
            .OrderByDescending(d => d.Version).FirstOrDefault();
        if (newestRemaining is not null && !workflow.HouseDesigns.Any(d => !d.IsArchived && d.Id != designId && d.IsCurrent))
            newestRemaining.IsCurrent = true;
        workflow.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { workflowId = id, designId, action = submitted ? "archived" : "deleted", selectionCleared = workflow.PreferredHouseDesignId is null });
    }

    [HttpPost("{id:guid}/submit-architect-review")]
    public async Task<IActionResult> SubmitArchitectReview(Guid id)
    {
        var workflow = await _context.WorkflowStates.Include(w => w.LandSubmission)
            .Include(w => w.HouseDesigns).FirstOrDefaultAsync(w => w.Id == id);
        if (workflow is null) return NotFound(new { message = $"Workflow {id} not found." });
        if (workflow.PreferredHouseDesignId is null ||
            !workflow.HouseDesigns.Any(d => d.Id == workflow.PreferredHouseDesignId))
            return BadRequest(new { message = "Select a design before submitting it for architect review." });
        var active = await _context.ValidationRequests.AnyAsync(r => r.WorkflowStateId == id &&
            (r.Status == "Pending" || r.Status == "Under Review"));
        if (!active)
            _context.ValidationRequests.Add(new HousePlanner.API.Entities.ValidationRequest
            {
                WorkflowStateId = id,
                ClientId = workflow.LandSubmission.ClientId,
                Status = "Pending"
            });
        workflow.Status = "awaiting_architect_review";
        workflow.ApprovalStatus = "awaiting_architect_review";
        workflow.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { workflowId = id, status = workflow.Status });
    }

    private static WorkflowDesignHistoryDto ToHistory(HousePlanner.API.Entities.WorkflowState workflow, bool includeArchived = true) =>
        new(workflow.Id, workflow.Status, workflow.PreferredHouseDesignId, workflow.CreatedAt,
            workflow.HouseDesigns.Where(d => includeArchived || !d.IsArchived).OrderByDescending(d => d.Version).Select(d =>
            {
                using var document = ParseLayout(d.LayoutJson);
                var root = document.RootElement;
                var summary = root.TryGetProperty("candidate_summary", out var value) && value.ValueKind == JsonValueKind.Object
                    ? value : default;
                string? SummaryString(string key) => summary.ValueKind == JsonValueKind.Object &&
                    summary.TryGetProperty(key, out var item) && item.ValueKind == JsonValueKind.String ? item.GetString() : null;
                decimal? Number(JsonElement container, string key) => container.ValueKind == JsonValueKind.Object &&
                    container.TryGetProperty(key, out var item) && item.ValueKind == JsonValueKind.Number ? item.GetDecimal() : null;
                var bedrooms = d.Rooms.Count(r => r.RoomType.Contains("bedroom", StringComparison.OrdinalIgnoreCase));
                var bathrooms = d.Rooms.Count(r => r.RoomType.Contains("bathroom", StringComparison.OrdinalIgnoreCase));
                return new DesignHistoryDto(
                    d.Id, d.Version, d.IsCurrent, workflow.PreferredHouseDesignId == d.Id, d.IsArchived,
                    root.TryGetProperty("template_family", out var topology) ? topology.GetString() : d.TemplateId,
                    bedrooms, bathrooms, d.FloorCount, d.TotalBuiltUpAreaSqft, d.FoundationType,
                    SummaryString("generation_mode"), SummaryString("selected_plan_code") ?? SummaryString("base_plan_code"),
                    root.TryGetProperty("geometry_fingerprint", out var fingerprint) ? fingerprint.GetString() : null,
                    Number(summary, "compatibility_score") ?? Number(summary, "suitability_score"),
                    Number(root, "design_score"),
                    d.Rooms.Select(r => new DesignPreviewRoomDto(r.RoomType, r.FloorNumber, r.X, r.Y, r.Width, r.Length)).ToList(),
                    d.CreatedAt);
            }).ToList());

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveWorkflow(Guid id, [FromBody] ApprovalRequestDto request)
    {
        var user = await _currentUserService.GetAsync(HttpContext);
        var result = await _workflowService.ProcessApprovalAsync(id, request, user?.Email, user?.Role);

        switch (result.Outcome)
        {
            case ApprovalOutcome.NotFound:
                return NotFound(new { message = result.ErrorMessage });
            case ApprovalOutcome.Conflict:
                return Conflict(new { message = result.ErrorMessage });
            case ApprovalOutcome.InvalidState:
            case ApprovalOutcome.ValidationFailed:
            case ApprovalOutcome.BadRequest:
                return BadRequest(new { message = result.ErrorMessage });
            case ApprovalOutcome.Unauthorized:
                return StatusCode(StatusCodes.Status403Forbidden, new { message = result.ErrorMessage });
            case ApprovalOutcome.Success:
                if (request.Decision.Trim().ToLowerInvariant() is "request_revision" or "revision_requested" or "revision")
                {
                    // Resume LangGraph workflow in Python
                    var workflow = await _context.WorkflowStates
                        .Include(w => w.LandSubmission)
                        .Include(w => w.HouseDesigns)
                        .FirstOrDefaultAsync(w => w.Id == id);
                    if (workflow != null && workflow.LandSubmission != null)
                    {
                        var current = workflow.HouseDesigns.OrderByDescending(d => d.Version).FirstOrDefault();
                        if (current != null)
                        {
                            using var currentLayout = ParseLayout(current.LayoutJson);
                            var root = currentLayout.RootElement;
                            var currentSeed = GetMetadata(root, "design_seed")?.GetInt64() ?? 0;
                            var nextSeed = currentSeed + 1;

                            var payload = new
                            {
                                workflow_id = id,
                                resume_from = "design",
                                user_revision_prompt = request.RevisionNotes,
                                budget_lkr = workflow.LandSubmission.BudgetLkr,
                                land_size_perches = workflow.LandSubmission.LandSizePerches,
                                manual_terrain_type = workflow.LandSubmission.ManualTerrainType,
                                preferences = BuildRevisionPreferences(root, workflow.LandSubmission),
                                terrain_result = new
                                {
                                    terrain_type = workflow.TerrainType,
                                    slope_estimate = workflow.SlopeEstimate
                                },
                                previous_design = root.Clone(),
                                plot_constraints = GetMetadata(root, "plot_constraints"),
                                design_seed = nextSeed
                            };

                            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                            await _agenticServiceClient.PostAsync("/workflows/resume", content);
                        }
                    }
                }
                return Ok(result.Response);
            default:
                return BadRequest(new { message = "Unknown approval outcome." });
        }
    }

    private static Dictionary<string, object?> BuildRevisionPreferences(
        JsonElement layout, HousePlanner.API.Entities.LandSubmission submission)
    {
        var preferences = new Dictionary<string, object?>
        {
            ["bedrooms"] = submission.PreferredBedrooms,
            ["floors"] = submission.PreferredFloors,
            ["style"] = submission.StylePreference
        };

        if (layout.TryGetProperty("candidate_summary", out var summary)
            && summary.TryGetProperty("normalized_input", out var normalized)
            && normalized.ValueKind == JsonValueKind.Object)
        {
            var mappings = new Dictionary<string, string>
            {
                ["bedrooms"] = "bedrooms", ["bathrooms"] = "bathrooms", ["floors"] = "floors",
                ["architectural_style"] = "style", ["space_priority"] = "space_priority",
                ["open_plan"] = "open_plan", ["master_ensuite"] = "attached_bathroom",
                ["separate_dining"] = "dining_required", ["home_office"] = "home_office",
                ["balcony"] = "balcony", ["veranda"] = "veranda",
                ["utility_room"] = "utility_room", ["parking_required"] = "parking",
                ["accessibility"] = "accessibility"
            };
            foreach (var mapping in mappings)
                if (normalized.TryGetProperty(mapping.Key, out var value) && value.ValueKind != JsonValueKind.Null)
                    preferences[mapping.Value] = value.Clone();
        }

        if (!preferences.ContainsKey("bathrooms"))
        {
            var bathroomCount = layout.TryGetProperty("rooms", out var rooms)
                ? rooms.EnumerateArray().Count(room => room.TryGetProperty("room_type", out var type)
                    && (type.GetString() ?? string.Empty).Contains("bathroom", StringComparison.OrdinalIgnoreCase))
                : 0;
            if (bathroomCount > 0)
                preferences["bathrooms"] = bathroomCount;
        }
        return preferences;
    }
}
