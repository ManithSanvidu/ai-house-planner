using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HousePlanner.API.Data;
using HousePlanner.API.Entities;
using HousePlanner.API.DTOs;
using System.Security.Claims;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace HousePlanner.API.Controllers
{
    [ApiController]
    [Route("api/validation-requests")]
    [Authorize(Roles = "Customer,Architect")]
    public class ValidationRequestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrentUserContextService _currentUser;

        public ValidationRequestController(ApplicationDbContext context, ICurrentUserContextService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }



        [HttpPost]
        public async Task<IActionResult> CreateValidationRequest([FromBody] CreateValidationRequestDto dto)
        {
            var userCtx = await _currentUser.GetAsync(HttpContext);
            if (userCtx == null || userCtx.Id == null) return Unauthorized(new { message = "User not identified." });
            var userId = userCtx.Id;
            if (userCtx.Role != "Customer") return StatusCode(403);

            var workflow = await _context.WorkflowStates.Include(w => w.HouseDesigns).FirstOrDefaultAsync(w =>
                w.Id == dto.WorkflowStateId && w.LandSubmission.ClientId == userId.Value);
            if (workflow == null) return NotFound(new { message = "Workflow/Design not found." });
            var selected = workflow.PreferredHouseDesignId is Guid selectedId
                ? workflow.HouseDesigns.FirstOrDefault(d => d.Id == selectedId && !d.IsArchived) : null;
            if (selected is null) return BadRequest(new { message = "Select a design before submitting it for architect review." });

            var existingRequest = await _context.ValidationRequests
                .FirstOrDefaultAsync(v => v.WorkflowStateId == dto.WorkflowStateId && (v.Status == "Pending" || v.Status == "Under Review"));
            if (existingRequest != null) return Conflict(new { message = "A validation request is already active for this design." });

            var request = new ValidationRequest
            {
                Id = Guid.NewGuid(),
                WorkflowStateId = dto.WorkflowStateId,
                HouseDesignId = selected.Id,
                ClientId = userId.Value,
                Status = "Pending"
            };

            _context.ValidationRequests.Add(request);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Validation request submitted.", id = request.Id });
        }

        [HttpGet("summary")]
        [Authorize(Roles = "Architect")]
        public async Task<IActionResult> GetSummary()
        {
            var userCtx = await _currentUser.GetAsync(HttpContext);
            if (userCtx == null || userCtx.Id == null) return Unauthorized();
            if (userCtx.Role != "Architect")
                return StatusCode(403, new { message = "Unauthorized access." });

            var counts = await _context.ValidationRequests
                .AsNoTracking()
                .GroupBy(v => v.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            int Get(string s) => counts.FirstOrDefault(c => c.Status == s)?.Count ?? 0;

            return Ok(new
            {
                pending    = Get("Pending"),
                underReview = Get("Under Review"),
                approved   = Get("Approved"),
                rejected   = Get("Rejected")
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetRequests(
            [FromQuery] string[]? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var userCtx = await _currentUser.GetAsync(HttpContext);
            if (userCtx == null || userCtx.Id == null) return Unauthorized();
            var role = userCtx.Role;
            var userId = userCtx.Id;

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 100;

            IQueryable<ValidationRequest> query = _context.ValidationRequests.AsNoTracking();

            if (role == "Architect")
            {
                if (status != null && status.Length > 0)
                    query = query.Where(v => status.Contains(v.Status));
            }
            else if (role == "Customer")
            {
                query = query.Where(v => v.ClientId == userId.Value);
            }
            else
            {
                return StatusCode(403, new { message = "Unauthorized access." });
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(v => v.CreatedAt)
                .ThenByDescending(v => v.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(req => new
                {
                    id = req.Id,
                    clientName = req.Client.FullName,
                    submissionDate = req.CreatedAt,
                    status = req.Status,
                    budget = req.WorkflowState.LandSubmission.BudgetLkr,
                    landSize = req.WorkflowState.LandSubmission.LandSizePerches,
                    bedrooms = req.WorkflowState.LandSubmission.PreferredBedrooms,
                    floors = req.WorkflowState.LandSubmission.PreferredFloors,
                    style = req.WorkflowState.LandSubmission.StylePreference,
                    designVersion = req.HouseDesign != null ? req.HouseDesign.Version : (int?)null,
                    bathrooms = req.HouseDesign != null ? req.HouseDesign.Rooms.Count(r => r.RoomType.Contains("bathroom")) : 0,
                    area = req.HouseDesign != null ? req.HouseDesign.TotalBuiltUpAreaSqft : (decimal?)null
                })
                .ToListAsync();

            return Ok(new
            {
                items,
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRequestDetails(Guid id)
        {
            var userCtx = await _currentUser.GetAsync(HttpContext);
            if (userCtx == null || userCtx.Id == null) return Unauthorized();
            var role = userCtx.Role;
            var userId = userCtx.Id;

            var request = await _context.ValidationRequests
                .Include(v => v.Client)
                .Include(v => v.WorkflowState)
                    .ThenInclude(w => w.LandSubmission)
                .Include(v => v.WorkflowState)
                    .ThenInclude(w => w.HouseDesigns).ThenInclude(d => d.CostEstimates)
                .Include(v => v.HouseDesign).ThenInclude(d => d!.Rooms)
                .Include(v => v.HouseDesign).ThenInclude(d => d!.CostEstimates)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (request == null) return NotFound();

            if (role != "Architect" && request.ClientId != userId.Value)
            {
                return StatusCode(403, new { message = "Unauthorized access." });
            }

            if (role == "Architect" && request.ArchitectId.HasValue && request.ArchitectId != userId.Value)
                return StatusCode(403, new { message = "This request is assigned to another architect." });
            if (role == "Architect" && request.Status == "Pending")
            {
                request.Status = "Under Review";
                request.ArchitectId = userId.Value;
                await _context.SaveChangesAsync();
            }

            return Ok(MapToDetailedDto(request));
        }

        [HttpPatch("{id}/approve")]
        public async Task<IActionResult> ApproveRequest(Guid id, [FromBody] ArchitectReviewDto dto)
        {
            var userCtx = await _currentUser.GetAsync(HttpContext);
            if (userCtx == null || userCtx.Id == null) return Unauthorized();
            var role = userCtx.Role;
            if (role != "Architect") return StatusCode(403);

            var userId = userCtx.Id;
            var request = await _context.ValidationRequests
                .Include(v => v.WorkflowState)
                .Include(v => v.HouseDesign).ThenInclude(d => d!.CostEstimates)
                .FirstOrDefaultAsync(v => v.Id == id);
            if (request == null) return NotFound();

            if (request.Status is not ("Pending" or "Under Review")) return Conflict(new { message = "This request has already been finalized." });
            if (request.ArchitectId.HasValue && request.ArchitectId != userId) return StatusCode(403);
            if (request.HouseDesign is null) return BadRequest(new { message = "A selected design is required before approval." });
            if (!request.HouseDesign.CostEstimates.Any()) return BadRequest(new { message = "A cost estimate is required before approval." });

            request.Status = "Approved";
            request.ArchitectReview = dto.Review;
            request.ArchitectId = userId;
            request.DecisionAt = DateTimeOffset.UtcNow;
            request.WorkflowState.Status = "approved";
            request.WorkflowState.ApprovalStatus = "approved";
            request.WorkflowState.ApprovedByUserId = userId;
            request.WorkflowState.ApprovedAt = request.DecisionAt;
            request.WorkflowState.UpdatedAt = DateTimeOffset.UtcNow;

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.WorkflowStateId == request.WorkflowStateId);
            if (project == null)
            {
                project = new Project
                {
                    Id = Guid.NewGuid(),
                    WorkflowStateId = request.WorkflowStateId,
                    HouseDesignId = request.HouseDesignId,
                    Status = "awaiting_constructor",
                    ConstructionPhases = new List<ConstructionPhase>
                    {
                        new() { Id = Guid.NewGuid(), PhaseName = "Site Preparation", Status = "pending", SequenceOrder = 1 },
                        new() { Id = Guid.NewGuid(), PhaseName = "Foundation", Status = "pending", SequenceOrder = 2 },
                        new() { Id = Guid.NewGuid(), PhaseName = "Framing", Status = "pending", SequenceOrder = 3 },
                        new() { Id = Guid.NewGuid(), PhaseName = "Roofing", Status = "pending", SequenceOrder = 4 },
                        new() { Id = Guid.NewGuid(), PhaseName = "Interior & Finish", Status = "pending", SequenceOrder = 5 }
                    }
                };
                _context.Projects.Add(project);
            }
            else
            {
                project.HouseDesignId = request.HouseDesignId;
                project.Status = project.ContractorId.HasValue ? project.Status : "awaiting_constructor";
                project.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Request approved." });
        }

        [HttpPatch("{id}/reject")]
        public async Task<IActionResult> RejectRequest(Guid id, [FromBody] ArchitectReviewDto dto)
        {
            var userCtx = await _currentUser.GetAsync(HttpContext);
            if (userCtx == null || userCtx.Id == null) return Unauthorized();
            var role = userCtx.Role;
            if (role != "Architect") return StatusCode(403);

            var userId = userCtx.Id;
            var request = await _context.ValidationRequests.Include(v => v.WorkflowState).FirstOrDefaultAsync(v => v.Id == id);
            if (request == null) return NotFound();

            if (request.Status is not ("Pending" or "Under Review")) return Conflict(new { message = "This request has already been finalized." });
            if (request.ArchitectId.HasValue && request.ArchitectId != userId) return StatusCode(403);
            if (string.IsNullOrWhiteSpace(dto.Review)) return BadRequest(new { message = "A rejection reason is required." });

            request.Status = "Rejected";
            request.ArchitectReview = dto.Review;
            request.ArchitectId = userId;
            request.DecisionAt = DateTimeOffset.UtcNow;
            request.WorkflowState.Status = "revision_requested";
            request.WorkflowState.ApprovalStatus = "revision_requested";
            request.WorkflowState.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Request rejected." });
        }

        private object MapToDto(ValidationRequest req)
        {
            return new
            {
                id = req.Id,
                clientName = req.Client?.FullName,
                submissionDate = req.CreatedAt,
                status = req.Status,
                budget = req.WorkflowState?.LandSubmission?.BudgetLkr,
                landSize = req.WorkflowState?.LandSubmission?.LandSizePerches,
                bedrooms = req.WorkflowState?.LandSubmission?.PreferredBedrooms,
                floors = req.WorkflowState?.LandSubmission?.PreferredFloors,
                style = req.WorkflowState?.LandSubmission?.StylePreference
                ,
                designVersion = req.HouseDesign?.Version
                ,
                bathrooms = req.HouseDesign?.Rooms.Count(r => r.RoomType.Contains("bathroom"))
                ,
                area = req.HouseDesign?.TotalBuiltUpAreaSqft
            };
        }

        private object MapToDetailedDto(ValidationRequest req)
        {
            var design = req.HouseDesign ?? req.WorkflowState?.HouseDesigns?.OrderByDescending(d => d.Version).FirstOrDefault();
            var cost = design?.CostEstimates.OrderByDescending(c => c.CreatedAt).FirstOrDefault();
            var canApprove = design is not null && cost is not null && req.Status is "Pending" or "Under Review";
            return new
            {
                id = req.Id,
                clientName = req.Client?.FullName,
                clientEmail = req.Client?.Email,
                submissionDate = req.CreatedAt,
                status = req.Status,
                budget = req.WorkflowState?.LandSubmission?.BudgetLkr,
                landSize = req.WorkflowState?.LandSubmission?.LandSizePerches,
                bedrooms = req.WorkflowState?.LandSubmission?.PreferredBedrooms,
                floors = req.WorkflowState?.LandSubmission?.PreferredFloors,
                style = req.WorkflowState?.LandSubmission?.StylePreference,
                terrainType = req.WorkflowState?.TerrainType,
                architectReview = req.ArchitectReview,
                decisionAt = req.DecisionAt,
                approvalEligibility = new
                {
                    canApprove,
                    reason = design is null
                        ? "A selected design is required before approval."
                        : cost is null
                            ? "A cost estimate is required before approval."
                            : req.Status is not ("Pending" or "Under Review")
                                ? "This request has already been finalized."
                                : null,
                    budgetStatus = cost?.BudgetDeltaPercent is decimal budgetPercent
                        ? budgetPercent < 100m ? "within_budget" : budgetPercent == 100m ? "at_budget" : "over_budget"
                        : "unavailable"
                },
                cost = cost == null ? null : new
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
                design = design != null ? new
                {
                    designId = design.Id,
                    version = design.Version,
                    floorCount = design.FloorCount,
                    totalBuiltUpAreaSqft = design.TotalBuiltUpAreaSqft,
                    layoutJson = design.LayoutJson,
                    rooms = ExtractRoomsWithOpenings(design)
                } : null
            };
        }

        private static List<object> ExtractRoomsWithOpenings(HouseDesign design)
        {
            // Parse doors/windows from LayoutJson, keyed by (roomType, floor, x, y)
            var openingsMap = new Dictionary<(string, int, decimal, decimal), (Guid? sourceId, List<object> doors, List<object> windows)>();
            try
            {
                using var doc = JsonDocument.Parse(design.LayoutJson ?? "{}");
                if (doc.RootElement.TryGetProperty("rooms", out var roomsEl) && roomsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var roomEl in roomsEl.EnumerateArray())
                    {
                        var roomType = roomEl.TryGetProperty("room_type", out var rt) ? rt.GetString() ?? "" : "";
                        var floor    = roomEl.TryGetProperty("floor",     out var fl) ? fl.GetInt32()    : 1;
                        var x        = roomEl.TryGetProperty("x",         out var xp) ? xp.GetDecimal()  : 0m;
                        var y        = roomEl.TryGetProperty("y",         out var yp) ? yp.GetDecimal()  : 0m;
                        Guid? sourceId = roomEl.TryGetProperty("room_id", out var rid) && rid.TryGetGuid(out var g) ? g : null;

                        var doors = new List<object>();
                        if (roomEl.TryGetProperty("doors", out var doorsEl) && doorsEl.ValueKind == JsonValueKind.Array)
                            foreach (var d in doorsEl.EnumerateArray())
                                doors.Add(new
                                {
                                    wall   = d.TryGetProperty("wall",   out var dw) ? dw.GetString() ?? "" : "",
                                    offset = d.TryGetProperty("offset", out var do_) ? do_.GetDecimal()    : 0m,
                                    width  = d.TryGetProperty("width",  out var dwd) ? dwd.GetDecimal()    : 0m
                                });

                        var windows = new List<object>();
                        if (roomEl.TryGetProperty("windows", out var winsEl) && winsEl.ValueKind == JsonValueKind.Array)
                            foreach (var w in winsEl.EnumerateArray())
                                windows.Add(new
                                {
                                    wall   = w.TryGetProperty("wall",   out var ww)  ? ww.GetString()  ?? "" : "",
                                    offset = w.TryGetProperty("offset", out var wo)  ? wo.GetDecimal()       : 0m,
                                    width  = w.TryGetProperty("width",  out var wwd) ? wwd.GetDecimal()      : 0m
                                });

                        openingsMap[(roomType, floor, x, y)] = (sourceId, doors, windows);
                    }
                }
            }
            catch (JsonException) { /* best-effort: proceed without openings */ }

            return design.Rooms
                .OrderBy(r => r.FloorNumber)
                .ThenBy(r => r.RoomType)
                .Select(r =>
                {
                    openingsMap.TryGetValue((r.RoomType, r.FloorNumber, r.X, r.Y), out var openings);
                    return (object)new
                    {
                        roomId      = openings.sourceId ?? r.Id,
                        roomType    = r.RoomType,
                        name        = r.Name,
                        floorNumber = r.FloorNumber,
                        x           = r.X,
                        y           = r.Y,
                        width       = r.Width,
                        length      = r.Length,
                        wallHeight  = r.WallHeight,
                        doors       = openings.doors   ?? new List<object>(),
                        windows     = openings.windows ?? new List<object>()
                    };
                })
                .ToList();
        }
    }

    public class CreateValidationRequestDto
    {
        public Guid WorkflowStateId { get; set; }
    }

    public class ArchitectReviewDto
    {
        public string? Review { get; set; }
    }
}
