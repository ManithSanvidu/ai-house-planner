using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HousePlanner.API.Tests.Services;

public sealed class StaffAccountServiceTests
{
    private static ApplicationDbContext Database(params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString());
        if (interceptors.Length > 0) options.AddInterceptors(interceptors);
        var db = new ApplicationDbContext(options.Options);
        db.Roles.AddRange(new Role { Id = 10, Name = "Architect" }, new Role { Id = 11, Name = "Constructor" },
            new Role { Id = 12, Name = "Customer" }, new Role { Id = 13, Name = "Admin" });
        db.SaveChanges();
        return db;
    }

    private static User AddStaff(ApplicationDbContext db, string role, string email = "staff@example.com")
    {
        var user = new User { Id = Guid.NewGuid(), Email = email, SupabaseUid = "staff-uid", PasswordHash = null,
            FullName = $"Original {role}", RoleId = role == "Architect" ? 10 : 11,
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        db.Users.Add(user); db.SaveChanges(); return user;
    }

    private static UpdateStaffRequestDto Update(string role, string email = "updated@example.com") => new()
        { FullName = "Updated Staff", Email = email, Role = role };

    private static CreateStaffRequestDto Request(string role) => new()
    { FullName = "Staff User", Email = $"{role.ToLowerInvariant()}@example.com", Password = "secret123", Role = role };

    // ── Staff creation contract tests ──────────────────────────────────────────

    /// <summary>CreateStaff_PassesEnteredPasswordToSupabase</summary>
    [Fact]
    public async Task CreateStaff_PassesEnteredPasswordToSupabase()
    {
        await using var db = Database();
        var supabase = new Mock<ISupabaseStaffAccountService>();
        supabase.Setup(x => x.CreateAsync("architect@example.com", "secret123", "Staff User", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupabaseStaffIdentity("uid-1", "architect@example.com"));

        await new StaffAccountService(db, supabase.Object, NullLogger<StaffAccountService>.Instance)
            .CreateAsync(Request("Architect"));

        // Verify password was forwarded exactly — no transformation, no substitution
        supabase.Verify(x => x.CreateAsync("architect@example.com", "secret123", "Staff User", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>CreateStaff_PassesEmailToSupabase</summary>
    [Fact]
    public async Task CreateStaff_PassesEmailToSupabase()
    {
        await using var db = Database();
        var supabase = new Mock<ISupabaseStaffAccountService>();
        supabase.Setup(x => x.CreateAsync("constructor@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupabaseStaffIdentity("uid-2", "constructor@example.com"));

        await new StaffAccountService(db, supabase.Object, NullLogger<StaffAccountService>.Instance)
            .CreateAsync(Request("Constructor"));

        supabase.Verify(x => x.CreateAsync("constructor@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>CreateStaff_SetsEmailConfirmedTrue — verified via Supabase service receiving the call (email_confirm=true is internal to the service impl)</summary>
    [Fact]
    public async Task CreateStaff_CallsSupabaseCreateOnce_NotPublicSignUp()
    {
        await using var db = Database();
        var supabase = new Mock<ISupabaseStaffAccountService>();
        supabase.Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupabaseStaffIdentity("uid-3", "architect@example.com"));

        await new StaffAccountService(db, supabase.Object, NullLogger<StaffAccountService>.Instance)
            .CreateAsync(Request("Architect"));

        // CreateAsync must be called exactly once; no other Supabase methods called during creation
        supabase.Verify(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        supabase.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>CreateStaff_UsesReturnedSupabaseUidForPublicUser</summary>
    [Fact]
    public async Task CreateStaff_UsesReturnedSupabaseUidForPublicUser()
    {
        await using var db = Database();
        const string expectedUid = "real-supabase-uid-from-response";
        var supabase = new Mock<ISupabaseStaffAccountService>();
        supabase.Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupabaseStaffIdentity(expectedUid, "architect@example.com"));

        await new StaffAccountService(db, supabase.Object, NullLogger<StaffAccountService>.Instance)
            .CreateAsync(Request("Architect"));

        var saved = await db.Users.SingleAsync();
        Assert.Equal(expectedUid, saved.SupabaseUid);
    }

    /// <summary>CreateStaff_RejectsMissingPassword</summary>
    [Theory]
    [InlineData("")]
    [InlineData("12345")] // too short
    public async Task CreateStaff_RejectsTooShortPassword(string pw)
    {
        await using var db = Database();
        var supabase = new Mock<ISupabaseStaffAccountService>();
        var req = new CreateStaffRequestDto { FullName = "Test", Email = "x@example.com", Password = pw, Role = "Architect" };

        var error = await Assert.ThrowsAsync<StaffAccountException>(() =>
            new StaffAccountService(db, supabase.Object, NullLogger<StaffAccountService>.Instance)
                .CreateAsync(req));

        Assert.Equal("invalid_request", error.Code);
        supabase.VerifyNoOtherCalls();
    }

    /// <summary>CreateStaff_SupabaseFailure_DoesNotCreateDatabaseUser</summary>
    [Fact]
    public async Task CreateStaff_SupabaseFailure_DoesNotCreateDatabaseUser()
    {
        await using var db = Database();
        var supabase = new Mock<ISupabaseStaffAccountService>();
        supabase.Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StaffAccountException("supabase_error", "Supabase is unavailable."));

        await Assert.ThrowsAsync<StaffAccountException>(() =>
            new StaffAccountService(db, supabase.Object, NullLogger<StaffAccountService>.Instance)
                .CreateAsync(Request("Architect")));

        Assert.Empty(db.Users);
    }

    /// <summary>CreateStaff_DatabaseFailure_RollsBackSupabaseUser</summary>
    [Fact]
    public async Task CreateStaff_DatabaseFailure_RollsBackSupabaseUser()
    {
        var failure = new FailNextSaveInterceptor();
        await using var db = Database(failure);
        failure.Armed = true;
        const string newUid = "uid-to-rollback";
        var supabase = new Mock<ISupabaseStaffAccountService>();
        supabase.Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupabaseStaffIdentity(newUid, "architect@example.com"));

        var error = await Assert.ThrowsAsync<StaffAccountException>(() =>
            new StaffAccountService(db, supabase.Object, NullLogger<StaffAccountService>.Instance)
                .CreateAsync(Request("Architect")));

        Assert.Equal("profile_creation_failed", error.Code);
        // The rollback delete must be called with the UID returned by Supabase
        supabase.Verify(x => x.DeleteAsync(newUid, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>CreateStaff_DuplicateEmail_DoesNotCreatePartialUser</summary>
    [Fact]
    public async Task CreateStaff_DuplicateEmail_DoesNotCreatePartialUser()
    {
        await using var db = Database();
        db.Users.Add(new User { Id = Guid.NewGuid(), Email = "architect@example.com", SupabaseUid = "existing",
            FullName = "Existing", RoleId = 10, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        var supabase = new Mock<ISupabaseStaffAccountService>();

        var error = await Assert.ThrowsAsync<StaffAccountException>(() =>
            new StaffAccountService(db, supabase.Object, NullLogger<StaffAccountService>.Instance)
                .CreateAsync(Request("Architect")));

        Assert.Equal("duplicate_email", error.Code);
        // Supabase must NOT be called — duplicate check happens before Supabase
        supabase.VerifyNoOtherCalls();
        Assert.Single(db.Users); // no partial record created
    }

    // ── Legacy tests (unchanged below) ────────────────────────────────────────

    [Theory]
    [InlineData("Architect", 10)]
    [InlineData("Constructor", 11)]
    public async Task AdminCreation_PersistsSupabaseUidAndCorrectRoleWithoutPassword(string role, int roleId)
    {
        await using var db = Database();
        var supabase = new Mock<ISupabaseStaffAccountService>();
        supabase.Setup(x => x.CreateAsync(It.IsAny<string>(), "secret123", "Staff User", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupabaseStaffIdentity($"supabase-{role}", $"{role.ToLowerInvariant()}@example.com"));
        var result = await new StaffAccountService(db, supabase.Object, NullLogger<StaffAccountService>.Instance)
            .CreateAsync(Request(role));

        var saved = await db.Users.SingleAsync();
        Assert.Equal($"supabase-{role}", saved.SupabaseUid);
        Assert.Equal(roleId, saved.RoleId);
        Assert.Equal(role, result.Role);
        Assert.Null(saved.PasswordHash);
    }

    [Theory]
    [InlineData("Customer")]
    [InlineData("Admin")]
    [InlineData("Unknown")]
    public async Task StaffEndpointRoles_RejectNonStaffRolesBeforeSupabase(string role)
    {
        await using var db = Database();
        var supabase = new Mock<ISupabaseStaffAccountService>();
        var error = await Assert.ThrowsAsync<StaffAccountException>(() =>
            new StaffAccountService(db, supabase.Object, NullLogger<StaffAccountService>.Instance)
                .CreateAsync(Request(role)));
        Assert.Equal("invalid_request", error.Code);
        supabase.Verify(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DuplicateDatabaseEmail_IsRejectedBeforeSupabaseCreation()
    {
        await using var db = Database();
        db.Users.Add(new User { Id = Guid.NewGuid(), Email = "architect@example.com", SupabaseUid = "existing",
            FullName = "Existing", RoleId = 10, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        var supabase = new Mock<ISupabaseStaffAccountService>();

        var error = await Assert.ThrowsAsync<StaffAccountException>(() =>
            new StaffAccountService(db, supabase.Object, NullLogger<StaffAccountService>.Instance)
                .CreateAsync(Request("Architect")));
        Assert.Equal("duplicate_email", error.Code);
        supabase.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProfileConflict_RollsBackOnlyNewSupabaseIdentity()
    {
        await using var db = Database();
        db.Users.Add(new User { Id = Guid.NewGuid(), Email = "different@example.com", SupabaseUid = "duplicate-uid",
            FullName = "Existing", RoleId = 10, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        var supabase = new Mock<ISupabaseStaffAccountService>();
        supabase.Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupabaseStaffIdentity("duplicate-uid", "architect@example.com"));

        await Assert.ThrowsAsync<StaffAccountException>(() =>
            new StaffAccountService(db, supabase.Object, NullLogger<StaffAccountService>.Instance)
                .CreateAsync(Request("Architect")));
        supabase.Verify(x => x.DeleteAsync("duplicate-uid", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Single(db.Users);
    }

    [Fact]
    public async Task ExistingStaffLoginSync_DoesNotOverwriteRole()
    {
        await using var db = Database();
        db.Users.Add(new User { Id = Guid.NewGuid(), Email = "architect@example.com", SupabaseUid = "staff-uid",
            FullName = "Architect", RoleId = 10, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        var user = await new SupabaseUserSyncService(db).SynchronizeAsync(
            new UserInfoResponseDto { Uid = "staff-uid", Email = "architect@example.com", Role = "Customer" });
        Assert.Equal("Architect", user.Role.Name);
    }

    [Theory]
    [InlineData("Architect", "Architect")]
    [InlineData("Constructor", "Constructor")]
    [InlineData("Architect", "Constructor")]
    [InlineData("Constructor", "Architect")]
    public async Task UpdateStaff_ChangesSafeProfileFieldsAndKeepsIdentity(string originalRole, string newRole)
    {
        await using var db = Database();
        var original = AddStaff(db, originalRole);
        var supabase = new Mock<ISupabaseStaffAccountService>();
        supabase.Setup(x => x.IsDisabledAsync("staff-uid", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await new StaffAccountService(db, supabase.Object, NullLogger<StaffAccountService>.Instance)
            .UpdateAsync(original.Id, Update(newRole));

        var saved = await db.Users.AsNoTracking().SingleAsync(x => x.Id == original.Id);
        Assert.Equal("Updated Staff", saved.FullName);
        Assert.Equal("updated@example.com", saved.Email);
        Assert.Equal(newRole == "Architect" ? 10 : 11, saved.RoleId);
        Assert.Equal("staff-uid", saved.SupabaseUid);
        Assert.Null(saved.PasswordHash);
        Assert.Equal(newRole, result.Role);
        supabase.Verify(x => x.UpdateProfileAsync("staff-uid", "updated@example.com", "Updated Staff",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Customer")]
    public async Task UpdateStaff_RejectsNonStaffRoleBeforeSupabase(string role)
    {
        await using var db = Database(); var staff = AddStaff(db, "Architect");
        var supabase = new Mock<ISupabaseStaffAccountService>();
        var error = await Assert.ThrowsAsync<StaffAccountException>(() =>
            new StaffAccountService(db, supabase.Object, NullLogger<StaffAccountService>.Instance)
                .UpdateAsync(staff.Id, Update(role)));
        Assert.Equal("invalid_request", error.Code);
        supabase.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateStaff_RejectsDuplicateDatabaseEmailBeforeSupabase()
    {
        await using var db = Database(); var staff = AddStaff(db, "Architect");
        db.Users.Add(new User { Id = Guid.NewGuid(), Email = "taken@example.com", SupabaseUid = "other-uid",
            FullName = "Other", RoleId = 11, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        var supabase = new Mock<ISupabaseStaffAccountService>();
        var error = await Assert.ThrowsAsync<StaffAccountException>(() =>
            new StaffAccountService(db, supabase.Object, NullLogger<StaffAccountService>.Instance)
                .UpdateAsync(staff.Id, Update("Architect", "taken@example.com")));
        Assert.Equal("duplicate_email", error.Code);
        supabase.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateStaff_PropagatesSafeDuplicateSupabaseEmailError()
    {
        await using var db = Database(); var staff = AddStaff(db, "Architect");
        var supabase = new Mock<ISupabaseStaffAccountService>();
        supabase.Setup(x => x.UpdateProfileAsync("staff-uid", It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StaffAccountException("duplicate_email", "An account with this email already exists."));
        var error = await Assert.ThrowsAsync<StaffAccountException>(() =>
            new StaffAccountService(db, supabase.Object, NullLogger<StaffAccountService>.Instance)
                .UpdateAsync(staff.Id, Update("Architect")));
        Assert.Equal("duplicate_email", error.Code);
        Assert.Equal("staff@example.com", (await db.Users.AsNoTracking().SingleAsync()).Email);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    public async Task UpdateStaff_RejectsInvalidEmail(string email)
    {
        await using var db = Database(); var staff = AddStaff(db, "Architect");
        var supabase = new Mock<ISupabaseStaffAccountService>();
        var error = await Assert.ThrowsAsync<StaffAccountException>(() =>
            new StaffAccountService(db, supabase.Object, NullLogger<StaffAccountService>.Instance)
                .UpdateAsync(staff.Id, Update("Architect", email)));
        Assert.Equal("invalid_request", error.Code);
        supabase.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateStaff_PreservesDisabledState()
    {
        await using var db = Database(); var staff = AddStaff(db, "Constructor");
        var supabase = new Mock<ISupabaseStaffAccountService>();
        supabase.Setup(x => x.IsDisabledAsync("staff-uid", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var result = await new StaffAccountService(db, supabase.Object, NullLogger<StaffAccountService>.Instance)
            .UpdateAsync(staff.Id, Update("Constructor"));
        Assert.Equal("Disabled", result.Status);
        supabase.Verify(x => x.SetDisabledAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStaff_RollsBackSupabaseWhenDatabaseSaveFails()
    {
        var failure = new FailNextSaveInterceptor();
        await using var db = Database(failure); var staff = AddStaff(db, "Architect");
        failure.Armed = true;
        var supabase = new Mock<ISupabaseStaffAccountService>();
        var error = await Assert.ThrowsAsync<StaffAccountException>(() =>
            new StaffAccountService(db, supabase.Object, NullLogger<StaffAccountService>.Instance)
                .UpdateAsync(staff.Id, Update("Constructor")));
        Assert.Equal("profile_update_failed", error.Code);
        supabase.Verify(x => x.UpdateProfileAsync("staff-uid", "staff@example.com", "Original Architect",
            CancellationToken.None), Times.Once);
    }

    private sealed class FailNextSaveInterceptor : SaveChangesInterceptor
    {
        public bool Armed { get; set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => Armed
                ? ValueTask.FromException<InterceptionResult<int>>(new DbUpdateException("Simulated failure"))
                : base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    // ── Consistency invariant tests ────────────────────────────────────────────

    /// <summary>
    /// CreateStaff_PublicSupabaseUidEqualsReturnedAuthUserId
    /// The SupabaseUid stored in public.Users MUST equal the id returned by Supabase —
    /// never a locally-generated Guid.
    /// </summary>
    [Fact]
    public async Task CreateStaff_PublicSupabaseUidEqualsReturnedAuthUserId()
    {
        await using var db = Database();
        const string authUid = "authoritative-uid-from-supabase-auth";
        var supabase = new Mock<ISupabaseStaffAccountService>();
        supabase.Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupabaseStaffIdentity(authUid, "architect@example.com"));

        await new StaffAccountService(db, supabase.Object, NullLogger<StaffAccountService>.Instance)
            .CreateAsync(Request("Architect"));

        var saved = await db.Users.SingleAsync();
        // The invariant: public.Users.SupabaseUid == auth.users.id (from response)
        Assert.Equal(authUid, saved.SupabaseUid);
        // Sanity: application PK is different — it's a Guid but not the auth uid
        Assert.NotEqual(saved.Id.ToString(), authUid);
    }

    /// <summary>
    /// CreateStaff_AuthFailureLeavesNoPublicUser
    /// If Supabase Auth creation fails, no public.Users record must be inserted.
    /// </summary>
    [Fact]
    public async Task CreateStaff_AuthFailureLeavesNoPublicUser()
    {
        await using var db = Database();
        var supabase = new Mock<ISupabaseStaffAccountService>();
        supabase.Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StaffAccountException("supabase_error", "Network timeout."));

        await Assert.ThrowsAsync<StaffAccountException>(() =>
            new StaffAccountService(db, supabase.Object, NullLogger<StaffAccountService>.Instance)
                .CreateAsync(Request("Architect")));

        Assert.Empty(db.Users); // zero partial records
    }

    /// <summary>
    /// CreateStaff_DbFailureDeletesNewAuthUser
    /// If Supabase Auth succeeds but DB save fails, the new auth user must be deleted.
    /// </summary>
    [Fact]
    public async Task CreateStaff_DbFailureDeletesNewAuthUser()
    {
        var failure = new FailNextSaveInterceptor();
        await using var db = Database(failure);
        failure.Armed = true;

        const string newAuthUid = "uid-from-supabase-to-roll-back";
        var supabase = new Mock<ISupabaseStaffAccountService>();
        supabase.Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupabaseStaffIdentity(newAuthUid, "architect@example.com"));

        var ex = await Assert.ThrowsAsync<StaffAccountException>(() =>
            new StaffAccountService(db, supabase.Object, NullLogger<StaffAccountService>.Instance)
                .CreateAsync(Request("Architect")));

        Assert.Equal("profile_creation_failed", ex.Code);
        // The rollback must target the exact UID returned by Supabase
        supabase.Verify(x => x.DeleteAsync(newAuthUid, CancellationToken.None), Times.Once);
        Assert.Empty(db.Users); // DB was rolled back by the interceptor before the save
    }

    /// <summary>
    /// CreateStaff_DetectsOrphanPublicProfile
    /// If a public.Users row with this email exists but has no SupabaseUid,
    /// return a distinct orphan_public_profile error without touching Supabase.
    /// </summary>
    [Fact]
    public async Task CreateStaff_DetectsOrphanPublicProfile()
    {
        await using var db = Database();
        // Seed an orphaned public.Users row: email present, no SupabaseUid
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(), Email = "architect@example.com",
            SupabaseUid = null!,  // deliberately orphaned
            FullName = "Orphan", RoleId = 10,
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var supabase = new Mock<ISupabaseStaffAccountService>();

        var error = await Assert.ThrowsAsync<StaffAccountException>(() =>
            new StaffAccountService(db, supabase.Object, NullLogger<StaffAccountService>.Instance)
                .CreateAsync(Request("Architect")));

        Assert.Equal("orphan_public_profile", error.Code);
        supabase.VerifyNoOtherCalls(); // Supabase must NOT be called
    }

    /// <summary>
    /// CreateStaff_DetectsOrphanAuthIdentity
    /// If auth.users already has the email (Supabase returns duplicate_email)
    /// but public.Users does NOT exist, return orphan_auth_identity — not duplicate_email.
    /// </summary>
    [Fact]
    public async Task CreateStaff_DetectsOrphanAuthIdentity()
    {
        await using var db = Database(); // empty — no public.Users record
        var supabase = new Mock<ISupabaseStaffAccountService>();
        // Supabase reports the email is already taken in auth.users
        supabase.Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StaffAccountException("duplicate_email", "Email already registered in Supabase."));

        var error = await Assert.ThrowsAsync<StaffAccountException>(() =>
            new StaffAccountService(db, supabase.Object, NullLogger<StaffAccountService>.Instance)
                .CreateAsync(Request("Architect")));

        // Must be the orphan identity code, NOT duplicate_email
        Assert.Equal("orphan_auth_identity", error.Code);
        // No public.Users record created
        Assert.Empty(db.Users);
    }
}
