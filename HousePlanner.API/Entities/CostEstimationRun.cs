using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HousePlanner.API.Entities;

[Table("CostEstimationRuns")]
public class CostEstimationRun
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid WorkflowStateId { get; set; }

    [ForeignKey(nameof(WorkflowStateId))]
    public WorkflowState WorkflowState { get; set; } = null!;

    public Guid? HouseDesignId { get; set; }

    [ForeignKey(nameof(HouseDesignId))]
    public HouseDesign? HouseDesign { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "failed";

    [Required, MaxLength(50)]
    public string FormulaVersion { get; set; } = "category-area-v1";

    public int PricingRecordCount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? AppliedAreaSqft { get; set; }

    [MaxLength(30)]
    public string? TerrainType { get; set; }

    [MaxLength(1000)]
    public string? FailureReason { get; set; }

    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
}
