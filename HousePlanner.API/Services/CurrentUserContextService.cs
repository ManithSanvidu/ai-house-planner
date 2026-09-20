using HousePlanner.API.Data;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Services;

public record CurrentUserContext(Guid? Id, string Email, string Role);

public interface ICurrentUserContextService
{
    Task<CurrentUserContext?> GetAsync(HttpContext context);
}

public class CurrentUserContextService : ICurrentUserContextService
{
    private readonly IFirebaseAuthService _firebase;
    private readonly ApplicationDbContext _db;
    public CurrentUserContextService(IFirebaseAuthService firebase, ApplicationDbContext db)
    { _firebase = firebase; _db = db; }

    public async Task<CurrentUserContext?> GetAsync(HttpContext context)
    {
        var header = context.Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;
        HousePlanner.API.DTOs.UserInfoResponseDto verified;
        try
        {
            verified = await _firebase.VerifyTokenAsync(header[7..].Trim());
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        if (verified is null || string.IsNullOrWhiteSpace(verified.Uid)) return null;
        var user = await _db.Users.AsNoTracking().Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.FirebaseUid == verified.Uid);
        if (user is null) return null;
        return new CurrentUserContext(user.Id, user.Email, user.Role?.Name ?? "Customer");
    }
}
