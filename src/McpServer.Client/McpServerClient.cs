using System;
using System.Net.Http;

namespace McpServer.Client;

/// <summary>
/// Facade client providing access to all MCP Server API sub-clients.
/// Use <see cref="ServiceCollectionExtensions.AddMcpServerClient"/> for DI
/// or <see cref="McpServerClientFactory.Create(McpServerClientOptions)"/> for standalone usage.
/// </summary>
public sealed class McpServerClient
{
    /// <summary>TODO management endpoints.</summary>
    public TodoClient Todo { get; }

    /// <summary>Context search and pack endpoints.</summary>
    public ContextClient Context { get; }

    /// <summary>Session log endpoints.</summary>
    public SessionLogClient SessionLog { get; }

    /// <summary>GitHub integration endpoints.</summary>
    public GitHubClient GitHub { get; }

    /// <summary>Repository file endpoints.</summary>
    public RepoClient Repo { get; }

    /// <summary>Sync endpoints.</summary>
    public SyncClient Sync { get; }

    /// <summary>Workspace management endpoints.</summary>
    public WorkspaceClient Workspace { get; }

    /// <summary>Tool registry endpoints.</summary>
    public ToolRegistryClient Tools { get; }

    /// <summary>Initializes a new instance of <see cref="McpServerClient"/>.</summary>
    public McpServerClient(HttpClient http, McpServerClientOptions options)
    {
        if (http is null) throw new ArgumentNullException(nameof(http));
        if (options is null) throw new ArgumentNullException(nameof(options));

        Todo = new TodoClient(http, options);
        Context = new ContextClient(http, options);
        SessionLog = new SessionLogClient(http, options);
        GitHub = new GitHubClient(http, options);
        Repo = new RepoClient(http, options);
        Sync = new SyncClient(http, options);
        Workspace = new WorkspaceClient(http, options);
        Tools = new ToolRegistryClient(http, options);
    }
}
