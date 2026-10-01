using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousePlanner.API.Migrations
{
    /// <inheritdoc />
    public partial class AddEstimatedConstructionCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedConstructionCost",
                table: "PreDesignedHousePlans",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValidationRequests_Status_CreatedAt",
                table: "ValidationRequests",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ValidationRequests_Status_CreatedAt",
                table: "ValidationRequests");

            migrationBuilder.DropColumn(
                name: "EstimatedConstructionCost",
                table: "PreDesignedHousePlans");
        }
    }
}
