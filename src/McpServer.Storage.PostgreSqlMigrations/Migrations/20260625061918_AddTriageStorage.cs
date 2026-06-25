using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddTriageStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TriageGroups",
                columns: table => new
                {
                    GroupId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    WorkspaceId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    GroupKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EffectiveWorkspacePath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReportCount = table.Column<int>(type: "integer", nullable: false),
                    FirstReportAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastReportAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    QuietDeadlineUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsMcpServerRelated = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedTodoId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    DeleteReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TriageGroups", x => x.GroupId);
                    table.ForeignKey(
                        name: "FK_TriageGroups_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TriageReports",
                columns: table => new
                {
                    ReportId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    WorkspaceId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    GroupId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OriginalWorkspacePath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    EffectiveWorkspacePath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    ObservedBehavior = table.Column<string>(type: "text", nullable: true),
                    ExpectedBehavior = table.Column<string>(type: "text", nullable: true),
                    Severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Component = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DedupeKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ErrorSignature = table.Column<string>(type: "text", nullable: true),
                    Fingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AffectedPathsJson = table.Column<string>(type: "text", nullable: true),
                    AffectedSymbolsJson = table.Column<string>(type: "text", nullable: true),
                    EvidenceJson = table.Column<string>(type: "text", nullable: true),
                    ReproductionHintsJson = table.Column<string>(type: "text", nullable: true),
                    TagsJson = table.Column<string>(type: "text", nullable: true),
                    ReporterAgent = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TurnId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CurrentTodoId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeleteReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TriageReports", x => x.ReportId);
                    table.ForeignKey(
                        name: "FK_TriageReports_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TriageResearchRuns",
                columns: table => new
                {
                    RunId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    WorkspaceId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    GroupId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PromptTemplateId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Prompt = table.Column<string>(type: "text", nullable: true),
                    GroupJson = table.Column<string>(type: "text", nullable: true),
                    RawOutput = table.Column<string>(type: "text", nullable: true),
                    ResponseJson = table.Column<string>(type: "text", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true),
                    StartedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedTodoId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DeleteReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TriageResearchRuns", x => x.RunId);
                    table.ForeignKey(
                        name: "FK_TriageResearchRuns_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TriageGroups_CreatedTodoId",
                table: "TriageGroups",
                column: "CreatedTodoId");

            migrationBuilder.CreateIndex(
                name: "IX_TriageGroups_WorkspaceId",
                table: "TriageGroups",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_TriageGroups_WorkspaceId_GroupKey",
                table: "TriageGroups",
                columns: new[] { "WorkspaceId", "GroupKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TriageGroups_WorkspaceId_Status_QuietDeadlineUtc",
                table: "TriageGroups",
                columns: new[] { "WorkspaceId", "Status", "QuietDeadlineUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TriageReports_CreatedUtc",
                table: "TriageReports",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TriageReports_WorkspaceId",
                table: "TriageReports",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_TriageReports_WorkspaceId_Fingerprint",
                table: "TriageReports",
                columns: new[] { "WorkspaceId", "Fingerprint" });

            migrationBuilder.CreateIndex(
                name: "IX_TriageReports_WorkspaceId_GroupId",
                table: "TriageReports",
                columns: new[] { "WorkspaceId", "GroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_TriageReports_WorkspaceId_IdempotencyKey",
                table: "TriageReports",
                columns: new[] { "WorkspaceId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TriageResearchRuns_Status",
                table: "TriageResearchRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TriageResearchRuns_WorkspaceId",
                table: "TriageResearchRuns",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_TriageResearchRuns_WorkspaceId_GroupId_StartedUtc",
                table: "TriageResearchRuns",
                columns: new[] { "WorkspaceId", "GroupId", "StartedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TriageGroups");

            migrationBuilder.DropTable(
                name: "TriageReports");

            migrationBuilder.DropTable(
                name: "TriageResearchRuns");
        }
    }
}
