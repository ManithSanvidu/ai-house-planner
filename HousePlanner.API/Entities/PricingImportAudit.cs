using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HousePlanner.API.Entities;

[Table("PricingImportAudits")]
public class PricingImportAudit
{
    [Key]
    public Guid Id { get; set; }

    [Required, StringLength(100)]
    public string Provider { get; set; } = null!;

    [Required, StringLength(30)]
    public string Status { get; set; } = null!;

    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    [Column(TypeName = "text")]
    public string? DetailsJson { get; set; }

    [StringLength(2000)]
    public string? FailureMessage { get; set; }
}
