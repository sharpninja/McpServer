using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionLogCommitsAndStringLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionLogCommits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkspaceId = table.Column<string>(type: "TEXT", nullable: false),
                    SessionLogEntryId = table.Column<long>(type: "INTEGER", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Sha = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Branch = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    Author = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CommitTimestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    FilesChangedJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogCommits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogCommits_SessionLogEntries_SessionLogEntryId",
                        column: x => x.SessionLogEntryId,
                        principalTable: "SessionLogEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionLogEntryStringLists",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkspaceId = table.Column<string>(type: "TEXT", nullable: false),
                    SessionLogEntryId = table.Column<long>(type: "INTEGER", nullable: false),
                    ListType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogEntryStringLists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogEntryStringLists_SessionLogEntries_SessionLogEntryId",
                        column: x => x.SessionLogEntryId,
                        principalTable: "SessionLogEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogCommits_SessionLogEntryId",
                table: "SessionLogCommits",
                column: "SessionLogEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogEntryStringLists_SessionLogEntryId_ListType",
                table: "SessionLogEntryStringLists",
                columns: new[] { "SessionLogEntryId", "ListType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionLogCommits");

            migrationBuilder.DropTable(
                name: "SessionLogEntryStringLists");
        }
    }
}
