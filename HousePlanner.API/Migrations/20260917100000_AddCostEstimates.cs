using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HousePlanner.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCostEstimates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CostEstimates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseDesignId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialCostLkr = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    LabourCostLkr = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    TotalCostLkr = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    BudgetDeltaPercent = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostEstimates_HouseDesigns_HouseDesignId",
                        column: x => x.HouseDesignId,
                        principalTable: "HouseDesigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PricingData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UnitCostLkr = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TerrainMultiplier = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingData", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimates_HouseDesignId",
                table: "CostEstimates",
                column: "HouseDesignId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CostEstimates");

            migrationBuilder.DropTable(
                name: "PricingData");
        }
    }
}
