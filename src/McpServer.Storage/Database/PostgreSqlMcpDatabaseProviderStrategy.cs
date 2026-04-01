using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace McpServer.Support.Mcp.Storage.Database;

/// <summary>
/// Configures <see cref="McpDbContext"/> for PostgreSQL-backed storage.
/// </summary>
public sealed class PostgreSqlMcpDatabaseProviderStrategy : IMcpDatabaseProviderStrategy
{
    /// <inheritdoc />
    public McpDatabaseProviderKind Kind => McpDatabaseProviderKind.PostgreSql;

    /// <inheritdoc />
    public string CanonicalName => "postgresql";

    /// <inheritdoc />
    public IReadOnlyCollection<string> Aliases { get; } = ["postgres", "postgresql", "npgsql"];

    /// <inheritdoc />
    public string DefaultMigrationsAssembly => "McpServer.Storage.PostgreSqlMigrations";

    /// <inheritdoc />
    public void Configure(DbContextOptionsBuilder optionsBuilder, McpDatabaseProviderOptions providerOptions)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentNullException.ThrowIfNull(providerOptions);

        optionsBuilder.UseNpgsql(
            providerOptions.ConnectionString,
            npgsql => npgsql.MigrationsAssembly(providerOptions.MigrationsAssembly));
        optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
    }
}
