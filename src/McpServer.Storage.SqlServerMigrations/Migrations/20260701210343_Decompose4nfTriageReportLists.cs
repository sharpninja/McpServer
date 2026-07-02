using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations
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
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", nullable: false),
                    ReportId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ListType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
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

        private static string BackfillListSql(string jsonColumn, string listType) => $@"
INSERT INTO [TriageReportListItems] ([WorkspaceId], [ReportId], [ListType], [Ordinal], [Value])
SELECT r.[WorkspaceId], r.[ReportId], '{listType}', CAST(j.[key] AS int), j.[value]
FROM [TriageReports] r
CROSS APPLY OPENJSON(r.[{jsonColumn}]) j
WHERE r.[{jsonColumn}] IS NOT NULL AND ISJSON(r.[{jsonColumn}]) = 1;";

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "AffectedPathsJson", table: "TriageReports", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "AffectedSymbolsJson", table: "TriageReports", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ReproductionHintsJson", table: "TriageReports", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "TagsJson", table: "TriageReports", type: "nvarchar(max)", nullable: true);

            migrationBuilder.Sql(RestoreListSql("AffectedPathsJson", "AffectedPath"));
            migrationBuilder.Sql(RestoreListSql("AffectedSymbolsJson", "AffectedSymbol"));
            migrationBuilder.Sql(RestoreListSql("ReproductionHintsJson", "ReproductionHint"));
            migrationBuilder.Sql(RestoreListSql("TagsJson", "Tag"));

            migrationBuilder.DropTable(name: "TriageReportListItems");
        }

        private static string RestoreListSql(string jsonColumn, string listType) => $@"
UPDATE r SET [{jsonColumn}] = j.[json]
FROM [TriageReports] r
CROSS APPLY (
    SELECT CONCAT('[', STRING_AGG(CONCAT('""', STRING_ESCAPE(i.[Value], 'json'), '""'), ',') WITHIN GROUP (ORDER BY i.[Ordinal]), ']') AS [json]
    FROM [TriageReportListItems] i
    WHERE i.[ReportId] = r.[ReportId] AND i.[ListType] = '{listType}' AND i.[IsDeleted] = 0
) j
WHERE j.[json] IS NOT NULL;";
    }
}
