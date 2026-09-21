using System.ComponentModel.DataAnnotations;

namespace HousePlanner.API.Models;

public sealed class ExternalPricingFeed
{
    public List<ExternalPriceRecord> Records { get; set; } = [];
}

public sealed class ExternalPriceRecord
{
    [Required]
    public string ExternalItemId { get; set; } = null!;

    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string Unit { get; set; } = null!;

    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")]
    public decimal Price { get; set; }

    public string Currency { get; set; } = "LKR";
    public string? Category { get; set; }
    public string? Region { get; set; }
    public DateTimeOffset? ObservedAt { get; set; }
    public DateTimeOffset? EffectiveAt { get; set; }
    public string? SourceUrl { get; set; }
    public string? SourceReference { get; set; }
    public string? DisplayGroup { get; set; }
}

public sealed class NormalizedExternalPrice
{
    public required string ItemName { get; init; }
    public required string Category { get; init; }
    public required string Unit { get; init; }
    public required decimal UnitCostLkr { get; init; }
    public string? DisplayGroup { get; init; }
    public decimal FlatMultiplier { get; init; }
    public decimal HillsideMultiplier { get; init; }
    public decimal CoastalMultiplier { get; init; }
}

public enum PricingNormalizationStatus
{
    Normalized,
    Skipped,
    Failed
}

public sealed record PricingNormalizationResult(
    PricingNormalizationStatus Status,
    string? Reason = null,
    NormalizedExternalPrice? Value = null);
