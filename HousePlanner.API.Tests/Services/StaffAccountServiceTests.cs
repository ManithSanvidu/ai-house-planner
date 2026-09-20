using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HousePlanner.API.Tests.Services;

public sealed class StaffAccountServiceTests
{
    private static ApplicationDbContext Database()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Roles.AddRange(new Role { Id = 10, Name = "Architect" }, new Role { Id = 11, Name = "Constructor" },
            new Role { Id = 12, Name = "Customer" }, new Role { Id = 13, Name = "Admin" });
        db.SaveChanges();
        return db;
    }

    private static CreateStaffRequestDto Request(string role) => new()
    { FullName = "Staff User", Email = $"{role.ToLowerInvariant()}@example.com", Password = "secret123", Role = role };

    [Theory]
    [InlineData("Architect", 10)]
    [InlineData("Constructor", 11)]
    public async Task AdminCreation_PersistsFirebaseUidAndCorrectRoleWithoutPassword(string role, int roleId)
    {
        await using var db = Database();
        var firebase = new Mock<IFirebaseStaffAccountService>();
        firebase.Setup(x => x.CreateAsync(It.IsAny<string>(), "secret123", "Staff User", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FirebaseStaffIdentity($"firebase-{role}", $"{role.ToLowerInvariant()}@example.com"));
        var result = await new StaffAccountService(db, firebase.Object, NullLogger<StaffAccountService>.Instance)
            .CreateAsync(Request(role));

        var saved = await db.Users.SingleAsync();
        Assert.Equal($"firebase-{role}", saved.FirebaseUid);
        Assert.Equal(roleId, saved.RoleId);
        Assert.Equal(role, result.Role);
        Assert.Null(saved.PasswordHash);
    }

    [Theory]
    [InlineData("Customer")]
    [InlineData("Admin")]
    [InlineData("Unknown")]
    public async Task StaffEndpointRoles_RejectNonStaffRolesBeforeFirebase(string role)
    {
        await using var db = Database();
        var firebase = new Mock<IFirebaseStaffAccountService>();
        var error = await Assert.ThrowsAsync<StaffAccountException>(() =>
            new StaffAccountService(db, firebase.Object, NullLogger<StaffAccountService>.Instance)
                .CreateAsync(Request(role)));
        Assert.Equal("invalid_request", error.Code);
        firebase.Verify(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DuplicateDatabaseEmail_IsRejectedBeforeFirebaseCreation()
    {
        await using var db = Database();
        db.Users.Add(new User { Id = Guid.NewGuid(), Email = "architect@example.com", FirebaseUid = "existing",
            FullName = "Existing", RoleId = 10, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        var firebase = new Mock<IFirebaseStaffAccountService>();

        var error = await Assert.ThrowsAsync<StaffAccountException>(() =>
            new StaffAccountService(db, firebase.Object, NullLogger<StaffAccountService>.Instance)
                .CreateAsync(Request("Architect")));
        Assert.Equal("duplicate_email", error.Code);
        firebase.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProfileConflict_RollsBackOnlyNewFirebaseIdentity()
    {
        await using var db = Database();
        db.Users.Add(new User { Id = Guid.NewGuid(), Email = "different@example.com", FirebaseUid = "duplicate-uid",
            FullName = "Existing", RoleId = 10, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        var firebase = new Mock<IFirebaseStaffAccountService>();
        firebase.Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FirebaseStaffIdentity("duplicate-uid", "architect@example.com"));

        await Assert.ThrowsAsync<StaffAccountException>(() =>
            new StaffAccountService(db, firebase.Object, NullLogger<StaffAccountService>.Instance)
                .CreateAsync(Request("Architect")));
        firebase.Verify(x => x.DeleteAsync("duplicate-uid", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Single(db.Users);
    }

    [Fact]
    public async Task ExistingStaffLoginSync_DoesNotOverwriteRole()
    {
        await using var db = Database();
        db.Users.Add(new User { Id = Guid.NewGuid(), Email = "architect@example.com", FirebaseUid = "staff-uid",
            FullName = "Architect", RoleId = 10, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        var user = await new ApplicationUserSyncService(db).SynchronizeAsync(
            new UserInfoResponseDto { Uid = "staff-uid", Email = "architect@example.com", Role = "Customer" });
        Assert.Equal("Architect", user.Role.Name);
    }
}
