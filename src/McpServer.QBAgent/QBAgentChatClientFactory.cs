using McpServer.McpAgent;
using Microsoft.Extensions.AI;

namespace McpServer.QBAgent;

/// <summary>
/// FR-MCP-QBOPENAI-001 / FR-MCP-QBAGENT-001: Builds the OpenAI-compatible <see cref="IChatClient"/> that QBAgent
/// uses as its model. It targets the QuadBrain chat-completions endpoint (<c>{BaseUrl}/v1</c>) with the marker
/// API key as the bearer credential, so QuadBrain is a drop-in OpenAI model behind the Agent Framework loop.
/// </summary>
public static class QBAgentChatClientFactory
{
    /// <summary>The model id advertised to the endpoint (QuadBrain orchestration backs every model id).</summary>
    public const string ModelId = "QuadBrain";

    /// <summary>
    /// Floor for the OpenAI client network timeout. Live CLI QuadBrain turns (Grok + Codex at xhigh)
    /// exceed the SDK default of 100 seconds; integration tests already use 10 minutes.
    /// </summary>
    public static readonly TimeSpan MinimumNetworkTimeout = TimeSpan.FromMinutes(10);

    /// <summary>Creates an <see cref="IChatClient"/> bound to the QuadBrain OpenAI-compatible endpoint.</summary>
    /// <param name="options">Agent options carrying the marker-bound <see cref="McpAgentOptions.BaseUrl"/> and API key.</param>
    /// <returns>An OpenAI-compatible chat client targeting <c>{BaseUrl}/v1</c>.</returns>
    public static IChatClient Create(McpAgentOptions options) => Create(options, httpClient: null, roleProgress: null);

    /// <summary>
    /// Creates an <see cref="IChatClient"/> bound to the QuadBrain endpoint over a caller-supplied
    /// <see cref="HttpClient"/>. The wire format is unchanged; only the underlying transport is replaced, which lets
    /// hosts route through a configured client (proxy, custom certificate handling) or an in-memory test server.
    /// </summary>
    /// <param name="options">Agent options carrying the marker-bound <see cref="McpAgentOptions.BaseUrl"/> and API key.</param>
    /// <param name="httpClient">The HTTP client whose handler pipeline carries the request.</param>
    /// <returns>An OpenAI-compatible chat client targeting <c>{BaseUrl}/v1</c> over <paramref name="httpClient"/>.</returns>
    public static IChatClient Create(McpAgentOptions options, HttpClient httpClient)
        => Create(options, httpClient, roleProgress: null);

    /// <summary>
    /// FR-MCP-QBPROGRESS-001: same as <see cref="Create(McpAgentOptions)"/> plus live brain-role progress.
    /// </summary>
    public static IChatClient Create(McpAgentOptions options, Action<string>? roleProgress)
        => Create(options, httpClient: null, roleProgress);

    /// <summary>
    /// FR-MCP-QBPROGRESS-001: test/host overload with a caller-supplied transport and live brain-role progress.
    /// </summary>
    public static IChatClient Create(
        McpAgentOptions options,
        HttpClient? httpClient,
        Action<string>? roleProgress,
        bool showIntent = false)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.BaseUrl);
        if (httpClient is not null)
            return new QBAgentRoleStreamChatClient(options, httpClient, ownsHttp: false, roleProgress, showIntent);

        var owned = new HttpClient { Timeout = ResolveNetworkTimeout(options) };
        return new QBAgentRoleStreamChatClient(options, owned, ownsHttp: true, roleProgress, showIntent);
    }

    /// <summary>Resolves the OpenAI client timeout, never below <see cref="MinimumNetworkTimeout"/>.</summary>
    public static TimeSpan ResolveNetworkTimeout(McpAgentOptions? options)
    {
        var configured = options?.Timeout ?? TimeSpan.Zero;
        return configured > MinimumNetworkTimeout ? configured : MinimumNetworkTimeout;
    }

    /// <summary>Resolves the OpenAI endpoint base from the workspace base URL, appending <c>/v1</c> once.</summary>
    /// <param name="baseUrl">The workspace base URL from the marker.</param>
    /// <returns>The OpenAI endpoint base (<c>{BaseUrl}/v1</c>).</returns>
    public static Uri BuildEndpoint(Uri baseUrl)
    {
        ArgumentNullException.ThrowIfNull(baseUrl);
        var trimmed = baseUrl.GetLeftPart(UriPartial.Path).TrimEnd('/');
        if (trimmed.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            return new Uri(trimmed);
        return new Uri($"{trimmed}/v1");
    }
}
