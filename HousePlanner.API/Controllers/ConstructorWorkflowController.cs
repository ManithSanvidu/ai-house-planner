using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HousePlanner.API.Controllers
{
    [ApiController]
    [Route("api/constructor/workflow")]
    [Authorize] // Require auth, specify roles on actions
    public class ConstructorWorkflowController : ControllerBase
    {
        private readonly IConstructorWorkflowService _workflowService;
        private readonly ICurrentUserContextService _currentUserContext;

        public ConstructorWorkflowController(
            IConstructorWorkflowService workflowService,
            ICurrentUserContextService currentUserContext)
        {
            _workflowService = workflowService;
            _currentUserContext = currentUserContext;
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
        [Authorize(Roles = "Constructor,Admin,User")]
        public async Task<IActionResult> GetProjectDetails(Guid projectId)
        {
            var user = await _currentUserContext.GetAsync(HttpContext);
            if (user?.Id == null) return Unauthorized();

            var project = await _workflowService.GetProjectDetailsAsync(projectId, user.Id.Value, user.Role);
            if (project == null) return NotFound("Project not found or unauthorized.");

            return Ok(project);
        }

        [HttpGet("projects/{projectId}/logs")]
        [Authorize(Roles = "Constructor,Admin,User")]
        public async Task<IActionResult> GetWorkflowLogs(Guid projectId)
        {
            var user = await _currentUserContext.GetAsync(HttpContext);
            if (user?.Id == null) return Unauthorized();

            var logs = await _workflowService.GetWorkflowLogsAsync(projectId, user.Id.Value, user.Role);
            return Ok(logs);
        }

        [HttpGet("projects/{projectId}/progress")]
        [Authorize(Roles = "Constructor,Admin,User")]
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
            var user = await _currentUserContext.GetAsync(HttpContext);
            if (user?.Id == null) return Unauthorized();

            try
            {
                var request = await _workflowService.RequestProjectAssignmentAsync(projectId, user.Id.Value);
                return Ok(request);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Project not found.");
            }
        }

        [HttpPost("approve/{requestId}")]
        [Authorize(Roles = "User,Admin")]
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
        [Authorize(Roles = "User,Admin")]
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

            var requests = await _workflowService.GetConstructorRequestsAsync(user.Id.Value);
            return Ok(requests);
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
}
