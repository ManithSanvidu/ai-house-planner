using HousePlanner.API.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousePlanner.API.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261003040000_AddValidationResultJson")]
    public partial class AddValidationResultJson : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ValidationResultJson",
                table: "WorkflowStates",
                type: "jsonb",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValidationResultJson",
                table: "WorkflowStates");
        }
    }
}
