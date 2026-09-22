namespace HousePlanner.API.DTOs;

// ──────────────────────────────────────────────────
// Workflow Status Response (public endpoint)
// ──────────────────────────────────────────────────

public record WorkflowStatusResponseDto(
    Guid WorkflowId,
    string Status,
    string? TerrainType,
    string? SlopeEstimate,
    HouseDesignSummaryDto? Design,
    CostSummaryDto? Cost,
    System.Text.Json.JsonElement? ConstructionPlan,
    string ApprovalStatus,
    string? FailureReason = null,
    Guid? PreferredHouseDesignId = null,
    string? ArchitectReviewStatus = null,
    string? ArchitectFeedback = null
);

public record HouseDesignSummaryDto(
    Guid DesignId,
    int Version,
    int FloorCount,
    decimal TotalBuiltUpAreaSqft,
    string FoundationType,
    string? TemplateId,
    string? TerrainType,
    bool IsCurrent,
    List<RoomSummaryDto> Rooms,
    string? TemplateFamily = null,
    long? DesignSeed = null,
    decimal? DesignScore = null,
    string? GeometryFingerprint = null,
    decimal? GroundFootprintSqft = null,
    System.Text.Json.JsonElement? Connections = null,
    System.Text.Json.JsonElement? Entrances = null,
    System.Text.Json.JsonElement? PlotConstraints = null,
    System.Text.Json.JsonElement? CandidateSummary = null
);

public record RoomSummaryDto(
    Guid RoomId,
    string RoomType,
    string? Name,
    int FloorNumber,
    decimal X,
    decimal Y,
    decimal Width,
    decimal Length,
    decimal AreaSqft,
    decimal WallHeight,
    List<OpeningDto>? Doors,
    List<OpeningDto>? Windows
);

public record OpeningDto(
    string Wall,
    decimal Offset,
    decimal Width
);

public record CostSummaryDto(
    decimal MaterialCostLkr,
    decimal LabourCostLkr,
    decimal TotalCostLkr,
    decimal BudgetDeltaPercent
);

public record DesignHistoryDto(
    Guid DesignId,
    int Version,
    bool IsCurrent,
    bool IsPreferred,
    bool IsArchived,
    bool IsArchitectApproved,
    string? Topology,
    int Bedrooms,
    int Bathrooms,
    int FloorCount,
    decimal TotalBuiltUpAreaSqft,
    string FoundationType,
    string? GenerationMode,
    string? SelectedBasePlan,
    string? GeometryFingerprint,
    decimal? SuitabilityScore,
    decimal? ArchitecturalQualityScore,
    List<DesignPreviewRoomDto> PreviewRooms,
    DateTimeOffset CreatedAt
);

public record DesignPreviewRoomDto(
    string RoomType, int Floor, decimal X, decimal Y, decimal Width, decimal Length
);

public record WorkflowDesignHistoryDto(
    Guid WorkflowId,
    string Status,
    Guid? PreferredHouseDesignId,
    DateTimeOffset CreatedAt,
    List<DesignHistoryDto> Designs,
    Guid? ProjectId = null,
    string? ArchitectReviewStatus = null,
    string? ArchitectFeedback = null
);

// ──────────────────────────────────────────────────
// Constructor Workflow DTOs
// ──────────────────────────────────────────────────

public record ConstructionPhaseDto(
    Guid Id,
    string PhaseName,
    int SequenceOrder,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int AiEstimatedDurationDays,
    int PlannedDurationDays,
    DateOnly? PlannedStartDate,
    DateOnly? PlannedEndDate
);

public record ConstructorProjectDto(
    Guid Id,
    Guid WorkflowStateId,
    Guid? HouseDesignId,
    Guid? ContractorId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int AiEstimatedTotalDurationDays,
    int PlannedTotalDurationDays,
    List<ConstructionPhaseDto> ConstructionPhases
);

public record UpdatePhaseScheduleRequest(
    int PlannedDurationDays
);
