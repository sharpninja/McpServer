using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace QBrainAi.Support.Mcp.Options;

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

        var runtimeOptions = ResolveRuntimeOptions(configuration, instanceName);
        var providerOptions = runtimeOptions.ProviderOptions;
        services.AddSingleton(runtimeOptions);
        services.AddSingleton(providerOptions);
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
        => ResolveRuntimeOptions(configuration, instanceName).ProviderOptions;

    /// <summary>
    /// Resolves the configured provider, connection string, and native encryption settings into one runtime contract.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="instanceName">Optional MCP instance name.</param>
    /// <returns>The resolved runtime options.</returns>
    public static McpDatabaseRuntimeOptions ResolveRuntimeOptions(IConfiguration configuration, string? instanceName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var requestedProvider = ResolveRequestedProvider(configuration, instanceName);
        var strategy = McpDatabaseProviderFactory.ResolveStrategy(requestedProvider);
        var migrationsAssembly = GetEffectiveDatabaseValue(configuration, instanceName, "MigrationsAssembly")
            ?? McpInstanceResolver.GetEffectiveMcpValue(configuration, instanceName, "DatabaseMigrationsAssembly")
            ?? QBrainAi.Common.AgentCli.ProductEnvironment.GetLegacy("MCP_DATABASE_MIGRATIONS_ASSEMBLY");

        var connectionString = strategy.Kind switch
        {
            McpDatabaseProviderKind.Sqlite => ResolveSqliteConnectionString(configuration, instanceName),
            McpDatabaseProviderKind.PostgreSql => ResolvePostgreSqlConnectionString(configuration, instanceName),
            McpDatabaseProviderKind.SqlServer => ResolveSqlServerConnectionString(configuration, instanceName),
            _ => throw new InvalidOperationException($"Unsupported MCP database provider '{requestedProvider}'."),
        };

        var autoMigrateRaw = GetEffectiveDatabaseValue(configuration, instanceName, "AutoMigrate");
        if (autoMigrateRaw is not null && !bool.TryParse(autoMigrateRaw, out _))
            throw new InvalidOperationException("Mcp:Database:AutoMigrate must be true or false.");
        var autoMigrate = autoMigrateRaw is null || bool.Parse(autoMigrateRaw);

        return new McpDatabaseRuntimeOptions(
            McpDatabaseProviderFactory.CreateOptions(requestedProvider, connectionString, migrationsAssembly),
            ResolveEncryptionOptions(configuration, instanceName),
            autoMigrate);
    }

    private static bool ShouldUseInMemoryDatabase(IConfiguration configuration, string? instanceName)
    {
        var raw = McpInstanceResolver.GetEffectiveMcpValue(configuration, instanceName, "UseInMemoryDatabaseForTests");
        return !bool.TryParse(raw, out var enabled) || enabled;
    }

    private static string ResolveSqliteConnectionString(IConfiguration configuration, string? instanceName)
    {
        var configuredConnectionString = GetEffectiveDatabaseValue(configuration, instanceName, "Sqlite:ConnectionString");
        if (!string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return configuredConnectionString;
        }

        var configuredDataSource = GetEffectiveDatabaseValue(configuration, instanceName, "Sqlite:DataSource")
            ?? QBrainAi.Common.AgentCli.ProductEnvironment.GetLegacy("MCP_SQLITE_DATA_SOURCE");
        if (!string.IsNullOrWhiteSpace(configuredDataSource))
        {
            return configuredDataSource.Contains('=', StringComparison.Ordinal)
                ? configuredDataSource
                : $"Data Source={McpInstanceResolver.ResolveDataPath(configuration, instanceName, configuredDataSource)}";
        }

        var dataSource = McpInstanceResolver.ResolveSqliteDataSource(configuration, instanceName);
        return $"Data Source={dataSource}";
    }

    private static string ResolvePostgreSqlConnectionString(IConfiguration configuration, string? instanceName)
    {
        var configured = GetEffectiveDatabaseValue(configuration, instanceName, "PostgreSql:ConnectionString")
            ?? McpInstanceResolver.GetEffectiveMcpValue(configuration, instanceName, "PostgresConnectionString")
            ?? configuration.GetConnectionString("Mcp");
        var connectionConfiguration = new ConfigurationManager();
        if (!string.IsNullOrWhiteSpace(configured))
        {
            connectionConfiguration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Mcp"] = configured
            });
        }

        var resolved = new RailwayConnectionStringBuilder(connectionConfiguration)
            .WithConfigKey("Mcp")
            .WithEnvironmentFallback(
                "DATABASE_URL",
                "POSTGRES_CONNECTION_STRING",
                "MCP_POSTGRES_CONNECTION_STRING")
            .Build();

        if (string.IsNullOrWhiteSpace(resolved))
        {
            throw new InvalidOperationException(
                "Mcp:PostgresConnectionString (or ConnectionStrings:Mcp) is required when Mcp:DatabaseProvider is postgresql.");
        }

        return resolved;
    }

    private static string ResolveSqlServerConnectionString(IConfiguration configuration, string? instanceName)
    {
        var resolved = GetEffectiveDatabaseValue(configuration, instanceName, "SqlServer:ConnectionString")
            ?? McpInstanceResolver.GetEffectiveMcpValue(configuration, instanceName, "SqlServerConnectionString")
            ?? configuration.GetConnectionString("McpSqlServer")
            ?? QBrainAi.Common.AgentCli.ProductEnvironment.GetLegacy("MCP_SQLSERVER_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(resolved))
        {
            throw new InvalidOperationException(
                "Mcp:SqlServerConnectionString (or ConnectionStrings:McpSqlServer) is required when Mcp:DatabaseProvider is sqlserver.");
        }

        return resolved;
    }

    private static string ResolveRequestedProvider(IConfiguration configuration, string? instanceName)
        => GetEffectiveDatabaseValue(configuration, instanceName, "Provider")
            ?? McpInstanceResolver.GetEffectiveMcpValue(configuration, instanceName, "DatabaseProvider")
            ?? QBrainAi.Common.AgentCli.ProductEnvironment.GetLegacy("MCP_DATABASE_PROVIDER")
            ?? "sqlite";

    private static McpDatabaseEncryptionOptions ResolveEncryptionOptions(IConfiguration configuration, string? instanceName)
    {
        var enabledRaw = GetEffectiveDatabaseValue(configuration, instanceName, "Encryption:Enabled")
            ?? QBrainAi.Common.AgentCli.ProductEnvironment.GetLegacy("MCP_DATABASE_ENCRYPTION_ENABLED");
        var enabled = bool.TryParse(enabledRaw, out var parsedEnabled) && parsedEnabled;

        return new McpDatabaseEncryptionOptions(
            enabled,
            GetEffectiveDatabaseValue(configuration, instanceName, "Encryption:Sqlite:Key")
                ?? QBrainAi.Common.AgentCli.ProductEnvironment.GetLegacy("MCP_SQLITE_ENCRYPTION_KEY"),
            GetEffectiveDatabaseValue(configuration, instanceName, "Encryption:Sqlite:SeeToolPath")
                ?? QBrainAi.Common.AgentCli.ProductEnvironment.GetLegacy("MCP_SQLITE_SEE_TOOL_PATH"),
            GetEffectiveDatabaseValue(configuration, instanceName, "Encryption:PostgreSql:KeyProvider")
                ?? QBrainAi.Common.AgentCli.ProductEnvironment.GetLegacy("MCP_POSTGRES_TDE_KEY_PROVIDER"),
            GetEffectiveDatabaseValue(configuration, instanceName, "Encryption:PostgreSql:PrincipalKey")
                ?? QBrainAi.Common.AgentCli.ProductEnvironment.GetLegacy("MCP_POSTGRES_TDE_PRINCIPAL_KEY"),
            GetEffectiveDatabaseValue(configuration, instanceName, "Encryption:SqlServer:CertificateName")
                ?? QBrainAi.Common.AgentCli.ProductEnvironment.GetLegacy("MCP_SQLSERVER_TDE_CERTIFICATE"),
            GetEffectiveDatabaseValue(configuration, instanceName, "Encryption:SqlServer:DatabaseEncryptionKeyName")
                ?? QBrainAi.Common.AgentCli.ProductEnvironment.GetLegacy("MCP_SQLSERVER_TDE_DATABASE_ENCRYPTION_KEY"));
    }

    private static string? GetEffectiveDatabaseValue(IConfiguration configuration, string? instanceName, string nestedKey)
    {
        if (!string.IsNullOrWhiteSpace(instanceName))
        {
            var instanceValue = configuration[$"Mcp:Instances:{instanceName}:Database:{nestedKey}"];
            if (!string.IsNullOrWhiteSpace(instanceValue))
            {
                return instanceValue;
            }
        }

        return configuration[$"Mcp:Database:{nestedKey}"];
    }
}
