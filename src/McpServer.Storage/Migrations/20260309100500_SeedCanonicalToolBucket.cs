using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.Migrations
{
    /// <summary>
    /// Seeds the canonical global tool bucket for the official
    /// <c>sharpninja/McpServerTools</c> repository so fresh and existing
    /// installations always have the primary manifest source registered.
    /// Validates FR-MCP-022 and TR-MCP-TR-003.
    /// </summary>
    [DbContext(typeof(McpDbContext))]
    [Migration("20260309100500_SeedCanonicalToolBucket")]
    public sealed class SeedCanonicalToolBucket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("Npgsql", StringComparison.Ordinal))
            {
                migrationBuilder.Sql(
                    """
                    INSERT INTO "ToolBuckets" ("Name", "Owner", "Repo", "Branch", "ManifestPath", "DateTimeCreated", "DateTimeLastSynced", "WorkspaceId")
                    SELECT 'official', 'sharpninja', 'McpServerTools', 'main', '/', NOW(), NULL, ''
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM "ToolBuckets"
                        WHERE "Name" = 'official'
                          AND COALESCE("WorkspaceId", '') = ''
                    );
                    """);
                return;
            }

            migrationBuilder.Sql(
                """
                INSERT INTO "ToolBuckets" ("Name", "Owner", "Repo", "Branch", "ManifestPath", "DateTimeCreated", "DateTimeLastSynced", "WorkspaceId")
                SELECT 'official', 'sharpninja', 'McpServerTools', 'main', '/', CURRENT_TIMESTAMP, NULL, ''
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "ToolBuckets"
                    WHERE "Name" = 'official'
                      AND IFNULL("WorkspaceId", '') = ''
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally left blank. The canonical tool bucket may pre-exist in
            // upgraded databases, so rollback must not delete user data.
        }
    }
}
