using HousePlanner.API.Entities;
using HousePlanner.API.Models;
using HousePlanner.API.Controllers;

namespace HousePlanner.API.Services;

public interface IDesignOptionsService
{
    Task<DesignOptionsResponseDto> GetAvailableOptionsAsync(DesignOptionsRequestDto request, CancellationToken cancellationToken = default);
    Task<DesignOptionsValidationResult> ValidateFinalSelectionAsync(AiGenerationRequest request, CancellationToken cancellationToken = default);
}
