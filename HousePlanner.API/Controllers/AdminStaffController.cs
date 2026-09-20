using HousePlanner.API.DTOs;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HousePlanner.API.Controllers;

[ApiController]
[Route("api/v1/admin/staff")]
[Authorize(Roles = "Admin")]
public sealed class AdminStaffController(
    IStaffAccountService staff,
    ICurrentUserContextService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? role, CancellationToken cancellationToken)
    {
        if (await RequireAdmin() is { } denied) return denied;
        try { return Ok(await staff.ListAsync(role, cancellationToken)); }
        catch (StaffAccountException ex) { return Error(ex); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStaffRequestDto request, CancellationToken cancellationToken)
    {
        if (await RequireAdmin() is { } denied) return denied;
        try { return StatusCode(StatusCodes.Status201Created, await staff.CreateAsync(request, cancellationToken)); }
        catch (StaffAccountException ex) { return Error(ex); }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] UpdateStaffStatusDto request,
        CancellationToken cancellationToken)
    {
        if (await RequireAdmin() is { } denied) return denied;
        try { return Ok(await staff.SetDisabledAsync(id, request.Disabled, cancellationToken)); }
        catch (StaffAccountException ex) { return Error(ex); }
    }

    private async Task<IActionResult?> RequireAdmin()
    {
        var user = await currentUser.GetAsync(HttpContext);
        if (user?.Id is null) return Unauthorized();
        return string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase) ? null : Forbid();
    }

    private IActionResult Error(StaffAccountException exception) => exception.Code switch
    {
        "duplicate_email" or "duplicate_identity" => Conflict(new { error = exception.Code, message = exception.Message }),
        "not_found" => NotFound(new { error = exception.Code, message = exception.Message }),
        "invalid_request" => BadRequest(new { error = exception.Code, message = exception.Message }),
        _ => StatusCode(StatusCodes.Status500InternalServerError,
            new { error = exception.Code, message = exception.Message })
    };
}
