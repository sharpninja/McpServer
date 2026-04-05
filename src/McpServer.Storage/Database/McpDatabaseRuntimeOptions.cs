namespace McpServer.Support.Mcp.Storage.Database;

/// <summary>
/// Combines resolved provider and encryption settings for the active MCP database runtime.
/// </summary>
public sealed class McpDatabaseRuntimeOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="McpDatabaseRuntimeOptions"/> class.
    /// </summary>
    /// <param name="providerOptions">Resolved provider settings.</param>
    /// <param name="encryptionOptions">Resolved native encryption settings.</param>
    public McpDatabaseRuntimeOptions(
        McpDatabaseProviderOptions providerOptions,
        McpDatabaseEncryptionOptions encryptionOptions)
    {
        ProviderOptions = providerOptions ?? throw new ArgumentNullException(nameof(providerOptions));
        EncryptionOptions = encryptionOptions ?? throw new ArgumentNullException(nameof(encryptionOptions));
    }

    /// <summary>Gets the resolved provider settings.</summary>
    public McpDatabaseProviderOptions ProviderOptions { get; }

    /// <summary>Gets the resolved native encryption settings.</summary>
    public McpDatabaseEncryptionOptions EncryptionOptions { get; }
}
