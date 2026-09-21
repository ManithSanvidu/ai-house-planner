using HousePlanner.API.Models;
using HousePlanner.API.Options;
using HousePlanner.API.Services;
using Microsoft.Extensions.Options;

namespace HousePlanner.API.Tests.Services;

public class PricingNormalizationServiceTests
{
    [Fact]
    public void Normalize_AppliesExplicitMaterialConversionRule()
    {
        var service = CreateService(new PricingConversionRule
        {
            ExternalItemId = "cement-001",
            OriginalUnit = "bag",
            Category = "material",
            TargetUnit = "per_sqft",
            PriceMultiplier = 0.08m,
            NormalizedItemName = "Cement allowance",
            DisplayGroup = "Concrete"
        });

        var result = service.Normalize(new ExternalPriceRecord
        {
            ExternalItemId = "cement-001",
            Name = "50 kg cement bag",
            Unit = "bag",
            Price = 2500m
        });

        Assert.Equal(PricingNormalizationStatus.Normalized, result.Status);
        Assert.Equal("material", result.Value!.Category);
        Assert.Equal("per_sqft", result.Value.Unit);
        Assert.Equal(200m, result.Value.UnitCostLkr);
        Assert.Equal("Concrete", result.Value.DisplayGroup);
    }

    [Fact]
    public void Normalize_RejectsUnsupportedUnitWithoutSilentConversion()
    {
        var service = CreateService();

        var result = service.Normalize(new ExternalPriceRecord
        {
            ExternalItemId = "cement-001",
            Name = "50 kg cement bag",
            Unit = "bag",
            Price = 2500m
        });

        Assert.Equal(PricingNormalizationStatus.Skipped, result.Status);
        Assert.Contains("No conversion rule", result.Reason);
    }

    [Fact]
    public void Normalize_PreservesLabourFactorContract()
    {
        var service = CreateService(new PricingConversionRule
        {
            ExternalItemId = "labour-index-001",
            OriginalUnit = "index",
            Category = "labour",
            TargetUnit = "factor",
            PriceMultiplier = 0.001m
        });

        var result = service.Normalize(new ExternalPriceRecord
        {
            ExternalItemId = "labour-index-001",
            Name = "Regional labour index",
            Unit = "index",
            Price = 1100m
        });

        Assert.Equal(PricingNormalizationStatus.Normalized, result.Status);
        Assert.Equal("labour", result.Value!.Category);
        Assert.Equal("factor", result.Value.Unit);
        Assert.Equal(1.10m, result.Value.UnitCostLkr);
    }

    [Fact]
    public void Normalize_UsesOnlyCostAgentMachineCategories()
    {
        var service = CreateService(new PricingConversionRule
        {
            ExternalItemId = "labour-001",
            OriginalUnit = "day",
            Category = "workforce",
            TargetUnit = "factor",
            PriceMultiplier = 1m
        });

        var result = service.Normalize(new ExternalPriceRecord
        {
            ExternalItemId = "labour-001",
            Name = "Mason day rate",
            Unit = "day",
            Price = 5000m
        });

        Assert.Equal(PricingNormalizationStatus.Failed, result.Status);
    }

    private static PricingNormalizationService CreateService(params PricingConversionRule[] rules) =>
        new(Microsoft.Extensions.Options.Options.Create(new ExternalPricingOptions { ConversionRules = [.. rules] }));
}
