using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class ScopeSessionLogUniqueIndexByWorkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SessionLogs_SourceType_SessionId",
                table: "SessionLogs");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_WorkspaceId_SourceType_SessionId",
                table: "SessionLogs",
                columns: new[] { "WorkspaceId", "SourceType", "SessionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SessionLogs_WorkspaceId_SourceType_SessionId",
                table: "SessionLogs");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_SourceType_SessionId",
                table: "SessionLogs",
                columns: new[] { "SourceType", "SessionId" },
                unique: true);
        }
    }
}
