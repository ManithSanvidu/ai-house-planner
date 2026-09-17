namespace HousePlanner.API.DTOs;

// ──────────────────────────────────────────────────
// House Design & Cost Summaries
// ──────────────────────────────────────────────────

public record HouseDesignSummaryDto(
    Guid DesignId,
    int Version,
    int FloorCount,
    decimal TotalBuiltUpAreaSqft,
    string FoundationType,
    string? TemplateId,
    string? TerrainType,
    bool IsCurrent,
    List<RoomSummaryDto> Rooms
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
