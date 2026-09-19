namespace HousePlanner.API.DTOs
{
    public record ApprovalRequestDto(
        string Decision,
        string? RevisionNotes
    );

    public record ApprovalResponseDto
    {
        public Guid WorkflowId { get; init; }
        public string Decision { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public Guid? ProjectId { get; init; }
        public string Message { get; init; } = string.Empty;
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    }
}
