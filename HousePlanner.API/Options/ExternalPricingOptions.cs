namespace HousePlanner.API.Options;

public sealed class ExternalPricingOptions
{
    public const string SectionName = "ExternalPricing";

    public string Provider { get; set; } = "File";
    public string ProviderName { get; set; } = "ApprovedFile";
    public string FilePath { get; set; } = "Data/Pricing/approved-pricing-feed.json";
    public List<PricingConversionRule> ConversionRules { get; set; } = [];
}

public sealed class PricingConversionRule
{
    public string ExternalItemId { get; set; } = null!;
    public string OriginalUnit { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string TargetUnit { get; set; } = null!;
    public decimal PriceMultiplier { get; set; }
    public string? NormalizedItemName { get; set; }
    public string? DisplayGroup { get; set; }
    public decimal FlatMultiplier { get; set; } = 1m;
    public decimal HillsideMultiplier { get; set; } = 1.25m;
    public decimal CoastalMultiplier { get; set; } = 1.35m;
}
