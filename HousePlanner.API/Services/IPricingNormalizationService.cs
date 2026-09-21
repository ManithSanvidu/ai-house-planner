using HousePlanner.API.Models;

namespace HousePlanner.API.Services;

public interface IPricingNormalizationService
{
    PricingNormalizationResult Normalize(ExternalPriceRecord record);
}
