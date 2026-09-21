using HousePlanner.API.Models;
using HousePlanner.API.Options;
using Microsoft.Extensions.Options;

namespace HousePlanner.API.Services;

public sealed class PricingNormalizationService : IPricingNormalizationService
{
    private readonly ExternalPricingOptions _options;

    public PricingNormalizationService(IOptions<ExternalPricingOptions> options)
    {
        _options = options.Value;
    }

    public PricingNormalizationResult Normalize(ExternalPriceRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.ExternalItemId) ||
            string.IsNullOrWhiteSpace(record.Name) ||
            string.IsNullOrWhiteSpace(record.Unit) ||
            string.IsNullOrWhiteSpace(record.Currency) ||
            record.Price <= 0)
        {
            return new(PricingNormalizationStatus.Failed, "External item id, name, unit, and a positive price are required.");
        }

        if (record.ExternalItemId.Trim().Length > 255 || record.Name.Trim().Length > 255 ||
            record.Unit.Trim().Length > 50 || record.Currency.Trim().Length > 10 ||
            record.Region?.Trim().Length > 150 || record.SourceUrl?.Trim().Length > 2048 ||
            record.SourceReference?.Trim().Length > 500 || record.DisplayGroup?.Trim().Length > 100)
        {
            return new(PricingNormalizationStatus.Failed, "One or more provider fields exceed the supported length.");
        }

        if (!string.Equals(record.Currency.Trim(), "LKR", StringComparison.OrdinalIgnoreCase))
        {
            return new(PricingNormalizationStatus.Skipped, $"Currency '{record.Currency}' is unsupported; no exchange-rate rule is configured.");
        }

        var rule = _options.ConversionRules.FirstOrDefault(candidate =>
            string.Equals(candidate.ExternalItemId?.Trim(), record.ExternalItemId.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(candidate.OriginalUnit?.Trim(), record.Unit.Trim(), StringComparison.OrdinalIgnoreCase));

        if (rule is null)
        {
            return new(PricingNormalizationStatus.Skipped, $"No conversion rule is configured for unit '{record.Unit}'.");
        }

        var category = rule.Category?.Trim().ToLowerInvariant();
        var targetUnit = rule.TargetUnit?.Trim().ToLowerInvariant();
        if (category is not ("material" or "labour"))
        {
            return new(PricingNormalizationStatus.Failed, $"Configured category '{rule.Category}' must be material or labour.");
        }

        var unitIsValid = category == "material"
            ? targetUnit == "per_sqft"
            : targetUnit is "factor" or "ratio";
        if (!unitIsValid)
        {
            return new(PricingNormalizationStatus.Failed,
                $"Configured target unit '{rule.TargetUnit}' is invalid for category '{category}'.");
        }

        if (rule.PriceMultiplier <= 0 || rule.FlatMultiplier <= 0 ||
            rule.HillsideMultiplier <= 0 || rule.CoastalMultiplier <= 0)
        {
            return new(PricingNormalizationStatus.Failed, "Configured price and terrain multipliers must be greater than zero.");
        }

        if (rule.NormalizedItemName?.Trim().Length > 255 || rule.DisplayGroup?.Trim().Length > 100)
        {
            return new(PricingNormalizationStatus.Failed, "The configured normalized name or display group is too long.");
        }

        decimal normalizedPrice;
        try
        {
            normalizedPrice = decimal.Round(record.Price * rule.PriceMultiplier, 2, MidpointRounding.AwayFromZero);
        }
        catch (OverflowException)
        {
            return new(PricingNormalizationStatus.Failed, "The configured conversion produced a price outside the supported range.");
        }
        if (normalizedPrice <= 0)
        {
            return new(PricingNormalizationStatus.Failed, "The configured conversion produced a non-positive normalized price.");
        }

        return new(PricingNormalizationStatus.Normalized, Value: new NormalizedExternalPrice
        {
            ItemName = string.IsNullOrWhiteSpace(rule.NormalizedItemName) ? record.Name.Trim() : rule.NormalizedItemName.Trim(),
            Category = category,
            Unit = targetUnit!,
            UnitCostLkr = normalizedPrice,
            DisplayGroup = string.IsNullOrWhiteSpace(rule.DisplayGroup) ? record.DisplayGroup?.Trim() : rule.DisplayGroup.Trim(),
            FlatMultiplier = rule.FlatMultiplier,
            HillsideMultiplier = rule.HillsideMultiplier,
            CoastalMultiplier = rule.CoastalMultiplier
        });
    }
}
