using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HousePlanner.API.Entities
{
    [Table("ValidationRequests")]
    public class ValidationRequest
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid WorkflowStateId { get; set; }

        [ForeignKey("WorkflowStateId")]
        public virtual WorkflowState WorkflowState { get; set; } = null!;

        [Required]
        public Guid ClientId { get; set; }

        [ForeignKey("ClientId")]
        public virtual User Client { get; set; } = null!;

        public Guid? ArchitectId { get; set; }

        [ForeignKey("ArchitectId")]
        public virtual User? Architect { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Under Review, Approved, Rejected

        [StringLength(2000)]
        public string? ArchitectReview { get; set; }

        public DateTimeOffset? DecisionAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
