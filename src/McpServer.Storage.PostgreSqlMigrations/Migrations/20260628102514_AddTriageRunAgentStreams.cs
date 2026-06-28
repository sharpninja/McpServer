using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddTriageRunAgentStreams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AgentExitCode",
                table: "TriageResearchRuns",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgentStderr",
                table: "TriageResearchRuns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgentStdout",
                table: "TriageResearchRuns",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgentExitCode",
                table: "TriageResearchRuns");

            migrationBuilder.DropColumn(
                name: "AgentStderr",
                table: "TriageResearchRuns");

            migrationBuilder.DropColumn(
                name: "AgentStdout",
                table: "TriageResearchRuns");
        }
    }
}
