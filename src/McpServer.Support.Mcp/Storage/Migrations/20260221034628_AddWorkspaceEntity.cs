using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkspaceEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Embedding",
                table: "Chunks",
                type: "BLOB",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Workspaces",
                columns: table => new
                {
                    WorkspacePath = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    TodoPath = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    WorkspacePort = table.Column<int>(type: "INTEGER", nullable: false),
                    TunnelProvider = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    DateTimeCreated = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DateTimeModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RunAs = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspaces", x => x.WorkspacePath);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_WorkspacePort",
                table: "Workspaces",
                column: "WorkspacePort",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Workspaces");

            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "Chunks");
        }
    }
}
