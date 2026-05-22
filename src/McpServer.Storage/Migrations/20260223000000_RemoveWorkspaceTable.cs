using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(McpDbContext))]
    [Migration("20260223000000_RemoveWorkspaceTable")]
    public partial class RemoveWorkspaceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            if (ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.Sql("PRAGMA foreign_keys=OFF;", suppressTransaction: true);
                migrationBuilder.Sql(
                    """
                    DROP INDEX IF EXISTS "IX_ToolDefinitions_WorkspacePath";
                    DROP INDEX IF EXISTS "IX_ToolDefinitions_Name_WorkspacePath";

                    CREATE TABLE "__ef_temp_ToolDefinitions" (
                        "Id" INTEGER NOT NULL CONSTRAINT "PK_ToolDefinitions" PRIMARY KEY AUTOINCREMENT,
                        "Name" TEXT NOT NULL,
                        "Description" TEXT NOT NULL,
                        "ParameterSchema" TEXT NULL,
                        "CommandTemplate" TEXT NULL,
                        "WorkspacePath" TEXT NULL,
                        "BucketName" TEXT NULL,
                        "DateTimeCreated" TEXT NOT NULL,
                        "DateTimeModified" TEXT NOT NULL
                    );

                    INSERT INTO "__ef_temp_ToolDefinitions" (
                        "Id",
                        "Name",
                        "Description",
                        "ParameterSchema",
                        "CommandTemplate",
                        "WorkspacePath",
                        "BucketName",
                        "DateTimeCreated",
                        "DateTimeModified")
                    SELECT
                        "Id",
                        "Name",
                        "Description",
                        "ParameterSchema",
                        "CommandTemplate",
                        "WorkspacePath",
                        "BucketName",
                        "DateTimeCreated",
                        "DateTimeModified"
                    FROM "ToolDefinitions";

                    DROP TABLE "ToolDefinitions";
                    ALTER TABLE "__ef_temp_ToolDefinitions" RENAME TO "ToolDefinitions";

                    CREATE UNIQUE INDEX "IX_ToolDefinitions_Name_WorkspacePath"
                    ON "ToolDefinitions" ("Name", "WorkspacePath");

                    CREATE INDEX "IX_ToolDefinitions_WorkspacePath"
                    ON "ToolDefinitions" ("WorkspacePath");

                    DROP INDEX IF EXISTS "IX_Workspaces_WorkspacePort";
                    DROP TABLE IF EXISTS "Workspaces";
                    """);
                migrationBuilder.Sql("PRAGMA foreign_keys=ON;", suppressTransaction: true);
                return;
            }

            if (!ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.DropForeignKey(
                    name: "FK_ToolDefinitions_Workspaces_WorkspacePath",
                    table: "ToolDefinitions");
            }

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

            if (!ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
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
}
