using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqliteMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionLogTagsAndAgentSessionHeaders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            AddNullableTextColumnIfMissing(migrationBuilder, "AgentSessionId");
            AddNullableTextColumnIfMissing(migrationBuilder, "AgentSessionTranscriptFile");
            AddNullableTextColumnIfMissing(migrationBuilder, "AgentExecutablePath");
            AddNullableTextColumnIfMissing(migrationBuilder, "AgentExecutableVersion");

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "SessionLogTags" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_SessionLogTags" PRIMARY KEY AUTOINCREMENT,
                    "WorkspaceId" TEXT NOT NULL,
                    "SessionLogId" INTEGER NOT NULL,
                    "Tag" TEXT NOT NULL,
                    "DeleteReason" TEXT NULL,
                    "DeletedAtUtc" TEXT NULL,
                    "DeletedBy" TEXT NULL,
                    "IsDeleted" INTEGER NOT NULL DEFAULT 0,
                    CONSTRAINT "FK_SessionLogTags_SessionLogs_SessionLogId" FOREIGN KEY ("SessionLogId") REFERENCES "SessionLogs" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_SessionLogTags_Workspaces_WorkspaceId" FOREIGN KEY ("WorkspaceId") REFERENCES "Workspaces" ("WorkspaceId") ON DELETE RESTRICT
                );
                """);
            migrationBuilder.Sql("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_SessionLogTags_SessionLogId_Tag" ON "SessionLogTags" ("SessionLogId", "Tag");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_SessionLogTags_WorkspaceId" ON "SessionLogTags" ("WorkspaceId");""");
        }

        /// <summary>
        /// TR-MCP-TRIAGESCHEMA-001: add a nullable TEXT column on SessionLogs when missing.
        /// </summary>
        private static void AddNullableTextColumnIfMissing(MigrationBuilder migrationBuilder, string column)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);
            ArgumentException.ThrowIfNullOrWhiteSpace(column);
            if (column.AsSpan().IndexOfAny("\"';[]") >= 0)
                throw new ArgumentException("Column name must be a simple identifier.", nameof(column));

            // Microsoft.Data.Sqlite runs one statement per Sql(). Skip ADD when pragma_table_info lists the column.
            migrationBuilder.Sql(
                $"""
                SELECT mcp_add_sessionlog_text_column_if_missing('{column}')
                WHERE NOT EXISTS (
                    SELECT 1 FROM pragma_table_info('SessionLogs') WHERE name = '{column}'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionLogTags");
        }
    }
}
