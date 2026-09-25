using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousePlanner.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingLifecycleHistoryAndSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"PricingData\" SET \"Region\" = 'Sri Lanka' WHERE \"Region\" IS NULL OR btrim(\"Region\") = ''; ");

            migrationBuilder.AlterColumn<string>(
                name: "Region",
                table: "PricingData",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "Sri Lanka",
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "PricingData",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "PricingData",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "QualityLevel",
                table: "PricingData",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Standard");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "PricingData",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PricingSnapshotJson",
                table: "CostEstimates",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.CreateTable(
                name: "PricingHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PricingDataId = table.Column<int>(type: "integer", nullable: false),
                    PreviousValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NewValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PricingHistory_PricingData_PricingDataId",
                        column: x => x.PricingDataId,
                        principalTable: "PricingData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_PricingData_Active_Item_Region_Quality",
                table: "PricingData",
                columns: new[] { "ItemName", "Region", "QualityLevel" },
                unique: true,
                filter: "\"IsActive\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "UX_PricingData_Active_Labour_Region_Quality",
                table: "PricingData",
                columns: new[] { "Region", "QualityLevel" },
                unique: true,
                filter: "\"IsActive\" = TRUE AND \"Category\" = 'labour'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PricingData_QualityLevel",
                table: "PricingData",
                sql: "\"QualityLevel\" IN ('Basic', 'Standard', 'Premium', 'Luxury')");

            migrationBuilder.CreateIndex(
                name: "IX_PricingHistory_PricingDataId_ChangedAt",
                table: "PricingHistory",
                columns: new[] { "PricingDataId", "ChangedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PricingHistory");

            migrationBuilder.DropIndex(
                name: "UX_PricingData_Active_Item_Region_Quality",
                table: "PricingData");

            migrationBuilder.DropIndex(
                name: "UX_PricingData_Active_Labour_Region_Quality",
                table: "PricingData");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PricingData_QualityLevel",
                table: "PricingData");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PricingData");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "PricingData");

            migrationBuilder.DropColumn(
                name: "QualityLevel",
                table: "PricingData");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "PricingData");

            migrationBuilder.DropColumn(
                name: "PricingSnapshotJson",
                table: "CostEstimates");

            migrationBuilder.AlterColumn<string>(
                name: "Region",
                table: "PricingData",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);
        }
    }
}
