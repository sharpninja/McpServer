using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTodoRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TodoRequirementLinks_TodoRecords_WorkspaceId_TodoId",
                table: "TodoRequirementLinks");

            migrationBuilder.Sql("""
                WITH repair AS (
                    SELECT
                        COALESCE(
                            (
                                SELECT "WorkspaceId"
                                FROM "TriageGroups"
                                WHERE "TriageGroups"."CreatedTodoId" = record."TodoId"
                                ORDER BY "LastReportAtUtc" DESC
                                LIMIT 1
                            ),
                            (
                                SELECT "WorkspaceId"
                                FROM "TriageResearchRuns"
                                WHERE "TriageResearchRuns"."CreatedTodoId" = record."TodoId"
                                ORDER BY COALESCE("CompletedUtc", "StartedUtc") DESC
                                LIMIT 1
                            ),
                            record."WorkspaceId"
                        ) AS "TargetWorkspaceId",
                        record."TodoId",
                        COALESCE(
                            (
                                SELECT "Title"
                                FROM "TodoItems"
                                WHERE "TodoItems"."Id" = record."TodoId"
                                ORDER BY "TodoItems"."WorkspaceId"
                                LIMIT 1
                            ),
                            (
                                SELECT "Title"
                                FROM "TriageGroups"
                                WHERE "TriageGroups"."CreatedTodoId" = record."TodoId"
                                ORDER BY "LastReportAtUtc" DESC
                                LIMIT 1
                            ),
                            'Recovered TODO ' || record."TodoId"
                        ) AS "Title"
                    FROM "TodoRecords" AS record
                    WHERE record."IsDeleted" = FALSE
                )
                INSERT INTO "TodoItems" (
                    "WorkspaceId",
                    "Id",
                    "Title",
                    "Section",
                    "Priority",
                    "Done",
                    "ItemKind",
                    "SectionOrder",
                    "ItemOrder"
                )
                SELECT
                    repair."TargetWorkspaceId",
                    repair."TodoId",
                    repair."Title",
                    'Backlog',
                    'medium',
                    FALSE,
                    'standard',
                    0,
                    0
                FROM repair
                WHERE NOT EXISTS (
                      SELECT 1
                      FROM "TodoItems"
                      WHERE "TodoItems"."WorkspaceId" = repair."TargetWorkspaceId"
                        AND "TodoItems"."Id" = repair."TodoId"
                  );
                """);

            migrationBuilder.DropTable(
                name: "TodoRecords");

            migrationBuilder.Sql("""
                DELETE FROM "TodoRequirementLinks"
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "TodoItems"
                    WHERE "TodoItems"."WorkspaceId" = "TodoRequirementLinks"."WorkspaceId"
                      AND "TodoItems"."Id" = "TodoRequirementLinks"."TodoId"
                );
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_TodoRequirementLinks_TodoItems_WorkspaceId_TodoId",
                table: "TodoRequirementLinks",
                columns: new[] { "WorkspaceId", "TodoId" },
                principalTable: "TodoItems",
                principalColumns: new[] { "WorkspaceId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TodoRequirementLinks_TodoItems_WorkspaceId_TodoId",
                table: "TodoRequirementLinks");

            migrationBuilder.CreateTable(
                name: "TodoRecords",
                columns: table => new
                {
                    WorkspaceId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    TodoId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeleteReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoRecords", x => new { x.WorkspaceId, x.TodoId });
                    table.ForeignKey(
                        name: "FK_TodoRecords_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TodoRecords_UpdatedAtUtc",
                table: "TodoRecords",
                column: "UpdatedAtUtc");

            migrationBuilder.AddForeignKey(
                name: "FK_TodoRequirementLinks_TodoRecords_WorkspaceId_TodoId",
                table: "TodoRequirementLinks",
                columns: new[] { "WorkspaceId", "TodoId" },
                principalTable: "TodoRecords",
                principalColumns: new[] { "WorkspaceId", "TodoId" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
