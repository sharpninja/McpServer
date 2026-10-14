using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QBrainAi.Support.Mcp.Storage.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddHostileReviewTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "RequirementsRecoveryRuns",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "HostileReviewRequests",
                columns: table => new
                {
                    RequestId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ScopeStatement = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    RequestingAgent = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    HttpStatus = table.Column<int>(type: "int", nullable: false),
                    Verdict = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    QualityDisclosure = table.Column<double>(type: "float", nullable: true),
                    QualityScopeClarity = table.Column<double>(type: "float", nullable: true),
                    QualityObjectiveClarity = table.Column<double>(type: "float", nullable: true),
                    QualityConfidence = table.Column<double>(type: "float", nullable: true),
                    QualityEnoughContext = table.Column<bool>(type: "bit", nullable: true),
                    LastErrorCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostileReviewRequests", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_HostileReviewRequests_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HostileReviewArtifactLinks",
                columns: table => new
                {
                    LinkId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ArtifactType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ArtifactId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ContentSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Resolved = table.Column<bool>(type: "bit", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostileReviewArtifactLinks", x => x.LinkId);
                    table.ForeignKey(
                        name: "FK_HostileReviewArtifactLinks_HostileReviewRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "HostileReviewRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HostileReviewArtifactLinks_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HostileReviewDiagnostics",
                columns: table => new
                {
                    DiagnosticId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostileReviewDiagnostics", x => x.DiagnosticId);
                    table.ForeignKey(
                        name: "FK_HostileReviewDiagnostics_HostileReviewRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "HostileReviewRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HostileReviewDiagnostics_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HostileReviewExecutions",
                columns: table => new
                {
                    ExecutionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ReviewerAgent = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Effort = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SourceSurface = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PromptTemplateId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PromptVersion = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    StartedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InputTokens = table.Column<int>(type: "int", nullable: true),
                    OutputTokens = table.Column<int>(type: "int", nullable: true),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostileReviewExecutions", x => x.ExecutionId);
                    table.ForeignKey(
                        name: "FK_HostileReviewExecutions_HostileReviewRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "HostileReviewRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HostileReviewExecutions_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HostileReviewFindings",
                columns: table => new
                {
                    FindingId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ArtifactId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    RequirementId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    EvidenceSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Recommendation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostileReviewFindings", x => x.FindingId);
                    table.ForeignKey(
                        name: "FK_HostileReviewFindings_HostileReviewRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "HostileReviewRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HostileReviewFindings_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HostileReviewArtifactLinks_RequestId_ArtifactType_ArtifactId",
                table: "HostileReviewArtifactLinks",
                columns: new[] { "RequestId", "ArtifactType", "ArtifactId" });

            migrationBuilder.CreateIndex(
                name: "IX_HostileReviewArtifactLinks_WorkspaceId",
                table: "HostileReviewArtifactLinks",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_HostileReviewDiagnostics_RequestId",
                table: "HostileReviewDiagnostics",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_HostileReviewDiagnostics_WorkspaceId",
                table: "HostileReviewDiagnostics",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_HostileReviewExecutions_RequestId_Model_Effort",
                table: "HostileReviewExecutions",
                columns: new[] { "RequestId", "Model", "Effort" });

            migrationBuilder.CreateIndex(
                name: "IX_HostileReviewExecutions_WorkspaceId",
                table: "HostileReviewExecutions",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_HostileReviewFindings_RequestId_Category_Severity",
                table: "HostileReviewFindings",
                columns: new[] { "RequestId", "Category", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_HostileReviewFindings_WorkspaceId",
                table: "HostileReviewFindings",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_HostileReviewRequests_WorkspaceId_CreatedUtc",
                table: "HostileReviewRequests",
                columns: new[] { "WorkspaceId", "CreatedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HostileReviewArtifactLinks");

            migrationBuilder.DropTable(
                name: "HostileReviewDiagnostics");

            migrationBuilder.DropTable(
                name: "HostileReviewExecutions");

            migrationBuilder.DropTable(
                name: "HostileReviewFindings");

            migrationBuilder.DropTable(
                name: "HostileReviewRequests");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "DeletedAtUtc",
                table: "RequirementsRecoveryRuns",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
