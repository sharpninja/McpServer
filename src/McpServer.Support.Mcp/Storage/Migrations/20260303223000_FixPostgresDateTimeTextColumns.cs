using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.Migrations
{
    /// <summary>
    /// Fixes DateTime columns that were created as <c>text</c> by SQLite-oriented migrations
    /// but need to be <c>timestamp with time zone</c> when running on PostgreSQL.
    /// </summary>
    [DbContext(typeof(McpDbContext))]
    [Migration("20260303223000_FixPostgresDateTimeTextColumns")]
    public class FixPostgresDateTimeTextColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (!ActiveProvider.Contains("Npgsql", StringComparison.Ordinal))
                return;

            // AgentDefinitions.CreatedAt: text → timestamptz
            AlterTextToTimestamptz(migrationBuilder, "AgentDefinitions", "CreatedAt");

            // AgentDefinitions.ModifiedAt: text → timestamptz
            AlterTextToTimestamptz(migrationBuilder, "AgentDefinitions", "ModifiedAt");

            // AgentWorkspaces.AddedAt: text → timestamptz
            AlterTextToTimestamptz(migrationBuilder, "AgentWorkspaces", "AddedAt");

            // AgentWorkspaces.LastLaunchedAt: text → timestamptz (nullable)
            AlterTextToTimestamptz(migrationBuilder, "AgentWorkspaces", "LastLaunchedAt");

            // Documents.IngestedAt: text → timestamptz
            AlterTextToTimestamptz(migrationBuilder, "Documents", "IngestedAt");

            // ToolBuckets.DateTimeLastSynced: text → timestamptz (nullable)
            AlterTextToTimestamptz(migrationBuilder, "ToolBuckets", "DateTimeLastSynced");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (!ActiveProvider.Contains("Npgsql", StringComparison.Ordinal))
                return;

            AlterTimestamptzToText(migrationBuilder, "AgentDefinitions", "CreatedAt");
            AlterTimestamptzToText(migrationBuilder, "AgentDefinitions", "ModifiedAt");
            AlterTimestamptzToText(migrationBuilder, "AgentWorkspaces", "AddedAt");
            AlterTimestamptzToText(migrationBuilder, "AgentWorkspaces", "LastLaunchedAt");
            AlterTimestamptzToText(migrationBuilder, "Documents", "IngestedAt");
            AlterTimestamptzToText(migrationBuilder, "ToolBuckets", "DateTimeLastSynced");
        }

        private static void AlterTextToTimestamptz(MigrationBuilder migrationBuilder, string table, string column)
        {
            migrationBuilder.Sql(
                $"""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = '{table}'
                          AND column_name = '{column}'
                          AND data_type = 'text'
                    ) THEN
                        EXECUTE 'ALTER TABLE "{table}" ALTER COLUMN "{column}" TYPE timestamp with time zone USING CASE WHEN "{column}" IS NULL OR "{column}" = '''' THEN NULL ELSE "{column}"::timestamp with time zone END';
                    END IF;
                END $$;
                """);
        }

        private static void AlterTimestamptzToText(MigrationBuilder migrationBuilder, string table, string column)
        {
            migrationBuilder.Sql(
                $"""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND table_name = '{table}'
                          AND column_name = '{column}'
                          AND data_type = 'timestamp with time zone'
                    ) THEN
                        EXECUTE 'ALTER TABLE "{table}" ALTER COLUMN "{column}" TYPE text USING "{column}"::text';
                    END IF;
                END $$;
                """);
        }
    }
}
