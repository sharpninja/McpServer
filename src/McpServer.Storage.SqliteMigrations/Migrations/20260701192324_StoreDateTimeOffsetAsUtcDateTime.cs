using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqliteMigrations.Migrations
{
    /// <inheritdoc />
    public partial class StoreDateTimeOffsetAsUtcDateTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // TR-MCP-DB-DTO-001: DateTimeOffset columns now persist as offset-less UTC DateTime
            // so SQLite can translate timestamp predicates/ordering to SQL. SQLite stores both
            // as TEXT (no column type change), but pre-existing rows keep the legacy "...+00:00"
            // text. Upgrade decision: local per-workspace dev mcp.db files are regeneratable and
            // should be deleted/recreated on this upgrade rather than reformatted in place.
            migrationBuilder.UpdateData(
                table: "Workspaces",
                keyColumn: "WorkspaceId",
                keyValue: "",
                columns: new[] { "DateTimeCreated", "DateTimeModified", "DeletedAtUtc" },
                values: new object[] { new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Workspaces",
                keyColumn: "WorkspaceId",
                keyValue: "",
                columns: new[] { "DateTimeCreated", "DateTimeModified", "DeletedAtUtc" },
                values: new object[] { new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null });
        }
    }
}
