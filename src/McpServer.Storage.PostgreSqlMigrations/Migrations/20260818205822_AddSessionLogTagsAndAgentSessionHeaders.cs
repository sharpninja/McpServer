using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionLogTagsAndAgentSessionHeaders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "SessionLogs" ADD COLUMN IF NOT EXISTS "AgentSessionId" character varying(256);
                ALTER TABLE "SessionLogs" ADD COLUMN IF NOT EXISTS "AgentSessionTranscriptFile" character varying(2048);
                ALTER TABLE "SessionLogs" ADD COLUMN IF NOT EXISTS "AgentExecutablePath" character varying(2048);
                ALTER TABLE "SessionLogs" ADD COLUMN IF NOT EXISTS "AgentExecutableVersion" character varying(128);
                """);

            migrationBuilder.CreateTable(
                name: "SessionLogTags",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "character varying(1024)", nullable: false),
                    SessionLogId = table.Column<long>(type: "bigint", nullable: false),
                    Tag = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DeleteReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionLogTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionLogTags_SessionLogs_SessionLogId",
                        column: x => x.SessionLogId,
                        principalTable: "SessionLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionLogTags_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogTags_SessionLogId_Tag",
                table: "SessionLogTags",
                columns: new[] { "SessionLogId", "Tag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogTags_WorkspaceId",
                table: "SessionLogTags",
                column: "WorkspaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionLogTags");
        }
    }
}
