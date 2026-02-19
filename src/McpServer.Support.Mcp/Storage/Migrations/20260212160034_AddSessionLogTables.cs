using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.Migrations
{
    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA1062:Validate arguments of public methods", Justification = "Auto-generated EF Core migration")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Auto-generated EF Core migration")]
    public partial class AddSessionLogTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SessionId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    Model = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Started = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    EntryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalTokens = table.Column<int>(type: "INTEGER", nullable: true),
                    CursorSessionLabel = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    CopilotAvgSuccessScore = table.Column<double>(type: "REAL", nullable: true),
                    CopilotTotalNetTokens = table.Column<int>(type: "INTEGER", nullable: true),
                    CopilotTotalNetPremiumRequests = table.Column<int>(type: "INTEGER", nullable: true),
                    CopilotCompletedCount = table.Column<int>(type: "INTEGER", nullable: true),
                    CopilotInProgressCount = table.Column<int>(type: "INTEGER", nullable: true),
                    Project = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    TargetFramework = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Repository = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Branch = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SessionLogEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionLogId = table.Column<long>(type: "INTEGER", nullable: false),
                    RequestId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Model = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ModelProvider = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    QueryText = table.Column<string>(type: "TEXT", nullable: true),
                    QueryTitle = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    Response = table.Column<string>(type: "TEXT", nullable: true),
                    Interpretation = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    TokenCount = table.Column<int>(type: "INTEGER", nullable: true),
                    FailureNote = table.Column<string>(type: "TEXT", nullable: true),
                    Score = table.Column<double>(type: "REAL", nullable: true),
                    IsPremium = table.Column<bool>(type: "INTEGER", nullable: true),
                    RawContextJson = table.Column<string>(type: "TEXT", nullable: true),
                    OriginalEntryJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogEntries_SessionLogs_SessionLogId",
                        column: x => x.SessionLogId,
                        principalTable: "SessionLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionLogActions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionLogEntryId = table.Column<long>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogActions_SessionLogEntries_SessionLogEntryId",
                        column: x => x.SessionLogEntryId,
                        principalTable: "SessionLogEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionLogEntryContexts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionLogEntryId = table.Column<long>(type: "INTEGER", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    ContextItem = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogEntryContexts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogEntryContexts_SessionLogEntries_SessionLogEntryId",
                        column: x => x.SessionLogEntryId,
                        principalTable: "SessionLogEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionLogEntryTags",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionLogEntryId = table.Column<long>(type: "INTEGER", nullable: false),
                    Tag = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogEntryTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogEntryTags_SessionLogEntries_SessionLogEntryId",
                        column: x => x.SessionLogEntryId,
                        principalTable: "SessionLogEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogActions_SessionLogEntryId",
                table: "SessionLogActions",
                column: "SessionLogEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogEntries_SessionLogId_RequestId",
                table: "SessionLogEntries",
                columns: new[] { "SessionLogId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogEntryContexts_SessionLogEntryId",
                table: "SessionLogEntryContexts",
                column: "SessionLogEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogEntryTags_SessionLogEntryId",
                table: "SessionLogEntryTags",
                column: "SessionLogEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_LastUpdated",
                table: "SessionLogs",
                column: "LastUpdated");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_SourceType",
                table: "SessionLogs",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_SourceType_SessionId",
                table: "SessionLogs",
                columns: new[] { "SourceType", "SessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_Started",
                table: "SessionLogs",
                column: "Started");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionLogActions");

            migrationBuilder.DropTable(
                name: "SessionLogEntryContexts");

            migrationBuilder.DropTable(
                name: "SessionLogEntryTags");

            migrationBuilder.DropTable(
                name: "SessionLogEntries");

            migrationBuilder.DropTable(
                name: "SessionLogs");
        }
    }
}
