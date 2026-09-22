using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using HousePlanner.API.Exceptions;
using HousePlanner.API.Services;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Tests.Services;

public sealed class SupabaseUserSyncServiceTests
{
    private static ApplicationDbContext Database()
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    // ── SynchronizeAsync (login / Google auth) ──────────────────────────────

    [Fact]
    public async Task NewGoogleSupabaseUser_ThrowsUserNotRegisteredException_WhenProfileMissing()
    {
        await using var db = Database();
        var service = new SupabaseUserSyncService(db);
        
        await Assert.ThrowsAsync<UserNotRegisteredException>(() =>
            service.SynchronizeAsync(new UserInfoResponseDto { Uid = "supabase-123", Email = "customer@example.com" }));
    }

    [Fact]
    public async Task ExistingSupabaseUser_IsUpdatedWithoutDuplicate_AndKeepsServerRole()
    {
        await using var db = Database();
        var architect = new Role { Id = 8, Name = "Architect" };
        db.Roles.Add(architect);
        db.Users.Add(new User { Id = Guid.NewGuid(), SupabaseUid = "supabase-123", Email = "old@example.com", FullName = "Existing", RoleId = 8, Role = architect, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var user = await new SupabaseUserSyncService(db).SynchronizeAsync(
            new UserInfoResponseDto { Uid = "supabase-123", Email = "new@example.com", Role = "Admin" });

        Assert.Single(db.Users);
        Assert.Equal("new@example.com", user.Email);
        // Role must not change — server is authority
        Assert.Equal("Architect", user.Role.Name);
    }

    // ── RegisterAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task PublicRegistration_CreatesCustomer()
    {
        await using var db = Database();
        var service = new SupabaseUserSyncService(db);
        var user = await service.RegisterCustomerAsync(
            new UserInfoResponseDto { Uid = "uid-reg-1", Email = "test@example.com" },
            fullName: "Test User");

        Assert.Equal("Customer", user.Role.Name);
        Assert.Equal("Test User", user.FullName);
        Assert.Null(user.PasswordHash);
        Assert.Single(db.Users);
    }

    [Theory]
    [InlineData("Architect")]
    [InlineData("Constructor")]
    [InlineData("Admin")]
    public async Task PublicRegistration_IgnoresInjectedRoleAndCreatesCustomer(string injectedRole)
    {
        await using var db = Database();
        var service = new SupabaseUserSyncService(db);

        var json = $$"""{"fullName":"Bad Actor","requestedRole":"{{injectedRole}}","roleId":2,"supabaseUid":"spoofed"}""";
        var request = System.Text.Json.JsonSerializer.Deserialize<RegisterRequestDto>(json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        var user = await service.RegisterCustomerAsync(
            new UserInfoResponseDto { Uid = "verified-uid", Email = "customer@example.com" }, request.FullName);

        Assert.Equal("Customer", user.Role.Name);
        Assert.Equal("verified-uid", user.SupabaseUid);
    }

    [Fact]
    public async Task Register_ExistingSupabaseUid_ReturnsExistingUser_WithoutChangingRole()
    {
        await using var db = Database();
        var existingRole = new Role { Id = 3, Name = "Architect" };
        db.Roles.Add(existingRole);
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(), SupabaseUid = "uid-existing", Email = "arch@example.com",
            FullName = "Existing Architect", RoleId = 3, Role = existingRole,
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new SupabaseUserSyncService(db);
        // Attempt to register again with a different role
        var user = await service.RegisterCustomerAsync(
            new UserInfoResponseDto { Uid = "uid-existing", Email = "arch@example.com" },
            fullName: "New Name");

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
        var service = new SupabaseUserSyncService(db);
        var user = await service.RegisterCustomerAsync(
            new UserInfoResponseDto { Uid = "uid-pass-test", Email = "pass@example.com" },
            fullName: "Pass Test");

        Assert.Null(user.PasswordHash);
    }

    [Fact]
    public async Task SynchronizeAsync_NoUid_ThrowsUnauthorized()
    {
        await using var db = Database();
        var service = new SupabaseUserSyncService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.SynchronizeAsync(new UserInfoResponseDto { Uid = "", Email = "test@example.com" }));
    }

    [Fact]
    public async Task RegisterAsync_NoUid_ThrowsUnauthorized()
    {
        await using var db = Database();
        var service = new SupabaseUserSyncService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.RegisterCustomerAsync(
                new UserInfoResponseDto { Uid = "", Email = "test@example.com" },
                fullName: "Test"));
    }
}
