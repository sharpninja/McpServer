using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqliteMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddTodoStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TodoAuditHistory",
                columns: table => new
                {
                    AuditId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TodoId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    RecordedAtUtc = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SnapshotJson = table.Column<string>(type: "TEXT", nullable: true),
                    PreviousSnapshotJson = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoAuditHistory", x => x.AuditId);
                });

            migrationBuilder.CreateTable(
                name: "TodoDocumentMetadata",
                columns: table => new
                {
                    SingletonId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NotesJson = table.Column<string>(type: "TEXT", nullable: true),
                    CompletedJson = table.Column<string>(type: "TEXT", nullable: true),
                    CodeReviewReference = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    LastImportedFromYamlUtc = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    LastProjectedToYamlUtc = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    LastProjectionFailureUtc = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    LastProjectionFailureMessage = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoDocumentMetadata", x => x.SingletonId);
                    table.CheckConstraint("CK_TodoDocumentMetadata_Singleton", "\"SingletonId\" = 1");
                });

            migrationBuilder.CreateTable(
                name: "TodoItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Section = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Done = table.Column<bool>(type: "INTEGER", nullable: false),
                    Estimate = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Note = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: true),
                    DescriptionJson = table.Column<string>(type: "TEXT", nullable: true),
                    TechnicalDetailsJson = table.Column<string>(type: "TEXT", nullable: true),
                    ImplementationTasksJson = table.Column<string>(type: "TEXT", nullable: true),
                    CompletedDate = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    DoneSummary = table.Column<string>(type: "TEXT", nullable: true),
                    Remaining = table.Column<string>(type: "TEXT", nullable: true),
                    PriorityNote = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    DependsOnJson = table.Column<string>(type: "TEXT", nullable: true),
                    FunctionalRequirementsJson = table.Column<string>(type: "TEXT", nullable: true),
                    TechnicalRequirementsJson = table.Column<string>(type: "TEXT", nullable: true),
                    ItemKind = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    SectionOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    PhaseLabel = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TodoAuditHistory_Action",
                table: "TodoAuditHistory",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_TodoAuditHistory_TodoId_RecordedAtUtc",
                table: "TodoAuditHistory",
                columns: new[] { "TodoId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoAuditHistory_TodoId_Version",
                table: "TodoAuditHistory",
                columns: new[] { "TodoId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_Done",
                table: "TodoItems",
                column: "Done");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_Priority",
                table: "TodoItems",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_TodoItems_Section",
                table: "TodoItems",
                column: "Section");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TodoAuditHistory");

            migrationBuilder.DropTable(
                name: "TodoDocumentMetadata");

            migrationBuilder.DropTable(
                name: "TodoItems");
        }
    }
}
