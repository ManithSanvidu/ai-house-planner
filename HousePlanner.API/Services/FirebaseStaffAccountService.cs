using FirebaseAdmin.Auth;

namespace HousePlanner.API.Services;

public sealed record FirebaseStaffIdentity(string Uid, string Email);

public interface IFirebaseStaffAccountService
{
    Task<FirebaseStaffIdentity> CreateAsync(string email, string password, string displayName,
        CancellationToken cancellationToken = default);
    Task DeleteAsync(string uid, CancellationToken cancellationToken = default);
    Task<bool?> IsDisabledAsync(string uid, CancellationToken cancellationToken = default);
    Task SetDisabledAsync(string uid, bool disabled, CancellationToken cancellationToken = default);
    Task UpdateProfileAsync(string uid, string email, string displayName,
        CancellationToken cancellationToken = default);
}

public sealed class FirebaseStaffAccountService : IFirebaseStaffAccountService
{
    public async Task<FirebaseStaffIdentity> CreateAsync(string email, string password, string displayName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var record = await FirebaseAuth.DefaultInstance.CreateUserAsync(new UserRecordArgs
            {
                Email = email,
                Password = password,
                DisplayName = displayName,
                Disabled = false,
                EmailVerified = false
            }, cancellationToken);
            return new FirebaseStaffIdentity(record.Uid, record.Email);
        }
        catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.EmailAlreadyExists)
        {
            throw new StaffAccountException("duplicate_email", "An account with this email already exists.", ex);
        }
        catch (FirebaseAuthException ex)
        {
            throw new StaffAccountException("firebase_creation_failed", "The staff sign-in account could not be created.", ex);
        }
    }

    public Task DeleteAsync(string uid, CancellationToken cancellationToken = default)
        => FirebaseAuth.DefaultInstance.DeleteUserAsync(uid, cancellationToken);

    public async Task<bool?> IsDisabledAsync(string uid, CancellationToken cancellationToken = default)
    {
        try { return (await FirebaseAuth.DefaultInstance.GetUserAsync(uid, cancellationToken)).Disabled; }
        catch (FirebaseAuthException) { return null; }
    }

    public async Task SetDisabledAsync(string uid, bool disabled, CancellationToken cancellationToken = default)
    {
        await FirebaseAuth.DefaultInstance.UpdateUserAsync(
            new UserRecordArgs { Uid = uid, Disabled = disabled }, cancellationToken);
        if (disabled)
            await FirebaseAuth.DefaultInstance.RevokeRefreshTokensAsync(uid, cancellationToken);
    }

    public async Task UpdateProfileAsync(string uid, string email, string displayName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await FirebaseAuth.DefaultInstance.UpdateUserAsync(new UserRecordArgs
            {
                Uid = uid,
                Email = email,
                DisplayName = displayName
            }, cancellationToken);
        }
        catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.EmailAlreadyExists)
        {
            throw new StaffAccountException("duplicate_email", "An account with this email already exists.", ex);
        }
        catch (FirebaseAuthException ex)
        {
            throw new StaffAccountException("firebase_update_failed", "The staff sign-in account could not be updated.", ex);
        }
    }
}
