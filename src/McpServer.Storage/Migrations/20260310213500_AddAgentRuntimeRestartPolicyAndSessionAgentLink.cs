using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.Migrations
{
    /// <summary>
    /// Adds restart policy support for workspace agent runtimes and links session logs
    /// to agent definitions when sourceType matches a known agent identifier.
    /// </summary>
    [DbContext(typeof(McpDbContext))]
    [Migration("20260310213500_AddAgentRuntimeRestartPolicyAndSessionAgentLink")]
    public sealed class AddAgentRuntimeRestartPolicyAndSessionAgentLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RestartPolicy",
                table: "AgentWorkspaces",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "never");

            migrationBuilder.AddColumn<string>(
                name: "AgentDefinitionId",
                table: "SessionLogs",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_AgentDefinitionId",
                table: "SessionLogs",
                column: "AgentDefinitionId");

            // SQLite does not support adding foreign keys to existing tables.
            if (ActiveProvider.Contains("Npgsql", StringComparison.Ordinal))
            {
                migrationBuilder.AddForeignKey(
                    name: "FK_SessionLogs_AgentDefinitions_AgentDefinitionId",
                    table: "SessionLogs",
                    column: "AgentDefinitionId",
                    principalTable: "AgentDefinitions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            }

            if (ActiveProvider.Contains("Npgsql", StringComparison.Ordinal))
            {
                migrationBuilder.Sql(
                    """
                    UPDATE "SessionLogs" AS logs
                    SET "AgentDefinitionId" = defs."Id"
                    FROM "AgentDefinitions" AS defs
                    WHERE lower(logs."SourceType") = lower(defs."Id")
                      AND logs."AgentDefinitionId" IS NULL;
                    """);
            }
            else
            {
                migrationBuilder.Sql(
                    """
                    UPDATE "SessionLogs"
                    SET "AgentDefinitionId" = (
                        SELECT "Id"
                        FROM "AgentDefinitions"
                        WHERE lower("AgentDefinitions"."Id") = lower("SessionLogs"."SourceType")
                        LIMIT 1)
                    WHERE "AgentDefinitionId" IS NULL
                      AND EXISTS (
                        SELECT 1
                        FROM "AgentDefinitions"
                        WHERE lower("AgentDefinitions"."Id") = lower("SessionLogs"."SourceType"));
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // SQLite does not support dropping foreign keys on existing tables.
            if (ActiveProvider.Contains("Npgsql", StringComparison.Ordinal))
            {
                migrationBuilder.DropForeignKey(
                    name: "FK_SessionLogs_AgentDefinitions_AgentDefinitionId",
                    table: "SessionLogs");
            }

            migrationBuilder.DropIndex(
                name: "IX_SessionLogs_AgentDefinitionId",
                table: "SessionLogs");

            migrationBuilder.DropColumn(
                name: "AgentDefinitionId",
                table: "SessionLogs");

            migrationBuilder.DropColumn(
                name: "RestartPolicy",
                table: "AgentWorkspaces");
        }
    }
}
