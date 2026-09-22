using System.Security.Claims;
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
    private readonly ApplicationDbContext _db;
    public CurrentUserContextService(ApplicationDbContext db)
    { _db = db; }

    public async Task<CurrentUserContext?> GetAsync(HttpContext context)
    {
        var userClaims = context.User;
        if (userClaims?.Identity?.IsAuthenticated != true) return null;

        var uid = userClaims.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(uid)) return null;

        var user = await _db.Users.AsNoTracking().Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.SupabaseUid == uid);
        
        if (user is null) return null;
        
        return new CurrentUserContext(user.Id, user.Email, user.Role?.Name ?? "Customer");
    }
}
