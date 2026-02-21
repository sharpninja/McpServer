using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddToolRegistryAndBuckets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ToolBuckets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Owner = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Repo = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Branch = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ManifestPath = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    DateTimeCreated = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DateTimeLastSynced = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolBuckets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ToolDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    ParameterSchema = table.Column<string>(type: "TEXT", maxLength: 8192, nullable: true),
                    CommandTemplate = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    WorkspacePath = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    BucketName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    DateTimeCreated = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DateTimeModified = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToolDefinitions_Workspaces_WorkspacePath",
                        column: x => x.WorkspacePath,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspacePath",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ToolDefinitionTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ToolDefinitionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tag = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolDefinitionTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToolDefinitionTags_ToolDefinitions_ToolDefinitionId",
                        column: x => x.ToolDefinitionId,
                        principalTable: "ToolDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ToolBuckets_Name",
                table: "ToolBuckets",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToolDefinitions_Name_WorkspacePath",
                table: "ToolDefinitions",
                columns: new[] { "Name", "WorkspacePath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToolDefinitions_WorkspacePath",
                table: "ToolDefinitions",
                column: "WorkspacePath");

            migrationBuilder.CreateIndex(
                name: "IX_ToolDefinitionTags_Tag",
                table: "ToolDefinitionTags",
                column: "Tag");

            migrationBuilder.CreateIndex(
                name: "IX_ToolDefinitionTags_ToolDefinitionId_Tag",
                table: "ToolDefinitionTags",
                columns: new[] { "ToolDefinitionId", "Tag" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ToolBuckets");

            migrationBuilder.DropTable(
                name: "ToolDefinitionTags");

            migrationBuilder.DropTable(
                name: "ToolDefinitions");
        }
    }
}
