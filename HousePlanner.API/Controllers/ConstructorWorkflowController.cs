using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HousePlanner.API.Controllers
{
    [ApiController]
    [Route("api/constructor/workflow")]
    [Authorize(Roles = "Constructor,Admin")]
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
        public async Task<IActionResult> GetProjects()
        {
            var user = await _currentUserContext.GetAsync(HttpContext);
            if (user?.Id == null) return Unauthorized();

            var projects = await _workflowService.GetConstructorProjectsAsync(user.Id.Value, user.Role);
            return Ok(projects);
        }

        [HttpGet("projects/{projectId}")]
        public async Task<IActionResult> GetProjectDetails(Guid projectId)
        {
            var user = await _currentUserContext.GetAsync(HttpContext);
            if (user?.Id == null) return Unauthorized();

            var project = await _workflowService.GetProjectDetailsAsync(projectId, user.Id.Value, user.Role);
            if (project == null) return NotFound("Project not found or unauthorized.");

            return Ok(project);
        }

        [HttpGet("projects/{projectId}/logs")]
        public async Task<IActionResult> GetWorkflowLogs(Guid projectId)
        {
            var user = await _currentUserContext.GetAsync(HttpContext);
            if (user?.Id == null) return Unauthorized();

            var logs = await _workflowService.GetWorkflowLogsAsync(projectId, user.Id.Value, user.Role);
            return Ok(logs);
        }

        [HttpGet("projects/{projectId}/progress")]
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
        public async Task<IActionResult> CreateLog([FromBody] ConstructorWorkflowLog log)
        {
            var user = await _currentUserContext.GetAsync(HttpContext);
            if (user?.Id == null) return Unauthorized();
            
            if (user.Role == "Admin") return StatusCode(403, "Admins cannot submit logs.");

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
        public async Task<IActionResult> UpdateLog(Guid id, [FromBody] ConstructorWorkflowLog updatedLog)
        {
            var user = await _currentUserContext.GetAsync(HttpContext);
            if (user?.Id == null) return Unauthorized();
            
            if (user.Role == "Admin") return StatusCode(403, "Admins cannot update logs.");

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
    }
}
