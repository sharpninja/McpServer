using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations
{
    /// <summary>
    /// Establishes the SQL Server provider-owned migration history at the current model shape
    /// without requiring schema recreation during first-time adoption.
    /// </summary>
    public partial class SqlServerProviderBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op by design. The provider-specific model snapshot becomes the baseline for
            // future SQL Server migrations, while adoption does not recreate schema objects.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op by design. Removing the SQL Server provider baseline must not drop schema.
        }
    }
}
