using HousePlanner.API.Data;
using HousePlanner.API.Entities;
using HousePlanner.API.Services;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Tests.Services;

public sealed class ApplicationRoleSeederTests
{
    [Fact]
    public async Task SeedAsync_AddsConstructorAndIsIdempotent()
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Roles.AddRange(new Role { Id = 1, Name = "User" }, new Role { Id = 2, Name = "Admin" });
        await db.SaveChangesAsync();
        var seeder = new ApplicationRoleSeeder(db);

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        Assert.Contains(db.Roles, r => r.Name == "Constructor");
        Assert.Equal(1, await db.Roles.CountAsync(r => r.Name == "Constructor"));
        Assert.All(new[] { "Customer", "Architect", "Constructor", "Admin" },
            role => Assert.Contains(db.Roles, r => r.Name == role));
    }
}
