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
    /// Creates or synchronizes an application-user profile for a verified Firebase identity.
    /// Used by Google sign-in and existing session restoration. A new identity is provisioned as
    /// Customer; an existing identity keeps its PostgreSQL role.
    /// The UID and email are derived from the validated Authorization bearer token only.
    /// </summary>
    [Authorize]
    [HttpPost("session")]
    [ProducesResponseType(typeof(UserInfoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserInfoResponseDto>> CreateSession(CancellationToken cancellationToken)
    {
        var firebaseUser = ExtractFirebaseIdentity();
        var applicationUser = await users.SynchronizeAsync(firebaseUser, cancellationToken);
        return Ok(ToDto(applicationUser));
    }

    /// <summary>
    /// Public registration always provisions Customer. Firebase UID, email, and role are trusted
    /// server-side values; extra client fields such as role, RoleId, or FirebaseUid are ignored.
    /// </summary>
    [Authorize]
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserInfoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserInfoResponseDto>> Register(
        [FromBody] RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        var firebaseUser = ExtractFirebaseIdentity();
        var applicationUser = await users.RegisterCustomerAsync(
            firebaseUser, request.FullName, cancellationToken);
        return Ok(ToDto(applicationUser));
    }

    /// <summary>Loads the current application user and server-owned role after Firebase token validation.</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserInfoResponseDto>> Me(CancellationToken cancellationToken)
        => await CreateSession(cancellationToken);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private UserInfoResponseDto ExtractFirebaseIdentity() => new()
    {
        Uid = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
        Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
    };

    private static UserInfoResponseDto ToDto(Entities.User user) => new()
    {
        Uid = user.FirebaseUid ?? string.Empty,
        Email = user.Email,
        FullName = user.FullName,
        Role = user.Role.Name,
    };
}
