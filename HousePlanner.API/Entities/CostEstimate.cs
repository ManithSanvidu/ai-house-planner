using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HousePlanner.API.Entities;

[Table("CostEstimates")]
public class CostEstimate
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid HouseDesignId { get; set; }

    [ForeignKey(nameof(HouseDesignId))]
    public virtual HouseDesign HouseDesign { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(14,2)")]
    public decimal MaterialCostLkr { get; set; }

    [Required]
    [Column(TypeName = "decimal(14,2)")]
    public decimal LabourCostLkr { get; set; }

    [Required]
    [Column(TypeName = "decimal(14,2)")]
    public decimal TotalCostLkr { get; set; }

    [Required]
    [Column(TypeName = "decimal(6,2)")]
    public decimal BudgetDeltaPercent { get; set; }

    [Column(TypeName = "timestamp with time zone")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
