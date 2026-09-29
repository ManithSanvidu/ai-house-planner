using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousePlanner.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAIVisualizationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AIVisualizationStatus",
                table: "HouseDesigns",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "generating");

            migrationBuilder.Sql(
                "UPDATE \"HouseDesigns\" SET \"AIVisualizationStatus\" = " +
                "CASE WHEN \"AIVisualizationImage\" IS NULL OR \"AIVisualizationImage\" = '' " +
                "THEN 'failed' ELSE 'completed' END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AIVisualizationStatus",
                table: "HouseDesigns");
        }
    }
}
