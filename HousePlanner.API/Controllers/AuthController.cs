using System.Security.Claims;
using HousePlanner.API.DTOs;
using HousePlanner.API.Exceptions;
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
    /// Used by Google sign-in and existing session restoration.
    /// The UID and email are derived from the validated Authorization bearer token only.
    /// </summary>
    [Authorize]
    [HttpPost("session")]
    [ProducesResponseType(typeof(UserInfoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserInfoResponseDto>> CreateSession(CancellationToken cancellationToken)
    {
        var firebaseUser = ExtractFirebaseIdentity();
        try 
        {
            var applicationUser = await users.SynchronizeAsync(firebaseUser, cancellationToken);
            return Ok(ToDto(applicationUser));
        }
        catch (UserNotRegisteredException)
        {
            return NotFound(new { error = "registration_required", message = "User is authenticated but not registered." });
        }
    }

    /// <summary>
    /// Registers a new application-user with an explicit, publicly-allowed account type.
    /// 
    /// The Firebase UID and email are taken exclusively from the validated bearer token.
    /// The client-supplied requestedRole must be one of: "Customer", "Architect", "Constructor".
    /// Any other role (including "Admin") is rejected with 400.
    /// 
    /// If the Firebase UID already maps to an existing application user, that existing
    /// profile is returned unchanged — the role is NEVER updated via this endpoint.
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
        if (string.IsNullOrWhiteSpace(request.RequestedRole))
            return BadRequest(new { error = "requestedRole is required." });

        try
        {
            var firebaseUser = ExtractFirebaseIdentity();
            var applicationUser = await users.RegisterAsync(
                firebaseUser,
                request.FullName,
                request.RequestedRole,
                cancellationToken);
            return Ok(ToDto(applicationUser));
        }
        catch (InvalidOperationException ex)
        {
            // Role not in the public allowlist
            return BadRequest(new { error = ex.Message });
        }
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
