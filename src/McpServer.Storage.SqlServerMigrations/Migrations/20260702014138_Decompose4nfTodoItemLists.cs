using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations
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
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    TodoId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ListType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
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
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    TodoId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    Task = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Done = table.Column<bool>(type: "bit", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
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
            migrationBuilder.Sql(@"
INSERT INTO [TodoItemTasks] ([WorkspaceId], [TodoId], [Ordinal], [Task], [Done])
SELECT t.[WorkspaceId], t.[Id], CAST(arr.[key] AS int),
       COALESCE(task.[task], ''), COALESCE(task.[done], 0)
FROM [TodoItems] t
CROSS APPLY OPENJSON(t.[ImplementationTasksJson]) arr
CROSS APPLY OPENJSON(arr.[value]) WITH (
    [task] nvarchar(max) '$.task',
    [done] bit '$.done'
) task
WHERE t.[ImplementationTasksJson] IS NOT NULL AND ISJSON(t.[ImplementationTasksJson]) = 1;");

            migrationBuilder.DropColumn(name: "DependsOnJson", table: "TodoItems");
            migrationBuilder.DropColumn(name: "DescriptionJson", table: "TodoItems");
            migrationBuilder.DropColumn(name: "FunctionalRequirementsJson", table: "TodoItems");
            migrationBuilder.DropColumn(name: "ImplementationTasksJson", table: "TodoItems");
            migrationBuilder.DropColumn(name: "TechnicalDetailsJson", table: "TodoItems");
            migrationBuilder.DropColumn(name: "TechnicalRequirementsJson", table: "TodoItems");
        }

        private static string BackfillListSql(string jsonColumn, string listType) => $@"
INSERT INTO [TodoItemListItems] ([WorkspaceId], [TodoId], [ListType], [Ordinal], [Value])
SELECT t.[WorkspaceId], t.[Id], '{listType}', CAST(j.[key] AS int), j.[value]
FROM [TodoItems] t
CROSS APPLY OPENJSON(t.[{jsonColumn}]) j
WHERE t.[{jsonColumn}] IS NOT NULL AND ISJSON(t.[{jsonColumn}]) = 1;";

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<string>(
                name: "DependsOnJson",
                table: "TodoItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionJson",
                table: "TodoItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FunctionalRequirementsJson",
                table: "TodoItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImplementationTasksJson",
                table: "TodoItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicalDetailsJson",
                table: "TodoItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TechnicalRequirementsJson",
                table: "TodoItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql(RestoreListSql("DescriptionJson", "Description"));
            migrationBuilder.Sql(RestoreListSql("TechnicalDetailsJson", "TechnicalDetail"));
            migrationBuilder.Sql(RestoreListSql("DependsOnJson", "DependsOn"));
            migrationBuilder.Sql(RestoreListSql("FunctionalRequirementsJson", "FunctionalRequirement"));
            migrationBuilder.Sql(RestoreListSql("TechnicalRequirementsJson", "TechnicalRequirement"));
            migrationBuilder.Sql(@"
UPDATE t SET [ImplementationTasksJson] = (
    SELECT r.[Task] AS [task], r.[Done] AS [done]
    FROM [TodoItemTasks] r
    WHERE r.[WorkspaceId] = t.[WorkspaceId] AND r.[TodoId] = t.[Id] AND r.[IsDeleted] = 0
    ORDER BY r.[Ordinal]
    FOR JSON PATH
)
FROM [TodoItems] t
WHERE EXISTS (
    SELECT 1 FROM [TodoItemTasks] r
    WHERE r.[WorkspaceId] = t.[WorkspaceId] AND r.[TodoId] = t.[Id] AND r.[IsDeleted] = 0);");

            migrationBuilder.DropTable(
                name: "TodoItemListItems");

            migrationBuilder.DropTable(
                name: "TodoItemTasks");
        }

        private static string RestoreListSql(string jsonColumn, string listType) => $@"
UPDATE t SET [{jsonColumn}] = j.[json]
FROM [TodoItems] t
CROSS APPLY (
    SELECT CONCAT('[', STRING_AGG(CONCAT('""', STRING_ESCAPE(i.[Value], 'json'), '""'), ',') WITHIN GROUP (ORDER BY i.[Ordinal]), ']') AS [json]
    FROM [TodoItemListItems] i
    WHERE i.[WorkspaceId] = t.[WorkspaceId] AND i.[TodoId] = t.[Id] AND i.[ListType] = '{listType}' AND i.[IsDeleted] = 0
) j
WHERE j.[json] IS NOT NULL;";
    }
}
