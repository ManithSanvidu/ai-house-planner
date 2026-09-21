using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousePlanner.API.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalPricingProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ConstructorProjectRequests_ProjectId",
                table: "ConstructorProjectRequests");

            migrationBuilder.AddColumn<string>(
                name: "DisplayGroup",
                table: "PricingData",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EffectiveAt",
                table: "PricingData",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalItemId",
                table: "PricingData",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalItemName",
                table: "PricingData",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ImportedAt",
                table: "PricingData",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ObservedAt",
                table: "PricingData",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalCurrency",
                table: "PricingData",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalPrice",
                table: "PricingData",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalUnit",
                table: "PricingData",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "PricingData",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "PricingData",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceReference",
                table: "PricingData",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "PricingData",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PricingImportAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ImportedCount = table.Column<int>(type: "integer", nullable: false),
                    SkippedCount = table.Column<int>(type: "integer", nullable: false),
                    FailedCount = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DetailsJson = table.Column<string>(type: "text", nullable: true),
                    FailureMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingImportAudits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_PricingData_Provider_ExternalItemId",
                table: "PricingData",
                columns: new[] { "Provider", "ExternalItemId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_PricingData_Category",
                table: "PricingData",
                sql: "\"Category\" IN ('material', 'labour')");

            migrationBuilder.CreateIndex(
                name: "IX_PricingImportAudits_StartedAt",
                table: "PricingImportAudits",
                column: "StartedAt");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PricingImportAudits");

            migrationBuilder.DropIndex(
                name: "UX_PricingData_Provider_ExternalItemId",
                table: "PricingData");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PricingData_Category",
                table: "PricingData");

            migrationBuilder.DropColumn(
                name: "DisplayGroup",
                table: "PricingData");

            migrationBuilder.DropColumn(
                name: "EffectiveAt",
                table: "PricingData");

            migrationBuilder.DropColumn(
                name: "ExternalItemId",
                table: "PricingData");

            migrationBuilder.DropColumn(
                name: "ExternalItemName",
                table: "PricingData");

            migrationBuilder.DropColumn(
                name: "ImportedAt",
                table: "PricingData");

            migrationBuilder.DropColumn(
                name: "ObservedAt",
                table: "PricingData");

            migrationBuilder.DropColumn(
                name: "OriginalCurrency",
                table: "PricingData");

            migrationBuilder.DropColumn(
                name: "OriginalPrice",
                table: "PricingData");

            migrationBuilder.DropColumn(
                name: "OriginalUnit",
                table: "PricingData");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "PricingData");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "PricingData");

            migrationBuilder.DropColumn(
                name: "SourceReference",
                table: "PricingData");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "PricingData");

            migrationBuilder.CreateIndex(
                name: "IX_ConstructorProjectRequests_ProjectId",
                table: "ConstructorProjectRequests",
                column: "ProjectId");
        }
    }
}
