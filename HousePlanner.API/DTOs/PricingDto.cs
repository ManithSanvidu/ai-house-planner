using HousePlanner.API.Entities;

namespace HousePlanner.API.DTOs
{
    public class PricingDto
    {
        public int Id { get; set; }
        public string ItemName { get; set; } = null!;
        public string Category { get; set; } = null!;
        public decimal UnitCostLkr { get; set; }
        public string Unit { get; set; } = null!;
        public TerrainMultiplierData TerrainMultiplier { get; set; } = null!;
        public string? DisplayGroup { get; set; }
        public string? Provider { get; set; }
        public string? ExternalItemId { get; set; }
        public string? ExternalItemName { get; set; }
        public string? OriginalUnit { get; set; }
        public decimal? OriginalPrice { get; set; }
        public string? OriginalCurrency { get; set; }
        public string Region { get; set; } = "Sri Lanka";
        public string QualityLevel { get; set; } = "Standard";
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string? UpdatedByUserId { get; set; }
        public DateTimeOffset? ObservedAt { get; set; }
        public DateTimeOffset? EffectiveAt { get; set; }
        public string? SourceUrl { get; set; }
        public string? SourceReference { get; set; }
        public DateTimeOffset? ImportedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class UpdatePricingDto
    {
        public decimal UnitCostLkr { get; set; }
        public TerrainMultiplierData TerrainMultiplier { get; set; } = null!;
        public string? Reason { get; set; }
    }

    public class CreatePricingDto
    {
        public string ItemName { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string? DisplayGroup { get; set; }
        public decimal UnitCostLkr { get; set; }
        public TerrainMultiplierData TerrainMultiplier { get; set; } = new();
        public string? SourceReference { get; set; }
        public string Region { get; set; } = "Sri Lanka";
        public string QualityLevel { get; set; } = "Standard";
    }

    public sealed class DeactivatePricingDto
    {
        public string? Reason { get; set; }
    }

    public sealed class PricingHistoryDto
    {
        public Guid Id { get; set; }
        public int PricingDataId { get; set; }
        public decimal PreviousValue { get; set; }
        public decimal NewValue { get; set; }
        public string? ChangedByUserId { get; set; }
        public DateTimeOffset ChangedAt { get; set; }
        public string? Reason { get; set; }
    }

}
