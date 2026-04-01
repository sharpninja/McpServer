using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Storage.Database;

/// <summary>
/// Configures <see cref="McpDbContext"/> for SQL Server-backed storage.
/// </summary>
public sealed class SqlServerMcpDatabaseProviderStrategy : IMcpDatabaseProviderStrategy
{
    /// <inheritdoc />
    public McpDatabaseProviderKind Kind => McpDatabaseProviderKind.SqlServer;

    /// <inheritdoc />
    public string CanonicalName => "sqlserver";

    /// <inheritdoc />
    public IReadOnlyCollection<string> Aliases { get; } = ["sqlserver", "sql-server", "mssql"];

    /// <inheritdoc />
    public string DefaultMigrationsAssembly => "McpServer.Storage.SqlServerMigrations";

    /// <inheritdoc />
    public void Configure(DbContextOptionsBuilder optionsBuilder, McpDatabaseProviderOptions providerOptions)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentNullException.ThrowIfNull(providerOptions);

        optionsBuilder.UseSqlServer(
            providerOptions.ConnectionString,
            sqlServer => sqlServer.MigrationsAssembly(providerOptions.MigrationsAssembly));
    }
}
