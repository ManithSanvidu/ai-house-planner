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

            var workflow = await _context.WorkflowStates.FirstOrDefaultAsync(w =>
                w.Id == dto.WorkflowStateId && w.LandSubmission.ClientId == userId.Value);
            if (workflow == null) return NotFound(new { message = "Workflow/Design not found." });

            var existingRequest = await _context.ValidationRequests
                .FirstOrDefaultAsync(v => v.WorkflowStateId == dto.WorkflowStateId && (v.Status == "Pending" || v.Status == "Under Review"));
            if (existingRequest != null) return Conflict(new { message = "A validation request is already active for this design." });

            var request = new ValidationRequest
            {
                Id = Guid.NewGuid(),
                WorkflowStateId = dto.WorkflowStateId,
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
                    .ThenInclude(w => w.HouseDesigns)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (request == null) return NotFound();

            if (role != "Architect" && request.ClientId != userId.Value)
            {
                return StatusCode(403, new { message = "Unauthorized access." });
            }

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
            var request = await _context.ValidationRequests.Include(v => v.WorkflowState).FirstOrDefaultAsync(v => v.Id == id);
            if (request == null) return NotFound();

            if (request.Status == "Approved") return Conflict(new { message = "Already approved." });

            request.Status = "Approved";
            request.ArchitectReview = dto.Review;
            request.ArchitectId = userId;
            request.DecisionAt = DateTimeOffset.UtcNow;
            request.WorkflowState.Status = "approved";
            request.WorkflowState.ApprovalStatus = "approved";
            request.WorkflowState.ApprovedByUserId = userId;
            request.WorkflowState.ApprovedAt = request.DecisionAt;
            request.WorkflowState.UpdatedAt = DateTimeOffset.UtcNow;
            
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

            if (request.Status == "Rejected") return Conflict(new { message = "Already rejected." });

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
            };
        }

        private object MapToDetailedDto(ValidationRequest req)
        {
            var design = req.WorkflowState?.PreferredHouseDesignId is Guid preferredId
                ? req.WorkflowState.HouseDesigns.FirstOrDefault(d => d.Id == preferredId)
                : req.WorkflowState?.HouseDesigns?.OrderByDescending(d => d.Version).FirstOrDefault();
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
                design = design != null ? new
                {
                    designId = design.Id,
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
