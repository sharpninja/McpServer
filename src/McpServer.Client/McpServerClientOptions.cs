using System;

namespace McpServer.Client;

/// <summary>
/// Configuration options for the MCP Server client library. Instances of this class are
/// passed to <see cref="McpServerClient"/>, <see cref="McpServerClientFactory"/>, or
/// <see cref="ServiceCollectionExtensions.AddMcpServerClient"/> to configure all sub-clients.
///
/// <para><see cref="BaseUrl"/> determines the scheme, host, and initial port for API calls.
/// <see cref="ApiKey"/> provides an optional seed value — the key can also be set (or rotated)
/// at any time via <see cref="McpClientBase.ApiKey"/> or <see cref="McpServerClient.ApiKey"/>.</para>
/// </summary>
/// <example>
/// <code>
/// var options = new McpServerClientOptions
/// {
///     BaseUrl = new Uri("http://localhost:7147"),
///     ApiKey  = "workspace-token-from-marker-file",
///     Timeout = TimeSpan.FromSeconds(60)
/// };
/// var client = McpServerClientFactory.Create(options);
/// </code>
/// </example>
public sealed class McpServerClientOptions
{
    /// <summary>
    /// Base URL of the MCP Server workspace host. The scheme, host, and port are extracted
    /// to construct per-request URIs. The default value targets <c>http://localhost:7148</c>.
    /// </summary>
    /// <remarks>
    /// The path component is ignored — each sub-client appends its own endpoint paths.
    /// </remarks>
    public Uri BaseUrl { get; set; } = new Uri("http://localhost:7148");

    /// <summary>
    /// Optional seed API key for workspace authentication. When non-null, the value is
    /// copied to <see cref="McpClientBase.ApiKey"/> at construction time. If left null,
    /// callers <strong>must</strong> set <see cref="McpClientBase.ApiKey"/> (or
    /// <see cref="McpServerClient.ApiKey"/>) before making any API call, or an
    /// <see cref="InvalidOperationException"/> will be thrown at call time.
    ///
    /// <para>Obtain the key from the <c>AGENTS-README-FIRST.yaml</c> marker file written
    /// to each workspace root on server startup.</para>
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Optional JWT bearer token for user authentication. When set, sub-clients send an
    /// <c>Authorization: Bearer</c> header on every request. This can be used instead of an
    /// API key when the server accepts authenticated user tokens for the target endpoint.
    /// </summary>
    public string? BearerToken { get; set; }

    /// <summary>
    /// HTTP request timeout applied to the internally-created <see cref="System.Net.Http.HttpClient"/>
    /// when using <see cref="McpServerClientFactory.Create(McpServerClientOptions)"/>.
    /// Defaults to 30 seconds.
    /// </summary>
    /// <remarks>
    /// If you supply your own <see cref="System.Net.Http.HttpClient"/>, this property is
    /// ignored — configure the timeout directly on your <see cref="System.Net.Http.HttpClient"/> instead.
    /// </remarks>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
