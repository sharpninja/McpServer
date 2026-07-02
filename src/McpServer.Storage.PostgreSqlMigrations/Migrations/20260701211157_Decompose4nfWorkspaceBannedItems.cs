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
            migrationBuilder.DropColumn(
                name: "BannedCountriesOfOriginJson",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "BannedIndividualsJson",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "BannedLicensesJson",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "BannedOrganizationsJson",
                table: "Workspaces");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkspaceBannedItems");

            migrationBuilder.AddColumn<string>(
                name: "BannedCountriesOfOriginJson",
                table: "Workspaces",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannedIndividualsJson",
                table: "Workspaces",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannedLicensesJson",
                table: "Workspaces",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannedOrganizationsJson",
                table: "Workspaces",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Workspaces",
                keyColumn: "WorkspaceId",
                keyValue: "",
                columns: new[] { "BannedCountriesOfOriginJson", "BannedIndividualsJson", "BannedLicensesJson", "BannedOrganizationsJson" },
                values: new object[] { null, null, null, null });
        }
    }
}
