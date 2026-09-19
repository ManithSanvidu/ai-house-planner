using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HousePlanner.API.Migrations
{
    /// <inheritdoc />
    public partial class AddConstructionPlanToWorkflowState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.AddColumn<string>(
                name: "ConstructionPlan",
                table: "WorkflowStates",
                type: "jsonb",
                nullable: true);





        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.DropColumn(
                name: "ConstructionPlan",
                table: "WorkflowStates");


        }
    }
}
