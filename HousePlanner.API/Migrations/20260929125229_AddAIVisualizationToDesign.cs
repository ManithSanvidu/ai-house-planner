using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousePlanner.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAIVisualizationToDesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AIVisualizationImage",
                table: "HouseDesigns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicalPlanImage",
                table: "HouseDesigns",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AIVisualizationImage",
                table: "HouseDesigns");

            migrationBuilder.DropColumn(
                name: "TechnicalPlanImage",
                table: "HouseDesigns");
        }
    }
}
