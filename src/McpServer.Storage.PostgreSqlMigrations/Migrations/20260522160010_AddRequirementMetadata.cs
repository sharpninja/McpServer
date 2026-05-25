using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddRequirementMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Requirements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "Requirements",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "medium");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Requirements",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "pending");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Requirements");
        }
    }
}
