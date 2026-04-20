using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations
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
                    AuditId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TodoId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RecordedAtUtc = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreviousSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoAuditHistory", x => x.AuditId);
                });

            migrationBuilder.CreateTable(
                name: "TodoDocumentMetadata",
                columns: table => new
                {
                    SingletonId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NotesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletedJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeReviewReference = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    LastImportedFromYamlUtc = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LastProjectedToYamlUtc = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LastProjectionFailureUtc = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LastProjectionFailureMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    Id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Section = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Done = table.Column<bool>(type: "bit", nullable: false),
                    Estimate = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: true),
                    DescriptionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TechnicalDetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImplementationTasksJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletedDate = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DoneSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remaining = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PriorityNote = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DependsOnJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FunctionalRequirementsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TechnicalRequirementsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItemKind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SectionOrder = table.Column<int>(type: "int", nullable: false),
                    ItemOrder = table.Column<int>(type: "int", nullable: false),
                    PhaseLabel = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
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
