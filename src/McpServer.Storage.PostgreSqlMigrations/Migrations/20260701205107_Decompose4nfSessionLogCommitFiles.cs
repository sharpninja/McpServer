using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Decompose4nfSessionLogCommitFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilesChangedJson",
                table: "SessionLogCommits");

            migrationBuilder.CreateTable(
                name: "SessionLogCommitFiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "character varying(1024)", nullable: false),
                    SessionLogCommitId = table.Column<long>(type: "bigint", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false),
                    DeleteReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionLogCommitFiles");

            migrationBuilder.AddColumn<string>(
                name: "FilesChangedJson",
                table: "SessionLogCommits",
                type: "text",
                nullable: true);
        }
    }
}
