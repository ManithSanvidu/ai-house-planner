namespace HousePlanner.API.DTOs;

public class SaveCostEstimateRequestDto
{
    public decimal MaterialCostLkr { get; set; }
    public decimal LabourCostLkr { get; set; }
    public decimal TotalCostLkr { get; set; }
    public decimal BudgetDeltaPercent { get; set; }
}

public class CostEstimateResponseDto
{
    public Guid CostEstimateId { get; set; }
    public Guid HouseDesignId { get; set; }
    public decimal MaterialCostLkr { get; set; }
    public decimal LabourCostLkr { get; set; }
    public decimal TotalCostLkr { get; set; }
    public decimal BudgetDeltaPercent { get; set; }
}
