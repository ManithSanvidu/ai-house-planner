using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HousePlanner.API.Data;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Controllers
{
    [ApiController]
    [Route("api/constructor/workflow")]
    [Authorize] // Require auth, specify roles on actions
    public class ConstructorWorkflowController : ControllerBase
    {
        private readonly IConstructorWorkflowService _workflowService;
        private readonly ICurrentUserContextService _currentUserContext;
        private readonly ApplicationDbContext _db;

        public ConstructorWorkflowController(
            IConstructorWorkflowService workflowService,
            ICurrentUserContextService currentUserContext, ApplicationDbContext db)
        {
            _workflowService = workflowService;
            _currentUserContext = currentUserContext;
            _db = db;
        }

        [HttpGet("projects")]
        [Authorize(Roles = "Constructor,Admin")]
        public async Task<IActionResult> GetProjects()
        {
            var user = await _currentUserContext.GetAsync(HttpContext);
            if (user?.Id == null) return Unauthorized();

            var projects = await _workflowService.GetConstructorProjectsAsync(user.Id.Value, user.Role);
            return Ok(projects);
        }

        [HttpGet("projects/{projectId}")]
        [Authorize(Roles = "Constructor,Admin")]
        public async Task<IActionResult> GetProjectDetails(Guid projectId)
        {
            var user = await _currentUserContext.GetAsync(HttpContext);
            if (user?.Id == null) return Unauthorized();

            var p = await _workflowService.GetProjectDetailsAsync(projectId, user.Id.Value, user.Role);
            if (p == null) return NotFound("Project not found or unauthorized.");

            var dto = new HousePlanner.API.DTOs.ConstructorProjectDto(
                p.Id,
                p.WorkflowStateId,
                p.HouseDesignId,
                p.ContractorId,
                p.Status,
                p.CreatedAt,
                p.UpdatedAt,
                p.ConstructionPhases.Select(cp => new HousePlanner.API.DTOs.ConstructionPhaseDto(
                    cp.Id,
                    cp.PhaseName,
                    cp.SequenceOrder,
                    cp.Status,
                    cp.StartedAt,
                    cp.CompletedAt,
                    cp.EstimatedDurationDays
                )).ToList()
            );

            return Ok(dto);
        }

        [HttpGet("projects/{projectId}/logs")]
        [Authorize(Roles = "Constructor,Admin")]
        public async Task<IActionResult> GetWorkflowLogs(Guid projectId)
        {
            var user = await _currentUserContext.GetAsync(HttpContext);
            if (user?.Id == null) return Unauthorized();

            var logs = await _workflowService.GetWorkflowLogsAsync(projectId, user.Id.Value, user.Role);
            return Ok(logs);
        }

        [HttpGet("projects/{projectId}/progress")]
        [Authorize(Roles = "Constructor,Admin")]
        public async Task<IActionResult> GetProjectProgress(Guid projectId)
        {
            var user = await _currentUserContext.GetAsync(HttpContext);
            if (user?.Id == null) return Unauthorized();

            try
            {
                var progress = await _workflowService.GetProjectProgressAsync(projectId, user.Id.Value, user.Role);
                return Ok(progress);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost("logs")]
        [Authorize(Roles = "Constructor")]
        public async Task<IActionResult> CreateLog([FromBody] ConstructorWorkflowLog log)
        {
            var user = await _currentUserContext.GetAsync(HttpContext);
            if (user?.Id == null) return Unauthorized();
            
            try
            {
                var createdLog = await _workflowService.CreateWorkflowLogAsync(user.Id.Value, log);
                return Ok(createdLog);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ex.Message);
            }
        }

        [HttpPut("logs/{id}")]
        [Authorize(Roles = "Constructor")]
        public async Task<IActionResult> UpdateLog(Guid id, [FromBody] ConstructorWorkflowLog updatedLog)
        {
            var user = await _currentUserContext.GetAsync(HttpContext);
            if (user?.Id == null) return Unauthorized();
            
            try
            {
                var log = await _workflowService.UpdateWorkflowLogAsync(user.Id.Value, id, updatedLog);
                return Ok(log);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Log not found.");
            }
        }

        // --- NEW ASSIGNMENT ENDPOINTS ---

        [HttpGet("search/{projectId}")]
        [Authorize(Roles = "Constructor,Admin")]
        public async Task<IActionResult> SearchProject(Guid projectId)
        {
            var user = await _currentUserContext.GetAsync(HttpContext);
            if (user?.Id == null) return Unauthorized();
            var allowed = user.Role == "Admin" || await _db.Projects.AnyAsync(p => p.Id == projectId &&
                (p.ContractorId == user.Id.Value || _db.ConstructorProjectRequests.Any(r => r.ProjectId == p.Id && r.ConstructorId == user.Id.Value)));
            if (!allowed) return NotFound("Project not found.");
            var project = await _workflowService.SearchProjectByIdAsync(projectId);
            if (project == null) return NotFound("Project not found.");
            
            return Ok(new {
                project.Id,
                project.Status,
                project.CreatedAt,
                HouseDesignId = project.WorkflowState?.PreferredHouseDesignId,
                DesignName = "Custom Design"
            });
        }

        [HttpPost("request/{projectId}")]
        [Authorize(Roles = "Constructor")]
        public async Task<IActionResult> RequestProject(Guid projectId)
        {
            await Task.CompletedTask;
            return StatusCode(StatusCodes.Status410Gone, new { message = "Customers now initiate construction requests from an architect-approved design." });
        }

        [HttpPost("approve/{requestId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveRequest(Guid requestId)
        {
            var user = await _currentUserContext.GetAsync(HttpContext);
            if (user?.Id == null) return Unauthorized();

            try
            {
                var request = await _workflowService.ApproveConstructorRequestAsync(requestId, user.Id.Value);
                return Ok(request);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Request not found.");
            }
        }

        [HttpGet("requests/project/{projectId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetProjectRequests(Guid projectId)
        {
            var user = await _currentUserContext.GetAsync(HttpContext);
            if (user?.Id == null) return Unauthorized();

            var requests = await _workflowService.GetPendingRequestsForProjectAsync(projectId, user.Id.Value);
            return Ok(requests);
        }

        [HttpGet("requests/constructor")]
        [Authorize(Roles = "Constructor")]
        public async Task<IActionResult> GetConstructorRequests()
        {
            var user = await _currentUserContext.GetAsync(HttpContext);
            if (user?.Id == null) return Unauthorized();

            var raw = await _db.ConstructorProjectRequests.AsNoTracking()
                .Where(r => r.ConstructorId == user.Id.Value && r.Status == "Pending")
                .Include(r => r.Customer).Include(r => r.HouseDesign)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new {
                    r.Id, r.ProjectId, r.HouseDesignId, r.Status, requestedAt = r.CreatedAt,
                    customerName = r.Customer != null ? r.Customer.FullName : "Unknown",
                    designVersion = r.HouseDesign != null ? (int?)r.HouseDesign.Version : null,
                    area = r.HouseDesign != null ? r.HouseDesign.TotalBuiltUpAreaSqft : 0m,
                    floorCount = r.HouseDesign != null ? r.HouseDesign.FloorCount : 0,
                    layoutJson = r.HouseDesign != null ? r.HouseDesign.LayoutJson : null,
                    r.DeclineReason
                })
                .ToListAsync();

            return Ok(raw.Select(r => new {
                r.Id, r.ProjectId, r.HouseDesignId, r.Status, r.requestedAt,
                r.customerName, r.designVersion, r.area, r.floorCount, r.DeclineReason,
                title = DesignTitle(r.layoutJson, r.designVersion ?? 0),
                bedrooms = CountRooms(r.layoutJson, "bedroom"),
                bathrooms = CountRooms(r.layoutJson, "bathroom")
            }));
        }

        private static int CountRooms(string? json, string type)
        {
            if (string.IsNullOrEmpty(json)) return 0;
            try { using var doc = System.Text.Json.JsonDocument.Parse(json); return doc.RootElement.GetProperty("rooms").EnumerateArray().Count(r => r.TryGetProperty("room_type", out var t) && t.GetString()?.Contains(type, StringComparison.OrdinalIgnoreCase) == true); }
            catch { return 0; }
        }
        private static string DesignTitle(string? json, int version)
        {
            if (!string.IsNullOrEmpty(json))
            {
                try { using var doc = System.Text.Json.JsonDocument.Parse(json); if (doc.RootElement.TryGetProperty("topology", out var t)) return $"{t.GetString()?.Replace('_', ' ')} Home"; }
                catch { }
            }
            return $"Approved Design v{version}";
        }

        [HttpPost("requests/{requestId:guid}/accept")]
        [Authorize(Roles = "Constructor")]
        public async Task<IActionResult> AcceptRequest(Guid requestId)
        {
            var user = await _currentUserContext.GetAsync(HttpContext);
            if (user?.Id == null) return Unauthorized();
            var request = await _db.ConstructorProjectRequests.Include(r => r.Project)
                .FirstOrDefaultAsync(r => r.Id == requestId && r.ConstructorId == user.Id.Value);
            if (request == null) return NotFound();
            if (request.Status != "Pending") return Conflict(new { message = "Only pending requests can be accepted." });
            var approved = await _db.ValidationRequests.Include(v => v.WorkflowState).AnyAsync(v => (v.HouseDesignId == request.HouseDesignId || v.WorkflowState.PreferredHouseDesignId == request.HouseDesignId) && v.ClientId == request.CustomerId && v.Status == "Approved");
            if (!approved) return Conflict(new { message = "The design is no longer approved for construction." });
            if (request.Project == null) return Conflict(new { message = "Construction project is unavailable." });
            if (request.Project.ContractorId != null) return Conflict(new { message = "This project already has an assigned constructor." });
            request.Status = "Accepted"; request.RespondedAt = DateTimeOffset.UtcNow; request.UpdatedAt = request.RespondedAt.Value;
            request.Project.ContractorId = user.Id.Value; request.Project.HouseDesignId = request.HouseDesignId;
            request.Project.Status = "active"; request.Project.UpdatedAt = DateTimeOffset.UtcNow;
            var competing = await _db.ConstructorProjectRequests.Where(r => r.ProjectId == request.ProjectId && r.Id != request.Id && r.Status == "Pending").ToListAsync();
            foreach (var other in competing) { other.Status = "Cancelled"; other.UpdatedAt = DateTimeOffset.UtcNow; }
            await _db.SaveChangesAsync();
            return Ok(new { request.Id, request.Status, request.ProjectId });
        }

        [HttpPost("requests/{requestId:guid}/decline")]
        [Authorize(Roles = "Constructor")]
        public async Task<IActionResult> DeclineRequest(Guid requestId, [FromBody] DeclineConstructionRequest? dto)
        {
            var user = await _currentUserContext.GetAsync(HttpContext);
            if (user?.Id == null) return Unauthorized();
            var request = await _db.ConstructorProjectRequests.FirstOrDefaultAsync(r => r.Id == requestId && r.ConstructorId == user.Id.Value);
            if (request == null) return NotFound();
            if (request.Status != "Pending") return Conflict(new { message = "Only pending requests can be declined." });
            request.Status = "Declined"; request.DeclineReason = string.IsNullOrWhiteSpace(dto?.Reason) ? null : dto.Reason.Trim();
            request.RespondedAt = DateTimeOffset.UtcNow; request.UpdatedAt = request.RespondedAt.Value;
            await _db.SaveChangesAsync();
            return Ok(new { request.Id, request.Status, request.DeclineReason });
        }

        [HttpPost("projects/{projectId}/duration")]
        [Authorize(Roles = "Constructor")]
        public async Task<IActionResult> SetEstimatedDuration(Guid projectId, [FromBody] int estimatedDays)
        {
            var user = await _currentUserContext.GetAsync(HttpContext);
            if (user?.Id == null) return Unauthorized();

            var success = await _workflowService.SetProjectEstimatedDurationAsync(projectId, user.Id.Value, estimatedDays);
            if (!success) return BadRequest("Failed to set estimated duration.");

            return Ok(new { success = true });
        }
    }

    public record DeclineConstructionRequest(string? Reason);
}
