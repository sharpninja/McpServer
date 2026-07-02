using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SessionLogActions_SessionLogTurnId",
                table: "SessionLogActions");

            migrationBuilder.CreateIndex(
                name: "IX_ToolDefinitions_BucketName",
                table: "ToolDefinitions",
                column: "BucketName");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_ItemOrder",
                table: "TodoItems",
                column: "ItemOrder");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_SectionOrder",
                table: "TodoItems",
                column: "SectionOrder");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogTurns_Timestamp",
                table: "SessionLogTurns",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogActions_SessionLogTurnId_Order",
                table: "SessionLogActions",
                columns: new[] { "SessionLogTurnId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_GraphRelationships_CreatedAtUtc",
                table: "GraphRelationships",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_GraphEntities_CreatedAtUtc",
                table: "GraphEntities",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_FederationOutbox_ProxyId_AcknowledgedAtUtc",
                table: "FederationOutbox",
                columns: new[] { "ProxyId", "AcknowledgedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FederationOperations_ProxyId_Status_AttemptCount_CreatedAtU~",
                table: "FederationOperations",
                columns: new[] { "ProxyId", "Status", "AttemptCount", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_SourceType_SourceKey",
                table: "Documents",
                columns: new[] { "SourceType", "SourceKey" });

            migrationBuilder.CreateIndex(
                name: "IX_Chunks_DocumentId_ChunkIndex",
                table: "Chunks",
                columns: new[] { "DocumentId", "ChunkIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentEventLogs_WorkspacePath_AgentId_Timestamp",
                table: "AgentEventLogs",
                columns: new[] { "WorkspacePath", "AgentId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentDefinitions_DisplayName",
                table: "AgentDefinitions",
                column: "DisplayName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ToolDefinitions_BucketName",
                table: "ToolDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_ItemOrder",
                table: "TodoItems");

            migrationBuilder.DropIndex(
                name: "IX_TodoItems_SectionOrder",
                table: "TodoItems");

            migrationBuilder.DropIndex(
                name: "IX_SessionLogTurns_Timestamp",
                table: "SessionLogTurns");

            migrationBuilder.DropIndex(
                name: "IX_SessionLogActions_SessionLogTurnId_Order",
                table: "SessionLogActions");

            migrationBuilder.DropIndex(
                name: "IX_GraphRelationships_CreatedAtUtc",
                table: "GraphRelationships");

            migrationBuilder.DropIndex(
                name: "IX_GraphEntities_CreatedAtUtc",
                table: "GraphEntities");

            migrationBuilder.DropIndex(
                name: "IX_FederationOutbox_ProxyId_AcknowledgedAtUtc",
                table: "FederationOutbox");

            migrationBuilder.DropIndex(
                name: "IX_FederationOperations_ProxyId_Status_AttemptCount_CreatedAtU~",
                table: "FederationOperations");

            migrationBuilder.DropIndex(
                name: "IX_Documents_SourceType_SourceKey",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Chunks_DocumentId_ChunkIndex",
                table: "Chunks");

            migrationBuilder.DropIndex(
                name: "IX_AgentEventLogs_WorkspacePath_AgentId_Timestamp",
                table: "AgentEventLogs");

            migrationBuilder.DropIndex(
                name: "IX_AgentDefinitions_DisplayName",
                table: "AgentDefinitions");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogActions_SessionLogTurnId",
                table: "SessionLogActions",
                column: "SessionLogTurnId");
        }
    }
}
