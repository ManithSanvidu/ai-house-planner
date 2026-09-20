using FirebaseAdmin.Auth;
using HousePlanner.API.DTOs;

namespace HousePlanner.API.Services
{
    public class FirebaseAuthService : IFirebaseAuthService
    {
        private readonly ILogger<FirebaseAuthService> _logger;

        public FirebaseAuthService(ILogger<FirebaseAuthService> logger)
        {
            _logger = logger;
        }

        public async Task<UserInfoResponseDto> VerifyTokenAsync(string token)
        {
            try
            {
                // Firebase Admin cryptographically verifies the signature, issuer, audience and expiry.
                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                var uid = decodedToken.Uid;
                var email = decodedToken.Claims.TryGetValue("email", out var emailObj)
                    ? emailObj?.ToString() ?? string.Empty
                    : string.Empty;
                if (string.IsNullOrWhiteSpace(uid))
                    throw new UnauthorizedAccessException("Firebase token did not contain a subject.");

                _logger.LogInformation("Verified Firebase token for UID {Uid}", uid);

                return new UserInfoResponseDto
                {
                    Uid = uid,
                    Email = email,
                    Role = string.Empty
                };
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogError(ex, "Firebase ID Token verification failed.");
                throw new UnauthorizedAccessException("Invalid Firebase ID Token.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during token verification.");
                throw;
            }
        }

    }
}
