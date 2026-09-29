using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousePlanner.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentExecutionLogToWorkflowState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgentExecutionLogJson",
                table: "WorkflowStates",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgentExecutionLogJson",
                table: "WorkflowStates");
        }
    }
}
