using System.Security.Claims;
using HousePlanner.API.DTOs;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HousePlanner.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IApplicationUserSyncService users) : ControllerBase
{
    /// <summary>
    /// Synchronizes the authenticated Firebase identity into the application database.
    /// The UID and email are derived from the validated Authorization bearer token only.
    /// </summary>
    [Authorize]
    [HttpPost("session")]
    [ProducesResponseType(typeof(UserInfoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserInfoResponseDto>> CreateSession(CancellationToken cancellationToken)
    {
        var firebaseUser = new UserInfoResponseDto
        {
            Uid = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty
        };
        var applicationUser = await users.SynchronizeAsync(firebaseUser, cancellationToken);
        return Ok(new UserInfoResponseDto
        {
            Uid = applicationUser.FirebaseUid ?? string.Empty,
            Email = applicationUser.Email,
            Role = applicationUser.Role.Name
        });
    }

    /// <summary>Loads the current application user and server-owned role after Firebase token validation.</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserInfoResponseDto>> Me(CancellationToken cancellationToken)
        => await CreateSession(cancellationToken);
}
