using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousePlanner.API.Migrations
{
    /// <inheritdoc />
    public partial class RenameWorkflowLogToConstructor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArchitectWorkflowLogs");

            migrationBuilder.CreateTable(
                name: "ConstructorWorkflowLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConstructorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConstructionPhaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    DayNumber = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedWork = table.Column<string>(type: "text", nullable: false),
                    ProgressPercentage = table.Column<int>(type: "integer", nullable: false),
                    Challenges = table.Column<string>(type: "text", nullable: true),
                    Issues = table.Column<string>(type: "text", nullable: true),
                    Resolution = table.Column<string>(type: "text", nullable: true),
                    TomorrowPlan = table.Column<string>(type: "text", nullable: true),
                    AdditionalNotes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConstructorWorkflowLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConstructorWorkflowLogs_ConstructionPhases_ConstructionPhas~",
                        column: x => x.ConstructionPhaseId,
                        principalTable: "ConstructionPhases",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ConstructorWorkflowLogs_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConstructorWorkflowLogs_Users_ConstructorId",
                        column: x => x.ConstructorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConstructorWorkflowLogs_ConstructionPhaseId",
                table: "ConstructorWorkflowLogs",
                column: "ConstructionPhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ConstructorWorkflowLogs_ConstructorId",
                table: "ConstructorWorkflowLogs",
                column: "ConstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_ConstructorWorkflowLogs_ProjectId",
                table: "ConstructorWorkflowLogs",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConstructorWorkflowLogs");

            migrationBuilder.CreateTable(
                name: "ArchitectWorkflowLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ArchitectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConstructionPhaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdditionalNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Challenges = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CompletedWork = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DayNumber = table.Column<int>(type: "integer", nullable: false),
                    Issues = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PlannedTask = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProgressPercentage = table.Column<int>(type: "integer", nullable: false),
                    Resolution = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TomorrowPlan = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchitectWorkflowLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchitectWorkflowLogs_ConstructionPhases_ConstructionPhaseId",
                        column: x => x.ConstructionPhaseId,
                        principalTable: "ConstructionPhases",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ArchitectWorkflowLogs_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArchitectWorkflowLogs_Users_ArchitectId",
                        column: x => x.ArchitectId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArchitectWorkflowLogs_ArchitectId",
                table: "ArchitectWorkflowLogs",
                column: "ArchitectId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchitectWorkflowLogs_ConstructionPhaseId",
                table: "ArchitectWorkflowLogs",
                column: "ConstructionPhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchitectWorkflowLogs_ProjectId",
                table: "ArchitectWorkflowLogs",
                column: "ProjectId");
        }
    }
}
