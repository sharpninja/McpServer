using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
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
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    RequirementKind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    RequirementId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    CriterionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    IsSatisfied = table.Column<bool>(type: "boolean", nullable: false),
                    Evidence = table.Column<string>(type: "text", nullable: true),
                    DeleteReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequirementAcceptanceCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequirementAcceptanceCriteria_Requirements_WorkspaceId_Requ~",
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
                name: "IX_RequirementAcceptanceCriteria_WorkspaceId_RequirementKind_R~",
                table: "RequirementAcceptanceCriteria",
                columns: new[] { "WorkspaceId", "RequirementKind", "RequirementId", "Ordinal" });

            // TR-MCP-REQAC-001 data migration: backfill AcceptanceCriteriaJson (array of
            // {id, text, isSatisfied, evidence} objects) into ordered 4NF child rows before the
            // source column is dropped.
            migrationBuilder.Sql("""
INSERT INTO "RequirementAcceptanceCriteria" ("WorkspaceId", "RequirementKind", "RequirementId", "Ordinal", "CriterionId", "Text", "IsSatisfied", "Evidence")
SELECT r."WorkspaceId", r."Kind", r."Id", (j.ordinality - 1)::int,
       COALESCE(j.value ->> 'id', ''), COALESCE(j.value ->> 'text', ''),
       COALESCE((j.value ->> 'isSatisfied')::boolean, false), j.value ->> 'evidence'
FROM "Requirements" r
CROSS JOIN LATERAL jsonb_array_elements(r."AcceptanceCriteriaJson"::jsonb) WITH ORDINALITY AS j(value, ordinality)
WHERE r."AcceptanceCriteriaJson" IS NOT NULL AND jsonb_typeof(r."AcceptanceCriteriaJson"::jsonb) = 'array';
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
                type: "text",
                nullable: true);

            // Reconstruct the JSON object array (camelCase keys, ordered) from the child rows.
            migrationBuilder.Sql("""
UPDATE "Requirements" r
SET "AcceptanceCriteriaJson" = j.json
FROM (
    SELECT "WorkspaceId", "RequirementKind", "RequirementId",
           jsonb_agg(jsonb_build_object(
               'id', "CriterionId",
               'text', "Text",
               'isSatisfied', "IsSatisfied",
               'evidence', "Evidence") ORDER BY "Ordinal")::text AS json
    FROM "RequirementAcceptanceCriteria"
    WHERE "IsDeleted" = false
    GROUP BY "WorkspaceId", "RequirementKind", "RequirementId"
) j
WHERE j."WorkspaceId" = r."WorkspaceId" AND j."RequirementKind" = r."Kind" AND j."RequirementId" = r."Id";
""");

            migrationBuilder.DropTable(
                name: "RequirementAcceptanceCriteria");
        }
    }
}
