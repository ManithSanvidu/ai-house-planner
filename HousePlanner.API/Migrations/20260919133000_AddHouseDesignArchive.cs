using HousePlanner.API.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousePlanner.API.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260919133000_AddHouseDesignArchive")]
public partial class AddHouseDesignArchive : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name: "IsArchived", table: "HouseDesigns", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.CreateIndex(name: "IX_HouseDesigns_WorkflowState_IsArchived", table: "HouseDesigns", columns: new[] { "WorkflowStateId", "IsArchived" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_HouseDesigns_WorkflowState_IsArchived", table: "HouseDesigns");
        migrationBuilder.DropColumn(name: "IsArchived", table: "HouseDesigns");
    }
}
