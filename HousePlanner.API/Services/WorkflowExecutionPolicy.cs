namespace HousePlanner.API.Services;

public static class WorkflowExecutionPolicy
{
    public static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(30);
    public const string IncompleteFailureReason = "Workflow execution did not complete.";

    public static bool IsActiveStatus(string? status) =>
        string.Equals(status, "running", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "processing", StringComparison.OrdinalIgnoreCase);

    public static bool IsStale(Entities.WorkflowState workflow, DateTimeOffset now) =>
        IsActiveStatus(workflow.Status) && workflow.UpdatedAt <= now - StaleAfter;

    public static void MarkStaleFailed(Entities.WorkflowState workflow, DateTimeOffset now)
    {
        workflow.Status = "failed";
        workflow.ApprovalStatus = "not_requested";
        workflow.FailureReason = IncompleteFailureReason;
        workflow.UpdatedAt = now;
    }
}
