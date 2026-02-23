using System;
using System.Net.Http;

namespace McpServer.Client;

/// <summary>
/// Facade client providing unified access to all MCP Server API sub-clients.
///
/// <para><strong>Authentication:</strong> Set <see cref="ApiKey"/> before making any API
/// call. The value is propagated to every sub-client and read at call time so it can be
/// rotated without creating a new instance. An <see cref="InvalidOperationException"/> is
/// thrown at request time if the key is empty.</para>
///
/// <para><strong>Port targeting:</strong> Set <see cref="Port"/> to retarget all sub-clients
/// to a different workspace host at runtime (e.g. after calling the workspace Start
/// endpoint).</para>
///
/// <para><strong>Usage:</strong> Create via
/// <see cref="McpServerClientFactory.Create(McpServerClientOptions)"/> for standalone usage,
/// or register via <see cref="ServiceCollectionExtensions.AddMcpServerClient"/> for DI.</para>
/// </summary>
/// <example>
/// <code>
/// var client = McpServerClientFactory.Create(new McpServerClientOptions
/// {
///     BaseUrl = new Uri("http://localhost:7147"),
///     ApiKey = "my-api-key"
/// });
/// var items = await client.Todo.QueryAsync();
/// </code>
/// </example>
public sealed class McpServerClient
{
    private readonly McpClientBase[] _allClients;
    private string _apiKey;
    private int _port;

    /// <summary>
    /// Initializes a new <see cref="McpServerClient"/> and all sub-clients from the
    /// supplied <paramref name="options"/>. The <see cref="ApiKey"/> and <see cref="Port"/>
    /// properties are seeded from the options and can be changed at any time.
    /// </summary>
    /// <param name="http">
    /// Shared <see cref="HttpClient"/> used by every sub-client for outbound HTTP calls.
    /// </param>
    /// <param name="options">
    /// Configuration snapshot supplying <see cref="McpServerClientOptions.BaseUrl"/>
    /// (scheme/host/port) and an optional seed <see cref="McpServerClientOptions.ApiKey"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="http"/> or <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
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

        _allClients = new McpClientBase[] { Todo, Context, SessionLog, GitHub, Repo, Sync, Workspace, Tools };
        _apiKey = options.ApiKey ?? string.Empty;
        _port = options.BaseUrl.Port;
    }

    /// <summary>
    /// API key for workspace authentication, propagated to every sub-client.
    /// Setting this property immediately updates <see cref="McpClientBase.ApiKey"/> on all
    /// sub-clients so the next call from <em>any</em> client uses the new key.
    /// </summary>
    /// <example>
    /// <code>
    /// client.ApiKey = "rotated-token-from-marker";
    /// // All sub-clients now use the new key:
    /// await client.Todo.QueryAsync();
    /// </code>
    /// </example>
    public string ApiKey
    {
        get => _apiKey;
        set
        {
            _apiKey = value;
            foreach (var c in _allClients) c.ApiKey = value;
        }
    }

    /// <summary>
    /// TCP port propagated to every sub-client. Changing this immediately retargets all
    /// API calls to the new port (e.g. switching between workspace hosts).
    /// </summary>
    /// <example>
    /// <code>
    /// client.Port = 7149; // retarget to another workspace
    /// await client.Workspace.ListAsync();
    /// </code>
    /// </example>
    public int Port
    {
        get => _port;
        set
        {
            _port = value;
            foreach (var c in _allClients) c.Port = value;
        }
    }

    /// <summary>
    /// TODO management endpoints — create, query, update, and delete TODO items.
    /// <para>See <see cref="TodoClient"/> for the full method list.</para>
    /// </summary>
    public TodoClient Todo { get; }

    /// <summary>
    /// Context search and pack endpoints — semantic search over indexed workspace content.
    /// <para>See <see cref="ContextClient"/> for the full method list.</para>
    /// </summary>
    public ContextClient Context { get; }

    /// <summary>
    /// Session log endpoints — submit, query, and append dialog items to session logs.
    /// <para>See <see cref="SessionLogClient"/> for the full method list.</para>
    /// </summary>
    public SessionLogClient SessionLog { get; }

    /// <summary>
    /// GitHub integration endpoints — issues, pull requests, comments, and sync.
    /// <para>See <see cref="GitHubClient"/> for the full method list.</para>
    /// </summary>
    public GitHubClient GitHub { get; }

    /// <summary>
    /// Repository file endpoints — read, write, and list files in the workspace repository.
    /// <para>See <see cref="RepoClient"/> for the full method list.</para>
    /// </summary>
    public RepoClient Repo { get; }

    /// <summary>
    /// Sync endpoints — trigger ingestion runs and check sync status.
    /// <para>See <see cref="SyncClient"/> for the full method list.</para>
    /// </summary>
    public SyncClient Sync { get; }

    /// <summary>
    /// Workspace management endpoints — list, create, update, delete, start, and stop workspaces.
    /// <para>See <see cref="WorkspaceClient"/> for the full method list.</para>
    /// </summary>
    public WorkspaceClient Workspace { get; }

    /// <summary>
    /// Tool registry endpoints — CRUD, search, bucket management, and tool installation.
    /// <para>See <see cref="ToolRegistryClient"/> for the full method list.</para>
    /// </summary>
    public ToolRegistryClient Tools { get; }
}
