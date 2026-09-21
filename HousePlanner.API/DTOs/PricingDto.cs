using System.Text.Json.Serialization;
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
        public string? Region { get; set; }
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
    }

    public sealed class PricingSyncResultDto
    {
        public Guid AuditId { get; set; }
        public string Provider { get; set; } = null!;
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public int FailedCount { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset CompletedAt { get; set; }
        public IReadOnlyList<PricingSyncIssueDto> Issues { get; set; } = [];
    }

    public sealed record PricingSyncIssueDto(string? ExternalItemId, string Reason, string Outcome);
}
