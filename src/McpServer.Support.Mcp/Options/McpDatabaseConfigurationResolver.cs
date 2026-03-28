using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Support.Mcp.Options;

/// <summary>
/// Resolves effective MCP database-provider configuration and applies it to service registration.
/// </summary>
internal static class McpDatabaseConfigurationResolver
{
    private const string InMemoryDatabaseName = "mcp-tests";

    /// <summary>
    /// Adds the configured <see cref="McpDbContext"/> registration for the selected provider.
    /// </summary>
    /// <param name="services">Service collection receiving the DbContext registration.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="instanceName">Optional MCP instance name.</param>
    /// <param name="isTestEnvironment">Whether the current host environment is <c>Test</c>.</param>
    public static void AddConfiguredMcpDbContext(
        this IServiceCollection services,
        IConfiguration configuration,
        string? instanceName,
        bool isTestEnvironment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (isTestEnvironment && ShouldUseInMemoryDatabase(configuration, instanceName))
        {
            services.AddDbContext<McpDbContext>(options =>
            {
                options.UseInMemoryDatabase(InMemoryDatabaseName);
                options.EnableSensitiveDataLogging();
            }, ServiceLifetime.Scoped, ServiceLifetime.Scoped);
            return;
        }

        var providerOptions = ResolveProviderOptions(configuration, instanceName);
        services.AddDbContext<McpDbContext>(options =>
        {
            McpDatabaseProviderFactory.Configure(options, providerOptions);
        }, ServiceLifetime.Scoped, ServiceLifetime.Scoped);
    }

    /// <summary>
    /// Resolves the configured provider and connection string to a normalized provider option set.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="instanceName">Optional MCP instance name.</param>
    /// <returns>The resolved provider options.</returns>
    public static McpDatabaseProviderOptions ResolveProviderOptions(IConfiguration configuration, string? instanceName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var requestedProvider = McpInstanceResolver.GetEffectiveMcpValue(configuration, instanceName, "DatabaseProvider") ?? "sqlite";
        var strategy = McpDatabaseProviderFactory.ResolveStrategy(requestedProvider);
        var migrationsAssembly = McpInstanceResolver.GetEffectiveMcpValue(configuration, instanceName, "DatabaseMigrationsAssembly");

        var connectionString = strategy.Kind switch
        {
            McpDatabaseProviderKind.Sqlite => ResolveSqliteConnectionString(configuration, instanceName),
            McpDatabaseProviderKind.PostgreSql => ResolvePostgreSqlConnectionString(configuration, instanceName),
            McpDatabaseProviderKind.SqlServer => ResolveSqlServerConnectionString(configuration, instanceName),
            _ => throw new InvalidOperationException($"Unsupported MCP database provider '{requestedProvider}'."),
        };

        return McpDatabaseProviderFactory.CreateOptions(requestedProvider, connectionString, migrationsAssembly);
    }

    private static bool ShouldUseInMemoryDatabase(IConfiguration configuration, string? instanceName)
    {
        var raw = McpInstanceResolver.GetEffectiveMcpValue(configuration, instanceName, "UseInMemoryDatabaseForTests");
        return !bool.TryParse(raw, out var enabled) || enabled;
    }

    private static string ResolveSqliteConnectionString(IConfiguration configuration, string? instanceName)
    {
        var dataSource = McpInstanceResolver.ResolveSqliteDataSource(configuration, instanceName);
        return $"Data Source={dataSource}";
    }

    private static string ResolvePostgreSqlConnectionString(IConfiguration configuration, string? instanceName)
    {
        var configured = McpInstanceResolver.GetEffectiveMcpValue(configuration, instanceName, "PostgresConnectionString")
            ?? configuration.GetConnectionString("Mcp");
#pragma warning disable CS0618
        var resolved = PostgresConnectionStringResolver.ResolveConnectionString(
            configured,
            "DATABASE_URL",
            "POSTGRES_CONNECTION_STRING",
            "MCP_POSTGRES_CONNECTION_STRING");
#pragma warning restore CS0618

        if (string.IsNullOrWhiteSpace(resolved))
        {
            throw new InvalidOperationException(
                "Mcp:PostgresConnectionString (or ConnectionStrings:Mcp) is required when Mcp:DatabaseProvider is postgresql.");
        }

        return resolved;
    }

    private static string ResolveSqlServerConnectionString(IConfiguration configuration, string? instanceName)
    {
        var resolved = McpInstanceResolver.GetEffectiveMcpValue(configuration, instanceName, "SqlServerConnectionString")
            ?? configuration.GetConnectionString("McpSqlServer")
            ?? Environment.GetEnvironmentVariable("MCP_SQLSERVER_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(resolved))
        {
            throw new InvalidOperationException(
                "Mcp:SqlServerConnectionString (or ConnectionStrings:McpSqlServer) is required when Mcp:DatabaseProvider is sqlserver.");
        }

        return resolved;
    }
}
