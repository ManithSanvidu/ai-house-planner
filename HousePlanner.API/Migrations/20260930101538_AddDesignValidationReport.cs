using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousePlanner.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDesignValidationReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DesignValidationReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseDesignId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverallPassed = table.Column<bool>(type: "boolean", nullable: false),
                    GeometryPassed = table.Column<bool>(type: "boolean", nullable: false),
                    BusinessPassed = table.Column<bool>(type: "boolean", nullable: false),
                    GeometryFailuresJson = table.Column<string>(type: "jsonb", nullable: false),
                    GeometryFailedRulesJson = table.Column<string>(type: "jsonb", nullable: false),
                    BusinessRulesJson = table.Column<string>(type: "jsonb", nullable: false),
                    ValidationSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    DesignVersion = table.Column<int>(type: "integer", nullable: false),
                    ValidationSourceVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ValidatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DesignValidationReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DesignValidationReports_HouseDesigns_HouseDesignId",
                        column: x => x.HouseDesignId,
                        principalTable: "HouseDesigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DesignValidationReports_Design_ValidatedAt",
                table: "DesignValidationReports",
                columns: new[] { "HouseDesignId", "ValidatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DesignValidationReports_HouseDesignId",
                table: "DesignValidationReports",
                column: "HouseDesignId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DesignValidationReports");
        }
    }
}
