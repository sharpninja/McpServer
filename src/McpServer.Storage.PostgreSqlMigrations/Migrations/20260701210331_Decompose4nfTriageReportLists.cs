using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Decompose4nfTriageReportLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TriageReportListItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "character varying(1024)", nullable: false),
                    ReportId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
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
                    table.PrimaryKey("PK_TriageReportListItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TriageReportListItems_TriageReports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "TriageReports",
                        principalColumn: "ReportId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TriageReportListItems_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TriageReportListItems_ReportId_ListType_Ordinal",
                table: "TriageReportListItems",
                columns: new[] { "ReportId", "ListType", "Ordinal" });

            migrationBuilder.CreateIndex(
                name: "IX_TriageReportListItems_WorkspaceId",
                table: "TriageReportListItems",
                column: "WorkspaceId");

            // TR-MCP-TRIAGE-001 data migration: backfill the four JSON string arrays into ordered
            // 4NF child rows (discriminated by ListType) before the source columns are dropped.
            migrationBuilder.Sql(BackfillListSql("AffectedPathsJson", "AffectedPath"));
            migrationBuilder.Sql(BackfillListSql("AffectedSymbolsJson", "AffectedSymbol"));
            migrationBuilder.Sql(BackfillListSql("ReproductionHintsJson", "ReproductionHint"));
            migrationBuilder.Sql(BackfillListSql("TagsJson", "Tag"));

            migrationBuilder.DropColumn(name: "AffectedPathsJson", table: "TriageReports");
            migrationBuilder.DropColumn(name: "AffectedSymbolsJson", table: "TriageReports");
            migrationBuilder.DropColumn(name: "ReproductionHintsJson", table: "TriageReports");
            migrationBuilder.DropColumn(name: "TagsJson", table: "TriageReports");
        }

        private static string BackfillListSql(string jsonColumn, string listType) => $"""
INSERT INTO "TriageReportListItems" ("WorkspaceId", "ReportId", "ListType", "Ordinal", "Value")
SELECT r."WorkspaceId", r."ReportId", '{listType}', (j.ordinality - 1)::int, j.value
FROM "TriageReports" r
CROSS JOIN LATERAL jsonb_array_elements_text(r."{jsonColumn}"::jsonb) WITH ORDINALITY AS j(value, ordinality)
WHERE r."{jsonColumn}" IS NOT NULL AND jsonb_typeof(r."{jsonColumn}"::jsonb) = 'array';
""";

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "AffectedPathsJson", table: "TriageReports", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "AffectedSymbolsJson", table: "TriageReports", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ReproductionHintsJson", table: "TriageReports", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "TagsJson", table: "TriageReports", type: "text", nullable: true);

            migrationBuilder.Sql(RestoreListSql("AffectedPathsJson", "AffectedPath"));
            migrationBuilder.Sql(RestoreListSql("AffectedSymbolsJson", "AffectedSymbol"));
            migrationBuilder.Sql(RestoreListSql("ReproductionHintsJson", "ReproductionHint"));
            migrationBuilder.Sql(RestoreListSql("TagsJson", "Tag"));

            migrationBuilder.DropTable(
                name: "TriageReportListItems");
        }

        private static string RestoreListSql(string jsonColumn, string listType) => $"""
UPDATE "TriageReports" r
SET "{jsonColumn}" = j.json
FROM (
    SELECT "ReportId", jsonb_agg("Value" ORDER BY "Ordinal")::text AS json
    FROM "TriageReportListItems"
    WHERE "ListType" = '{listType}' AND "IsDeleted" = false
    GROUP BY "ReportId"
) j
WHERE j."ReportId" = r."ReportId";
""";
    }
}
