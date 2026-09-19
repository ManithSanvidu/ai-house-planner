using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using HousePlanner.API.Data;

#nullable disable

namespace HousePlanner.API.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260919120500_AddPreferredHouseDesign")]
public partial class AddPreferredHouseDesign : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "PreferredHouseDesignId",
            table: "WorkflowStates",
            type: "uuid",
            nullable: true);
        migrationBuilder.CreateIndex(
            name: "IX_WorkflowStates_PreferredHouseDesignId",
            table: "WorkflowStates",
            column: "PreferredHouseDesignId");
        migrationBuilder.AddForeignKey(
            name: "FK_WorkflowStates_HouseDesigns_PreferredHouseDesignId",
            table: "WorkflowStates",
            column: "PreferredHouseDesignId",
            principalTable: "HouseDesigns",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_WorkflowStates_HouseDesigns_PreferredHouseDesignId", table: "WorkflowStates");
        migrationBuilder.DropIndex(name: "IX_WorkflowStates_PreferredHouseDesignId", table: "WorkflowStates");
        migrationBuilder.DropColumn(name: "PreferredHouseDesignId", table: "WorkflowStates");
    }
}
