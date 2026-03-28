using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations
{
    /// <summary>
    /// Adopts the current PostgreSQL schema into the provider-owned migration history without
    /// replaying the legacy shared migration stream against an existing database.
    /// </summary>
    public partial class PostgreSqlProviderBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op by design. The provider-specific model snapshot becomes the baseline for
            // future PostgreSQL migrations, while existing databases keep their current schema.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op by design. Removing the PostgreSQL provider baseline must not drop schema.
        }
    }
}
