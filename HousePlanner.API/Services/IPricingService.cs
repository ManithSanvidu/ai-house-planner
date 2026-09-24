using HousePlanner.API.DTOs;

namespace HousePlanner.API.Services
{
    public interface IPricingService
    {
        Task<IEnumerable<PricingDto>> GetAllPricingAsync();
        Task<IEnumerable<PricingDto>> GetActivePricingAsync(string? region, string? qualityLevel);
        Task<PricingDto?> GetPricingByIdAsync(int id);
        Task<PricingDto> CreatePricingAsync(CreatePricingDto createDto, string? updatedByUserId = null);
        Task<PricingDto?> UpdatePricingAsync(int id, UpdatePricingDto updateDto, string? updatedByUserId = null);
        Task<PricingDto?> DeactivatePricingAsync(int id, string? reason, string? updatedByUserId = null);
        Task<IReadOnlyList<PricingHistoryDto>> GetPricingHistoryAsync(int id);
    }
}
