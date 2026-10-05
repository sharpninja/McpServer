using System;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QBrainAi.Support.Mcp.Storage.SqlServerMigrations.Migrations
{
    /// <summary>
    /// FR-MCP-REQRECOVERY-001: RequirementsRecoveryRuns unique on (WorkspaceId, IdempotencyKey).
    /// Forward-only.
    /// </summary>
    [DbContext(typeof(McpDbContext))]
    [Migration("20260929020000_AddRequirementsRecoveryRuns")]
    public partial class AddRequirementsRecoveryRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequirementsRecoveryRuns",
                columns: table => new
                {
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PayloadHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "applied"),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequirementsRecoveryRuns", x => new { x.WorkspaceId, x.IdempotencyKey });
                    table.ForeignKey(
                        name: "FK_RequirementsRecoveryRuns_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequirementsRecoveryRuns_WorkspaceId",
                table: "RequirementsRecoveryRuns",
                column: "WorkspaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);
        }
    }
}
