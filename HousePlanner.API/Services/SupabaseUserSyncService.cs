using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Services;

public interface ISupabaseUserSyncService
{
    /// <summary>
    /// Idempotent login sync. New Supabase identities become Customers; existing application
    /// users keep their server-owned role.
    /// </summary>
    Task<User> SynchronizeAsync(UserInfoResponseDto supabaseUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Public registration is always Customer. Existing users are returned unchanged.
    /// </summary>
    Task<User> RegisterCustomerAsync(UserInfoResponseDto supabaseUser, string fullName, CancellationToken cancellationToken = default);
}

/// <summary>Owns the idempotent Supabase UID to application-user mapping.</summary>
public sealed class SupabaseUserSyncService(ApplicationDbContext db) : ISupabaseUserSyncService
{
    public async Task<User> SynchronizeAsync(UserInfoResponseDto supabaseUser, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(supabaseUser.Uid))
            throw new UnauthorizedAccessException("Supabase token did not contain a UID.");

        var user = await db.Users.Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.SupabaseUid == supabaseUser.Uid, cancellationToken);
        if (user is not null)
        {
            // Update email if it changed in Supabase (e.g. email verified / changed)
            if (!string.IsNullOrWhiteSpace(supabaseUser.Email) && user.Email != supabaseUser.Email)
                user.Email = supabaseUser.Email;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return user;
        }

        // A valid Supabase JWT without a public.Users profile means the user needs to complete registration.
        throw new Exceptions.UserNotRegisteredException("Application user profile not found. Please complete registration.");
    }

    public async Task<User> RegisterCustomerAsync(
        UserInfoResponseDto supabaseUser,
        string fullName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(supabaseUser.Uid))
            throw new UnauthorizedAccessException("Supabase token did not contain a UID.");

        // Idempotency: if a user already exists for this Supabase UID, return them unchanged.
        // We intentionally do NOT update the role here — this prevents role-escalation attacks
        // via repeated registration calls.
        var existing = await db.Users.Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.SupabaseUid == supabaseUser.Uid || x.Email.ToLower() == supabaseUser.Email.ToLower(), cancellationToken);
        
        if (existing is not null)
        {
            if (existing.SupabaseUid == supabaseUser.Uid)
                return existing;
            
            throw new InvalidOperationException("An account with this email already exists.");
        }

        return await CreateCustomerAsync(supabaseUser, fullName, cancellationToken);
    }

    private async Task<User> CreateCustomerAsync(
        UserInfoResponseDto supabaseUser,
        string fullName,
        CancellationToken cancellationToken)
    {
        var role = await EnsureRoleAsync("Customer", cancellationToken);

        var effectiveFullName = string.IsNullOrWhiteSpace(fullName) ? supabaseUser.Email : fullName;

        var user = new User
        {
            Id = Guid.NewGuid(),
            SupabaseUid = supabaseUser.Uid,
            Email = supabaseUser.Email,
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
