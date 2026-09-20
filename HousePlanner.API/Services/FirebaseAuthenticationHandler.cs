using System.Security.Claims;
using HousePlanner.API.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HousePlanner.API.Services;

/// <summary>Turns a cryptographically verified Firebase ID token into an ASP.NET principal.</summary>
public sealed class FirebaseAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Firebase";
    private readonly IFirebaseAuthService _firebase;
    private readonly ApplicationDbContext _db;

    public FirebaseAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder,
        IFirebaseAuthService firebase, ApplicationDbContext db)
        : base(options, logger, encoder)
    { _firebase = firebase; _db = db; }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogDebug("Firebase authentication rejected: authorizationHeader={Present}, bearerScheme={Bearer}",
                !string.IsNullOrWhiteSpace(header), false);
            return AuthenticateResult.NoResult();
        }

        var token = header[7..].Trim();
        var segmentCount = string.IsNullOrWhiteSpace(token) ? 0 : token.Split('.').Length;
        if (segmentCount != 3)
        {
            Logger.LogWarning("Firebase authentication rejected malformed bearer token: jwtSegmentCount={SegmentCount}", segmentCount);
            return AuthenticateResult.Fail("Malformed Firebase ID token.");
        }

        try
        {
            // Pass only the raw compact JWT; never the Authorization scheme.
            var identity = await _firebase.VerifyTokenAsync(token);
            var role = await _db.Users.AsNoTracking().Where(u => u.FirebaseUid == identity.Uid)
                .Select(u => u.Role.Name).SingleOrDefaultAsync();
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, identity.Uid),
                new(ClaimTypes.Email, identity.Email)
            };
            if (!string.IsNullOrWhiteSpace(role)) claims.Add(new Claim(ClaimTypes.Role, role));
            return AuthenticateResult.Success(new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName)), SchemeName));
        }
        catch (UnauthorizedAccessException)
        {
            return AuthenticateResult.Fail("Invalid Firebase ID token.");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Firebase authentication failed.");
            return AuthenticateResult.Fail("Invalid Firebase ID token.");
        }
    }
}
