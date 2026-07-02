using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Decompose4nfTodoDocumentMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TodoCompletedGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    SingletonId = table.Column<int>(type: "int", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoCompletedGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TodoCompletedGroups_TodoDocumentMetadata_WorkspaceId_SingletonId",
                        columns: x => new { x.WorkspaceId, x.SingletonId },
                        principalTable: "TodoDocumentMetadata",
                        principalColumns: new[] { "WorkspaceId", "SingletonId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TodoCompletedGroups_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TodoDocumentNotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    SingletonId = table.Column<int>(type: "int", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoDocumentNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TodoDocumentNotes_TodoDocumentMetadata_WorkspaceId_SingletonId",
                        columns: x => new { x.WorkspaceId, x.SingletonId },
                        principalTable: "TodoDocumentMetadata",
                        principalColumns: new[] { "WorkspaceId", "SingletonId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TodoDocumentNotes_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TodoCompletedItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    GroupId = table.Column<long>(type: "bigint", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Qualifier = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoCompletedItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TodoCompletedItems_TodoCompletedGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "TodoCompletedGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TodoCompletedItems_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TodoCompletedGroups_WorkspaceId_SingletonId_Ordinal",
                table: "TodoCompletedGroups",
                columns: new[] { "WorkspaceId", "SingletonId", "Ordinal" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoCompletedItems_GroupId_Ordinal",
                table: "TodoCompletedItems",
                columns: new[] { "GroupId", "Ordinal" });

            migrationBuilder.CreateIndex(
                name: "IX_TodoCompletedItems_WorkspaceId",
                table: "TodoCompletedItems",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_TodoDocumentNotes_WorkspaceId_SingletonId_Ordinal",
                table: "TodoDocumentNotes",
                columns: new[] { "WorkspaceId", "SingletonId", "Ordinal" });

            // TR-MCP-TODO-005 data migration: backfill NotesJson (string array) and CompletedJson
            // (array of {date, items:[{id, qualifier, summary}]} groups) into 4NF child rows
            // before the source columns are dropped. Groups first, then items joined back to the
            // freshly inserted groups by their ordinal.
            migrationBuilder.Sql(@"
INSERT INTO [TodoDocumentNotes] ([WorkspaceId], [SingletonId], [Ordinal], [Value])
SELECT m.[WorkspaceId], m.[SingletonId], CAST(j.[key] AS int), j.[value]
FROM [TodoDocumentMetadata] m
CROSS APPLY OPENJSON(m.[NotesJson]) j
WHERE m.[NotesJson] IS NOT NULL AND ISJSON(m.[NotesJson]) = 1;");

            migrationBuilder.Sql(@"
INSERT INTO [TodoCompletedGroups] ([WorkspaceId], [SingletonId], [Ordinal], [Date])
SELECT m.[WorkspaceId], m.[SingletonId], CAST(arr.[key] AS int), JSON_VALUE(arr.[value], '$.date')
FROM [TodoDocumentMetadata] m
CROSS APPLY OPENJSON(m.[CompletedJson]) arr
WHERE m.[CompletedJson] IS NOT NULL AND ISJSON(m.[CompletedJson]) = 1;");

            // Items: summary read as nvarchar(max) via OPENJSON WITH (JSON_VALUE truncates at 4000).
            migrationBuilder.Sql(@"
INSERT INTO [TodoCompletedItems] ([WorkspaceId], [GroupId], [Ordinal], [ItemId], [Qualifier], [Summary])
SELECT m.[WorkspaceId], g.[Id], CAST(itm.[key] AS int), fields.[id], fields.[qualifier], fields.[summary]
FROM [TodoDocumentMetadata] m
CROSS APPLY OPENJSON(m.[CompletedJson]) arr
CROSS APPLY OPENJSON(arr.[value], '$.items') itm
CROSS APPLY OPENJSON(itm.[value]) WITH (
    [id] nvarchar(128) '$.id',
    [qualifier] nvarchar(256) '$.qualifier',
    [summary] nvarchar(max) '$.summary'
) fields
JOIN [TodoCompletedGroups] g
  ON g.[WorkspaceId] = m.[WorkspaceId] AND g.[SingletonId] = m.[SingletonId] AND g.[Ordinal] = CAST(arr.[key] AS int)
WHERE m.[CompletedJson] IS NOT NULL AND ISJSON(m.[CompletedJson]) = 1;");

            migrationBuilder.DropColumn(name: "CompletedJson", table: "TodoDocumentMetadata");
            migrationBuilder.DropColumn(name: "NotesJson", table: "TodoDocumentMetadata");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompletedJson",
                table: "TodoDocumentMetadata",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotesJson",
                table: "TodoDocumentMetadata",
                type: "nvarchar(max)",
                nullable: true);

            // Reconstruct the JSON blocks (camelCase keys, ordered) from the child rows.
            migrationBuilder.Sql(@"
UPDATE m SET [NotesJson] = j.[json]
FROM [TodoDocumentMetadata] m
CROSS APPLY (
    SELECT CONCAT('[', STRING_AGG(CONCAT('""', STRING_ESCAPE(n.[Value], 'json'), '""'), ',') WITHIN GROUP (ORDER BY n.[Ordinal]), ']') AS [json]
    FROM [TodoDocumentNotes] n
    WHERE n.[WorkspaceId] = m.[WorkspaceId] AND n.[SingletonId] = m.[SingletonId] AND n.[IsDeleted] = 0
) j
WHERE j.[json] IS NOT NULL;");

            migrationBuilder.Sql(@"
UPDATE m SET [CompletedJson] = (
    SELECT g.[Date] AS [date],
           JSON_QUERY((
               SELECT i.[ItemId] AS [id], i.[Qualifier] AS [qualifier], i.[Summary] AS [summary]
               FROM [TodoCompletedItems] i
               WHERE i.[GroupId] = g.[Id] AND i.[IsDeleted] = 0
               ORDER BY i.[Ordinal]
               FOR JSON PATH, INCLUDE_NULL_VALUES)) AS [items]
    FROM [TodoCompletedGroups] g
    WHERE g.[WorkspaceId] = m.[WorkspaceId] AND g.[SingletonId] = m.[SingletonId] AND g.[IsDeleted] = 0
    ORDER BY g.[Ordinal]
    FOR JSON PATH, INCLUDE_NULL_VALUES
)
FROM [TodoDocumentMetadata] m
WHERE EXISTS (
    SELECT 1 FROM [TodoCompletedGroups] g
    WHERE g.[WorkspaceId] = m.[WorkspaceId] AND g.[SingletonId] = m.[SingletonId] AND g.[IsDeleted] = 0);");

            migrationBuilder.DropTable(
                name: "TodoCompletedItems");

            migrationBuilder.DropTable(
                name: "TodoDocumentNotes");

            migrationBuilder.DropTable(
                name: "TodoCompletedGroups");
        }
    }
}
