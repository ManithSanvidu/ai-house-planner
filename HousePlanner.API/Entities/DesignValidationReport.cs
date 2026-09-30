using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HousePlanner.API.Entities;

[Table("DesignValidationReports")]
public class DesignValidationReport
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid HouseDesignId { get; set; }

    [ForeignKey(nameof(HouseDesignId))]
    public virtual HouseDesign HouseDesign { get; set; } = null!;

    [Required]
    public bool OverallPassed { get; set; }

    [Required]
    public bool GeometryPassed { get; set; }

    [Required]
    public bool BusinessPassed { get; set; }

    [Required]
    [Column(TypeName = "jsonb")]
    public string GeometryFailuresJson { get; set; } = "[]";

    [Required]
    [Column(TypeName = "jsonb")]
    public string GeometryFailedRulesJson { get; set; } = "[]";

    [Required]
    [Column(TypeName = "jsonb")]
    public string BusinessRulesJson { get; set; } = "[]";

    [MaxLength(2000)]
    public string? ValidationSummary { get; set; }

    [Required]
    public int AttemptNumber { get; set; } = 1;

    [Required]
    public int DesignVersion { get; set; }

    [MaxLength(50)]
    public string? ValidationSourceVersion { get; set; }

    [Column(TypeName = "timestamp with time zone")]
    public DateTimeOffset ValidatedAt { get; set; } = DateTimeOffset.UtcNow;
}
