using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousePlanner.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingTerrainHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NewTerrainMultipliersJson",
                table: "PricingHistory",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousTerrainMultipliersJson",
                table: "PricingHistory",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewTerrainMultipliersJson",
                table: "PricingHistory");

            migrationBuilder.DropColumn(
                name: "PreviousTerrainMultipliersJson",
                table: "PricingHistory");
        }
    }
}
