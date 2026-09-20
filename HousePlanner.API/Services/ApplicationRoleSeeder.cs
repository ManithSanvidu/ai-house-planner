using HousePlanner.API.Data;
using HousePlanner.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace HousePlanner.API.Services;

/// <summary>Maintains the application role vocabulary without assuming database role IDs.</summary>
public sealed class ApplicationRoleSeeder(ApplicationDbContext db)
{
    private static readonly string[] RequiredRoles = ["Customer", "Architect", "Constructor", "Admin"];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await db.Roles.Select(x => x.Name).ToListAsync(cancellationToken);
        var missing = RequiredRoles.Where(role => !existing.Contains(role, StringComparer.OrdinalIgnoreCase));
        foreach (var role in missing) db.Roles.Add(new Role { Name = role });
        await db.SaveChangesAsync(cancellationToken);
    }
}
