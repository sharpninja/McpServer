using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqliteMigrations.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTodoRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    WHERE record."IsDeleted" = 0
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
                    0,
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

            migrationBuilder.Sql("""
                DELETE FROM "TodoRequirementLinks"
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "TodoItems"
                    WHERE "TodoItems"."WorkspaceId" = "TodoRequirementLinks"."WorkspaceId"
                      AND "TodoItems"."Id" = "TodoRequirementLinks"."TodoId"
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE "ef_temp_TodoRequirementLinks" (
                    "WorkspaceId" TEXT NOT NULL,
                    "TodoId" TEXT NOT NULL,
                    "RequirementKind" TEXT NOT NULL,
                    "RequirementId" TEXT NOT NULL,
                    "CreatedAtUtc" TEXT NOT NULL,
                    "DeleteReason" TEXT NULL,
                    "DeletedAtUtc" TEXT NULL,
                    "DeletedBy" TEXT NULL,
                    "IsDeleted" INTEGER NOT NULL DEFAULT 0,
                    CONSTRAINT "PK_TodoRequirementLinks" PRIMARY KEY ("WorkspaceId", "TodoId", "RequirementKind", "RequirementId"),
                    CONSTRAINT "FK_TodoRequirementLinks_Requirements_WorkspaceId_RequirementKind_RequirementId" FOREIGN KEY ("WorkspaceId", "RequirementKind", "RequirementId") REFERENCES "Requirements" ("WorkspaceId", "Kind", "Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_TodoRequirementLinks_TodoItems_WorkspaceId_TodoId" FOREIGN KEY ("WorkspaceId", "TodoId") REFERENCES "TodoItems" ("WorkspaceId", "Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_TodoRequirementLinks_Workspaces_WorkspaceId" FOREIGN KEY ("WorkspaceId") REFERENCES "Workspaces" ("WorkspaceId") ON DELETE RESTRICT
                );
                """);

            migrationBuilder.Sql("""
                INSERT INTO "ef_temp_TodoRequirementLinks" (
                    "WorkspaceId",
                    "TodoId",
                    "RequirementKind",
                    "RequirementId",
                    "CreatedAtUtc",
                    "DeleteReason",
                    "DeletedAtUtc",
                    "DeletedBy",
                    "IsDeleted"
                )
                SELECT
                    "WorkspaceId",
                    "TodoId",
                    "RequirementKind",
                    "RequirementId",
                    "CreatedAtUtc",
                    "DeleteReason",
                    "DeletedAtUtc",
                    "DeletedBy",
                    "IsDeleted"
                FROM "TodoRequirementLinks";
                """);

            migrationBuilder.Sql("""DROP TABLE "TodoRequirementLinks";""");
            migrationBuilder.Sql("""ALTER TABLE "ef_temp_TodoRequirementLinks" RENAME TO "TodoRequirementLinks";""");
            migrationBuilder.Sql("""
                CREATE INDEX "IX_TodoRequirementLinks_WorkspaceId_RequirementKind_RequirementId"
                ON "TodoRequirementLinks" ("WorkspaceId", "RequirementKind", "RequirementId");
                """);
            migrationBuilder.Sql("""DROP TABLE "TodoRecords";""");
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
                    WorkspaceId = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    TodoId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DeleteReason = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
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
