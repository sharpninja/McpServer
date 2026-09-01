using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqliteMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddHandoffIngestionStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HandoffIngestionRuns",
                columns: table => new
                {
                    RunId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    WorkspaceId = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    SourceKind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    SourceLocator = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    ContentSha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ExtractedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PromptVersion = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    TemplateVersion = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Agent = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Model = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Confidence = table.Column<double>(type: "REAL", nullable: true),
                    Mode = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ReviewState = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CreatedTodoId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    DraftJson = table.Column<string>(type: "TEXT", nullable: true),
                    Force = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReplayIdentity = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ProcessingState = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ProcessingOwner = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ProcessingLeaseExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ApprovalOwner = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ApprovalLeaseExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TodoCreationIntentId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Succeeded = table.Column<bool>(type: "INTEGER", nullable: false),
                    Error = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    ErrorCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    StateVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    Reviewer = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ReviewNotes = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    DeleteReason = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandoffIngestionRuns", x => x.RunId);
                    table.ForeignKey(
                        name: "FK_HandoffIngestionRuns_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HandoffDiagnostics",
                columns: table => new
                {
                    DiagnosticId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkspaceId = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    RunId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Field = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    DeleteReason = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandoffDiagnostics", x => x.DiagnosticId);
                    table.ForeignKey(
                        name: "FK_HandoffDiagnostics_HandoffIngestionRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "HandoffIngestionRuns",
                        principalColumn: "RunId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HandoffDiagnostics_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HandoffDiagnostics_RunId_Ordinal",
                table: "HandoffDiagnostics",
                columns: new[] { "RunId", "Ordinal" });

            migrationBuilder.CreateIndex(
                name: "IX_HandoffDiagnostics_WorkspaceId",
                table: "HandoffDiagnostics",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_HandoffIngestionRuns_CreatedTodoId",
                table: "HandoffIngestionRuns",
                column: "CreatedTodoId");

            migrationBuilder.CreateIndex(
                name: "IX_HandoffIngestionRuns_ReplayIdentity",
                table: "HandoffIngestionRuns",
                column: "ReplayIdentity",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HandoffIngestionRuns_WorkspaceId_ContentSha256_PromptVersion",
                table: "HandoffIngestionRuns",
                columns: new[] { "WorkspaceId", "ContentSha256", "PromptVersion" });

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "TodoItems",
                type: "TEXT",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HandoffDiagnostics");

            migrationBuilder.DropTable(
                name: "HandoffIngestionRuns");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "TodoItems");
        }
    }
}
