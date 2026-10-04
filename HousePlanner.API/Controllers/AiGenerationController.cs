using System.Text;
using System.Text.Json;
using HousePlanner.API.Data;
using HousePlanner.API.Entities;
using HousePlanner.API.Models;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Controllers;

[ApiController]
[Route("api/v1/ai-generation")]
public class AiGenerationController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly HttpClient _agenticServiceClient;
    private readonly IDesignOptionsService _designOptionsService;
    private readonly ICurrentUserContextService _currentUser;
    private readonly ILogger<AiGenerationController> _logger;

    public AiGenerationController(ApplicationDbContext context, IHttpClientFactory httpClientFactory,
        IDesignOptionsService designOptionsService, ICurrentUserContextService currentUser,
        ILogger<AiGenerationController> logger)
    {
        _context = context;
        _agenticServiceClient = httpClientFactory.CreateClient("AgenticService");
        _designOptionsService = designOptionsService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] StartDesignRequest request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[TRACE] received bathroom count: {request.Bathrooms}");
        var requirement = request.ToRequirement();
        var validation = await _designOptionsService.ValidateFinalSelectionAsync(requirement, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(new { code = validation.ErrorCode, message = validation.Message,
                conflicts = validation.Conflicts, suggestions = validation.Suggestions });

        WorkflowState workflow;
        try
        {
            var identity = await _currentUser.GetAsync(HttpContext);
            if (identity is null) return Unauthorized(new { Message = "Authentication required. Application profile not found." });
            var client = await _context.Users.FindAsync(identity.Id);
            if (client is null) return Unauthorized(new { Message = "Authentication required. Application profile not found." });

            var now = DateTimeOffset.UtcNow;
            var matchingWorkflows = await _context.WorkflowStates
                .Where(w => w.LandSubmission.ClientId == client.Id
                    && (w.Status.ToLower() == "running" || w.Status.ToLower() == "processing")
                    && w.LandSubmission.LandSizeCategory == requirement.LandSizeCategory
                    && w.LandSubmission.LandSizePerches == requirement.LandSizePerches
                    && w.LandSubmission.PreferredBedrooms == requirement.Bedrooms
                    && w.LandSubmission.PreferredBathrooms == requirement.Bathrooms
                    && w.LandSubmission.PreferredFloors == requirement.Floors
                    && w.LandSubmission.StylePreference == requirement.HouseType)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync(cancellationToken);
            foreach (var staleWorkflow in matchingWorkflows.Where(w => WorkflowExecutionPolicy.IsStale(w, now)))
                WorkflowExecutionPolicy.MarkStaleFailed(staleWorkflow, now);
            if (_context.ChangeTracker.HasChanges())
                await _context.SaveChangesAsync(cancellationToken);

            var activeWorkflow = matchingWorkflows.FirstOrDefault(w =>
                WorkflowExecutionPolicy.IsActiveStatus(w.Status));
            if (activeWorkflow is not null)
            {
                Console.WriteLine($"[Workflow Guard] Duplicate generation ignored for workflow {activeWorkflow.Id}");
                return Ok(new
                {
                    Message = "Workflow already running",
                    WorkflowId = activeWorkflow.Id,
                    Reused = true
                });
            }

            var submission = new LandSubmission
            {
                Id = Guid.NewGuid(), ClientId = client.Id, BudgetLkr = 0,
                LandSizePerches = requirement.LandSizePerches, ManualTerrainType = "flat",
                PreferredBedrooms = requirement.Bedrooms, PreferredBathrooms = requirement.Bathrooms,
                PreferredFloors = requirement.Floors, LandSizeCategory = requirement.LandSizeCategory,
                StylePreference = requirement.HouseType, CreatedAt = now,
                UpdatedAt = now
            };
            Console.WriteLine($"[TRACE] saved bathroom count: {submission.PreferredBathrooms}");
            workflow = new WorkflowState
            {
                Id = Guid.NewGuid(), LandSubmissionId = submission.Id, Status = "running",
                ApprovalStatus = "not_requested", CreatedAt = now,
                UpdatedAt = now
            };
            _context.LandSubmissions.Add(submission);
            _context.WorkflowStates.Add(workflow);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Database error while saving the submission.", Details = ex.InnerException?.Message ?? ex.Message });
        }

        var payload = new
        {
            workflow_id = workflow.Id,
            submission_id = workflow.LandSubmissionId,
            land_size_category = requirement.LandSizeCategory,
            land_size_perches = requirement.LandSizePerches,
            bedrooms = requirement.Bedrooms,
            bathrooms = requirement.Bathrooms,
            house_type = requirement.HouseType,
            target_duration_days = requirement.TargetDurationDays
        };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        try
        {
            var response = await _agenticServiceClient.PostAsync("/workflows/start", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var safeDetails = ExtractSafeAgenticDetails(responseBody);
                var failureReason = $"Agentic service rejected the workflow request (HTTP {(int)response.StatusCode}).";
                await MarkWorkflowFailedAsync(workflow, failureReason, cancellationToken);
                _logger.LogWarning(
                    "Agentic workflow start rejected for {WorkflowId} with HTTP {Status}: {Details}",
                    workflow.Id, (int)response.StatusCode, safeDetails);
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    message = "Agentic service rejected the workflow request.",
                    status = (int)response.StatusCode,
                    details = safeDetails
                });
            }
        }
        catch (Exception ex)
        {
            var failureReason = $"Could not connect to the AI agentic service. Error: {ex.Message}";
            await MarkWorkflowFailedAsync(workflow, failureReason, cancellationToken);
            _logger.LogError(ex, "Agentic workflow start connection failed for {WorkflowId}. Called URL: {Url}", workflow.Id, _agenticServiceClient.BaseAddress);
            return StatusCode(StatusCodes.Status504GatewayTimeout, new
            {
                message = "Cannot connect to the AI agentic service.",
                details = ex.Message,
                suggestion = "The workflow was recorded as failed and can be retried. The agentic service might be experiencing a cold start."
            });
        }

        return Ok(new { Message = "Workflow started successfully", WorkflowId = workflow.Id });
    }

    private async Task MarkWorkflowFailedAsync(
        WorkflowState workflow,
        string failureReason,
        CancellationToken cancellationToken)
    {
        workflow.Status = "failed";
        workflow.ApprovalStatus = "not_requested";
        workflow.FailureReason = failureReason;
        workflow.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string ExtractSafeAgenticDetails(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody)) return "No validation details were returned.";
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (!document.RootElement.TryGetProperty("detail", out var detail))
                return "The agentic service rejected the request.";
            if (detail.ValueKind == JsonValueKind.String)
                return Limit(detail.GetString());
            if (detail.ValueKind != JsonValueKind.Array)
                return "The agentic service rejected the request.";

            var messages = detail.EnumerateArray().Select(item =>
            {
                var field = item.TryGetProperty("loc", out var location) && location.ValueKind == JsonValueKind.Array
                    ? string.Join('.', location.EnumerateArray().Select(x => x.ToString()).Where(x => x != "body"))
                    : "request";
                var message = item.TryGetProperty("msg", out var msg) ? msg.GetString() : "Invalid value.";
                return $"{field}: {message}";
            });
            return Limit(string.Join("; ", messages));
        }
        catch (JsonException)
        {
            return "The agentic service returned an unreadable error response.";
        }
    }

    private static string Limit(string? value) => string.IsNullOrWhiteSpace(value)
        ? "The agentic service rejected the request."
        : value[..Math.Min(value.Length, 1000)];
}
