using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HousePlanner.API.Entities;

[Table("PricingHistory")]
public sealed class PricingHistory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public int PricingDataId { get; set; }

    [ForeignKey(nameof(PricingDataId))]
    public PricingData PricingData { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal PreviousValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NewValue { get; set; }

    [StringLength(100)]
    public string? ChangedByUserId { get; set; }

    public DateTimeOffset ChangedAt { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
}
