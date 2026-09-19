using System.Text.Json;
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
                string uid = string.Empty;
                string email = string.Empty;

                if (token == "mock_token")
                {
                    _logger.LogInformation("Using local mock_token bypass for Architect.");
                    return new UserInfoResponseDto
                    {
                        Uid = "mock-arch",
                        Email = "architect@homeplanner.com",
                        Role = "Architect"
                    };
                }
                
                if (token == "mock_constructor")
                {
                    _logger.LogInformation("Using local mock_token bypass for Constructor.");
                    return new UserInfoResponseDto
                    {
                        Uid = "mock-const",
                        Email = "constructor@homeplanner.com",
                        Role = "Constructor"
                    };
                }

                // 1. If Firebase Admin is initialized with credentials, perform real cryptographic verification
                if (FirebaseAuth.DefaultInstance != null)
                {
                    FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                    uid = decodedToken.Uid;
                    
                    if (decodedToken.Claims.TryGetValue("email", out var emailObj) && emailObj != null)
                    {
                        email = emailObj.ToString() ?? string.Empty;
                    }
                }
                else
                {
                    // Fallback for local development when firebase-service-account.json is absent
                    _logger.LogWarning("FirebaseAdmin.FirebaseAuth.DefaultInstance is null. Decoding JWT payload directly for local development.");
                    var decoded = DecodeJwtPayloadWithoutValidation(token);
                    uid = decoded.uid;
                    email = decoded.email;

                    if (string.IsNullOrEmpty(uid))
                    {
                        throw new UnauthorizedAccessException("Could not extract UID from token payload.");
                    }
                }

                // 2. Mock Role Mapping
                string role = "User";
                if (!string.IsNullOrEmpty(email))
                {
                    if (email.Contains("architect", StringComparison.OrdinalIgnoreCase))
                        role = "Architect";
                    else if (email.Contains("constructor", StringComparison.OrdinalIgnoreCase))
                        role = "Constructor";
                    else if (email.Contains("contractor", StringComparison.OrdinalIgnoreCase))
                        role = "Contractor";
                    else if (email.Contains("admin", StringComparison.OrdinalIgnoreCase))
                        role = "Admin";
                }

                _logger.LogInformation("Successfully verified token for User UID: {Uid}, Email: {Email}, Mapped Role: {Role}", uid, email, role);

                return new UserInfoResponseDto
                {
                    Uid = uid,
                    Email = email,
                    Role = role
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

        private static (string uid, string email) DecodeJwtPayloadWithoutValidation(string token)
        {
            try
            {
                var parts = token.Split('.');
                if (parts.Length < 2) return (string.Empty, string.Empty);

                var base64 = parts[1].Replace('-', '+').Replace('_', '/');
                switch (base64.Length % 4)
                {
                    case 2: base64 += "=="; break;
                    case 3: base64 += "="; break;
                }
                var jsonBytes = Convert.FromBase64String(base64);
                using var doc = JsonDocument.Parse(jsonBytes);
                var root = doc.RootElement;

                string uid = string.Empty;
                if (root.TryGetProperty("user_id", out var uidProp) || root.TryGetProperty("sub", out uidProp))
                {
                    uid = uidProp.GetString() ?? string.Empty;
                }

                string email = string.Empty;
                if (root.TryGetProperty("email", out var emailProp))
                {
                    email = emailProp.GetString() ?? string.Empty;
                }

                return (uid, email);
            }
            catch
            {
                return (string.Empty, string.Empty);
            }
        }
    }
}
