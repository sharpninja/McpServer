using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace McpServer.Client;

/// <summary>
/// Client for admin configuration endpoints (<c>/mcpserver/configuration</c>).
/// These endpoints require a JWT bearer token for a user in the <c>admin</c> role.
/// </summary>
/// <seealso cref="McpServerClient.Configuration"/>
public sealed class ConfigurationClient : McpClientBase
{
    /// <inheritdoc />
    public ConfigurationClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal ConfigurationClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>
    /// Gets the current effective configuration as flattened <c>section:key</c> pairs.
    /// </summary>
    public async Task<Dictionary<string, string>> GetValuesAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<Dictionary<string, string>>("mcpserver/configuration", cancellationToken);
    }

    /// <summary>
    /// Applies flattened configuration updates to <c>appsettings.yaml</c> and returns the updated effective
    /// configuration view. Explicit <see langword="null"/> values are preserved so callers can remove keys.
    /// </summary>
    /// <param name="values">Flattened configuration keys to set or remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task<Dictionary<string, string>> PatchValuesAsync(
        IReadOnlyDictionary<string, string?> values,
        CancellationToken cancellationToken = default)
    {
        return await PatchIncludingNullsAsync<Dictionary<string, string>>(
            "mcpserver/configuration",
            values,
            cancellationToken);
    }
}
