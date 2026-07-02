using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqliteMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Decompose4nfRequirementAcceptanceCriteria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequirementAcceptanceCriteria",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkspaceId = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    RequirementKind = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    RequirementId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    CriterionId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    IsSatisfied = table.Column<bool>(type: "INTEGER", nullable: false),
                    Evidence = table.Column<string>(type: "TEXT", nullable: true),
                    DeleteReason = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequirementAcceptanceCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequirementAcceptanceCriteria_Requirements_WorkspaceId_RequirementKind_RequirementId",
                        columns: x => new { x.WorkspaceId, x.RequirementKind, x.RequirementId },
                        principalTable: "Requirements",
                        principalColumns: new[] { "WorkspaceId", "Kind", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequirementAcceptanceCriteria_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequirementAcceptanceCriteria_WorkspaceId_RequirementKind_RequirementId_Ordinal",
                table: "RequirementAcceptanceCriteria",
                columns: new[] { "WorkspaceId", "RequirementKind", "RequirementId", "Ordinal" });

            // TR-MCP-REQAC-001 data migration: backfill AcceptanceCriteriaJson (array of
            // {id, text, isSatisfied, evidence} objects) into ordered 4NF child rows before the
            // source column is dropped.
            migrationBuilder.Sql("""
INSERT INTO "RequirementAcceptanceCriteria" ("WorkspaceId", "RequirementKind", "RequirementId", "Ordinal", "CriterionId", "Text", "IsSatisfied", "Evidence")
SELECT r."WorkspaceId", r."Kind", r."Id", j."key",
       COALESCE(json_extract(j."value", '$.id'), ''),
       COALESCE(json_extract(j."value", '$.text'), ''),
       COALESCE(json_extract(j."value", '$.isSatisfied'), 0),
       json_extract(j."value", '$.evidence')
FROM "Requirements" r, json_each(r."AcceptanceCriteriaJson") j
WHERE r."AcceptanceCriteriaJson" IS NOT NULL AND json_valid(r."AcceptanceCriteriaJson");
""");

            migrationBuilder.DropColumn(
                name: "AcceptanceCriteriaJson",
                table: "Requirements");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcceptanceCriteriaJson",
                table: "Requirements",
                type: "TEXT",
                nullable: true);

            // Reconstruct the JSON object array (camelCase keys, ordered) from the child rows.
            migrationBuilder.Sql("""
UPDATE "Requirements"
SET "AcceptanceCriteriaJson" = (
    SELECT json_group_array(json_object(
        'id', c."CriterionId",
        'text', c."Text",
        'isSatisfied', json(CASE WHEN c."IsSatisfied" THEN 'true' ELSE 'false' END),
        'evidence', c."Evidence") ORDER BY c."Ordinal")
    FROM "RequirementAcceptanceCriteria" c
    WHERE c."WorkspaceId" = "Requirements"."WorkspaceId" AND c."RequirementKind" = "Requirements"."Kind"
      AND c."RequirementId" = "Requirements"."Id" AND c."IsDeleted" = 0
)
WHERE EXISTS (
    SELECT 1 FROM "RequirementAcceptanceCriteria" c
    WHERE c."WorkspaceId" = "Requirements"."WorkspaceId" AND c."RequirementKind" = "Requirements"."Kind"
      AND c."RequirementId" = "Requirements"."Id" AND c."IsDeleted" = 0);
""");

            migrationBuilder.DropTable(
                name: "RequirementAcceptanceCriteria");
        }
    }
}
