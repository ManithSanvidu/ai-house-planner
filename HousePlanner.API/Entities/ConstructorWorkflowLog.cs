using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HousePlanner.API.Entities
{
    [Table("ConstructorWorkflowLogs")]
    public class ConstructorWorkflowLog
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; }

        [Required]
        public Guid ConstructorId { get; set; }

        [ForeignKey("ConstructorId")]
        public virtual User? Constructor { get; set; }

        public Guid? ConstructionPhaseId { get; set; }

        [ForeignKey("ConstructionPhaseId")]
        public virtual ConstructionPhase? ConstructionPhase { get; set; }

        public int DayNumber { get; set; }

        public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;

        [Required]
        public string CompletedWork { get; set; } = string.Empty;

        public int ProgressPercentage { get; set; }

        public string? Challenges { get; set; }
        public string? Issues { get; set; }
        public string? Resolution { get; set; }
        public string? TomorrowPlan { get; set; }
        public string? AdditionalNotes { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Completed"; // Completed, Planned, In Progress

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
