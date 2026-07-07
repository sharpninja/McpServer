using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqliteMigrations.Migrations
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
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkspaceId = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    SingletonId = table.Column<int>(type: "INTEGER", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    DeleteReason = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
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
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkspaceId = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    SingletonId = table.Column<int>(type: "INTEGER", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    DeleteReason = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
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
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkspaceId = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    GroupId = table.Column<long>(type: "INTEGER", nullable: false),
                    Ordinal = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Qualifier = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    DeleteReason = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
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
            migrationBuilder.Sql("""
INSERT INTO "TodoDocumentNotes" ("WorkspaceId", "SingletonId", "Ordinal", "Value")
SELECT m."WorkspaceId", m."SingletonId", j."key", j."value"
FROM "TodoDocumentMetadata" m, json_each(m."NotesJson") j
WHERE m."NotesJson" IS NOT NULL AND json_valid(m."NotesJson");
""");

            migrationBuilder.Sql("""
INSERT INTO "TodoCompletedGroups" ("WorkspaceId", "SingletonId", "Ordinal", "Date")
SELECT m."WorkspaceId", m."SingletonId", arr."key", json_extract(arr."value", '$.date')
FROM "TodoDocumentMetadata" m, json_each(m."CompletedJson") arr
WHERE m."CompletedJson" IS NOT NULL AND json_valid(m."CompletedJson");
""");

            migrationBuilder.Sql("""
INSERT INTO "TodoCompletedItems" ("WorkspaceId", "GroupId", "Ordinal", "ItemId", "Qualifier", "Summary")
SELECT m."WorkspaceId", g."Id", itm."key",
       json_extract(itm."value", '$.id'), json_extract(itm."value", '$.qualifier'), json_extract(itm."value", '$.summary')
FROM "TodoDocumentMetadata" m,
     json_each(m."CompletedJson") arr,
     json_each(arr."value", '$.items') itm
JOIN "TodoCompletedGroups" g
  ON g."WorkspaceId" = m."WorkspaceId" AND g."SingletonId" = m."SingletonId" AND g."Ordinal" = arr."key"
WHERE m."CompletedJson" IS NOT NULL AND json_valid(m."CompletedJson");
""");

            migrationBuilder.DropColumn(name: "CompletedJson", table: "TodoDocumentMetadata");
            migrationBuilder.DropColumn(name: "NotesJson", table: "TodoDocumentMetadata");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompletedJson",
                table: "TodoDocumentMetadata",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotesJson",
                table: "TodoDocumentMetadata",
                type: "TEXT",
                nullable: true);

            // Reconstruct the JSON blocks (camelCase keys, ordered) from the child rows.
            migrationBuilder.Sql("""
UPDATE "TodoDocumentMetadata"
SET "NotesJson" = (
    SELECT json_group_array(n."Value" ORDER BY n."Ordinal")
    FROM "TodoDocumentNotes" n
    WHERE n."WorkspaceId" = "TodoDocumentMetadata"."WorkspaceId" AND n."SingletonId" = "TodoDocumentMetadata"."SingletonId" AND n."IsDeleted" = 0
)
WHERE EXISTS (
    SELECT 1 FROM "TodoDocumentNotes" n
    WHERE n."WorkspaceId" = "TodoDocumentMetadata"."WorkspaceId" AND n."SingletonId" = "TodoDocumentMetadata"."SingletonId" AND n."IsDeleted" = 0);
""");

            migrationBuilder.Sql("""
UPDATE "TodoDocumentMetadata"
SET "CompletedJson" = (
    SELECT json_group_array(json_object(
        'date', g."Date",
        'items', (
            SELECT json_group_array(json_object('id', i."ItemId", 'qualifier', i."Qualifier", 'summary', i."Summary") ORDER BY i."Ordinal")
            FROM "TodoCompletedItems" i
            WHERE i."GroupId" = g."Id" AND i."IsDeleted" = 0
        )) ORDER BY g."Ordinal")
    FROM "TodoCompletedGroups" g
    WHERE g."WorkspaceId" = "TodoDocumentMetadata"."WorkspaceId" AND g."SingletonId" = "TodoDocumentMetadata"."SingletonId" AND g."IsDeleted" = 0
)
WHERE EXISTS (
    SELECT 1 FROM "TodoCompletedGroups" g
    WHERE g."WorkspaceId" = "TodoDocumentMetadata"."WorkspaceId" AND g."SingletonId" = "TodoDocumentMetadata"."SingletonId" AND g."IsDeleted" = 0);
""");

            migrationBuilder.DropTable(
                name: "TodoCompletedItems");

            migrationBuilder.DropTable(
                name: "TodoDocumentNotes");

            migrationBuilder.DropTable(
                name: "TodoCompletedGroups");
        }
    }
}
