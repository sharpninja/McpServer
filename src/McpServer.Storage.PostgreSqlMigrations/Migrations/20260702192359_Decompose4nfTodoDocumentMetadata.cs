using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    SingletonId = table.Column<int>(type: "integer", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DeleteReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoCompletedGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TodoCompletedGroups_TodoDocumentMetadata_WorkspaceId_Single~",
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    SingletonId = table.Column<int>(type: "integer", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    DeleteReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TodoDocumentNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TodoDocumentNotes_TodoDocumentMetadata_WorkspaceId_Singleto~",
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    GroupId = table.Column<long>(type: "bigint", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Qualifier = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    DeleteReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
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
SELECT m."WorkspaceId", m."SingletonId", (j.ordinality - 1)::int, j.value
FROM "TodoDocumentMetadata" m
CROSS JOIN LATERAL jsonb_array_elements_text(m."NotesJson"::jsonb) WITH ORDINALITY AS j(value, ordinality)
WHERE m."NotesJson" IS NOT NULL AND jsonb_typeof(m."NotesJson"::jsonb) = 'array';
""");

            migrationBuilder.Sql("""
INSERT INTO "TodoCompletedGroups" ("WorkspaceId", "SingletonId", "Ordinal", "Date")
SELECT m."WorkspaceId", m."SingletonId", (arr.ordinality - 1)::int, arr.value ->> 'date'
FROM "TodoDocumentMetadata" m
CROSS JOIN LATERAL jsonb_array_elements(m."CompletedJson"::jsonb) WITH ORDINALITY AS arr(value, ordinality)
WHERE m."CompletedJson" IS NOT NULL AND jsonb_typeof(m."CompletedJson"::jsonb) = 'array';
""");

            migrationBuilder.Sql("""
INSERT INTO "TodoCompletedItems" ("WorkspaceId", "GroupId", "Ordinal", "ItemId", "Qualifier", "Summary")
SELECT m."WorkspaceId", g."Id", (itm.ordinality - 1)::int,
       itm.value ->> 'id', itm.value ->> 'qualifier', itm.value ->> 'summary'
FROM "TodoDocumentMetadata" m
CROSS JOIN LATERAL jsonb_array_elements(m."CompletedJson"::jsonb) WITH ORDINALITY AS arr(value, ordinality)
CROSS JOIN LATERAL jsonb_array_elements(arr.value -> 'items') WITH ORDINALITY AS itm(value, ordinality)
JOIN "TodoCompletedGroups" g
  ON g."WorkspaceId" = m."WorkspaceId" AND g."SingletonId" = m."SingletonId" AND g."Ordinal" = (arr.ordinality - 1)::int
WHERE m."CompletedJson" IS NOT NULL AND jsonb_typeof(m."CompletedJson"::jsonb) = 'array'
  AND jsonb_typeof(arr.value -> 'items') = 'array';
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
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotesJson",
                table: "TodoDocumentMetadata",
                type: "text",
                nullable: true);

            // Reconstruct the JSON blocks (camelCase keys, ordered) from the child rows.
            migrationBuilder.Sql("""
UPDATE "TodoDocumentMetadata" m
SET "NotesJson" = j.json
FROM (
    SELECT "WorkspaceId", "SingletonId", jsonb_agg("Value" ORDER BY "Ordinal")::text AS json
    FROM "TodoDocumentNotes"
    WHERE "IsDeleted" = false
    GROUP BY "WorkspaceId", "SingletonId"
) j
WHERE j."WorkspaceId" = m."WorkspaceId" AND j."SingletonId" = m."SingletonId";
""");

            migrationBuilder.Sql("""
UPDATE "TodoDocumentMetadata" m
SET "CompletedJson" = j.json
FROM (
    SELECT g."WorkspaceId", g."SingletonId",
           jsonb_agg(jsonb_build_object(
               'date', g."Date",
               'items', COALESCE(gi.items, 'null'::jsonb)) ORDER BY g."Ordinal")::text AS json
    FROM "TodoCompletedGroups" g
    LEFT JOIN LATERAL (
        SELECT jsonb_agg(jsonb_build_object('id', i."ItemId", 'qualifier', i."Qualifier", 'summary', i."Summary") ORDER BY i."Ordinal") AS items
        FROM "TodoCompletedItems" i
        WHERE i."GroupId" = g."Id" AND i."IsDeleted" = false
    ) gi ON true
    WHERE g."IsDeleted" = false
    GROUP BY g."WorkspaceId", g."SingletonId"
) j
WHERE j."WorkspaceId" = m."WorkspaceId" AND j."SingletonId" = m."SingletonId";
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
