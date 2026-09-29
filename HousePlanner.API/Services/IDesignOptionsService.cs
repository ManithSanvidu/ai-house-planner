using HousePlanner.API.Entities;
using HousePlanner.API.Models;

namespace HousePlanner.API.Services;

public interface IDesignOptionsService
{
    Task<DesignOptionsResponseDto> GetAvailableOptionsAsync(DesignOptionsRequestDto request, CancellationToken cancellationToken = default);
    Task<DesignOptionsValidationResult> ValidateFinalSelectionAsync(HouseRequirement request, CancellationToken cancellationToken = default);
}
