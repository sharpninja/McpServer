namespace McpServer.Support.Mcp.Storage.Database;

/// <summary>
/// Describes the resolved provider settings needed to configure <see cref="McpDbContext"/>
/// for a specific relational engine and migration assembly.
/// </summary>
public sealed class McpDatabaseProviderOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="McpDatabaseProviderOptions"/> class.
    /// </summary>
    /// <param name="providerKind">Resolved provider kind.</param>
    /// <param name="providerName">Canonical provider name.</param>
    /// <param name="connectionString">Resolved provider connection string.</param>
    /// <param name="migrationsAssembly">Assembly containing the provider-owned EF migrations.</param>
    public McpDatabaseProviderOptions(
        McpDatabaseProviderKind providerKind,
        string providerName,
        string connectionString,
        string migrationsAssembly)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("Provider name is required.", nameof(providerName));
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        if (string.IsNullOrWhiteSpace(migrationsAssembly))
            throw new ArgumentException("Migrations assembly is required.", nameof(migrationsAssembly));

        ProviderKind = providerKind;
        ProviderName = providerName;
        ConnectionString = connectionString;
        MigrationsAssembly = migrationsAssembly;
    }

    /// <summary>Gets the resolved provider kind.</summary>
    public McpDatabaseProviderKind ProviderKind { get; }

    /// <summary>Gets the canonical provider name used for diagnostics and configuration.</summary>
    public string ProviderName { get; }

    /// <summary>Gets the fully resolved provider connection string.</summary>
    public string ConnectionString { get; }

    /// <summary>Gets the assembly name that owns this provider's migration history.</summary>
    public string MigrationsAssembly { get; }
}
