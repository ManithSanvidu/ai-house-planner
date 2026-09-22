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
            migrationBuilder.DropIndex(
                name: "IX_ValidationRequests_WorkflowStateId",
                table: "ValidationRequests");

            migrationBuilder.DropIndex(
                name: "IX_ConstructorProjectRequests_ProjectId",
                table: "ConstructorProjectRequests");

            migrationBuilder.AddColumn<Guid>(
                name: "HouseDesignId",
                table: "ValidationRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRequests_HouseDesignId",
                table: "ValidationRequests",
                column: "HouseDesignId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRequests_Workflow_Design_Status",
                table: "ValidationRequests",
                columns: new[] { "WorkflowStateId", "HouseDesignId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_ValidationRequests_HouseDesigns_HouseDesignId",
                table: "ValidationRequests",
                column: "HouseDesignId",
                principalTable: "HouseDesigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.CreateIndex(
                name: "IX_ConstructorProjectRequests_ProjectId",
                table: "ConstructorProjectRequests",
                column: "ProjectId");
        }
    }
}
