using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
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
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "character varying(1024)", nullable: false),
                    AgentDefinitionId = table.Column<string>(type: "character varying(64)", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    DeleteReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
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
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "character varying(1024)", nullable: false),
                    AgentWorkspaceId = table.Column<int>(type: "integer", nullable: false),
                    ListType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    DeleteReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
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
SELECT a."WorkspaceId", a."Id", (j.ordinality - 1)::int, j.value
FROM "AgentDefinitions" a
CROSS JOIN LATERAL jsonb_array_elements_text(a."DefaultModelsJson"::jsonb) WITH ORDINALITY AS j(value, ordinality)
WHERE a."DefaultModelsJson" IS NOT NULL AND jsonb_typeof(a."DefaultModelsJson"::jsonb) = 'array';
""");

            migrationBuilder.Sql("""
INSERT INTO "AgentWorkspaceListItems" ("WorkspaceId", "AgentWorkspaceId", "ListType", "Ordinal", "Value")
SELECT w."WorkspaceId", w."Id", 'ModelOverride', (j.ordinality - 1)::int, j.value
FROM "AgentWorkspaces" w
CROSS JOIN LATERAL jsonb_array_elements_text(w."ModelsOverrideJson"::jsonb) WITH ORDINALITY AS j(value, ordinality)
WHERE w."ModelsOverrideJson" IS NOT NULL AND jsonb_typeof(w."ModelsOverrideJson"::jsonb) = 'array';
""");

            migrationBuilder.Sql("""
INSERT INTO "AgentWorkspaceListItems" ("WorkspaceId", "AgentWorkspaceId", "ListType", "Ordinal", "Value")
SELECT w."WorkspaceId", w."Id", 'InstructionFileOverride', (j.ordinality - 1)::int, j.value
FROM "AgentWorkspaces" w
CROSS JOIN LATERAL jsonb_array_elements_text(w."InstructionFilesOverrideJson"::jsonb) WITH ORDINALITY AS j(value, ordinality)
WHERE w."InstructionFilesOverrideJson" IS NOT NULL AND jsonb_typeof(w."InstructionFilesOverrideJson"::jsonb) = 'array';
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
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelsOverrideJson",
                table: "AgentWorkspaces",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultModelsJson",
                table: "AgentDefinitions",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Reconstruct the JSON arrays (ordered) from the child rows.
            migrationBuilder.Sql("""
UPDATE "AgentDefinitions" a
SET "DefaultModelsJson" = COALESCE(j.json, '[]')
FROM (
    SELECT "AgentDefinitionId", jsonb_agg("Model" ORDER BY "Ordinal")::text AS json
    FROM "AgentDefinitionModels"
    WHERE "IsDeleted" = false
    GROUP BY "AgentDefinitionId"
) j
WHERE j."AgentDefinitionId" = a."Id";
""");

            migrationBuilder.Sql("""
UPDATE "AgentWorkspaces" w
SET "ModelsOverrideJson" = j.json
FROM (
    SELECT "AgentWorkspaceId", jsonb_agg("Value" ORDER BY "Ordinal")::text AS json
    FROM "AgentWorkspaceListItems"
    WHERE "ListType" = 'ModelOverride' AND "IsDeleted" = false
    GROUP BY "AgentWorkspaceId"
) j
WHERE j."AgentWorkspaceId" = w."Id";
""");

            migrationBuilder.Sql("""
UPDATE "AgentWorkspaces" w
SET "InstructionFilesOverrideJson" = j.json
FROM (
    SELECT "AgentWorkspaceId", jsonb_agg("Value" ORDER BY "Ordinal")::text AS json
    FROM "AgentWorkspaceListItems"
    WHERE "ListType" = 'InstructionFileOverride' AND "IsDeleted" = false
    GROUP BY "AgentWorkspaceId"
) j
WHERE j."AgentWorkspaceId" = w."Id";
""");

            migrationBuilder.DropTable(
                name: "AgentDefinitionModels");

            migrationBuilder.DropTable(
                name: "AgentWorkspaceListItems");
        }
    }
}
