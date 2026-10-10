using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QBrainAi.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
{
    /// <summary>
    /// TR-MCP-QBRAIN-003: Renames TriageGroups.IsMcpServerRelated on databases created before the QBrain.AI column name.
    /// </summary>
    public partial class RenameTriageIsMcpServerRelatedColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsMcpServerRelated",
                table: "TriageGroups",
                newName: "IsQBrainAiRelated");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsQBrainAiRelated",
                table: "TriageGroups",
                newName: "IsMcpServerRelated");
        }
    }
}
