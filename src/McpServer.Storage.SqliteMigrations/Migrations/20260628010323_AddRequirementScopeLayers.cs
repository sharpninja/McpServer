using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqliteMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddRequirementScopeLayers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentRequirementLayerKey",
                table: "Workspaces",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ScopeEndLayerKey",
                table: "Requirements",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScopeStartLayerKey",
                table: "Requirements",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "layer-1");

            migrationBuilder.CreateTable(
                name: "RequirementScopeLayers",
                columns: table => new
                {
                    WorkspaceId = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ScopeEndLayerKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DeleteReason = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequirementScopeLayers", x => new { x.WorkspaceId, x.Key });
                    table.ForeignKey(
                        name: "FK_RequirementScopeLayers_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Workspaces",
                keyColumn: "WorkspaceId",
                keyValue: "",
                column: "CurrentRequirementLayerKey",
                value: "layer-1");

            migrationBuilder.CreateIndex(
                name: "IX_Requirements_WorkspaceId_ScopeEndLayerKey",
                table: "Requirements",
                columns: new[] { "WorkspaceId", "ScopeEndLayerKey" });

            migrationBuilder.CreateIndex(
                name: "IX_Requirements_WorkspaceId_ScopeStartLayerKey",
                table: "Requirements",
                columns: new[] { "WorkspaceId", "ScopeStartLayerKey" });

            migrationBuilder.CreateIndex(
                name: "IX_RequirementScopeLayers_WorkspaceId",
                table: "RequirementScopeLayers",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_RequirementScopeLayers_WorkspaceId_Order",
                table: "RequirementScopeLayers",
                columns: new[] { "WorkspaceId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequirementScopeLayers_WorkspaceId_ScopeEndLayerKey",
                table: "RequirementScopeLayers",
                columns: new[] { "WorkspaceId", "ScopeEndLayerKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequirementScopeLayers");

            migrationBuilder.DropIndex(
                name: "IX_Requirements_WorkspaceId_ScopeEndLayerKey",
                table: "Requirements");

            migrationBuilder.DropIndex(
                name: "IX_Requirements_WorkspaceId_ScopeStartLayerKey",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "CurrentRequirementLayerKey",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "ScopeEndLayerKey",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "ScopeStartLayerKey",
                table: "Requirements");
        }
    }
}
