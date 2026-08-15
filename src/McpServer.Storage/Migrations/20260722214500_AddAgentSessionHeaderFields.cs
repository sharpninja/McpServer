using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentSessionHeaderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.AddColumn<string>(
                name: "AgentSessionId",
                table: "SessionLogs",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgentSessionTranscriptFile",
                table: "SessionLogs",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgentExecutablePath",
                table: "SessionLogs",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgentExecutableVersion",
                table: "SessionLogs",
                type: "TEXT",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropColumn(
                name: "AgentSessionId",
                table: "SessionLogs");

            migrationBuilder.DropColumn(
                name: "AgentSessionTranscriptFile",
                table: "SessionLogs");

            migrationBuilder.DropColumn(
                name: "AgentExecutablePath",
                table: "SessionLogs");

            migrationBuilder.DropColumn(
                name: "AgentExecutableVersion",
                table: "SessionLogs");
        }
    }
}
