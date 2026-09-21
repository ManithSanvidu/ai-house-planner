using HousePlanner.API.Models;

namespace HousePlanner.API.Services;

public interface IExternalPricingProvider
{
    string Name { get; }
    Task<IReadOnlyList<ExternalPriceRecord>> GetPricesAsync(CancellationToken cancellationToken = default);
}

public sealed class ExternalPricingProviderException : Exception
{
    public ExternalPricingProviderException(string message) : base(message) { }
    public ExternalPricingProviderException(string message, Exception innerException) : base(message, innerException) { }
}
