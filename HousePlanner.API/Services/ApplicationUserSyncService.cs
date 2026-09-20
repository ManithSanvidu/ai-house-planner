using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Services;

public interface IApplicationUserSyncService
{
    /// <summary>
    /// Idempotent login sync: finds or creates an application user for a verified Firebase identity,
    /// always assigning the "Customer" role to new Google-auth users. Never changes the role
    /// of an existing user.
    /// </summary>
    Task<User> SynchronizeAsync(UserInfoResponseDto firebaseUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Idempotent registration: creates a new application user with the explicitly requested role,
    /// validated against the public-registration allowlist.
    /// If a user with this FirebaseUid already exists, the existing record is returned unchanged
    /// (role is NOT updated — prevents role-escalation via repeated registration calls).
    /// </summary>
    Task<User> RegisterAsync(UserInfoResponseDto firebaseUser, string fullName, string requestedRole, CancellationToken cancellationToken = default);
}

/// <summary>Owns the idempotent Firebase UID to application-user mapping.</summary>
public sealed class ApplicationUserSyncService(ApplicationDbContext db) : IApplicationUserSyncService
{
    /// <summary>
    /// Roles that can be self-assigned through the public registration endpoint.
    /// Admin, User, and any other privileged roles are NOT in this list.
    /// </summary>
    private static readonly HashSet<string> PublicRegistrationRoles =
        new(StringComparer.OrdinalIgnoreCase) { "Customer", "Architect" };

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

        // New identity via Google Auth — default to Customer role
        var customerRole = await EnsureRoleAsync("Customer", cancellationToken);
        user = new User
        {
            Id = Guid.NewGuid(),
            FirebaseUid = firebaseUser.Uid,
            Email = firebaseUser.Email,
            FullName = string.IsNullOrWhiteSpace(firebaseUser.Email) ? "Firebase User" : firebaseUser.Email,
            RoleId = customerRole.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        user.Role = customerRole;
        return user;
    }

    public async Task<User> RegisterAsync(
        UserInfoResponseDto firebaseUser,
        string fullName,
        string requestedRole,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(firebaseUser.Uid))
            throw new UnauthorizedAccessException("Firebase token did not contain a UID.");

        // Security: reject any role that is not on the public registration allowlist
        if (!PublicRegistrationRoles.Contains(requestedRole))
            throw new InvalidOperationException(
                $"Role '{requestedRole}' is not available for public registration.");

        // Idempotency: if a user already exists for this Firebase UID, return them unchanged.
        // We intentionally do NOT update the role here — this prevents role-escalation attacks
        // via repeated registration calls.
        var existing = await db.Users.Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.FirebaseUid == firebaseUser.Uid, cancellationToken);
        if (existing is not null)
            return existing;

        var role = await EnsureRoleAsync(requestedRole, cancellationToken);

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
