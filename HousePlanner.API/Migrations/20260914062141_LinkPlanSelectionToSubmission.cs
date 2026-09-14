using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousePlanner.API.Migrations
{
    /// <inheritdoc />
    public partial class LinkPlanSelectionToSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BasePreDesignedPlanId",
                table: "LandSubmissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanSelectionMode",
                table: "LandSubmissions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LandSubmissions_BasePreDesignedPlanId",
                table: "LandSubmissions",
                column: "BasePreDesignedPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_LandSubmissions_PreDesignedHousePlans_BasePreDesignedPlanId",
                table: "LandSubmissions",
                column: "BasePreDesignedPlanId",
                principalTable: "PreDesignedHousePlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LandSubmissions_PreDesignedHousePlans_BasePreDesignedPlanId",
                table: "LandSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_LandSubmissions_BasePreDesignedPlanId",
                table: "LandSubmissions");

            migrationBuilder.DropColumn(
                name: "BasePreDesignedPlanId",
                table: "LandSubmissions");

            migrationBuilder.DropColumn(
                name: "PlanSelectionMode",
                table: "LandSubmissions");
        }
    }
}
