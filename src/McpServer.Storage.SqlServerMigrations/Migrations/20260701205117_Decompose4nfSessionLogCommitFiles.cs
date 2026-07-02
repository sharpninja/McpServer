using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations
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
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", nullable: false),
                    SessionLogCommitId = table.Column<long>(type: "bigint", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
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
            migrationBuilder.Sql(@"
INSERT INTO [SessionLogCommitFiles] ([WorkspaceId], [SessionLogCommitId], [Ordinal], [Path])
SELECT c.[WorkspaceId], c.[Id], CAST(j.[key] AS int), j.[value]
FROM [SessionLogCommits] c
CROSS APPLY OPENJSON(c.[FilesChangedJson]) j
WHERE c.[FilesChangedJson] IS NOT NULL AND ISJSON(c.[FilesChangedJson]) = 1;");

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
                type: "nvarchar(max)",
                nullable: true);

            // Reconstruct the JSON string array from the ordered child rows before dropping them.
            migrationBuilder.Sql(@"
UPDATE c SET [FilesChangedJson] = j.[json]
FROM [SessionLogCommits] c
CROSS APPLY (
    SELECT CONCAT('[', STRING_AGG(CONCAT('""', STRING_ESCAPE(f.[Path], 'json'), '""'), ',') WITHIN GROUP (ORDER BY f.[Ordinal]), ']') AS [json]
    FROM [SessionLogCommitFiles] f
    WHERE f.[SessionLogCommitId] = c.[Id] AND f.[IsDeleted] = 0
) j
WHERE j.[json] IS NOT NULL;");

            migrationBuilder.DropTable(
                name: "SessionLogCommitFiles");
        }
    }
}
