using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddTodoWorkspaceScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TodoItems",
                table: "TodoItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TodoDocumentMetadata",
                table: "TodoDocumentMetadata");

            migrationBuilder.DropIndex(
                name: "IX_TodoAuditHistory_TodoId_RecordedAtUtc",
                table: "TodoAuditHistory");

            migrationBuilder.DropIndex(
                name: "IX_TodoAuditHistory_TodoId_Version",
                table: "TodoAuditHistory");

            migrationBuilder.AddColumn<string>(
                name: "WorkspaceId",
                table: "TodoItems",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkspaceId",
                table: "TodoDocumentMetadata",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkspaceId",
                table: "TodoAuditHistory",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TodoItems",
                table: "TodoItems",
                columns: new[] { "WorkspaceId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_TodoDocumentMetadata",
                table: "TodoDocumentMetadata",
                columns: new[] { "WorkspaceId", "SingletonId" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoAuditHistory_WorkspaceId",
                table: "TodoAuditHistory",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoAuditHistory_WorkspaceId_TodoId_RecordedAtUtc",
                table: "TodoAuditHistory",
                columns: new[] { "WorkspaceId", "TodoId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoAuditHistory_WorkspaceId_TodoId_Version",
                table: "TodoAuditHistory",
                columns: new[] { "WorkspaceId", "TodoId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TodoItems",
                table: "TodoItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TodoDocumentMetadata",
                table: "TodoDocumentMetadata");

            migrationBuilder.DropIndex(
                name: "IX_TodoAuditHistory_WorkspaceId",
                table: "TodoAuditHistory");

            migrationBuilder.DropIndex(
                name: "IX_TodoAuditHistory_WorkspaceId_TodoId_RecordedAtUtc",
                table: "TodoAuditHistory");

            migrationBuilder.DropIndex(
                name: "IX_TodoAuditHistory_WorkspaceId_TodoId_Version",
                table: "TodoAuditHistory");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "TodoDocumentMetadata");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "TodoAuditHistory");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TodoItems",
                table: "TodoItems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TodoDocumentMetadata",
                table: "TodoDocumentMetadata",
                column: "SingletonId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoAuditHistory_TodoId_RecordedAtUtc",
                table: "TodoAuditHistory",
                columns: new[] { "TodoId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoAuditHistory_TodoId_Version",
                table: "TodoAuditHistory",
                columns: new[] { "TodoId", "Version" },
                unique: true);
        }
    }
}
