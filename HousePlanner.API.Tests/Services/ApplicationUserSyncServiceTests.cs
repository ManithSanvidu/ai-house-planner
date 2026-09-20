using HousePlanner.API.Data;
using HousePlanner.API.DTOs;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Tests.Services;

public sealed class ApplicationUserSyncServiceTests
{
    private static ApplicationDbContext Database()
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task NewFirebaseUser_CreatesOneApplicationUser_WithCustomerRole()
    {
        await using var db = Database();
        var service = new ApplicationUserSyncService(db);
        var user = await service.SynchronizeAsync(new UserInfoResponseDto { Uid = "firebase-123", Email = "customer@example.com" });

        Assert.Equal("firebase-123", user.FirebaseUid);
        Assert.Equal("Customer", user.Role.Name);
        Assert.Null(user.PasswordHash);
        Assert.Single(db.Users);
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
        Assert.Equal("Architect", user.Role.Name);
    }
}
