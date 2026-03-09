using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.Migrations
{
    /// <summary>
    /// Normalizes legacy tool-bucket state so the canonical global
    /// <c>official</c> bucket points at <c>sharpninja/McpServerTools</c>,
    /// installed tool provenance references that canonical bucket, and the
    /// obsolete <c>mcpservertools</c> bucket alias is removed.
    /// Validates FR-MCP-022 and TR-MCP-TR-003.
    /// </summary>
    [DbContext(typeof(McpDbContext))]
    [Migration("20260309104000_NormalizeCanonicalToolBucket")]
    public sealed class NormalizeCanonicalToolBucket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("Npgsql", StringComparison.Ordinal))
            {
                migrationBuilder.Sql(
                    """
                    UPDATE "ToolBuckets"
                    SET "Owner" = 'sharpninja',
                        "Repo" = 'McpServerTools',
                        "Branch" = 'main',
                        "ManifestPath" = '/',
                        "WorkspaceId" = ''
                    WHERE "Name" = 'official';

                    INSERT INTO "ToolBuckets" ("Name", "Owner", "Repo", "Branch", "ManifestPath", "DateTimeCreated", "DateTimeLastSynced", "WorkspaceId")
                    SELECT 'official', 'sharpninja', 'McpServerTools', 'main', '/', NOW(), NULL, ''
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM "ToolBuckets"
                        WHERE "Name" = 'official'
                    );

                    UPDATE "ToolDefinitions"
                    SET "BucketName" = 'official'
                    WHERE "BucketName" = 'mcpservertools';

                    DELETE FROM "ToolBuckets"
                    WHERE "Name" = 'mcpservertools';
                    """);
                return;
            }

            migrationBuilder.Sql(
                """
                UPDATE "ToolBuckets"
                SET "Owner" = 'sharpninja',
                    "Repo" = 'McpServerTools',
                    "Branch" = 'main',
                    "ManifestPath" = '/',
                    "WorkspaceId" = ''
                WHERE "Name" = 'official';

                INSERT INTO "ToolBuckets" ("Name", "Owner", "Repo", "Branch", "ManifestPath", "DateTimeCreated", "DateTimeLastSynced", "WorkspaceId")
                SELECT 'official', 'sharpninja', 'McpServerTools', 'main', '/', CURRENT_TIMESTAMP, NULL, ''
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "ToolBuckets"
                    WHERE "Name" = 'official'
                );

                UPDATE "ToolDefinitions"
                SET "BucketName" = 'official'
                WHERE "BucketName" = 'mcpservertools';

                DELETE FROM "ToolBuckets"
                WHERE "Name" = 'mcpservertools';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally left blank. This migration normalizes persisted bucket
            // state and installed-tool provenance; rollback must not recreate the
            // obsolete alias bucket or revert user data.
        }
    }
}
