using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using HousePlanner.API.Data;

#nullable disable

namespace HousePlanner.API.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260920235500_EnforceConstructionRequestUniqueness")]
public partial class EnforceConstructionRequestUniqueness : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_ConstructorProjectRequests_HouseDesignId_ConstructorId_Status", table: "ConstructorProjectRequests");
        migrationBuilder.CreateIndex(name: "IX_ConstructorProjectRequests_HouseDesignId_ConstructorId_Status", table: "ConstructorProjectRequests", columns: new[] { "HouseDesignId", "ConstructorId", "Status" }, unique: true, filter: "\"Status\" = 'Pending'");
        migrationBuilder.CreateIndex(name: "IX_ConstructorProjectRequests_ProjectId_Status", table: "ConstructorProjectRequests", columns: new[] { "ProjectId", "Status" }, unique: true, filter: "\"Status\" = 'Accepted'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_ConstructorProjectRequests_HouseDesignId_ConstructorId_Status", table: "ConstructorProjectRequests");
        migrationBuilder.DropIndex(name: "IX_ConstructorProjectRequests_ProjectId_Status", table: "ConstructorProjectRequests");
        migrationBuilder.CreateIndex(name: "IX_ConstructorProjectRequests_HouseDesignId_ConstructorId_Status", table: "ConstructorProjectRequests", columns: new[] { "HouseDesignId", "ConstructorId", "Status" });
    }
}

