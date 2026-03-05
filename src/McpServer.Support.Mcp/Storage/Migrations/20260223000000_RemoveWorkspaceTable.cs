using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWorkspaceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropForeignKey(
                name: "FK_ToolDefinitions_Workspaces_WorkspacePath",
                table: "ToolDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_Workspaces_WorkspacePort",
                table: "Workspaces");

            migrationBuilder.DropTable(
                name: "Workspaces");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

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
                    RunAs = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
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

            migrationBuilder.AddForeignKey(
                name: "FK_ToolDefinitions_Workspaces_WorkspacePath",
                table: "ToolDefinitions",
                column: "WorkspacePath",
                principalTable: "Workspaces",
                principalColumn: "WorkspacePath",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
