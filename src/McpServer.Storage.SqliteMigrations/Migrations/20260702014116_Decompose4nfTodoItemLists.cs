using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqliteMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Decompose4nfTodoItemLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TodoItemListItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkspaceId = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    TodoId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ListType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    DeleteReason = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoItemListItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TodoItemListItems_TodoItems_WorkspaceId_TodoId",
                        columns: x => new { x.WorkspaceId, x.TodoId },
                        principalTable: "TodoItems",
                        principalColumns: new[] { "WorkspaceId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TodoItemListItems_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TodoItemTasks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkspaceId = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    TodoId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Task = table.Column<string>(type: "TEXT", nullable: false),
                    Done = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeleteReason = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoItemTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TodoItemTasks_TodoItems_WorkspaceId_TodoId",
                        columns: x => new { x.WorkspaceId, x.TodoId },
                        principalTable: "TodoItems",
                        principalColumns: new[] { "WorkspaceId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TodoItemTasks_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItemListItems_WorkspaceId_TodoId_ListType_Ordinal",
                table: "TodoItemListItems",
                columns: new[] { "WorkspaceId", "TodoId", "ListType", "Ordinal" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoItemTasks_WorkspaceId_TodoId_Ordinal",
                table: "TodoItemTasks",
                columns: new[] { "WorkspaceId", "TodoId", "Ordinal" });

            // TR-MCP-TODO-005 data migration: backfill the five JSON string arrays and the
            // implementation-task object array into ordered 4NF child rows before the source
            // columns are dropped.
            migrationBuilder.Sql(BackfillListSql("DescriptionJson", "Description"));
            migrationBuilder.Sql(BackfillListSql("TechnicalDetailsJson", "TechnicalDetail"));
            migrationBuilder.Sql(BackfillListSql("DependsOnJson", "DependsOn"));
            migrationBuilder.Sql(BackfillListSql("FunctionalRequirementsJson", "FunctionalRequirement"));
            migrationBuilder.Sql(BackfillListSql("TechnicalRequirementsJson", "TechnicalRequirement"));
            migrationBuilder.Sql("""
INSERT INTO "TodoItemTasks" ("WorkspaceId", "TodoId", "Ordinal", "Task", "Done")
SELECT t."WorkspaceId", t."Id", j."key",
       COALESCE(json_extract(j."value", '$.task'), ''),
       COALESCE(json_extract(j."value", '$.done'), 0)
FROM "TodoItems" t, json_each(t."ImplementationTasksJson") j
WHERE t."ImplementationTasksJson" IS NOT NULL AND json_valid(t."ImplementationTasksJson");
""");

            migrationBuilder.DropColumn(name: "DependsOnJson", table: "TodoItems");
            migrationBuilder.DropColumn(name: "DescriptionJson", table: "TodoItems");
            migrationBuilder.DropColumn(name: "FunctionalRequirementsJson", table: "TodoItems");
            migrationBuilder.DropColumn(name: "ImplementationTasksJson", table: "TodoItems");
            migrationBuilder.DropColumn(name: "TechnicalDetailsJson", table: "TodoItems");
            migrationBuilder.DropColumn(name: "TechnicalRequirementsJson", table: "TodoItems");
        }

        private static string BackfillListSql(string jsonColumn, string listType) => $"""
INSERT INTO "TodoItemListItems" ("WorkspaceId", "TodoId", "ListType", "Ordinal", "Value")
SELECT t."WorkspaceId", t."Id", '{listType}', j."key", j."value"
FROM "TodoItems" t, json_each(t."{jsonColumn}") j
WHERE t."{jsonColumn}" IS NOT NULL AND json_valid(t."{jsonColumn}");
""";

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DependsOnJson",
                table: "TodoItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionJson",
                table: "TodoItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FunctionalRequirementsJson",
                table: "TodoItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImplementationTasksJson",
                table: "TodoItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicalDetailsJson",
                table: "TodoItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicalRequirementsJson",
                table: "TodoItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql(RestoreListSql("DescriptionJson", "Description"));
            migrationBuilder.Sql(RestoreListSql("TechnicalDetailsJson", "TechnicalDetail"));
            migrationBuilder.Sql(RestoreListSql("DependsOnJson", "DependsOn"));
            migrationBuilder.Sql(RestoreListSql("FunctionalRequirementsJson", "FunctionalRequirement"));
            migrationBuilder.Sql(RestoreListSql("TechnicalRequirementsJson", "TechnicalRequirement"));
            migrationBuilder.Sql("""
UPDATE "TodoItems"
SET "ImplementationTasksJson" = (
    SELECT json_group_array(json_object('task', r."Task", 'done', json(CASE WHEN r."Done" THEN 'true' ELSE 'false' END)) ORDER BY r."Ordinal")
    FROM "TodoItemTasks" r
    WHERE r."WorkspaceId" = "TodoItems"."WorkspaceId" AND r."TodoId" = "TodoItems"."Id" AND r."IsDeleted" = 0
)
WHERE EXISTS (
    SELECT 1 FROM "TodoItemTasks" r
    WHERE r."WorkspaceId" = "TodoItems"."WorkspaceId" AND r."TodoId" = "TodoItems"."Id" AND r."IsDeleted" = 0);
""");

            migrationBuilder.DropTable(
                name: "TodoItemListItems");

            migrationBuilder.DropTable(
                name: "TodoItemTasks");
        }

        private static string RestoreListSql(string jsonColumn, string listType) => $"""
UPDATE "TodoItems"
SET "{jsonColumn}" = (
    SELECT json_group_array(i."Value" ORDER BY i."Ordinal")
    FROM "TodoItemListItems" i
    WHERE i."WorkspaceId" = "TodoItems"."WorkspaceId" AND i."TodoId" = "TodoItems"."Id" AND i."ListType" = '{listType}' AND i."IsDeleted" = 0
)
WHERE EXISTS (
    SELECT 1 FROM "TodoItemListItems" i
    WHERE i."WorkspaceId" = "TodoItems"."WorkspaceId" AND i."TodoId" = "TodoItems"."Id" AND i."ListType" = '{listType}' AND i."IsDeleted" = 0);
""";
    }
}
