using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HousePlanner.API.Data;
using HousePlanner.API.Entities;
using HousePlanner.API.DTOs;
using System.Security.Claims;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Authorization;

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

            var workflow = await _context.WorkflowStates.Include(w=>w.HouseDesigns).FirstOrDefaultAsync(w =>
                w.Id == dto.WorkflowStateId && w.LandSubmission.ClientId == userId.Value);
            if (workflow == null) return NotFound(new { message = "Workflow/Design not found." });
            var selected = workflow.PreferredHouseDesignId is Guid selectedId
                ? workflow.HouseDesigns.FirstOrDefault(d=>d.Id==selectedId && !d.IsArchived) : null;
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

        [HttpGet]
        public async Task<IActionResult> GetRequests([FromQuery] string? status)
        {
            var userCtx = await _currentUser.GetAsync(HttpContext);
            if (userCtx == null || userCtx.Id == null) return Unauthorized();
            var role = userCtx.Role;
            var userId = userCtx.Id;

            IQueryable<ValidationRequest> query = _context.ValidationRequests
                .Include(v => v.Client)
                .Include(v => v.WorkflowState)
                    .ThenInclude(w => w.LandSubmission)
                .Include(v=>v.HouseDesign).ThenInclude(d=>d!.Rooms)
                .OrderByDescending(v => v.CreatedAt);

            if (role == "Architect")
            {
                if (!string.IsNullOrEmpty(status))
                    query = query.Where(v => v.Status == status);
            }
            else if (role == "Customer")
            {
                query = query.Where(v => v.ClientId == userId.Value);
            }
            else
            {
                return StatusCode(403, new { message = "Unauthorized access." });
            }

            var requests = await query.ToListAsync();
            return Ok(requests.Select(MapToDto));
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
                .Include(v=>v.HouseDesign).ThenInclude(d=>d!.Rooms)
                .Include(v=>v.HouseDesign).ThenInclude(d=>d!.CostEstimates)
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
                    Id = Guid.NewGuid(), WorkflowStateId = request.WorkflowStateId,
                    HouseDesignId = request.HouseDesignId, Status = "awaiting_constructor",
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
                ,designVersion = req.HouseDesign?.Version
                ,bathrooms = req.HouseDesign?.Rooms.Count(r=>r.RoomType.Contains("bathroom"))
                ,area = req.HouseDesign?.TotalBuiltUpAreaSqft
            };
        }

        private object MapToDetailedDto(ValidationRequest req)
        {
            var design = req.HouseDesign ?? req.WorkflowState?.HouseDesigns?.OrderByDescending(d=>d.Version).FirstOrDefault();
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
                    budgetStatus = cost is null ? "unavailable" : cost.BudgetDeltaPercent < 100m
                        ? "within_budget" : cost.BudgetDeltaPercent == 100m ? "at_budget" : "over_budget"
                },
                cost = cost == null ? null : new
                {
                    materialCostLkr = cost.MaterialCostLkr,
                    labourCostLkr = cost.LabourCostLkr,
                    totalCostLkr = cost.TotalCostLkr,
                    budgetDeltaPercent = cost.BudgetDeltaPercent
                },
                design = design != null ? new
                {
                    designId = design.Id,
                    version = design.Version,
                    floorCount = design.FloorCount,
                    totalBuiltUpAreaSqft = design.TotalBuiltUpAreaSqft,
                    layoutJson = design.LayoutJson,
                    
                    
                } : null
            };
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
