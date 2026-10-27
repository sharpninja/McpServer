using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace QBrainAi.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionAuditEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuditEventId",
                table: "SessionLogTurnStringLists",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AuditEventOrder",
                table: "SessionLogTurnStringLists",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuditEventId",
                table: "SessionLogProcessingDialogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AuditEventOrder",
                table: "SessionLogProcessingDialogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalTimestamp",
                table: "SessionLogProcessingDialogs",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "Order",
                table: "SessionLogActions",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "AuditEventId",
                table: "SessionLogActions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AuditEventOrder",
                table: "SessionLogActions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SessionAuditEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "character varying(1024)", nullable: false),
                    SessionLogTurnId = table.Column<long>(type: "bigint", nullable: false),
                    CapturedWorkspacePath = table.Column<string>(type: "text", nullable: false),
                    AgentIdentity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    McpSessionId = table.Column<string>(type: "text", nullable: false),
                    RequestId = table.Column<string>(type: "text", nullable: false),
                    EventId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    CapturedOrder = table.Column<long>(type: "bigint", nullable: false),
                    PositionKey = table.Column<long>(type: "bigint", nullable: true),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Accepted = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    StoredStatus = table.Column<string>(type: "text", nullable: true),
                    DeleteReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionAuditEvents_SessionLogTurns_SessionLogTurnId",
                        column: x => x.SessionLogTurnId,
                        principalTable: "SessionLogTurns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionAuditEvents_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionAuditEvents_EventId",
                table: "SessionAuditEvents",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionAuditEvents_SessionLogTurnId_PositionKey",
                table: "SessionAuditEvents",
                columns: new[] { "SessionLogTurnId", "PositionKey" },
                unique: true,
                filter: "\"PositionKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SessionAuditEvents_WorkspaceId",
                table: "SessionAuditEvents",
                column: "WorkspaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionAuditEvents");

            migrationBuilder.DropColumn(
                name: "AuditEventId",
                table: "SessionLogTurnStringLists");

            migrationBuilder.DropColumn(
                name: "AuditEventOrder",
                table: "SessionLogTurnStringLists");

            migrationBuilder.DropColumn(
                name: "AuditEventId",
                table: "SessionLogProcessingDialogs");

            migrationBuilder.DropColumn(
                name: "AuditEventOrder",
                table: "SessionLogProcessingDialogs");

            migrationBuilder.DropColumn(
                name: "OriginalTimestamp",
                table: "SessionLogProcessingDialogs");

            migrationBuilder.DropColumn(
                name: "AuditEventId",
                table: "SessionLogActions");

            migrationBuilder.DropColumn(
                name: "AuditEventOrder",
                table: "SessionLogActions");

            migrationBuilder.AlterColumn<int>(
                name: "Order",
                table: "SessionLogActions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");
        }
    }
}
