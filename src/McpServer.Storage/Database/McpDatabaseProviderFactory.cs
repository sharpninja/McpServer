using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Storage.Database;

/// <summary>
/// Resolves provider aliases to canonical strategies and applies provider-specific
/// <see cref="McpDbContext"/> configuration.
/// </summary>
public static class McpDatabaseProviderFactory
{
    private static readonly IReadOnlyList<IMcpDatabaseProviderStrategy> s_strategies =
    [
        new SqliteMcpDatabaseProviderStrategy(),
        new PostgreSqlMcpDatabaseProviderStrategy(),
        new SqlServerMcpDatabaseProviderStrategy(),
    ];

    /// <summary>Gets the canonical provider names accepted by the factory.</summary>
    public static IReadOnlyCollection<string> SupportedProviders =>
        s_strategies.Select(x => x.CanonicalName).ToArray();

    /// <summary>
    /// Resolves the requested provider alias to the canonical provider strategy.
    /// </summary>
    /// <param name="providerName">Requested provider value from configuration or tooling.</param>
    /// <returns>The matching provider strategy.</returns>
    public static IMcpDatabaseProviderStrategy ResolveStrategy(string? providerName)
    {
        var normalized = providerName?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            normalized = "sqlite";

        var strategy = s_strategies.FirstOrDefault(candidate =>
            candidate.Aliases.Any(alias => string.Equals(alias, normalized, StringComparison.OrdinalIgnoreCase)));

        if (strategy is null)
        {
            throw new InvalidOperationException(
                $"Unsupported MCP database provider '{providerName}'. Allowed values: {string.Join(", ", SupportedProviders)}.");
        }

        return strategy;
    }

    /// <summary>
    /// Builds a resolved provider option set using the selected provider strategy.
    /// </summary>
    /// <param name="providerName">Requested provider value from configuration or tooling.</param>
    /// <param name="connectionString">Resolved provider connection string.</param>
    /// <param name="migrationsAssembly">Optional migration assembly override.</param>
    /// <returns>A normalized provider option set.</returns>
    public static McpDatabaseProviderOptions CreateOptions(
        string? providerName,
        string connectionString,
        string? migrationsAssembly = null)
    {
        var strategy = ResolveStrategy(providerName);
        return new McpDatabaseProviderOptions(
            strategy.Kind,
            strategy.CanonicalName,
            connectionString,
            string.IsNullOrWhiteSpace(migrationsAssembly) ? strategy.DefaultMigrationsAssembly : migrationsAssembly);
    }

    /// <summary>
    /// Applies the resolved provider configuration to an EF Core options builder.
    /// </summary>
    /// <param name="optionsBuilder">Options builder to configure.</param>
    /// <param name="providerOptions">Resolved provider settings.</param>
    public static void Configure(DbContextOptionsBuilder optionsBuilder, McpDatabaseProviderOptions providerOptions)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentNullException.ThrowIfNull(providerOptions);

        ResolveStrategy(providerOptions.ProviderName).Configure(optionsBuilder, providerOptions);
    }
}
