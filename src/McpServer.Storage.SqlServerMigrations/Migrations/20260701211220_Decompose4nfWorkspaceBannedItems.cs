using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations
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
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeleteReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
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

        private static string BackfillSql(string jsonColumn, string category) => $@"
INSERT INTO [WorkspaceBannedItems] ([WorkspaceId], [Category], [Ordinal], [Value])
SELECT w.[WorkspaceId], '{category}', CAST(j.[key] AS int), j.[value]
FROM [Workspaces] w
CROSS APPLY OPENJSON(w.[{jsonColumn}]) j
WHERE w.[{jsonColumn}] IS NOT NULL AND ISJSON(w.[{jsonColumn}]) = 1;";

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "BannedCountriesOfOriginJson", table: "Workspaces", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "BannedIndividualsJson", table: "Workspaces", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "BannedLicensesJson", table: "Workspaces", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "BannedOrganizationsJson", table: "Workspaces", type: "nvarchar(max)", nullable: true);

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

            migrationBuilder.DropTable(name: "WorkspaceBannedItems");
        }

        private static string RestoreSql(string jsonColumn, string category) => $@"
UPDATE w SET [{jsonColumn}] = j.[json]
FROM [Workspaces] w
CROSS APPLY (
    SELECT CONCAT('[', STRING_AGG(CONCAT('""', STRING_ESCAPE(i.[Value], 'json'), '""'), ',') WITHIN GROUP (ORDER BY i.[Ordinal]), ']') AS [json]
    FROM [WorkspaceBannedItems] i
    WHERE i.[WorkspaceId] = w.[WorkspaceId] AND i.[Category] = '{category}' AND i.[IsDeleted] = 0
) j
WHERE j.[json] IS NOT NULL;";
    }
}
