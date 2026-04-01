using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Storage.Database;

/// <summary>
/// Configures <see cref="McpDbContext"/> for SQLite-backed storage.
/// </summary>
public sealed class SqliteMcpDatabaseProviderStrategy : IMcpDatabaseProviderStrategy
{
    /// <inheritdoc />
    public McpDatabaseProviderKind Kind => McpDatabaseProviderKind.Sqlite;

    /// <inheritdoc />
    public string CanonicalName => "sqlite";

    /// <inheritdoc />
    public IReadOnlyCollection<string> Aliases { get; } = ["sqlite"];

    /// <inheritdoc />
    public string DefaultMigrationsAssembly => "McpServer.Storage.SqliteMigrations";

    /// <inheritdoc />
    public void Configure(DbContextOptionsBuilder optionsBuilder, McpDatabaseProviderOptions providerOptions)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentNullException.ThrowIfNull(providerOptions);

        optionsBuilder.UseSqlite(
            providerOptions.ConnectionString,
            sqlite => sqlite.MigrationsAssembly(providerOptions.MigrationsAssembly));
    }
}
