using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Services;

namespace HousePlanner.API.Controllers
{
    [ApiController]
    [Route("api/v1/workflows")]
    public class WorkflowController : ControllerBase
    {
        private readonly ApplicationDbContext? _context;
        private readonly IWorkflowService? _workflowService;
        private readonly ILogger<WorkflowController> _logger;
        private readonly HttpClient? _agenticServiceClient;

        public WorkflowController(
            ApplicationDbContext context,
            IWorkflowService workflowService,
            ILogger<WorkflowController> logger,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _workflowService = workflowService;
            _logger = logger;
            _agenticServiceClient = httpClientFactory?.CreateClient("AgenticService");
        }



        /// <summary>
        /// Retrieves the current execution status, validation outcome, and latest design summary.
        /// </summary>
        [HttpGet("{id:guid}/status")]
        [ProducesResponseType(typeof(WorkflowStatusResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetStatus(Guid id)
        {
            try
            {
                WorkflowSessionInfo? session = null;
                if (_workflowService != null)
                {
                    session = await _workflowService.GetWorkflowStatusAsync(id);
                }

                // Fetch latest design details from DB if available
                var dbWorkflow = _context != null ? await _context.WorkflowStates
                    .AsNoTracking()
                    .Where(w => w.Id == id)
                    .Select(w => new
                    {
                        w.Id,
                        w.Status,
                        w.TerrainType,
                        w.SlopeEstimate,
                        w.ApprovalStatus,
                        LatestDesign = w.HouseDesigns
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
                    .FirstOrDefaultAsync() : null;

                if (session == null && dbWorkflow == null)
                {
                    _logger.LogWarning("Workflow with ID {WorkflowId} not found.", id);
                    return NotFound(new { message = $"Workflow {id} not found." });
                }

                HouseDesignSummaryDto? designDto = null;
                if (dbWorkflow?.LatestDesign != null)
                {
                    var doorsWindowsMap = ExtractDoorsWindows(dbWorkflow.LatestDesign.LayoutJson);

                    var roomDtos = dbWorkflow.LatestDesign.Rooms.Select(r =>
                    {
                        var roomKey = r.RoomType;
                        doorsWindowsMap.TryGetValue(roomKey, out var openings);

                        return new RoomSummaryDto(
                            RoomId: r.Id,
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

                    designDto = new HouseDesignSummaryDto(
                        DesignId: dbWorkflow.LatestDesign.Id,
                        Version: dbWorkflow.LatestDesign.Version,
                        FloorCount: dbWorkflow.LatestDesign.FloorCount,
                        TotalBuiltUpAreaSqft: dbWorkflow.LatestDesign.TotalBuiltUpAreaSqft,
                        FoundationType: dbWorkflow.LatestDesign.FoundationType,
                        TemplateId: dbWorkflow.LatestDesign.TemplateId,
                        TerrainType: dbWorkflow.LatestDesign.TerrainType,
                        IsCurrent: dbWorkflow.LatestDesign.IsCurrent,
                        Rooms: roomDtos
                    );
                }

                var response = new WorkflowStatusResponseDto
                {
                    WorkflowId = session?.WorkflowId ?? dbWorkflow!.Id,
                    Status = session?.Status ?? dbWorkflow!.Status,
                    ApprovalStatus = session?.ApprovalStatus ?? dbWorkflow?.ApprovalStatus ?? "not_requested",
                    ValidationPassed = session?.ValidationPassed ?? (dbWorkflow?.ApprovalStatus == "approved" || dbWorkflow?.ApprovalStatus == "pending"),
                    RetryCount = session?.RetryCount ?? 0,
                    RevisionNotes = session?.RevisionNotes,
                    ValidationResult = session?.ValidationResult,
                    ProjectId = session?.ProjectId,
                    TerrainType = dbWorkflow?.TerrainType,
                    SlopeEstimate = dbWorkflow?.SlopeEstimate,
                    Design = designDto,
                    Cost = null,
                    CreatedAt = session?.CreatedAt ?? DateTimeOffset.UtcNow,
                    UpdatedAt = session?.UpdatedAt ?? DateTimeOffset.UtcNow
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workflow status for ID {WorkflowId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred retrieving workflow status." });
            }
        }

        [NonAction]
        public async Task<ActionResult<WorkflowStatusResponseDto>> GetWorkflowStatus(Guid id)
        {
            var actionResult = await GetStatus(id);
            return (ActionResult)actionResult;
        }

        /// <summary>
        /// Submits an authorized human approval decision (approve, reject, or request_revision)
        /// on a workflow proposal that has successfully passed deterministic validation.
        /// </summary>
        [HttpPost("{id:guid}/approve")]
        [ProducesResponseType(typeof(ApprovalResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Approve(Guid id, [FromBody] ApprovalRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (_workflowService != null)
            {
                var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                var result = await _workflowService.ProcessApprovalAsync(id, request, userEmail, userRole);

                if (result.Outcome == ApprovalOutcome.Success && (request.Decision.Trim().ToLowerInvariant() is "request_revision" or "revision_requested" or "revision"))
                {
                    if (_agenticServiceClient != null && _context != null)
                    {
                        var dbWorkflow = await _context.WorkflowStates.FindAsync(id);
                        if (dbWorkflow != null)
                        {
                            var submission = await _context.LandSubmissions.FindAsync(dbWorkflow.LandSubmissionId);
                            if (submission != null)
                            {
                                var payload = new {
                                    workflow_id = id,
                                    submission_id = submission.Id,
                                    budget_lkr = submission.BudgetLkr,
                                    land_size_perches = submission.LandSizePerches,
                                    manual_terrain_type = submission.ManualTerrainType,
                                    revision_notes = request.RevisionNotes,
                                    preferences = new {
                                        bedrooms = submission.PreferredBedrooms,
                                        floors = submission.PreferredFloors,
                                        architecturalStyle = submission.StylePreference
                                    }
                                };
                                _agenticServiceClient.DefaultRequestHeaders.Clear();
                                _agenticServiceClient.DefaultRequestHeaders.Add("X-Internal-API-Key", "shared-internal-secret");
                                try
                                {
                                    await _agenticServiceClient.PostAsJsonAsync("http://localhost:8001/workflows/resume", payload);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failed to call python /workflows/resume endpoint for {WorkflowId}", id);
                                }
                            }
                        }
                    }
                }

                return result.Outcome switch
                {
                    ApprovalOutcome.Success => Ok(result.Response),
                    ApprovalOutcome.NotFound => NotFound(new { Message = result.ErrorMessage }),
                    ApprovalOutcome.InvalidState => BadRequest(new { Message = result.ErrorMessage }),
                    ApprovalOutcome.ValidationFailed => BadRequest(new { Message = result.ErrorMessage }),
                    ApprovalOutcome.Conflict => Conflict(new { Message = result.ErrorMessage }),
                    ApprovalOutcome.BadRequest => BadRequest(new { Message = result.ErrorMessage }),
                    ApprovalOutcome.Unauthorized => Unauthorized(new { Message = result.ErrorMessage }),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An unexpected error occurred." })
                };
            }

            if (_context != null)
            {
                var dbWorkflow = await _context.WorkflowStates.FindAsync(id);
                if (dbWorkflow == null) return NotFound(new { Message = $"Workflow {id} not found." });

                var normalizedDecision = request.Decision.Trim().ToLowerInvariant();
                if (normalizedDecision is "request_revision" or "revision_requested" or "revision")
                {
                    dbWorkflow.Status = "running";
                    dbWorkflow.ApprovalStatus = "revision_requested";
                    dbWorkflow.UpdatedAt = DateTimeOffset.UtcNow;
                    await _context.SaveChangesAsync();

                    return Ok(new { Message = "Revision started" });
                }
                else if (normalizedDecision is "approve" or "approved")
                {
                    dbWorkflow.ApprovalStatus = "approved";
                    dbWorkflow.Status = "approved";
                    dbWorkflow.ApprovedAt = DateTimeOffset.UtcNow;
                    dbWorkflow.UpdatedAt = DateTimeOffset.UtcNow;
                    await _context.SaveChangesAsync();

                    return Ok(new { Message = "Workflow approved successfully" });
                }
                else if (normalizedDecision is "reject" or "rejected")
                {
                    dbWorkflow.ApprovalStatus = "rejected";
                    dbWorkflow.Status = "rejected";
                    dbWorkflow.UpdatedAt = DateTimeOffset.UtcNow;
                    await _context.SaveChangesAsync();

                    return Ok(new { Message = "Workflow rejected" });
                }

                return BadRequest(new { Message = "Invalid Decision. Use 'approve', 'reject', or 'request_revision'." });
            }

            return NotFound();
        }

        [NonAction]
        public Task<IActionResult> ApproveWorkflow(Guid id, [FromBody] ApprovalRequestDto request) => Approve(id, request);

        /// <summary>
        /// Extract doors and windows from the LayoutJson for each room (keyed by room_type).
        /// </summary>
        private static Dictionary<string, RoomOpenings> ExtractDoorsWindows(string? layoutJson)
        {
            var result = new Dictionary<string, RoomOpenings>();
            if (string.IsNullOrWhiteSpace(layoutJson)) return result;

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

                        result[roomType] = new RoomOpenings { Doors = doors, Windows = windows };
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Warning: Could not parse LayoutJson for doors/windows: {ex.Message}");
            }

            return result;
        }

        private class RoomOpenings
        {
            public List<OpeningDto> Doors { get; set; } = new();
            public List<OpeningDto> Windows { get; set; } = new();
        }
    }
}
