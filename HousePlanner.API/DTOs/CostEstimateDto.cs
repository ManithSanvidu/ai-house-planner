namespace HousePlanner.API.DTOs;

using System.Text.Json;

public class SaveCostEstimateRequestDto
{
    public decimal MaterialCostLkr { get; set; }
    public decimal LabourCostLkr { get; set; }
    public decimal TotalCostLkr { get; set; }
    public decimal? BudgetDeltaPercent { get; set; }
    public JsonElement PricingSnapshot { get; set; }
    public JsonElement Breakdown { get; set; }
    public string FormulaVersion { get; set; } = "category-area-v1";
    public decimal AppliedAreaSqft { get; set; }
    public string TerrainType { get; set; } = "flat";
}

public class CostEstimateResponseDto
{
    public Guid CostEstimateId { get; set; }
    public Guid HouseDesignId { get; set; }
    public decimal MaterialCostLkr { get; set; }
    public decimal LabourCostLkr { get; set; }
    public decimal TotalCostLkr { get; set; }
    public decimal? BudgetDeltaPercent { get; set; }
    public JsonElement PricingSnapshot { get; set; }
    public JsonElement Breakdown { get; set; }
    public string FormulaVersion { get; set; } = string.Empty;
    public decimal AppliedAreaSqft { get; set; }
    public string TerrainType { get; set; } = string.Empty;
    public bool Locked { get; set; }
}

public class SaveCostEstimationRunDto
{
    public string Status { get; set; } = "failed";
    public string FormulaVersion { get; set; } = "category-area-v1";
    public int PricingRecordCount { get; set; }
    public decimal? AppliedAreaSqft { get; set; }
    public string? TerrainType { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
}
