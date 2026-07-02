using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqliteMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Decompose4nfAgentModelLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentDefinitionModels",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkspaceId = table.Column<string>(type: "TEXT", nullable: false),
                    AgentDefinitionId = table.Column<string>(type: "TEXT", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: false),
                    DeleteReason = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentDefinitionModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentDefinitionModels_AgentDefinitions_AgentDefinitionId",
                        column: x => x.AgentDefinitionId,
                        principalTable: "AgentDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentDefinitionModels_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgentWorkspaceListItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkspaceId = table.Column<string>(type: "TEXT", nullable: false),
                    AgentWorkspaceId = table.Column<int>(type: "INTEGER", nullable: false),
                    ListType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    DeleteReason = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentWorkspaceListItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentWorkspaceListItems_AgentWorkspaces_AgentWorkspaceId",
                        column: x => x.AgentWorkspaceId,
                        principalTable: "AgentWorkspaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentWorkspaceListItems_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentDefinitionModels_AgentDefinitionId_Ordinal",
                table: "AgentDefinitionModels",
                columns: new[] { "AgentDefinitionId", "Ordinal" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentDefinitionModels_WorkspaceId",
                table: "AgentDefinitionModels",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkspaceListItems_AgentWorkspaceId_ListType_Ordinal",
                table: "AgentWorkspaceListItems",
                columns: new[] { "AgentWorkspaceId", "ListType", "Ordinal" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentWorkspaceListItems_WorkspaceId",
                table: "AgentWorkspaceListItems",
                column: "WorkspaceId");

            // Data migration: backfill DefaultModelsJson and the two override JSON arrays into
            // ordered 4NF child rows before the source columns are dropped.
            migrationBuilder.Sql("""
INSERT INTO "AgentDefinitionModels" ("WorkspaceId", "AgentDefinitionId", "Ordinal", "Model")
SELECT a."WorkspaceId", a."Id", j."key", j."value"
FROM "AgentDefinitions" a, json_each(a."DefaultModelsJson") j
WHERE a."DefaultModelsJson" IS NOT NULL AND json_valid(a."DefaultModelsJson");
""");

            migrationBuilder.Sql("""
INSERT INTO "AgentWorkspaceListItems" ("WorkspaceId", "AgentWorkspaceId", "ListType", "Ordinal", "Value")
SELECT w."WorkspaceId", w."Id", 'ModelOverride', j."key", j."value"
FROM "AgentWorkspaces" w, json_each(w."ModelsOverrideJson") j
WHERE w."ModelsOverrideJson" IS NOT NULL AND json_valid(w."ModelsOverrideJson");
""");

            migrationBuilder.Sql("""
INSERT INTO "AgentWorkspaceListItems" ("WorkspaceId", "AgentWorkspaceId", "ListType", "Ordinal", "Value")
SELECT w."WorkspaceId", w."Id", 'InstructionFileOverride', j."key", j."value"
FROM "AgentWorkspaces" w, json_each(w."InstructionFilesOverrideJson") j
WHERE w."InstructionFilesOverrideJson" IS NOT NULL AND json_valid(w."InstructionFilesOverrideJson");
""");

            migrationBuilder.DropColumn(name: "InstructionFilesOverrideJson", table: "AgentWorkspaces");
            migrationBuilder.DropColumn(name: "ModelsOverrideJson", table: "AgentWorkspaces");
            migrationBuilder.DropColumn(name: "DefaultModelsJson", table: "AgentDefinitions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InstructionFilesOverrideJson",
                table: "AgentWorkspaces",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelsOverrideJson",
                table: "AgentWorkspaces",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultModelsJson",
                table: "AgentDefinitions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            // Reconstruct the JSON arrays (ordered) from the child rows.
            migrationBuilder.Sql("""
UPDATE "AgentDefinitions"
SET "DefaultModelsJson" = COALESCE((
    SELECT json_group_array(m."Model" ORDER BY m."Ordinal")
    FROM "AgentDefinitionModels" m
    WHERE m."AgentDefinitionId" = "AgentDefinitions"."Id" AND m."IsDeleted" = 0
), '[]');
""");

            migrationBuilder.Sql("""
UPDATE "AgentWorkspaces"
SET "ModelsOverrideJson" = (
    SELECT json_group_array(i."Value" ORDER BY i."Ordinal")
    FROM "AgentWorkspaceListItems" i
    WHERE i."AgentWorkspaceId" = "AgentWorkspaces"."Id" AND i."ListType" = 'ModelOverride' AND i."IsDeleted" = 0
)
WHERE EXISTS (
    SELECT 1 FROM "AgentWorkspaceListItems" i
    WHERE i."AgentWorkspaceId" = "AgentWorkspaces"."Id" AND i."ListType" = 'ModelOverride' AND i."IsDeleted" = 0);
""");

            migrationBuilder.Sql("""
UPDATE "AgentWorkspaces"
SET "InstructionFilesOverrideJson" = (
    SELECT json_group_array(i."Value" ORDER BY i."Ordinal")
    FROM "AgentWorkspaceListItems" i
    WHERE i."AgentWorkspaceId" = "AgentWorkspaces"."Id" AND i."ListType" = 'InstructionFileOverride' AND i."IsDeleted" = 0
)
WHERE EXISTS (
    SELECT 1 FROM "AgentWorkspaceListItems" i
    WHERE i."AgentWorkspaceId" = "AgentWorkspaces"."Id" AND i."ListType" = 'InstructionFileOverride' AND i."IsDeleted" = 0);
""");

            migrationBuilder.DropTable(
                name: "AgentDefinitionModels");

            migrationBuilder.DropTable(
                name: "AgentWorkspaceListItems");
        }
    }
}
