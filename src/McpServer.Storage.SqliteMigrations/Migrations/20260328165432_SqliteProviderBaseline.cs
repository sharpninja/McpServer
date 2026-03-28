using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace McpServer.Support.Mcp.Storage.SqliteMigrations.Migrations
{
    /// <summary>
    /// Adopts the current SQLite schema into the provider-owned migration history without
    /// recreating objects that already exist from the legacy shared migration stream.
    /// </summary>
    public partial class SqliteProviderBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op by design. The provider-specific model snapshot becomes the baseline for
            // future SQLite migrations, while existing databases keep their current schema.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op by design. Removing the SQLite provider baseline must not drop schema.
        }
    }
}
