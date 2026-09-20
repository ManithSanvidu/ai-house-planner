using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HousePlanner.API.Entities
{
    [Table("Projects")]
    public class Project
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid WorkflowStateId { get; set; }

        [ForeignKey("WorkflowStateId")]
        public virtual WorkflowState? WorkflowState { get; set; }

        public Guid? HouseDesignId { get; set; }

        [ForeignKey(nameof(HouseDesignId))]
        public virtual HouseDesign? HouseDesign { get; set; }

        public Guid? ContractorId { get; set; }

        [ForeignKey("ContractorId")]
        public virtual User? Contractor { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "not_started";

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public virtual ICollection<ConstructionPhase> ConstructionPhases { get; set; } = new List<ConstructionPhase>();
    }
}
