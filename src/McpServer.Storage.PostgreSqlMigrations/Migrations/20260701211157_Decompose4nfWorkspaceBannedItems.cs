using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class Decompose4nfWorkspaceBannedItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkspaceBannedItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkspaceId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    DeleteReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceBannedItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspaceBannedItems_Workspaces_WorkspaceId",
                        column: x => x.WorkspaceId,
                        principalTable: "Workspaces",
                        principalColumn: "WorkspaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceBannedItems_WorkspaceId_Category_Ordinal",
                table: "WorkspaceBannedItems",
                columns: new[] { "WorkspaceId", "Category", "Ordinal" });

            // FR-MCP-105 data migration: backfill the four banned-policy JSON string arrays into
            // ordered 4NF child rows (discriminated by Category) before the source columns are dropped.
            migrationBuilder.Sql(BackfillSql("BannedLicensesJson", "License"));
            migrationBuilder.Sql(BackfillSql("BannedCountriesOfOriginJson", "Country"));
            migrationBuilder.Sql(BackfillSql("BannedOrganizationsJson", "Organization"));
            migrationBuilder.Sql(BackfillSql("BannedIndividualsJson", "Individual"));

            migrationBuilder.DropColumn(name: "BannedCountriesOfOriginJson", table: "Workspaces");
            migrationBuilder.DropColumn(name: "BannedIndividualsJson", table: "Workspaces");
            migrationBuilder.DropColumn(name: "BannedLicensesJson", table: "Workspaces");
            migrationBuilder.DropColumn(name: "BannedOrganizationsJson", table: "Workspaces");
        }

        private static string BackfillSql(string jsonColumn, string category) => $"""
INSERT INTO "WorkspaceBannedItems" ("WorkspaceId", "Category", "Ordinal", "Value")
SELECT w."WorkspaceId", '{category}', (j.ordinality - 1)::int, j.value
FROM "Workspaces" w
CROSS JOIN LATERAL jsonb_array_elements_text(w."{jsonColumn}"::jsonb) WITH ORDINALITY AS j(value, ordinality)
WHERE w."{jsonColumn}" IS NOT NULL AND jsonb_typeof(w."{jsonColumn}"::jsonb) = 'array';
""";

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "BannedCountriesOfOriginJson", table: "Workspaces", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "BannedIndividualsJson", table: "Workspaces", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "BannedLicensesJson", table: "Workspaces", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "BannedOrganizationsJson", table: "Workspaces", type: "text", nullable: true);

            migrationBuilder.Sql(RestoreSql("BannedLicensesJson", "License"));
            migrationBuilder.Sql(RestoreSql("BannedCountriesOfOriginJson", "Country"));
            migrationBuilder.Sql(RestoreSql("BannedOrganizationsJson", "Organization"));
            migrationBuilder.Sql(RestoreSql("BannedIndividualsJson", "Individual"));

            migrationBuilder.UpdateData(
                table: "Workspaces",
                keyColumn: "WorkspaceId",
                keyValue: "",
                columns: new[] { "BannedCountriesOfOriginJson", "BannedIndividualsJson", "BannedLicensesJson", "BannedOrganizationsJson" },
                values: new object[] { null, null, null, null });

            migrationBuilder.DropTable(
                name: "WorkspaceBannedItems");
        }

        private static string RestoreSql(string jsonColumn, string category) => $"""
UPDATE "Workspaces" w
SET "{jsonColumn}" = j.json
FROM (
    SELECT "WorkspaceId", jsonb_agg("Value" ORDER BY "Ordinal")::text AS json
    FROM "WorkspaceBannedItems"
    WHERE "Category" = '{category}' AND "IsDeleted" = false
    GROUP BY "WorkspaceId"
) j
WHERE j."WorkspaceId" = w."WorkspaceId";
""";
    }
}
