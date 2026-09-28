using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HousePlanner.API.Entities
{
    [Table("ConstructionMaterials")]
    public class ConstructionMaterial
    {
        [Key]
        public Guid Id { get; set; }

        public Guid? ProjectId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        public virtual Project? Project { get; set; }

        public Guid? HouseDesignId { get; set; }

        [ForeignKey(nameof(HouseDesignId))]
        public virtual HouseDesign? HouseDesign { get; set; }

        public Guid? PhaseId { get; set; }

        [ForeignKey(nameof(PhaseId))]
        public virtual ConstructionPhase? Phase { get; set; }

        [Required]
        [StringLength(200)]
        public string MaterialName { get; set; } = string.Empty;

        public decimal RequiredQuantity { get; set; }

        [StringLength(50)]
        public string Unit { get; set; } = string.Empty;

        public decimal AvailableQuantity { get; set; }

        public decimal OrderedQuantity { get; set; }

        [StringLength(200)]
        public string? Supplier { get; set; }

        public DateTimeOffset? ExpectedDeliveryDate { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending";
    }
}
