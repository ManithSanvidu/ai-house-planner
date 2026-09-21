using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using HousePlanner.API.Data;

#nullable disable

namespace HousePlanner.API.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260920234500_AddCustomerConstructionHandoff")]
public partial class AddCustomerConstructionHandoff : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "HouseDesignId", table: "Projects", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "CustomerId", table: "ConstructorProjectRequests", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "HouseDesignId", table: "ConstructorProjectRequests", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<string>(name: "DeclineReason", table: "ConstructorProjectRequests", type: "character varying(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(name: "RespondedAt", table: "ConstructorProjectRequests", type: "timestamp with time zone", nullable: true);

        migrationBuilder.Sql("""
            UPDATE "Projects" p SET "HouseDesignId" = COALESCE(
              (SELECT vr."HouseDesignId" FROM "ValidationRequests" vr
               WHERE vr."WorkflowStateId" = p."WorkflowStateId" AND vr."Status" = 'Approved' AND vr."HouseDesignId" IS NOT NULL
               ORDER BY vr."DecisionAt" DESC NULLS LAST LIMIT 1),
              (SELECT w."PreferredHouseDesignId" FROM "WorkflowStates" w WHERE w."Id" = p."WorkflowStateId"))
            WHERE p."HouseDesignId" IS NULL;
            UPDATE "ConstructorProjectRequests" r SET
              "HouseDesignId" = p."HouseDesignId",
              "CustomerId" = l."ClientId"
            FROM "Projects" p
            JOIN "WorkflowStates" w ON w."Id" = p."WorkflowStateId"
            JOIN "LandSubmissions" l ON l."Id" = w."LandSubmissionId"
            WHERE r."ProjectId" = p."Id";
            """);

        migrationBuilder.CreateIndex(name: "IX_Projects_HouseDesignId", table: "Projects", column: "HouseDesignId");
        migrationBuilder.CreateIndex(name: "IX_ConstructorProjectRequests_CustomerId", table: "ConstructorProjectRequests", column: "CustomerId");
        migrationBuilder.CreateIndex(name: "IX_ConstructorProjectRequests_HouseDesignId_ConstructorId_Status", table: "ConstructorProjectRequests", columns: new[] { "HouseDesignId", "ConstructorId", "Status" });
        migrationBuilder.CreateIndex(name: "IX_ConstructorProjectRequests_ConstructorId_Status", table: "ConstructorProjectRequests", columns: new[] { "ConstructorId", "Status" });
        migrationBuilder.AddForeignKey(name: "FK_Projects_HouseDesigns_HouseDesignId", table: "Projects", column: "HouseDesignId", principalTable: "HouseDesigns", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_ConstructorProjectRequests_Users_CustomerId", table: "ConstructorProjectRequests", column: "CustomerId", principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_ConstructorProjectRequests_Users_ConstructorId", table: "ConstructorProjectRequests", column: "ConstructorId", principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_ConstructorProjectRequests_HouseDesigns_HouseDesignId", table: "ConstructorProjectRequests", column: "HouseDesignId", principalTable: "HouseDesigns", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_Projects_HouseDesigns_HouseDesignId", table: "Projects");
        migrationBuilder.DropForeignKey(name: "FK_ConstructorProjectRequests_Users_CustomerId", table: "ConstructorProjectRequests");
        migrationBuilder.DropForeignKey(name: "FK_ConstructorProjectRequests_Users_ConstructorId", table: "ConstructorProjectRequests");
        migrationBuilder.DropForeignKey(name: "FK_ConstructorProjectRequests_HouseDesigns_HouseDesignId", table: "ConstructorProjectRequests");
        migrationBuilder.DropIndex(name: "IX_Projects_HouseDesignId", table: "Projects");
        migrationBuilder.DropIndex(name: "IX_ConstructorProjectRequests_CustomerId", table: "ConstructorProjectRequests");
        migrationBuilder.DropIndex(name: "IX_ConstructorProjectRequests_HouseDesignId_ConstructorId_Status", table: "ConstructorProjectRequests");
        migrationBuilder.DropIndex(name: "IX_ConstructorProjectRequests_ConstructorId_Status", table: "ConstructorProjectRequests");
        migrationBuilder.DropColumn(name: "HouseDesignId", table: "Projects");
        migrationBuilder.DropColumn(name: "CustomerId", table: "ConstructorProjectRequests");
        migrationBuilder.DropColumn(name: "HouseDesignId", table: "ConstructorProjectRequests");
        migrationBuilder.DropColumn(name: "DeclineReason", table: "ConstructorProjectRequests");
        migrationBuilder.DropColumn(name: "RespondedAt", table: "ConstructorProjectRequests");
    }
}

