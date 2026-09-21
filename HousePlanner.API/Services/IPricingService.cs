using HousePlanner.API.DTOs;

namespace HousePlanner.API.Services
{
    public interface IPricingService
    {
        Task<IEnumerable<PricingDto>> GetAllPricingAsync();
        Task<PricingDto?> GetPricingByIdAsync(int id);
        Task<PricingDto?> UpdatePricingAsync(int id, UpdatePricingDto updateDto);
        Task<PricingSyncResultDto> SyncExternalPricingAsync(CancellationToken cancellationToken = default);
    }

    public sealed class PricingSyncException : Exception
    {
        public PricingSyncException(string message, PricingSyncResultDto result, Exception innerException)
            : base(message, innerException) => Result = result;

        public PricingSyncResultDto Result { get; }
    }
}
