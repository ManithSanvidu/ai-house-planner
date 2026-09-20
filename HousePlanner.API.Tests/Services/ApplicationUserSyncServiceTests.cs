using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using HousePlanner.API.Exceptions;
using HousePlanner.API.Services;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Tests.Services;

public sealed class ApplicationUserSyncServiceTests
{
    private static ApplicationDbContext Database()
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    // ── SynchronizeAsync (login / Google auth) ──────────────────────────────

    [Fact]
    public async Task NewFirebaseUser_ThrowsUserNotRegisteredException_DoesNotAutoCreateUser()
    {
        await using var db = Database();
        var service = new ApplicationUserSyncService(db);
        
        await Assert.ThrowsAsync<UserNotRegisteredException>(() =>
            service.SynchronizeAsync(new UserInfoResponseDto { Uid = "firebase-123", Email = "customer@example.com" }));

        Assert.Empty(db.Users);
    }

    [Fact]
    public async Task ExistingFirebaseUser_IsUpdatedWithoutDuplicate_AndKeepsServerRole()
    {
        await using var db = Database();
        var architect = new Role { Id = 8, Name = "Architect" };
        db.Roles.Add(architect);
        db.Users.Add(new User { Id = Guid.NewGuid(), FirebaseUid = "firebase-123", Email = "old@example.com", FullName = "Existing", RoleId = 8, Role = architect, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var user = await new ApplicationUserSyncService(db).SynchronizeAsync(
            new UserInfoResponseDto { Uid = "firebase-123", Email = "new@example.com", Role = "Admin" });

        Assert.Single(db.Users);
        Assert.Equal("new@example.com", user.Email);
        // Role must not change — server is authority
        Assert.Equal("Architect", user.Role.Name);
    }

    // ── RegisterAsync ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("Customer")]
    [InlineData("Architect")]
    [InlineData("Constructor")]
    public async Task Register_AllowedRole_CreatesUser(string role)
    {
        await using var db = Database();
        var service = new ApplicationUserSyncService(db);
        var user = await service.RegisterAsync(
            new UserInfoResponseDto { Uid = "uid-reg-1", Email = "test@example.com" },
            fullName: "Test User",
            requestedRole: role);

        Assert.Equal(role, user.Role.Name);
        Assert.Equal("Test User", user.FullName);
        Assert.Null(user.PasswordHash);
        Assert.Single(db.Users);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("SuperAdmin")]
    [InlineData("User")]
    [InlineData("admin")]
    [InlineData("ADMIN")]
    [InlineData("")]
    [InlineData("Nonexistent")]
    public async Task Register_PrivilegedOrUnknownRole_Throws(string role)
    {
        await using var db = Database();
        var service = new ApplicationUserSyncService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterAsync(
                new UserInfoResponseDto { Uid = "uid-reg-2", Email = "bad@example.com" },
                fullName: "Bad Actor",
                requestedRole: role));

        Assert.Empty(db.Users);
    }

    [Fact]
    public async Task Register_ExistingFirebaseUid_ReturnsExistingUser_WithoutChangingRole()
    {
        await using var db = Database();
        var existingRole = new Role { Id = 3, Name = "Architect" };
        db.Roles.Add(existingRole);
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(), FirebaseUid = "uid-existing", Email = "arch@example.com",
            FullName = "Existing Architect", RoleId = 3, Role = existingRole,
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new ApplicationUserSyncService(db);
        // Attempt to register again with a different role
        var user = await service.RegisterAsync(
            new UserInfoResponseDto { Uid = "uid-existing", Email = "arch@example.com" },
            fullName: "New Name",
            requestedRole: "Customer");

        Assert.Single(db.Users);
        // Role must remain Architect — not changed to Customer
        Assert.Equal("Architect", user.Role.Name);
        // FullName must not be overwritten
        Assert.Equal("Existing Architect", user.FullName);
    }

    [Fact]
    public async Task Register_CustomerRole_NeverStoresPasswordHash()
    {
        await using var db = Database();
        var service = new ApplicationUserSyncService(db);
        var user = await service.RegisterAsync(
            new UserInfoResponseDto { Uid = "uid-pass-test", Email = "pass@example.com" },
            fullName: "Pass Test",
            requestedRole: "Customer");

        Assert.Null(user.PasswordHash);
    }

    [Fact]
    public async Task SynchronizeAsync_NoUid_ThrowsUnauthorized()
    {
        await using var db = Database();
        var service = new ApplicationUserSyncService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.SynchronizeAsync(new UserInfoResponseDto { Uid = "", Email = "test@example.com" }));
    }

    [Fact]
    public async Task RegisterAsync_NoUid_ThrowsUnauthorized()
    {
        await using var db = Database();
        var service = new ApplicationUserSyncService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.RegisterAsync(
                new UserInfoResponseDto { Uid = "", Email = "test@example.com" },
                fullName: "Test",
                requestedRole: "Customer"));
    }
}
