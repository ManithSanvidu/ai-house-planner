using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using HousePlanner.API.Data;

#nullable disable
namespace HousePlanner.API.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260920223000_AddValidationRequestDesign")]
public partial class AddValidationRequestDesign : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "ValidationRequests" ADD COLUMN IF NOT EXISTS "HouseDesignId" uuid;
            UPDATE "ValidationRequests" v SET "HouseDesignId" = w."PreferredHouseDesignId"
            FROM "WorkflowStates" w WHERE v."WorkflowStateId" = w."Id" AND v."HouseDesignId" IS NULL;
            UPDATE "ValidationRequests" v SET "HouseDesignId" =
              (SELECT h."Id" FROM "HouseDesigns" h WHERE h."WorkflowStateId" = v."WorkflowStateId" ORDER BY h."Version" DESC LIMIT 1)
            WHERE v."HouseDesignId" IS NULL;
            CREATE INDEX IF NOT EXISTS "IX_ValidationRequests_Workflow_Design_Status" ON "ValidationRequests" ("WorkflowStateId", "HouseDesignId", "Status");
            DO $$ BEGIN
              IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_ValidationRequests_HouseDesigns_HouseDesignId') THEN
                ALTER TABLE "ValidationRequests" ADD CONSTRAINT "FK_ValidationRequests_HouseDesigns_HouseDesignId" FOREIGN KEY ("HouseDesignId") REFERENCES "HouseDesigns" ("Id") ON DELETE RESTRICT;
              END IF;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE \"ValidationRequests\" DROP COLUMN IF EXISTS \"HouseDesignId\";");
    }
}
