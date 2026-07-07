using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations
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

            // Hardening: add the constraints WITH NOCHECK so pre-existing orphan rows (a report,
            // run, or event log whose parent group/definition is missing) cannot abort the
            // migration and block startup. The constraints are still enforced for all new inserts
            // and updates; only historical rows are exempt from the one-time validation scan.
            migrationBuilder.Sql(@"
ALTER TABLE [AgentEventLogs] WITH NOCHECK
ADD CONSTRAINT [FK_AgentEventLogs_AgentDefinitions_AgentId]
FOREIGN KEY ([AgentId]) REFERENCES [AgentDefinitions] ([Id]) ON DELETE NO ACTION;");

            migrationBuilder.Sql(@"
ALTER TABLE [TriageReports] WITH NOCHECK
ADD CONSTRAINT [FK_TriageReports_TriageGroups_GroupId]
FOREIGN KEY ([GroupId]) REFERENCES [TriageGroups] ([GroupId]) ON DELETE NO ACTION;");

            migrationBuilder.Sql(@"
ALTER TABLE [TriageResearchRuns] WITH NOCHECK
ADD CONSTRAINT [FK_TriageResearchRuns_TriageGroups_GroupId]
FOREIGN KEY ([GroupId]) REFERENCES [TriageGroups] ([GroupId]) ON DELETE NO ACTION;");
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
