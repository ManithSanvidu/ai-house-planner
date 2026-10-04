using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousePlanner.API.Migrations
{
    /// <inheritdoc />
    public partial class MigrateToSupabaseAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AddValidationRequestDesign and AddExternalPricingProvenance already perform
            // these steps; keep them idempotent so the chain also applies to a fresh database.
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_ValidationRequests_WorkflowStateId";
                DROP INDEX IF EXISTS "IX_ConstructorProjectRequests_ProjectId";
                ALTER TABLE "ValidationRequests" ADD COLUMN IF NOT EXISTS "HouseDesignId" uuid;
                CREATE INDEX IF NOT EXISTS "IX_ValidationRequests_HouseDesignId" ON "ValidationRequests" ("HouseDesignId");
                CREATE INDEX IF NOT EXISTS "IX_ValidationRequests_Workflow_Design_Status" ON "ValidationRequests" ("WorkflowStateId", "HouseDesignId", "Status");
                DO $$ BEGIN
                  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_ValidationRequests_HouseDesigns_HouseDesignId') THEN
                    ALTER TABLE "ValidationRequests" ADD CONSTRAINT "FK_ValidationRequests_HouseDesigns_HouseDesignId" FOREIGN KEY ("HouseDesignId") REFERENCES "HouseDesigns" ("Id") ON DELETE RESTRICT;
                  END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ValidationRequests_HouseDesigns_HouseDesignId",
                table: "ValidationRequests");

            migrationBuilder.DropIndex(
                name: "IX_ValidationRequests_HouseDesignId",
                table: "ValidationRequests");

            migrationBuilder.DropIndex(
                name: "IX_ValidationRequests_Workflow_Design_Status",
                table: "ValidationRequests");

            migrationBuilder.DropColumn(
                name: "HouseDesignId",
                table: "ValidationRequests");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRequests_WorkflowStateId",
                table: "ValidationRequests",
                column: "WorkflowStateId");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_ConstructorProjectRequests_ProjectId\" ON \"ConstructorProjectRequests\" (\"ProjectId\");");
        }
    }
}
