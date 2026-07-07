using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class StoreDateTimeOffsetAsUtcDateTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
