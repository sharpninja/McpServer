using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.Migrations
{
    /// <summary>
    /// Normalizes tool-definition and tool-tag workspace scope so persisted
    /// <c>WorkspaceId</c> values always match the declared <c>WorkspacePath</c>
    /// contract: <c>null</c> means global and any non-null path means the row is
    /// workspace-scoped to that exact path.
    /// Validates FR-MCP-022 and TR-MCP-MT-003.
    /// </summary>
    [DbContext(typeof(McpDbContext))]
    [Migration("20260309112000_NormalizeToolDefinitionWorkspaceScope")]
    public sealed class NormalizeToolDefinitionWorkspaceScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (ActiveProvider.Contains("Npgsql", StringComparison.Ordinal))
            {
                migrationBuilder.Sql(
                    """
                    UPDATE "ToolDefinitions"
                    SET "WorkspaceId" = COALESCE("WorkspacePath", '')
                    WHERE COALESCE("WorkspaceId", '') <> COALESCE("WorkspacePath", '');

                    UPDATE "ToolDefinitionTags" AS tags
                    SET "WorkspaceId" = COALESCE(defs."WorkspacePath", '')
                    FROM "ToolDefinitions" AS defs
                    WHERE defs."Id" = tags."ToolDefinitionId"
                      AND COALESCE(tags."WorkspaceId", '') <> COALESCE(defs."WorkspacePath", '');
                    """);
                return;
            }

            migrationBuilder.Sql(
                """
                UPDATE "ToolDefinitions"
                SET "WorkspaceId" = IFNULL("WorkspacePath", '')
                WHERE IFNULL("WorkspaceId", '') <> IFNULL("WorkspacePath", '');

                UPDATE "ToolDefinitionTags"
                SET "WorkspaceId" = IFNULL(
                    (SELECT "WorkspacePath"
                     FROM "ToolDefinitions"
                     WHERE "ToolDefinitions"."Id" = "ToolDefinitionTags"."ToolDefinitionId"),
                    '')
                WHERE IFNULL("WorkspaceId", '') <> IFNULL(
                    (SELECT "WorkspacePath"
                     FROM "ToolDefinitions"
                     WHERE "ToolDefinitions"."Id" = "ToolDefinitionTags"."ToolDefinitionId"),
                    '');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally left blank. This migration repairs persisted scope
            // metadata for existing tool definitions and tags; rollback must not
            // reintroduce incorrect workspace ownership.
        }
    }
}
