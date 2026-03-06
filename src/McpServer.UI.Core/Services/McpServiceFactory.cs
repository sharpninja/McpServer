using System;
using McpServer.Client;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Factory for creating MCP service instances. Extracts service construction
/// from MainWindowViewModel to the composition root.
/// </summary>
public sealed class McpServiceFactory
{
    /// <summary>
    /// Creates a <see cref="McpSessionLogService"/> backed by the given client.
    /// </summary>
    public McpSessionLogService CreateSessionLogService(McpServerClient client)
        => new(client);

    /// <summary>
    /// Creates a <see cref="McpTodoService"/> backed by the given clients.
    /// </summary>
    /// <param name="client">Client for standard TODO operations.</param>
    /// <param name="promptClient">Client with extended timeout for prompt generation.</param>
    public McpTodoService CreateTodoService(McpServerClient client, McpServerClient promptClient)
        => new(client, promptClient);

    /// <summary>
    /// Creates a <see cref="McpWorkspaceService"/> backed by the given client.
    /// </summary>
    public McpWorkspaceService CreateWorkspaceService(McpServerClient client, Uri baseUri)
        => new(client, baseUri);

    /// <summary>
    /// Creates a <see cref="McpVoiceConversationService"/> with resolver functions
    /// for dynamic connection state.
    /// </summary>
    public McpVoiceConversationService CreateVoiceService(
        string baseUrl,
        string? apiKey,
        string? bearerToken,
        Func<string> resolveBaseUrl,
        Func<string?> resolveBearerToken,
        Func<string?> resolveApiKey,
        Func<string?> resolveWorkspacePath)
    {
        return new McpVoiceConversationService(baseUrl, apiKey: apiKey, bearerToken: bearerToken)
        {
            ResolveBaseUrl = resolveBaseUrl,
            ResolveBearerToken = resolveBearerToken,
            ResolveApiKey = resolveApiKey,
            ResolveWorkspacePath = resolveWorkspacePath
        };
    }

    /// <summary>
    /// Creates a <see cref="McpAgentEventStreamService"/> via the existing factory.
    /// </summary>
    public McpAgentEventStreamService CreateEventStreamService(
        string baseUrl,
        string? apiKey,
        string? bearerToken,
        Func<string> resolveBaseUrl,
        Func<string?> resolveBearerToken,
        Func<string?> resolveApiKey,
        Func<string?> resolveWorkspacePath)
    {
        return AgentEventStreamFactory.Create(
            baseUrl,
            apiKey: apiKey,
            bearerToken: bearerToken,
            resolveBaseUrl: resolveBaseUrl,
            resolveBearerToken: resolveBearerToken,
            resolveApiKey: resolveApiKey,
            resolveWorkspacePath: resolveWorkspacePath);
    }

    /// <summary>
    /// Watches a directory for file changes matching the given filter.
    /// </summary>
    public IWatcherHandle WatchFileSystem(string directory, string filter, Action<string> onChanged)
        => new Infrastructure.FileSystemWatcherService().Watch(directory, filter, onChanged);
}

