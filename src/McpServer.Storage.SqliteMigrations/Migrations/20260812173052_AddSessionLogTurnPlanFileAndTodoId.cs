using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqliteMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionLogTurnPlanFileAndTodoId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlanFile",
                table: "SessionLogTurns",
                type: "TEXT",
                maxLength: 2048,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "TodoId",
                table: "SessionLogTurns",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogTurns_PlanFile",
                table: "SessionLogTurns",
                column: "PlanFile");

            migrationBuilder.CreateIndex(
                name: "IX_SessionLogTurns_TodoId",
                table: "SessionLogTurns",
                column: "TodoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SessionLogTurns_PlanFile",
                table: "SessionLogTurns");

            migrationBuilder.DropIndex(
                name: "IX_SessionLogTurns_TodoId",
                table: "SessionLogTurns");

            migrationBuilder.DropColumn(
                name: "PlanFile",
                table: "SessionLogTurns");

            migrationBuilder.DropColumn(
                name: "TodoId",
                table: "SessionLogTurns");
        }
    }
}
