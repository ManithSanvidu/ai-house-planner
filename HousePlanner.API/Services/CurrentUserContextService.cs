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
        var verified = await _firebase.VerifyTokenAsync(header[7..].Trim());
        if (verified is null || string.IsNullOrWhiteSpace(verified.Email)) return null;
        var user = await _db.Users.AsNoTracking().Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email == verified.Email);
            
        if (user == null && header[7..].Trim() == "mock_token")
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Architect");
            if (role == null)
            {
                role = new HousePlanner.API.Entities.Role { Name = "Architect" };
                _db.Roles.Add(role);
                await _db.SaveChangesAsync(); // Save role first to generate its ID
            }

            user = new HousePlanner.API.Entities.User
            {
                Id = Guid.NewGuid(),
                Email = verified.Email,
                FullName = "Mock Architect",
                PasswordHash = "MOCK",
                RoleId = role.Id,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        return new CurrentUserContext(user?.Id, verified.Email, user?.Role?.Name ?? verified.Role ?? "User");
    }
}
