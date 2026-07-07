using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionLogTables : Migration
    {
        private static readonly string[] SessionLogTurnRequestIndexColumns = { "SessionLogId", "RequestId" };
        private static readonly string[] SessionSourceSessionIndexColumns = { "SourceType", "SessionId" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

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
                name: "SessionLogTurns",
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
                    table.PrimaryKey("PK_SessionLogTurns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogTurns_SessionLogs_SessionLogId",
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
                    SessionLogTurnId = table.Column<long>(type: "INTEGER", nullable: false),
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
                        name: "FK_SessionLogActions_SessionLogTurns_SessionLogTurnId",
                        column: x => x.SessionLogTurnId,
                        principalTable: "SessionLogTurns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionLogTurnContexts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionLogTurnId = table.Column<long>(type: "INTEGER", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    ContextItem = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogTurnContexts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogTurnContexts_SessionLogTurns_SessionLogTurnId",
                        column: x => x.SessionLogTurnId,
                        principalTable: "SessionLogTurns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionLogTurnTags",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionLogTurnId = table.Column<long>(type: "INTEGER", nullable: false),
                    Tag = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogTurnTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogTurnTags_SessionLogTurns_SessionLogTurnId",
                        column: x => x.SessionLogTurnId,
                        principalTable: "SessionLogTurns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogActions_SessionLogTurnId",
                table: "SessionLogActions",
                column: "SessionLogTurnId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogTurns_SessionLogId_RequestId",
                table: "SessionLogTurns",
                columns: SessionLogTurnRequestIndexColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogTurnContexts_SessionLogTurnId",
                table: "SessionLogTurnContexts",
                column: "SessionLogTurnId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogTurnTags_SessionLogTurnId",
                table: "SessionLogTurnTags",
                column: "SessionLogTurnId");

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
                columns: SessionSourceSessionIndexColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogs_Started",
                table: "SessionLogs",
                column: "Started");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropTable(
                name: "SessionLogActions");

            migrationBuilder.DropTable(
                name: "SessionLogTurnContexts");

            migrationBuilder.DropTable(
                name: "SessionLogTurnTags");

            migrationBuilder.DropTable(
                name: "SessionLogTurns");

            migrationBuilder.DropTable(
                name: "SessionLogs");
        }
    }
}
