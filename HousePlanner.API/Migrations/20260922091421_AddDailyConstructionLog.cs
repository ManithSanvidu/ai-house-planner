using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousePlanner.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyConstructionLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyConstructionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConstructorId = table.Column<Guid>(type: "uuid", nullable: false),
                    LogDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ConstructionPhaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkCompleted = table.Column<string>(type: "text", nullable: false),
                    Challenges = table.Column<string>(type: "text", nullable: true),
                    MaterialsUsed = table.Column<string>(type: "text", nullable: true),
                    WorkforceCount = table.Column<int>(type: "integer", nullable: true),
                    WeatherCondition = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SafetyIssues = table.Column<string>(type: "text", nullable: true),
                    ProgressPercentage = table.Column<decimal>(type: "numeric", nullable: true),
                    TomorrowPlan = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyConstructionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyConstructionLogs_ConstructionPhases_ConstructionPhaseId",
                        column: x => x.ConstructionPhaseId,
                        principalTable: "ConstructionPhases",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DailyConstructionLogs_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DailyConstructionLogs_Users_ConstructorId",
                        column: x => x.ConstructorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyConstructionLogs_ConstructionPhaseId",
                table: "DailyConstructionLogs",
                column: "ConstructionPhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyConstructionLogs_ConstructorId",
                table: "DailyConstructionLogs",
                column: "ConstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyConstructionLogs_LogDate",
                table: "DailyConstructionLogs",
                column: "LogDate");

            migrationBuilder.CreateIndex(
                name: "IX_DailyConstructionLogs_ProjectId",
                table: "DailyConstructionLogs",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyConstructionLogs");
        }
    }
}
