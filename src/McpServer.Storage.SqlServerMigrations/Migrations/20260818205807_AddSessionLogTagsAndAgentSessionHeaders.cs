using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionLogTagsAndAgentSessionHeaders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'SessionLogs', N'AgentSessionId') IS NULL
                    ALTER TABLE [SessionLogs] ADD [AgentSessionId] nvarchar(256) NULL;
                IF COL_LENGTH(N'SessionLogs', N'AgentSessionTranscriptFile') IS NULL
                    ALTER TABLE [SessionLogs] ADD [AgentSessionTranscriptFile] nvarchar(2048) NULL;
                IF COL_LENGTH(N'SessionLogs', N'AgentExecutablePath') IS NULL
                    ALTER TABLE [SessionLogs] ADD [AgentExecutablePath] nvarchar(2048) NULL;
                IF COL_LENGTH(N'SessionLogs', N'AgentExecutableVersion') IS NULL
                    ALTER TABLE [SessionLogs] ADD [AgentExecutableVersion] nvarchar(128) NULL;
                """);

            migrationBuilder.CreateTable(
                name: "SessionLogTags",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", nullable: false),
                    SessionLogId = table.Column<long>(type: "bigint", nullable: false),
                    Tag = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
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
