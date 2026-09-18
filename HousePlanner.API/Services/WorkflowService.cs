using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;

namespace HousePlanner.API.Services
{
    public class WorkflowService : IWorkflowService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<WorkflowService> _logger;
        private readonly ConcurrentDictionary<Guid, WorkflowSessionInfo> _activeWorkflows = new();

        public WorkflowService(IServiceScopeFactory scopeFactory, ILogger<WorkflowService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public void SetWorkflowSession(WorkflowSessionInfo session)
        {
            _activeWorkflows[session.WorkflowId] = session;
        }

        public async Task<WorkflowSessionInfo?> GetWorkflowStatusAsync(Guid workflowId)
        {
            // Always read the latest state synced from database
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var existingWorkflow = await dbContext.WorkflowStates
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == workflowId);

            if (existingWorkflow != null)
            {
                var reconstructed = new WorkflowSessionInfo
                {
                    WorkflowId = existingWorkflow.Id,
                    Status = existingWorkflow.Status ?? "running",
                    ApprovalStatus = existingWorkflow.ApprovalStatus ?? "not_requested",
                    ValidationPassed = existingWorkflow.ApprovalStatus == "approved" || existingWorkflow.ApprovalStatus == "pending" || existingWorkflow.ApprovalStatus == "client_review",
                    CreatedAt = existingWorkflow.CreatedAt,
                    UpdatedAt = existingWorkflow.UpdatedAt
                };
                _activeWorkflows[workflowId] = reconstructed;
                return reconstructed;
            }

            // Fallback: check if an approved project already exists in the database for this workflow
            var existingProject = await dbContext.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.WorkflowStateId == workflowId);

            if (existingProject != null)
            {
                var reconstructed = new WorkflowSessionInfo
                {
                    WorkflowId = workflowId,
                    Status = "approved",
                    ApprovalStatus = "approved",
                    ValidationPassed = true,
                    ProjectId = existingProject.Id,
                    CreatedAt = existingProject.CreatedAt,
                    UpdatedAt = existingProject.UpdatedAt
                };
                _activeWorkflows[workflowId] = reconstructed;
                return reconstructed;
            }

            return null;
        }

        public async Task<ApprovalServiceResult> ProcessApprovalAsync(
            Guid workflowId,
            ApprovalRequestDto request,
            string? userEmail = null,
            string? userRole = null)
        {
            // 1. Confirm the workflow exists
            var session = await GetWorkflowStatusAsync(workflowId);
            if (session == null)
            {
                _logger.LogWarning("Approval attempt failed: Workflow '{WorkflowId}' not found.", workflowId);
                return ApprovalServiceResult.NotFound($"Workflow with ID '{workflowId}' was not found.");
            }

            // 2. Prevent invalid duplicate approvals
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var dbWorkflowState = await dbContext.WorkflowStates.FirstOrDefaultAsync(w => w.Id == workflowId);

            var existingProject = await dbContext.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.WorkflowStateId == workflowId);

            if (existingProject != null || session.ApprovalStatus == "approved")
            {
                _logger.LogWarning("Duplicate approval attempt for Workflow '{WorkflowId}'. Project already exists: '{ProjectId}'",
                    workflowId, existingProject?.Id ?? session.ProjectId);
                return ApprovalServiceResult.Conflict($"Workflow '{workflowId}' has already been approved and a project has been created.");
            }

            // 3. Confirm the workflow is in the human approval stage
            if (session.Status != "awaiting_approval" && session.ApprovalStatus != "pending" && session.ApprovalStatus != "client_review")
            {
                _logger.LogWarning("Approval rejected for Workflow '{WorkflowId}': Invalid status '{Status}'.",
                    workflowId, session.Status);
                return ApprovalServiceResult.InvalidState(
                    $"Workflow '{workflowId}' is not ready for human approval. Current status: '{session.Status}', approval status: '{session.ApprovalStatus}'.");
            }

            // 4. Confirm deterministic validation passed before allowing approval
            if (!session.ValidationPassed)
            {
                _logger.LogWarning("Approval rejected for Workflow '{WorkflowId}': Validation has not passed.", workflowId);
                return ApprovalServiceResult.ValidationFailed(
                    $"Cannot approve workflow '{workflowId}': Safety validation has not passed or is in a failed state.");
            }

            // 4.5. Enforce role-based access if role information is available (Architect, Client, Admin, User are authorized)
            if (!string.IsNullOrEmpty(userRole))
            {
                var lowerRole = userRole.ToLowerInvariant();
                if (lowerRole != "architect" && lowerRole != "client" && lowerRole != "admin" && lowerRole != "user")
                {
                    _logger.LogWarning("Approval rejected for Workflow '{WorkflowId}': User role '{Role}' is not authorized.", workflowId, userRole);
                    return ApprovalServiceResult.Unauthorized("Only Architects, Clients, or Admins are authorized to review workflows.");
                }
            }

            // 5. Normalize and apply requested decision
            var decision = request.Decision.Trim().ToLowerInvariant();

            switch (decision)
            {
                case "approve":
                case "approved":
                    // Create Project record atomically in database
                    var newProject = new Project
                    {
                        Id = Guid.NewGuid(),
                        WorkflowStateId = workflowId,
                        Status = "not_started",
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow,
                        ConstructionPhases = new List<ConstructionPhase>
                        {
                            new ConstructionPhase { Id = Guid.NewGuid(), PhaseName = "Site Preparation", Status = "pending", SequenceOrder = 1 },
                            new ConstructionPhase { Id = Guid.NewGuid(), PhaseName = "Foundation", Status = "pending", SequenceOrder = 2 },
                            new ConstructionPhase { Id = Guid.NewGuid(), PhaseName = "Framing", Status = "pending", SequenceOrder = 3 },
                            new ConstructionPhase { Id = Guid.NewGuid(), PhaseName = "Roofing", Status = "pending", SequenceOrder = 4 },
                            new ConstructionPhase { Id = Guid.NewGuid(), PhaseName = "Interior & Finish", Status = "pending", SequenceOrder = 5 }
                        }
                    };

                    dbContext.Projects.Add(newProject);
                    await dbContext.SaveChangesAsync();

                    session.Status = "approved";
                    session.ApprovalStatus = "approved";
                    session.ProjectId = newProject.Id;
                    session.UpdatedAt = DateTimeOffset.UtcNow;

                    if (dbWorkflowState != null)
                    {
                        dbWorkflowState.ApprovalStatus = "approved";
                        dbWorkflowState.Status = "approved";
                        dbWorkflowState.ApprovedAt = DateTimeOffset.UtcNow;
                        dbWorkflowState.UpdatedAt = session.UpdatedAt;
                        await dbContext.SaveChangesAsync();
                    }

                    _logger.LogInformation("Workflow '{WorkflowId}' successfully APPROVED. Created Project '{ProjectId}'.",
                        workflowId, newProject.Id);

                    return ApprovalServiceResult.Success(new ApprovalResponseDto
                    {
                        WorkflowId = workflowId,
                        Decision = "approved",
                        Status = "approved",
                        ProjectId = newProject.Id,
                        Message = "Workflow approved and construction project created successfully.",
                        Timestamp = DateTimeOffset.UtcNow
                    });

                case "reject":
                case "rejected":
                    session.Status = "rejected";
                    session.ApprovalStatus = "rejected";
                    session.RevisionNotes = request.RevisionNotes;
                    session.UpdatedAt = DateTimeOffset.UtcNow;

                    if (dbWorkflowState != null) {
                        dbWorkflowState.ApprovalStatus = session.ApprovalStatus;
                        dbWorkflowState.Status = session.Status;
                        dbWorkflowState.UpdatedAt = session.UpdatedAt;
                        await dbContext.SaveChangesAsync();
                    }

                    _logger.LogInformation("Workflow '{WorkflowId}' REJECTED.", workflowId);

                    return ApprovalServiceResult.Success(new ApprovalResponseDto
                    {
                        WorkflowId = workflowId,
                        Decision = "rejected",
                        Status = "rejected",
                        ProjectId = null,
                        Message = "Workflow has been rejected.",
                        Timestamp = DateTimeOffset.UtcNow
                    });

                case "request_revision":
                case "revision_requested":
                case "revision":
                    session.Status = "running";
                    session.ApprovalStatus = "revision_requested";
                    session.RevisionNotes = request.RevisionNotes;
                    session.RetryCount++;
                    session.UpdatedAt = DateTimeOffset.UtcNow;

                    if (dbWorkflowState != null) {
                        dbWorkflowState.ApprovalStatus = session.ApprovalStatus;
                        dbWorkflowState.Status = session.Status;
                        dbWorkflowState.UpdatedAt = session.UpdatedAt;
                        await dbContext.SaveChangesAsync();
                    }

                    _logger.LogInformation("Workflow '{WorkflowId}' REVISION REQUESTED (Retry count: {RetryCount}).",
                        workflowId, session.RetryCount);

                    return ApprovalServiceResult.Success(new ApprovalResponseDto
                    {
                        WorkflowId = workflowId,
                        Decision = "request_revision",
                        Status = "revision_requested",
                        ProjectId = null,
                        Message = "Revision requested. Workflow returned for design adjustments.",
                        Timestamp = DateTimeOffset.UtcNow
                    });

                default:
                    return ApprovalServiceResult.BadRequest(
                        $"Invalid approval decision '{request.Decision}'. Allowed values are: 'approve', 'reject', 'request_revision'.");
            }
        }
    }
}
