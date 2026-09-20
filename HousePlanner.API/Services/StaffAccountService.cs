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
    IFirebaseStaffAccountService firebase,
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
            var disabled = string.IsNullOrWhiteSpace(user.FirebaseUid)
                ? null
                : await firebase.IsDisabledAsync(user.FirebaseUid, cancellationToken);
            results.Add(ToDto(user, disabled));
        }
        return results;
    }

    public async Task<StaffAccountDto> CreateAsync(CreateStaffRequestDto request, CancellationToken cancellationToken = default)
    {
        var fullName = request.FullName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        var roleName = ValidateRole(request.Role);
        if (string.IsNullOrWhiteSpace(fullName)) throw Invalid("Full name is required.");
        if (!new EmailAddressAttribute().IsValid(email)) throw Invalid("A valid email address is required.");
        if (request.Password.Length < 6) throw Invalid("Password must be at least 6 characters.");
        if (await db.Users.AnyAsync(x => x.Email.ToLower() == email, cancellationToken))
            throw new StaffAccountException("duplicate_email", "An account with this email already exists.");

        var role = await db.Roles.SingleOrDefaultAsync(x => x.Name == roleName, cancellationToken)
            ?? throw new StaffAccountException("role_not_configured", "The requested staff role is not configured.");

        FirebaseStaffIdentity identity = await firebase.CreateAsync(email, request.Password, fullName, cancellationToken);
        try
        {
            if (await db.Users.AnyAsync(x => x.FirebaseUid == identity.Uid, cancellationToken))
                throw new StaffAccountException("duplicate_identity", "This sign-in account is already registered.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                FirebaseUid = identity.Uid,
                Email = identity.Email,
                FullName = fullName,
                RoleId = role.Id,
                Role = role,
                PasswordHash = null,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
            return ToDto(user, false);
        }
        catch (Exception ex)
        {
            // Rollback must still run if the client disconnected or canceled the original request.
            try { await firebase.DeleteAsync(identity.Uid, CancellationToken.None); }
            catch (Exception rollbackError)
            {
                logger.LogError(rollbackError,
                    "Could not roll back newly-created Firebase staff identity {FirebaseUid}", identity.Uid);
            }
            if (ex is StaffAccountException) throw;
            logger.LogError(ex, "Database profile creation failed for a newly-created staff identity.");
            throw new StaffAccountException("profile_creation_failed", "The staff account could not be created.", ex);
        }
    }

    public async Task<StaffAccountDto> SetDisabledAsync(Guid id, bool disabled, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.Id == id && StaffRoles.Contains(x.Role.Name), cancellationToken)
            ?? throw new StaffAccountException("not_found", "Staff account not found.");
        if (string.IsNullOrWhiteSpace(user.FirebaseUid))
            throw new StaffAccountException("identity_missing", "This staff profile has no Firebase identity.");
        try { await firebase.SetDisabledAsync(user.FirebaseUid, disabled, cancellationToken); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not update Firebase status for staff user {UserId}", id);
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
        if (string.IsNullOrWhiteSpace(user.FirebaseUid))
            throw new StaffAccountException("identity_missing", "This staff profile has no Firebase identity.");
        if (await db.Users.AnyAsync(x => x.Id != id && x.Email.ToLower() == email, cancellationToken))
            throw new StaffAccountException("duplicate_email", "An account with this email already exists.");
        var role = await db.Roles.SingleOrDefaultAsync(x => x.Name == roleName, cancellationToken)
            ?? throw new StaffAccountException("role_not_configured", "The requested staff role is not configured.");

        var originalEmail = user.Email;
        var originalName = user.FullName;
        await firebase.UpdateProfileAsync(user.FirebaseUid, email, fullName, cancellationToken);
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
                await firebase.UpdateProfileAsync(user.FirebaseUid, originalEmail, originalName, CancellationToken.None);
            }
            catch (Exception rollbackError)
            {
                logger.LogError(rollbackError, "Could not roll back Firebase profile for staff user {UserId}", id);
            }
            logger.LogError(ex, "Database profile update failed for staff user {UserId}", id);
            throw new StaffAccountException("profile_update_failed", "The staff account could not be updated.", ex);
        }

        var disabled = await firebase.IsDisabledAsync(user.FirebaseUid, cancellationToken);
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
