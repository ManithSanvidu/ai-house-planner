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
        var existingNames = await db.Roles
            .AsNoTracking()
            .Select(role => role.Name)
            .ToListAsync(cancellationToken);
        var missingRoles = RequiredRoles
            .Where(required => !existingNames.Contains(required, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (missingRoles.Length == 0)
            return;

        // InitialCreate inserts roles with explicit IDs, so PostgreSQL's identity
        // sequence may still point at an ID that is already in use.
        if (db.Database.IsNpgsql())
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                SELECT setval(
                    pg_get_serial_sequence('"Roles"', 'Id'),
                    COALESCE((SELECT MAX("Id") FROM "Roles"), 1),
                    EXISTS (SELECT 1 FROM "Roles"));
                """,
                cancellationToken);
        }

        db.Roles.AddRange(missingRoles.Select(name => new Role { Name = name }));
        await db.SaveChangesAsync(cancellationToken);
    }
}
