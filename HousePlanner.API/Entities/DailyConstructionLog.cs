using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HousePlanner.API.Entities
{
    [Table("DailyConstructionLogs")]
    public class DailyConstructionLog
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ProjectId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        public virtual Project? Project { get; set; }

        [Required]
        public Guid ConstructorId { get; set; }

        [ForeignKey(nameof(ConstructorId))]
        public virtual User? Constructor { get; set; }

        public DateOnly LogDate { get; set; }

        public Guid? ConstructionPhaseId { get; set; }

        [ForeignKey(nameof(ConstructionPhaseId))]
        public virtual ConstructionPhase? ConstructionPhase { get; set; }

        [Required]
        public string WorkCompleted { get; set; } = string.Empty;

        public string? Challenges { get; set; }
        public string? MaterialsUsed { get; set; }
        
        public int? WorkforceCount { get; set; }
        
        [StringLength(100)]
        public string? WeatherCondition { get; set; }
        
        public string? SafetyIssues { get; set; }
        
        [Range(0, 100)]
        public decimal? ProgressPercentage { get; set; }
        
        public string? TomorrowPlan { get; set; }
        
        public string? Notes { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
