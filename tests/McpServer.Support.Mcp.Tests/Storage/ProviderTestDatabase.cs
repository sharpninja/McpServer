using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// Isolated SQLite test database used by sixteenth identity consumer tests.
/// </summary>
internal sealed class ProviderTestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _sqliteConnection;

    private ProviderTestDatabase(
        DbContextOptions<McpDbContext> options,
        McpDatabaseProviderOptions providerOptions,
        SqliteConnection sqliteConnection)
    {
        Options = options;
        ProviderOptions = providerOptions;
        _sqliteConnection = sqliteConnection;
    }

    /// <summary>EF options bound to the isolated connection.</summary>
    public DbContextOptions<McpDbContext> Options { get; }

    /// <summary>Provider options used by <see cref="McpDatabaseMigrationCoordinator"/>.</summary>
    public McpDatabaseProviderOptions ProviderOptions { get; }

    /// <summary>Creates a context on the isolated connection.</summary>
    public McpDbContext CreateContext() => new(Options);

    /// <summary>Creates an open in-memory SQLite database.</summary>
    public static ProviderTestDatabase CreateSqlite()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return new ProviderTestDatabase(
            new DbContextOptionsBuilder<McpDbContext>()
                .UseSqlite(
                    connection,
                    sqlite => sqlite.MigrationsAssembly("McpServer.Storage.SqliteMigrations"))
                .Options,
            new McpDatabaseProviderOptions(
                McpDatabaseProviderKind.Sqlite,
                "sqlite",
                connection.ConnectionString,
                "McpServer.Storage.SqliteMigrations"),
            connection);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _sqliteConnection.DisposeAsync().ConfigureAwait(false);
    }
}
