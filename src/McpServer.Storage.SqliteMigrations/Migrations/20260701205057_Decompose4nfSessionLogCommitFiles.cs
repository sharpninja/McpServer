using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqliteMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Decompose4nfSessionLogCommitFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionLogCommitFiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkspaceId = table.Column<string>(type: "TEXT", nullable: false),
                    SessionLogCommitId = table.Column<long>(type: "INTEGER", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    DeleteReason = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogCommitFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogCommitFiles_SessionLogCommits_SessionLogCommitId",
                        column: x => x.SessionLogCommitId,
                        principalTable: "SessionLogCommits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionLogCommitFiles_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogCommitFiles_SessionLogCommitId_Ordinal",
                table: "SessionLogCommitFiles",
                columns: new[] { "SessionLogCommitId", "Ordinal" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogCommitFiles_WorkspaceId",
                table: "SessionLogCommitFiles",
                column: "WorkspaceId");

            // TR-PLANNED-CORE-013 data migration: backfill each commit's FilesChangedJson (JSON
            // string array) into ordered 4NF child rows before the source column is dropped.
            migrationBuilder.Sql("""
INSERT INTO "SessionLogCommitFiles" ("WorkspaceId", "SessionLogCommitId", "Ordinal", "Path")
SELECT c."WorkspaceId", c."Id", j."key", j."value"
FROM "SessionLogCommits" c, json_each(c."FilesChangedJson") j
WHERE c."FilesChangedJson" IS NOT NULL AND json_valid(c."FilesChangedJson");
""");

            migrationBuilder.DropColumn(
                name: "FilesChangedJson",
                table: "SessionLogCommits");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FilesChangedJson",
                table: "SessionLogCommits",
                type: "TEXT",
                nullable: true);

            // Reconstruct the JSON string array from the ordered child rows before dropping them.
            migrationBuilder.Sql("""
UPDATE "SessionLogCommits"
SET "FilesChangedJson" = (
    SELECT json_group_array(f."Path" ORDER BY f."Ordinal")
    FROM "SessionLogCommitFiles" f
    WHERE f."SessionLogCommitId" = "SessionLogCommits"."Id" AND f."IsDeleted" = 0
)
WHERE EXISTS (
    SELECT 1 FROM "SessionLogCommitFiles" f
    WHERE f."SessionLogCommitId" = "SessionLogCommits"."Id" AND f."IsDeleted" = 0);
""");

            migrationBuilder.DropTable(
                name: "SessionLogCommitFiles");
        }
    }
}
