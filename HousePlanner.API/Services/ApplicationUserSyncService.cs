using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Services;

public interface IApplicationUserSyncService
{
    Task<User> SynchronizeAsync(UserInfoResponseDto firebaseUser, CancellationToken cancellationToken = default);
}

/// <summary>Owns the idempotent Firebase UID to application-user mapping.</summary>
public sealed class ApplicationUserSyncService(ApplicationDbContext db) : IApplicationUserSyncService
{
    public async Task<User> SynchronizeAsync(UserInfoResponseDto firebaseUser, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(firebaseUser.Uid))
            throw new UnauthorizedAccessException("Firebase token did not contain a UID.");

        var user = await db.Users.Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.FirebaseUid == firebaseUser.Uid, cancellationToken);
        if (user is not null)
        {
            if (!string.IsNullOrWhiteSpace(firebaseUser.Email) && user.Email != firebaseUser.Email)
                user.Email = firebaseUser.Email;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return user;
        }

        var customerRole = await db.Roles.SingleOrDefaultAsync(x => x.Name == "Customer", cancellationToken);
        if (customerRole is null)
        {
            customerRole = new Role { Name = "Customer" };
            db.Roles.Add(customerRole);
            await db.SaveChangesAsync(cancellationToken);
        }
        user = new User
        {
            Id = Guid.NewGuid(), FirebaseUid = firebaseUser.Uid, Email = firebaseUser.Email,
            FullName = string.IsNullOrWhiteSpace(firebaseUser.Email) ? "Firebase User" : firebaseUser.Email,
            RoleId = customerRole.Id, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        user.Role = customerRole;
        return user;
    }
}
