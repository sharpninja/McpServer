using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqliteMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Decompose4nfRequirementAcceptanceCriteria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptanceCriteriaJson",
                table: "Requirements");

            migrationBuilder.CreateTable(
                name: "RequirementAcceptanceCriteria",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkspaceId = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    RequirementKind = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    RequirementId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    CriterionId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    IsSatisfied = table.Column<bool>(type: "INTEGER", nullable: false),
                    Evidence = table.Column<string>(type: "TEXT", nullable: true),
                    DeleteReason = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequirementAcceptanceCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequirementAcceptanceCriteria_Requirements_WorkspaceId_RequirementKind_RequirementId",
                        columns: x => new { x.WorkspaceId, x.RequirementKind, x.RequirementId },
                        principalTable: "Requirements",
                        principalColumns: new[] { "WorkspaceId", "Kind", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequirementAcceptanceCriteria_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequirementAcceptanceCriteria_WorkspaceId_RequirementKind_RequirementId_Ordinal",
                table: "RequirementAcceptanceCriteria",
                columns: new[] { "WorkspaceId", "RequirementKind", "RequirementId", "Ordinal" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequirementAcceptanceCriteria");

            migrationBuilder.AddColumn<string>(
                name: "AcceptanceCriteriaJson",
                table: "Requirements",
                type: "TEXT",
                nullable: true);
        }
    }
}
