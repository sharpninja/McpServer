using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Storage.Database;

/// <summary>
/// Configures <see cref="McpDbContext"/> for SQL Server-backed storage.
/// </summary>
public sealed class SqlServerMcpDatabaseProviderStrategy : IMcpDatabaseProviderStrategy
{
    // SqlClient connection resiliency defaults applied when the operator connection string does
    // not specify them. Command-level EnableRetryOnFailure is intentionally NOT used because the
    // session-log/requirements write paths open user-initiated transactions, which the retrying
    // execution strategy rejects. 6 x 10 s covers the observed transient windows where the
    // connection pool could not re-establish connections while SQL Server stayed up
    // (triage-report-0009bcac98de435dbae803806f846c11).
    private const int DefaultConnectRetryCount = 6;
    private const int DefaultConnectRetryIntervalSeconds = 10;

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
            BuildResilientConnectionString(providerOptions.ConnectionString),
            sqlServer => sqlServer.MigrationsAssembly(providerOptions.MigrationsAssembly));
    }

    private static string BuildResilientConnectionString(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        // SqlClient accepts both "ConnectRetryCount" and the spaced "Connect Retry Count"
        // keyword forms, so detect operator-specified values space-insensitively.
        var normalized = connectionString.Replace(" ", string.Empty, StringComparison.Ordinal);
        if (!normalized.Contains("ConnectRetryCount=", StringComparison.OrdinalIgnoreCase))
            builder.ConnectRetryCount = DefaultConnectRetryCount;
        if (!normalized.Contains("ConnectRetryInterval=", StringComparison.OrdinalIgnoreCase))
            builder.ConnectRetryInterval = DefaultConnectRetryIntervalSeconds;
        return builder.ConnectionString;
    }
}
