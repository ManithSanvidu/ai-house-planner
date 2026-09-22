using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousePlanner.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPlannedDurationsToPhases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EstimatedDurationDays",
                table: "ConstructionPhases",
                newName: "PlannedDurationDays");

            migrationBuilder.AddColumn<int>(
                name: "AiEstimatedTotalDurationDays",
                table: "Projects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PlannedTotalDurationDays",
                table: "Projects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AiEstimatedDurationDays",
                table: "ConstructionPhases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PlannedEndDate",
                table: "ConstructionPhases",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PlannedStartDate",
                table: "ConstructionPhases",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiEstimatedTotalDurationDays",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "PlannedTotalDurationDays",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "AiEstimatedDurationDays",
                table: "ConstructionPhases");

            migrationBuilder.DropColumn(
                name: "PlannedEndDate",
                table: "ConstructionPhases");

            migrationBuilder.DropColumn(
                name: "PlannedStartDate",
                table: "ConstructionPhases");

            migrationBuilder.RenameColumn(
                name: "PlannedDurationDays",
                table: "ConstructionPhases",
                newName: "EstimatedDurationDays");
        }
    }
}
