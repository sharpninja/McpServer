using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using McpServer.Support.Mcp.Storage.Database;

namespace McpServer.Support.Mcp.Storage;

/// <summary>
/// Design-time factory for <see cref="McpDbContext"/>.
/// Used by EF Core tooling (dotnet-ef) to create a context instance at design time.
/// </summary>
public sealed class McpDbContextFactory : IDesignTimeDbContextFactory<McpDbContext>
{
    private const string DefaultSqliteConnectionString = "Data Source=mcp_design_time.db";
    private const string DefaultPostgreSqlConnectionString = "Host=localhost;Port=5432;Database=mcp_design_time;Username=postgres;Password=postgres";
    private const string DefaultSqlServerConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=mcp_design_time;Trusted_Connection=True;TrustServerCertificate=True";

    /// <inheritdoc />
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members attributed with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "This factory is only for EF Core design-time tooling; trimmed runtime hosts do not invoke it.")]
    public McpDbContext CreateDbContext(string[] args)
    {
        var providerName = GetArgumentValue(args, "provider")
            ?? Environment.GetEnvironmentVariable("MCP_EF_PROVIDER")
            ?? "sqlite";
        var connectionString = ResolveConnectionString(providerName, args);
        var migrationsAssembly = GetArgumentValue(args, "migrations-assembly")
            ?? Environment.GetEnvironmentVariable("MCP_EF_MIGRATIONS_ASSEMBLY");
        var providerOptions = McpDatabaseProviderFactory.CreateOptions(providerName, connectionString, migrationsAssembly);

        var optionsBuilder = new DbContextOptionsBuilder<McpDbContext>();
        McpDatabaseProviderFactory.Configure(optionsBuilder, providerOptions);
        return new McpDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString(string providerName, string[] args)
    {
        var explicitConnectionString = GetArgumentValue(args, "connection")
            ?? Environment.GetEnvironmentVariable("MCP_EF_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(explicitConnectionString))
        {
            return NormalizeSqliteConnectionString(providerName, explicitConnectionString);
        }

        return McpDatabaseProviderFactory.ResolveStrategy(providerName).Kind switch
        {
            McpDatabaseProviderKind.Sqlite => NormalizeSqliteConnectionString(
                providerName,
                GetArgumentValue(args, "sqlite-data-source")
                    ?? Environment.GetEnvironmentVariable("MCP_SQLITE_DATA_SOURCE")
                    ?? DefaultSqliteConnectionString),
            McpDatabaseProviderKind.PostgreSql => Environment.GetEnvironmentVariable("MCP_POSTGRES_CONNECTION_STRING")
                ?? DefaultPostgreSqlConnectionString,
            McpDatabaseProviderKind.SqlServer => Environment.GetEnvironmentVariable("MCP_SQLSERVER_CONNECTION_STRING")
                ?? DefaultSqlServerConnectionString,
            _ => throw new InvalidOperationException($"Unsupported MCP database provider '{providerName}'."),
        };
    }

    private static string NormalizeSqliteConnectionString(string providerName, string connectionString)
    {
        if (McpDatabaseProviderFactory.ResolveStrategy(providerName).Kind != McpDatabaseProviderKind.Sqlite)
        {
            return connectionString;
        }

        return connectionString.Contains('=', StringComparison.Ordinal)
            ? connectionString
            : $"Data Source={connectionString}";
    }

    private static string? GetArgumentValue(string[] args, string key)
    {
        if (args.Length == 0)
        {
            return null;
        }

        var prefix = $"--{key}=";
        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return arg[prefix.Length..];
            }

            if (string.Equals(arg, $"--{key}", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
            {
                return args[index + 1];
            }
        }

        return null;
    }
}
