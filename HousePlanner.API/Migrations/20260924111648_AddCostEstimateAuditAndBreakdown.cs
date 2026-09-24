using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousePlanner.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCostEstimateAuditAndBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AppliedAreaSqft",
                table: "CostEstimates",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BreakdownJson",
                table: "CostEstimates",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "FormulaVersion",
                table: "CostEstimates",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "category-area-v1");

            migrationBuilder.AddColumn<string>(
                name: "TerrainType",
                table: "CostEstimates",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "flat");

            migrationBuilder.CreateTable(
                name: "CostEstimationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowStateId = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseDesignId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FormulaVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PricingRecordCount = table.Column<int>(type: "integer", nullable: false),
                    AppliedAreaSqft = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    TerrainType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostEstimationRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostEstimationRuns_HouseDesigns_HouseDesignId",
                        column: x => x.HouseDesignId,
                        principalTable: "HouseDesigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CostEstimationRuns_WorkflowStates_WorkflowStateId",
                        column: x => x.WorkflowStateId,
                        principalTable: "WorkflowStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimationRuns_HouseDesignId",
                table: "CostEstimationRuns",
                column: "HouseDesignId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimationRuns_Workflow_StartedAt",
                table: "CostEstimationRuns",
                columns: new[] { "WorkflowStateId", "StartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CostEstimationRuns");

            migrationBuilder.DropColumn(
                name: "AppliedAreaSqft",
                table: "CostEstimates");

            migrationBuilder.DropColumn(
                name: "BreakdownJson",
                table: "CostEstimates");

            migrationBuilder.DropColumn(
                name: "FormulaVersion",
                table: "CostEstimates");

            migrationBuilder.DropColumn(
                name: "TerrainType",
                table: "CostEstimates");
        }
    }
}
