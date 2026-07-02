using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
{
    /// <inheritdoc />
    public partial class AddTriageAndAgentEventForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TriageResearchRuns_GroupId",
                table: "TriageResearchRuns",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TriageReports_GroupId",
                table: "TriageReports",
                column: "GroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_AgentEventLogs_AgentDefinitions_AgentId",
                table: "AgentEventLogs",
                column: "AgentId",
                principalTable: "AgentDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TriageReports_TriageGroups_GroupId",
                table: "TriageReports",
                column: "GroupId",
                principalTable: "TriageGroups",
                principalColumn: "GroupId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TriageResearchRuns_TriageGroups_GroupId",
                table: "TriageResearchRuns",
                column: "GroupId",
                principalTable: "TriageGroups",
                principalColumn: "GroupId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgentEventLogs_AgentDefinitions_AgentId",
                table: "AgentEventLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_TriageReports_TriageGroups_GroupId",
                table: "TriageReports");

            migrationBuilder.DropForeignKey(
                name: "FK_TriageResearchRuns_TriageGroups_GroupId",
                table: "TriageResearchRuns");

            migrationBuilder.DropIndex(
                name: "IX_TriageResearchRuns_GroupId",
                table: "TriageResearchRuns");

            migrationBuilder.DropIndex(
                name: "IX_TriageReports_GroupId",
                table: "TriageReports");
        }
    }
}
