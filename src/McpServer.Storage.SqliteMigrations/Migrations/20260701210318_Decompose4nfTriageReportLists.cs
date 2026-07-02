using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqliteMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Decompose4nfTriageReportLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AffectedPathsJson",
                table: "TriageReports");

            migrationBuilder.DropColumn(
                name: "AffectedSymbolsJson",
                table: "TriageReports");

            migrationBuilder.DropColumn(
                name: "ReproductionHintsJson",
                table: "TriageReports");

            migrationBuilder.DropColumn(
                name: "TagsJson",
                table: "TriageReports");

            migrationBuilder.CreateTable(
                name: "TriageReportListItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkspaceId = table.Column<string>(type: "TEXT", nullable: false),
                    ReportId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TriageReportListItems");

            migrationBuilder.AddColumn<string>(
                name: "AffectedPathsJson",
                table: "TriageReports",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AffectedSymbolsJson",
                table: "TriageReports",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReproductionHintsJson",
                table: "TriageReports",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TagsJson",
                table: "TriageReports",
                type: "TEXT",
                nullable: true);
        }
    }
}
