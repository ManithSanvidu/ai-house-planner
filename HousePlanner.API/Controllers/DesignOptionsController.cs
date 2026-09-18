using Microsoft.AspNetCore.Mvc;
using HousePlanner.API.Models;
using HousePlanner.API.Services;

namespace HousePlanner.API.Controllers;

[ApiController]
[Route("api/v1/design-options")]
public class DesignOptionsController : ControllerBase
{
    private readonly IDesignOptionsService _designOptionsService;

    public DesignOptionsController(IDesignOptionsService designOptionsService)
    {
        _designOptionsService = designOptionsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetOptions(CancellationToken cancellationToken)
    {
        var options = await _designOptionsService.GetAvailableOptionsAsync(new DesignOptionsRequestDto(), cancellationToken);
        return Ok(options);
    }

    [HttpPost("compatible")]
    public async Task<IActionResult> GetCompatibleOptions([FromBody] DesignOptionsRequestDto request, CancellationToken cancellationToken)
    {
        var options = await _designOptionsService.GetAvailableOptionsAsync(request, cancellationToken);
        return Ok(options);
    }
}
