namespace HousePlanner.API.DTOs
{
    public class WorkflowStatusResponseDto
    {
        public Guid WorkflowId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ApprovalStatus { get; set; } = string.Empty;
        public bool ValidationPassed { get; set; }
        public int RetryCount { get; set; }
        public string? RevisionNotes { get; set; }
        public object? ValidationResult { get; set; }
        public Guid? ProjectId { get; set; }
        public string? TerrainType { get; set; }
        public string? SlopeEstimate { get; set; }
        public HouseDesignSummaryDto? Design { get; set; }
        public CostSummaryDto? Cost { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public WorkflowStatusResponseDto() { }

        public WorkflowStatusResponseDto(
            Guid WorkflowId,
            string Status,
            string? TerrainType,
            string? SlopeEstimate,
            HouseDesignSummaryDto? Design,
            CostSummaryDto? Cost,
            string ApprovalStatus
        )
        {
            this.WorkflowId = WorkflowId;
            this.Status = Status;
            this.TerrainType = TerrainType;
            this.SlopeEstimate = SlopeEstimate;
            this.Design = Design;
            this.Cost = Cost;
            this.ApprovalStatus = ApprovalStatus;
        }
    }
}
