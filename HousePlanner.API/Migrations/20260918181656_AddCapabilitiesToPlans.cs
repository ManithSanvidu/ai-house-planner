using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousePlanner.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCapabilitiesToPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasMasterEnsuite",
                table: "PreDesignedHousePlans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasOpenPlan",
                table: "PreDesignedHousePlans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasSeparateDining",
                table: "PreDesignedHousePlans",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasMasterEnsuite",
                table: "PreDesignedHousePlans");

            migrationBuilder.DropColumn(
                name: "HasOpenPlan",
                table: "PreDesignedHousePlans");

            migrationBuilder.DropColumn(
                name: "HasSeparateDining",
                table: "PreDesignedHousePlans");
        }
    }
}
