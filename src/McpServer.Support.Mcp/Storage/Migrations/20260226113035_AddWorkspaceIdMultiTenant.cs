using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkspaceIdMultiTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkspaceId",
                table: "ToolDefinitionTags",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkspaceId",
                table: "ToolDefinitions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkspaceId",
                table: "ToolBuckets",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogProcessingDialogs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogEntryTags",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogEntryContexts",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkspaceId",
                table: "SessionLogActions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkspaceId",
                table: "Documents",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkspaceId",
                table: "Chunks",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkspaceId",
                table: "AgentWorkspaces",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkspaceId",
                table: "AgentEventLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkspaceId",
                table: "AgentDefinitions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ToolDefinitionTags_WorkspaceId",
                table: "ToolDefinitionTags",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolDefinitions_WorkspaceId",
                table: "ToolDefinitions",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolBuckets_WorkspaceId",
                table: "ToolBuckets",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_WorkspaceId",
                table: "SessionLogs",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogProcessingDialogs_WorkspaceId",
                table: "SessionLogProcessingDialogs",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogEntryTags_WorkspaceId",
                table: "SessionLogEntryTags",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogEntryContexts_WorkspaceId",
                table: "SessionLogEntryContexts",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogEntries_WorkspaceId",
                table: "SessionLogEntries",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogActions_WorkspaceId",
                table: "SessionLogActions",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_WorkspaceId",
                table: "Documents",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Chunks_WorkspaceId",
                table: "Chunks",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkspaces_WorkspaceId",
                table: "AgentWorkspaces",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentEventLogs_WorkspaceId",
                table: "AgentEventLogs",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentDefinitions_WorkspaceId",
                table: "AgentDefinitions",
                column: "WorkspaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ToolDefinitionTags_WorkspaceId",
                table: "ToolDefinitionTags");

            migrationBuilder.DropIndex(
                name: "IX_ToolDefinitions_WorkspaceId",
                table: "ToolDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_ToolBuckets_WorkspaceId",
                table: "ToolBuckets");

            migrationBuilder.DropIndex(
                name: "IX_SessionLogs_WorkspaceId",
                table: "SessionLogs");

            migrationBuilder.DropIndex(
                name: "IX_SessionLogProcessingDialogs_WorkspaceId",
                table: "SessionLogProcessingDialogs");

            migrationBuilder.DropIndex(
                name: "IX_SessionLogEntryTags_WorkspaceId",
                table: "SessionLogEntryTags");

            migrationBuilder.DropIndex(
                name: "IX_SessionLogEntryContexts_WorkspaceId",
                table: "SessionLogEntryContexts");

            migrationBuilder.DropIndex(
                name: "IX_SessionLogEntries_WorkspaceId",
                table: "SessionLogEntries");

            migrationBuilder.DropIndex(
                name: "IX_SessionLogActions_WorkspaceId",
                table: "SessionLogActions");

            migrationBuilder.DropIndex(
                name: "IX_Documents_WorkspaceId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Chunks_WorkspaceId",
                table: "Chunks");

            migrationBuilder.DropIndex(
                name: "IX_AgentWorkspaces_WorkspaceId",
                table: "AgentWorkspaces");

            migrationBuilder.DropIndex(
                name: "IX_AgentEventLogs_WorkspaceId",
                table: "AgentEventLogs");

            migrationBuilder.DropIndex(
                name: "IX_AgentDefinitions_WorkspaceId",
                table: "AgentDefinitions");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "ToolDefinitionTags");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "ToolDefinitions");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "ToolBuckets");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "SessionLogs");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "SessionLogProcessingDialogs");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "SessionLogEntryTags");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "SessionLogEntryContexts");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "SessionLogEntries");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "SessionLogActions");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "Chunks");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "AgentWorkspaces");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "AgentEventLogs");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "AgentDefinitions");
        }
    }
}
