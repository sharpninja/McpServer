using System;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Builds configured <see cref="McpAgentEventStreamService"/> instances outside ViewModel composition.
/// </summary>
internal static class AgentEventStreamFactory
{
    public static McpAgentEventStreamService Create(
        string baseUrl,
        string? apiKey,
        string? bearerToken,
        Func<string>? resolveBaseUrl,
        Func<string?>? resolveBearerToken,
        Func<string?>? resolveApiKey,
        Func<string?>? resolveWorkspacePath)
    {
        var service = new McpAgentEventStreamService(baseUrl, apiKey, bearerToken)
        {
            ResolveBaseUrl = resolveBaseUrl,
            ResolveBearerToken = resolveBearerToken,
            ResolveApiKey = resolveApiKey,
            ResolveWorkspacePath = resolveWorkspacePath
        };

        return service;
    }
}

