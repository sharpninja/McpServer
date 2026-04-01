using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Storage.Database;

/// <summary>
/// Configures <see cref="McpDbContext"/> for one supported relational provider.
/// </summary>
public interface IMcpDatabaseProviderStrategy
{
    /// <summary>Gets the provider kind handled by this strategy.</summary>
    McpDatabaseProviderKind Kind { get; }

    /// <summary>Gets the canonical provider name used in configuration and diagnostics.</summary>
    string CanonicalName { get; }

    /// <summary>Gets accepted configuration aliases for this provider.</summary>
    IReadOnlyCollection<string> Aliases { get; }

    /// <summary>Gets the default migration assembly name for this provider.</summary>
    string DefaultMigrationsAssembly { get; }

    /// <summary>
    /// Configures the supplied options builder for this provider using the resolved provider options.
    /// </summary>
    /// <param name="optionsBuilder">Options builder to configure.</param>
    /// <param name="providerOptions">Resolved provider settings.</param>
    void Configure(DbContextOptionsBuilder optionsBuilder, McpDatabaseProviderOptions providerOptions);
}
