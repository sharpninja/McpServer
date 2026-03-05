using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.Migrations
{
    /// <inheritdoc />
    public partial class PendingModelSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentDefinitions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    DefaultLaunchCommand = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    DefaultInstructionFile = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    DefaultModelsJson = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultBranchStrategy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    DefaultSeedPrompt = table.Column<string>(type: "TEXT", nullable: false),
                    IsBuiltIn = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentEventLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AgentId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    WorkspacePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    DetailsJson = table.Column<string>(type: "TEXT", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentEventLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentWorkspaces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AgentDefinitionId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    WorkspacePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Banned = table.Column<bool>(type: "INTEGER", nullable: false),
                    BannedReason = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    BannedUntilPr = table.Column<int>(type: "INTEGER", nullable: true),
                    AgentIsolation = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    LaunchCommandOverride = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    ModelsOverrideJson = table.Column<string>(type: "TEXT", nullable: true),
                    BranchStrategyOverride = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    SeedPromptOverride = table.Column<string>(type: "TEXT", nullable: true),
                    MarkerAdditions = table.Column<string>(type: "TEXT", nullable: false),
                    InstructionFilesOverrideJson = table.Column<string>(type: "TEXT", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLaunchedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentWorkspaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentWorkspaces_AgentDefinitions_AgentDefinitionId",
                        column: x => x.AgentDefinitionId,
                        principalTable: "AgentDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentDefinitions_IsBuiltIn",
                table: "AgentDefinitions",
                column: "IsBuiltIn");

            migrationBuilder.CreateIndex(
                name: "IX_AgentEventLogs_AgentId",
                table: "AgentEventLogs",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentEventLogs_EventType",
                table: "AgentEventLogs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AgentEventLogs_Timestamp",
                table: "AgentEventLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AgentEventLogs_WorkspacePath",
                table: "AgentEventLogs",
                column: "WorkspacePath");

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkspaces_AgentDefinitionId_WorkspacePath",
                table: "AgentWorkspaces",
                columns: new[] { "AgentDefinitionId", "WorkspacePath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkspaces_WorkspacePath",
                table: "AgentWorkspaces",
                column: "WorkspacePath");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentEventLogs");

            migrationBuilder.DropTable(
                name: "AgentWorkspaces");

            migrationBuilder.DropTable(
                name: "AgentDefinitions");
        }
    }
}
