using System.ComponentModel.DataAnnotations;
using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Services;

public sealed class StaffAccountException(string code, string message, Exception? inner = null)
    : Exception(message, inner)
{
    public string Code { get; } = code;
}

public interface IStaffAccountService
{
    Task<IReadOnlyList<StaffAccountDto>> ListAsync(string? role, CancellationToken cancellationToken = default);
    Task<StaffAccountDto> CreateAsync(CreateStaffRequestDto request, CancellationToken cancellationToken = default);
    Task<StaffAccountDto> UpdateAsync(Guid id, UpdateStaffRequestDto request,
        CancellationToken cancellationToken = default);
    Task<StaffAccountDto> SetDisabledAsync(Guid id, bool disabled, CancellationToken cancellationToken = default);
}

public sealed class StaffAccountService(
    ApplicationDbContext db,
    ISupabaseStaffAccountService supabase,
    ILogger<StaffAccountService> logger) : IStaffAccountService
{
    private static readonly string[] StaffRoles = ["Architect", "Constructor"];

    public async Task<IReadOnlyList<StaffAccountDto>> ListAsync(string? role, CancellationToken cancellationToken = default)
    {
        var canonicalRole = string.IsNullOrWhiteSpace(role) ? null : ValidateRole(role);
        var users = await db.Users.AsNoTracking().Include(x => x.Role)
            .Where(x => StaffRoles.Contains(x.Role.Name) && (canonicalRole == null || x.Role.Name == canonicalRole))
            .OrderBy(x => x.FullName).ToListAsync(cancellationToken);
        var results = new List<StaffAccountDto>(users.Count);
        foreach (var user in users)
        {
            var disabled = string.IsNullOrWhiteSpace(user.SupabaseUid)
                ? null
                : await supabase.IsDisabledAsync(user.SupabaseUid, cancellationToken);
            results.Add(ToDto(user, disabled));
        }
        return results;
    }

    public async Task<StaffAccountDto> CreateAsync(CreateStaffRequestDto request, CancellationToken cancellationToken = default)
    {
        var fullName = request.FullName.Trim();
        var email    = request.Email.Trim().ToLowerInvariant();
        var roleName = ValidateRole(request.Role);

        if (string.IsNullOrWhiteSpace(fullName)) throw Invalid("Full name is required.");
        if (!new EmailAddressAttribute().IsValid(email)) throw Invalid("A valid email address is required.");
        if (request.Password.Length < 6) throw Invalid("Password must be at least 6 characters.");

        // ── Orphan detection ─────────────────────────────────────────────────
        // Case A: public.Users row exists with this email.
        //   → This covers: a previous failed creation that left a public.Users row
        //     without a matching auth.users row (SupabaseUid pointing to nothing).
        //     We must never silently create another auth.users and stack up a second
        //     public.Users against the same email.
        var existingPublicUser = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email, cancellationToken);

        if (existingPublicUser is not null)
        {
            // If there is a public.Users row with no SupabaseUid it is definitely orphaned.
            if (string.IsNullOrWhiteSpace(existingPublicUser.SupabaseUid))
                throw new StaffAccountException(
                    "orphan_public_profile",
                    $"A public profile for '{email}' exists without a linked Supabase identity. " +
                    "Delete the orphan record from public.Users before retrying.");

            // public.Users row with a SupabaseUid — this is a normal duplicate.
            throw new StaffAccountException("duplicate_email", "An account with this email already exists.");
        }

        var role = await db.Roles.SingleOrDefaultAsync(x => x.Name == roleName, cancellationToken)
            ?? throw new StaffAccountException("role_not_configured", "The requested staff role is not configured.");

        // ── Create Supabase Auth user ────────────────────────────────────────
        // If this throws duplicate_email it means auth.users already has the email
        // but we have no matching public.Users row — that is an orphan auth identity.
        SupabaseStaffIdentity identity;
        try
        {
            identity = await supabase.CreateAsync(email, request.Password, fullName, cancellationToken);
        }
        catch (StaffAccountException ex) when (ex.Code == "duplicate_email")
        {
            // Case B: auth.users already has this email but public.Users does not.
            //   → Orphan auth identity. Throw a distinct code so the caller can surface
            //     a meaningful message and the admin can decide to clean up via
            //     the Supabase dashboard.
            throw new StaffAccountException(
                "orphan_auth_identity",
                $"A Supabase Auth identity for '{email}' already exists without a matching " +
                "application profile. Remove the orphan from Supabase Auth (Dashboard → Authentication → Users) " +
                "then retry, or contact support to link the existing identity.");
        }

        // ── Create public.Users row using the AUTHORITATIVE UID from Supabase ─
        // SupabaseUid MUST equal the id returned by auth.users — never Guid.NewGuid().
        try
        {
            if (await db.Users.AnyAsync(x => x.SupabaseUid == identity.Uid, cancellationToken))
                throw new StaffAccountException("duplicate_identity", "This sign-in account is already registered.");

            var user = new User
            {
                Id          = Guid.NewGuid(),       // application PK — may be any Guid
                SupabaseUid = identity.Uid,          // AUTHORITATIVE: from auth.users.id
                Email       = identity.Email,
                FullName    = fullName,
                RoleId      = role.Id,
                Role        = role,
                PasswordHash = null,
                CreatedAt   = DateTimeOffset.UtcNow,
                UpdatedAt   = DateTimeOffset.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "[Staff] Created staff account. Email: {Email}, SupabaseUid: {Uid}, Role: {Role}",
                user.Email, user.SupabaseUid, roleName);

            return ToDto(user, false);
        }
        catch (Exception ex)
        {
            // DB creation failed — roll back the Supabase Auth user so we don't leave an orphan.
            // Use CancellationToken.None: the original request may already be cancelled.
            try { await supabase.DeleteAsync(identity.Uid, CancellationToken.None); }
            catch (Exception rollbackError)
            {
                logger.LogError(rollbackError,
                    "[Staff] Could not roll back Supabase Auth user {Uid} after DB failure. " +
                    "Manual cleanup required in Supabase Dashboard.", identity.Uid);
            }
            if (ex is StaffAccountException) throw;
            logger.LogError(ex, "[Staff] DB profile creation failed for Supabase uid {Uid}.", identity.Uid);
            throw new StaffAccountException("profile_creation_failed", "The staff account could not be created.", ex);
        }
    }


    public async Task<StaffAccountDto> SetDisabledAsync(Guid id, bool disabled, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.Id == id && StaffRoles.Contains(x.Role.Name), cancellationToken)
            ?? throw new StaffAccountException("not_found", "Staff account not found.");
        if (string.IsNullOrWhiteSpace(user.SupabaseUid))
            throw new StaffAccountException("identity_missing", "This staff profile has no Supabase identity.");
        try { await supabase.SetDisabledAsync(user.SupabaseUid, disabled, cancellationToken); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not update Supabase status for staff user {UserId}", id);
            throw new StaffAccountException("status_update_failed", "The staff account status could not be updated.", ex);
        }
        return ToDto(user, disabled);
    }

    public async Task<StaffAccountDto> UpdateAsync(Guid id, UpdateStaffRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var fullName = request.FullName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        var roleName = ValidateRole(request.Role);
        if (string.IsNullOrWhiteSpace(fullName)) throw Invalid("Full name is required.");
        if (!new EmailAddressAttribute().IsValid(email)) throw Invalid("A valid email address is required.");

        var user = await db.Users.Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.Id == id && StaffRoles.Contains(x.Role.Name), cancellationToken)
            ?? throw new StaffAccountException("not_found", "Staff account not found.");
        if (string.IsNullOrWhiteSpace(user.SupabaseUid))
            throw new StaffAccountException("identity_missing", "This staff profile has no Supabase identity.");
        if (await db.Users.AnyAsync(x => x.Id != id && x.Email.ToLower() == email, cancellationToken))
            throw new StaffAccountException("duplicate_email", "An account with this email already exists.");
        var role = await db.Roles.SingleOrDefaultAsync(x => x.Name == roleName, cancellationToken)
            ?? throw new StaffAccountException("role_not_configured", "The requested staff role is not configured.");

        var originalEmail = user.Email;
        var originalName = user.FullName;
        await supabase.UpdateProfileAsync(user.SupabaseUid, email, fullName, cancellationToken);
        try
        {
            user.FullName = fullName;
            user.Email = email;
            user.RoleId = role.Id;
            user.Role = role;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            try
            {
                await supabase.UpdateProfileAsync(user.SupabaseUid, originalEmail, originalName, CancellationToken.None);
            }
            catch (Exception rollbackError)
            {
                logger.LogError(rollbackError, "Could not roll back Supabase profile for staff user {UserId}", id);
            }
            logger.LogError(ex, "Database profile update failed for staff user {UserId}", id);
            throw new StaffAccountException("profile_update_failed", "The staff account could not be updated.", ex);
        }

        var disabled = await supabase.IsDisabledAsync(user.SupabaseUid, cancellationToken);
        return ToDto(user, disabled);
    }

    private static string ValidateRole(string role)
        => StaffRoles.FirstOrDefault(x => string.Equals(x, role?.Trim(), StringComparison.OrdinalIgnoreCase))
           ?? throw Invalid("Role must be Architect or Constructor.");

    private static StaffAccountException Invalid(string message) => new("invalid_request", message);

    private static StaffAccountDto ToDto(User user, bool? disabled)
        => new(user.Id, user.FullName, user.Email, user.Role.Name,
            disabled is null ? "Unavailable" : disabled.Value ? "Disabled" : "Active");
}
