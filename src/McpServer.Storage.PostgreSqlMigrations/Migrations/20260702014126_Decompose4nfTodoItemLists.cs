using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
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
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    TodoId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ListType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    DeleteReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
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
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    TodoId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    Task = table.Column<string>(type: "text", nullable: false),
                    Done = table.Column<bool>(type: "boolean", nullable: false),
                    DeleteReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
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
SELECT t."WorkspaceId", t."Id", (j.ordinality - 1)::int,
       COALESCE(j.value ->> 'task', ''), COALESCE((j.value ->> 'done')::boolean, false)
FROM "TodoItems" t
CROSS JOIN LATERAL jsonb_array_elements(t."ImplementationTasksJson"::jsonb) WITH ORDINALITY AS j(value, ordinality)
WHERE t."ImplementationTasksJson" IS NOT NULL AND jsonb_typeof(t."ImplementationTasksJson"::jsonb) = 'array';
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
SELECT t."WorkspaceId", t."Id", '{listType}', (j.ordinality - 1)::int, j.value
FROM "TodoItems" t
CROSS JOIN LATERAL jsonb_array_elements_text(t."{jsonColumn}"::jsonb) WITH ORDINALITY AS j(value, ordinality)
WHERE t."{jsonColumn}" IS NOT NULL AND jsonb_typeof(t."{jsonColumn}"::jsonb) = 'array';
""";

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DependsOnJson",
                table: "TodoItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionJson",
                table: "TodoItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FunctionalRequirementsJson",
                table: "TodoItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImplementationTasksJson",
                table: "TodoItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicalDetailsJson",
                table: "TodoItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicalRequirementsJson",
                table: "TodoItems",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(RestoreListSql("DescriptionJson", "Description"));
            migrationBuilder.Sql(RestoreListSql("TechnicalDetailsJson", "TechnicalDetail"));
            migrationBuilder.Sql(RestoreListSql("DependsOnJson", "DependsOn"));
            migrationBuilder.Sql(RestoreListSql("FunctionalRequirementsJson", "FunctionalRequirement"));
            migrationBuilder.Sql(RestoreListSql("TechnicalRequirementsJson", "TechnicalRequirement"));
            migrationBuilder.Sql("""
UPDATE "TodoItems" t
SET "ImplementationTasksJson" = j.json
FROM (
    SELECT "WorkspaceId", "TodoId",
           jsonb_agg(jsonb_build_object('task', "Task", 'done', "Done") ORDER BY "Ordinal")::text AS json
    FROM "TodoItemTasks"
    WHERE "IsDeleted" = false
    GROUP BY "WorkspaceId", "TodoId"
) j
WHERE j."WorkspaceId" = t."WorkspaceId" AND j."TodoId" = t."Id";
""");

            migrationBuilder.DropTable(
                name: "TodoItemListItems");

            migrationBuilder.DropTable(
                name: "TodoItemTasks");
        }

        private static string RestoreListSql(string jsonColumn, string listType) => $"""
UPDATE "TodoItems" t
SET "{jsonColumn}" = j.json
FROM (
    SELECT "WorkspaceId", "TodoId", jsonb_agg("Value" ORDER BY "Ordinal")::text AS json
    FROM "TodoItemListItems"
    WHERE "ListType" = '{listType}' AND "IsDeleted" = false
    GROUP BY "WorkspaceId", "TodoId"
) j
WHERE j."WorkspaceId" = t."WorkspaceId" AND j."TodoId" = t."Id";
""";
    }
}
