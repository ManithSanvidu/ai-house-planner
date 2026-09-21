using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Services;

public interface IApplicationUserSyncService
{
    /// <summary>
    /// Idempotent login sync. New Firebase identities become Customers; existing application
    /// users keep their server-owned role.
    /// </summary>
    Task<User> SynchronizeAsync(UserInfoResponseDto firebaseUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Public registration is always Customer. Existing users are returned unchanged.
    /// </summary>
    Task<User> RegisterCustomerAsync(UserInfoResponseDto firebaseUser, string fullName, CancellationToken cancellationToken = default);
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
            // Update email if it changed in Firebase (e.g. email verified / changed)
            if (!string.IsNullOrWhiteSpace(firebaseUser.Email) && user.Email != firebaseUser.Email)
                user.Email = firebaseUser.Email;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return user;
        }

        // A new Google/Firebase identity is always a Customer. Staff profiles are created by Admin
        // before their first login and therefore take the existing-user path above.
        return await CreateCustomerAsync(firebaseUser, firebaseUser.Email, cancellationToken);
    }

    public async Task<User> RegisterCustomerAsync(
        UserInfoResponseDto firebaseUser,
        string fullName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(firebaseUser.Uid))
            throw new UnauthorizedAccessException("Firebase token did not contain a UID.");

        // Idempotency: if a user already exists for this Firebase UID, return them unchanged.
        // We intentionally do NOT update the role here — this prevents role-escalation attacks
        // via repeated registration calls.
        var existing = await db.Users.Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.FirebaseUid == firebaseUser.Uid, cancellationToken);
        if (existing is not null)
            return existing;

        return await CreateCustomerAsync(firebaseUser, fullName, cancellationToken);
    }

    private async Task<User> CreateCustomerAsync(
        UserInfoResponseDto firebaseUser,
        string fullName,
        CancellationToken cancellationToken)
    {
        var role = await EnsureRoleAsync("Customer", cancellationToken);

        var effectiveFullName = string.IsNullOrWhiteSpace(fullName) ? firebaseUser.Email : fullName;

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirebaseUid = firebaseUser.Uid,
            Email = firebaseUser.Email,
            FullName = effectiveFullName,
            RoleId = role.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        user.Role = role;
        return user;
    }

    /// <summary>Gets the named role, or creates it if it is somehow missing from the seed data.</summary>
    private async Task<Role> EnsureRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var role = await db.Roles.SingleOrDefaultAsync(x => x.Name == roleName, cancellationToken);
        if (role is null)
        {
            role = new Role { Name = roleName };
            db.Roles.Add(role);
            await db.SaveChangesAsync(cancellationToken);
        }
        return role;
    }
}
