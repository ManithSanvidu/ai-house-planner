<<<<<<< HEAD
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HousePlanner.API.DTOs;
using HousePlanner.API.Services;
using HousePlanner.API.Data;
using HousePlanner.API.Entities;

namespace HousePlanner.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IFirebaseAuthService _firebaseAuthService;
        private readonly ApplicationDbContext _dbContext;

        public AuthController(IFirebaseAuthService firebaseAuthService, ApplicationDbContext dbContext)
        {
            _firebaseAuthService = firebaseAuthService;
            _dbContext = dbContext;
        }

        ///<summary>
        /// Register a new user in local database after Firebase authenticates
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody]VerifyTokenRequestDto requestDto)
        {
            if(!ModelState.IsValid)
                return BadRequest(ModelState);

            var userInfo = await _firebaseAuthService.VerifyTokenAsync(requestDto.Token);
            if(userInfo == null)
                return Unauthorized("Invalid Firebase Token");
            
            // Map by email since FirebaseUid doesn't exist on User.cs
            var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == userInfo.Email);
            if(existingUser == null)
            {
                _dbContext.Users.Add(new User 
                {
                    Email = userInfo.Email,
                    FullName = "Firebase User", // Fallback name
                    PasswordHash = "FIREBASE_AUTH", // Placeholder since Firebase manages the actual password
                    RoleId = 1, // Assuming 1 is a default role like "User"
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
                await _dbContext.SaveChangesAsync();
            }
            return Ok(new {Message="User successfully registered in local database", User=userInfo});
        }

        /// <summary>
        /// Verifies a Firebase ID token and returns authenticated user details and role.
        /// </summary>
        /// <param name="requestDto">Contains the Firebase ID Token</param>
        /// <returns>UserInfoResponseDto</returns>
        [HttpPost("verify")]
        [ProducesResponseType(typeof(UserInfoResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Verify([FromBody] VerifyTokenRequestDto requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userInfo = await _firebaseAuthService.VerifyTokenAsync(requestDto.Token);
            var localUser = await _dbContext.Users.AsNoTracking().Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == userInfo.Email);
            if (localUser is null) return Unauthorized("Authenticated Firebase user is not registered locally.");
            userInfo.Role = localUser.Role?.Name ?? "User";
            return Ok(userInfo);
        }

        /// <summary>
        /// Native PostgreSQL Registration (No Firebase)
        /// </summary>
        [HttpPost("local/register")]
        public async Task<IActionResult> LocalRegister([FromBody] LocalRegisterRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existing = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existing != null) return BadRequest("User with this email already exists.");

            var newUser = new User
            {
                Email = request.Email,
                FullName = request.FullName,
                PasswordHash = request.Password, // TODO: Use a proper password hasher like BCrypt in production
                RoleId = request.RoleId == 0 ? 1 : request.RoleId, // Default to 1
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.Users.Add(newUser);
            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = "Registration successful", UserId = newUser.Id });
        }

        /// <summary>
        /// Native PostgreSQL Login (No Firebase)
        /// </summary>
        [HttpPost("local/login")]
        public async Task<IActionResult> LocalLogin([FromBody] LocalLoginRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _dbContext.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || user.PasswordHash != request.Password) 
            {
                return Unauthorized("Invalid email or password.");
            }

            var payload = new { user_id = user.Id, email = user.Email };
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json))
                .Replace('+', '-').Replace('/', '_').TrimEnd('=');
            var token = $"fake.{base64}.fake";

            return Ok(new 
            { 
                Message = "Login successful", 
                Token = token,
                User = new { user.Id, user.Email, user.FullName, Role = user.Role?.Name }
            });
        }
    }
}
=======
using System.Security.Claims;
using HousePlanner.API.DTOs;
using HousePlanner.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HousePlanner.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(ISupabaseUserSyncService users) : ControllerBase
{
    /// <summary>
    /// Creates or synchronizes an application-user profile for a verified Supabase identity.
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
            var supabaseUser = ExtractSupabaseIdentity();
            var applicationUser = await users.SynchronizeAsync(supabaseUser, cancellationToken);
            return Ok(ToDto(applicationUser));
    }

    /// <summary>
    /// Public registration always provisions Customer. Supabase UID, email, and role are trusted
    /// server-side values; extra client fields such as role, RoleId, or SupabaseUid are ignored.
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
        var supabaseUser = ExtractSupabaseIdentity();
        var applicationUser = await users.RegisterCustomerAsync(
            supabaseUser, request.FullName, cancellationToken);
        return Ok(ToDto(applicationUser));
    }

    /// <summary>Loads the current application user and server-owned role after Supabase token validation.</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserInfoResponseDto>> Me(CancellationToken cancellationToken)
        => await CreateSession(cancellationToken);

    [HttpGet("debug")]
    public IActionResult Debug()
    {
        return Ok(new
        {
            authenticated = User.Identity?.IsAuthenticated ?? false,
            userId = User.FindFirst("sub")?.Value,
            claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private UserInfoResponseDto ExtractSupabaseIdentity() => new()
    {
        Uid = User.FindFirst("sub")?.Value ?? string.Empty,
        Email = User.FindFirst("email")?.Value ?? string.Empty,
    };

    private static UserInfoResponseDto ToDto(Entities.User user) => new()
    {
        Uid = user.SupabaseUid ?? string.Empty,
        Email = user.Email,
        FullName = user.FullName,
        Role = user.Role.Name,
    };
}
>>>>>>> 53b100e (refactor: migrate authentication from Firebase to Supabase and remove legacy agent scripts)
