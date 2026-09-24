using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace HousePlanner.API.Entities
{
    [Table("PricingData")]
    public class PricingData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string ItemName { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = null!; // e.g. material, labour

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCostLkr { get; set; }

        [Required]
        [StringLength(50)]
        public string Unit { get; set; } = null!;

        [Required]
        public TerrainMultiplierData TerrainMultiplier { get; set; } = new TerrainMultiplierData();

        [StringLength(100)]
        public string? DisplayGroup { get; set; }

        [StringLength(100)]
        public string? Provider { get; set; }

        [StringLength(255)]
        public string? ExternalItemId { get; set; }

        [StringLength(255)]
        public string? ExternalItemName { get; set; }

        [StringLength(50)]
        public string? OriginalUnit { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? OriginalPrice { get; set; }

        [StringLength(10)]
        public string? OriginalCurrency { get; set; }

        [StringLength(150)]
        public string Region { get; set; } = "Sri Lanka";

        [Required]
        [StringLength(20)]
        public string QualityLevel { get; set; } = "Standard";

        public bool IsActive { get; set; } = true;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [StringLength(100)]
        public string? UpdatedByUserId { get; set; }

        public DateTimeOffset? ObservedAt { get; set; }

        public DateTimeOffset? EffectiveAt { get; set; }

        [StringLength(2048)]
        public string? SourceUrl { get; set; }

        [StringLength(500)]
        public string? SourceReference { get; set; }

        public DateTimeOffset? ImportedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public ICollection<PricingHistory> History { get; set; } = new List<PricingHistory>();
    }

    public class TerrainMultiplierData
    {
        [JsonPropertyName("flat")]
        public decimal Flat { get; set; } = 1.0m;

        [JsonPropertyName("hillside")]
        public decimal Hillside { get; set; } = 1.25m;

        [JsonPropertyName("coastal")]
        public decimal Coastal { get; set; } = 1.35m;
    }
}
