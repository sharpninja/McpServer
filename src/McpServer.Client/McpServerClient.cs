using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace McpServer.Client;

/// <summary>
/// Facade client providing unified access to all MCP Server API sub-clients.
///
/// <para><strong>Authentication:</strong> Set <see cref="ApiKey"/> before making any API
/// call, or call <see cref="InitializeAsync"/> to automatically fetch the default
/// (anonymous) API key from the server. The value is propagated to every sub-client and
/// read at call time so it can be rotated without creating a new instance. An
/// <see cref="InvalidOperationException"/> is thrown at request time if the key is empty.</para>
///
/// <para><strong>Default key:</strong> The default key returned by <see cref="InitializeAsync"/>
/// grants <em>read-only</em> access only. Consumers with access to the
/// <c>AGENTS-README-FIRST.yaml</c> marker file should use the full-access token from that file
/// for write operations or other privileged flows instead.</para>
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
/// // Option A: Auto-initialize with default key (no marker file needed)
/// var client = McpServerClientFactory.Create(new McpServerClientOptions
/// {
///     BaseUrl = new Uri("http://localhost:7147")
/// });
/// await client.InitializeAsync();
/// var items = await client.Todo.QueryAsync();
///
/// // Option B: Use full-access key from marker file
/// var client = McpServerClientFactory.Create(new McpServerClientOptions
/// {
///     BaseUrl = new Uri("http://localhost:7147"),
///     ApiKey = "full-access-token-from-marker"
/// });
/// </code>
/// </example>
public sealed class McpServerClient
{
    private readonly McpClientBase[] _allClients;
    private readonly HttpClient _http;
    private readonly McpServerClientOptions _options;
    private string _apiKey;
    private string _bearerToken;
    private string _workspacePath;
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

        _http = http;
        _options = options;

        // Single shared holder — all sub-clients read/write the same workspace path.
        var holder = new WorkspacePathHolder { Path = options.WorkspacePath ?? string.Empty };

        Todo = new TodoClient(http, options, holder);
        Context = new ContextClient(http, options, holder);
        GraphRag = new GraphRagClient(http, options, holder);
        SessionLog = new SessionLogClient(http, options, holder);
        Memory = new MemoryClient(http, options, holder);
        GitHub = new GitHubClient(http, options, holder);
        Requirements = new RequirementsClient(http, options, holder);
        Voice = new VoiceClient(http, options, holder);
        Events = new EventStreamClient(http, options, holder);
        Repo = new RepoClient(http, options, holder);
        Desktop = new DesktopClient(http, options, holder);
        Tunnel = new TunnelClient(http, options, holder);
        Workspace = new WorkspaceClient(http, options, holder);
        Configuration = new ConfigurationClient(http, options, holder);
        Tools = new ToolRegistryClient(http, options, holder);
        AuthConfig = new AuthConfigClient(http, options, holder);
        Diagnostic = new DiagnosticClient(http, options, holder);
        Template = new TemplateClient(http, options, holder);
        AgentPool = new AgentPoolClient(http, options, holder);
        Agent = new AgentClient(http, options, holder);
        Health = new HealthClient(http, options, holder);
        Federation = new FederationClient(http, options, holder);
        KeyServer = new KeyServerClient(http, options, holder);
        Subscriber = new SubscriberClient(http, options, holder);
        TurnTransactions = new TurnTransactionsClient(http, options, holder);
        BrainSlots = new BrainSlotClient(http, options, holder);
        Triage = new TriageClient(http, options, holder);
        AgentHelp = new AgentHelpClient(http, options, holder);

        _allClients = new McpClientBase[]
        {
            Todo, Context, GraphRag, SessionLog, Memory, GitHub, Requirements, Voice, Events,
            Repo, Desktop, Tunnel, Workspace, Configuration, Tools, AuthConfig, Diagnostic, Template, AgentPool, Agent, Health,
            Federation, KeyServer, Subscriber, TurnTransactions, BrainSlots, Triage, AgentHelp
        };
        _apiKey = options.ApiKey ?? string.Empty;
        _bearerToken = options.BearerToken ?? string.Empty;
        _workspacePath = holder.Path;
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
            _apiKey = value ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                _bearerToken = string.Empty;
            }
            foreach (var c in _allClients) c.ApiKey = _apiKey;
        }
    }

    /// <summary>
    /// JWT bearer token for user authentication, propagated to every sub-client.
    /// Setting this property immediately updates <see cref="McpClientBase.BearerToken"/> on all
    /// sub-clients so the next call from <em>any</em> client uses the new token.
    /// </summary>
    public string BearerToken
    {
        get => _bearerToken;
        set
        {
            _bearerToken = value ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(_bearerToken))
            {
                _apiKey = string.Empty;
            }
            foreach (var c in _allClients) c.BearerToken = _bearerToken;
        }
    }

    /// <summary>
    /// Workspace path for multi-tenant routing. All sub-clients share a single
    /// <see cref="WorkspacePathHolder"/>, so setting this once is instantly visible
    /// to every sub-client at the next request — no propagation loop needed.
    /// </summary>
    public string WorkspacePath
    {
        get => _workspacePath;
        set
        {
            _workspacePath = value;
            // All sub-clients share the same holder — one write, all clients see it.
            _allClients[0].WorkspacePath = value;
        }
    }

    /// <summary>
    /// Clears both API key and bearer token on all sub-clients, resetting the client
    /// to an unauthenticated state. After calling this method, a new API key or bearer
    /// token can be set.
    /// </summary>
    public void Logout()
    {
        _apiKey = string.Empty;
        _bearerToken = string.Empty;
        foreach (var c in _allClients) c.Logout();
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
    /// Fetches the default (anonymous) API key from the server's unprotected
    /// <c>GET /api-key</c> endpoint and sets it on all sub-clients. This is the
    /// recommended startup call for consumers that do <strong>not</strong> have access
    /// to the <c>AGENTS-README-FIRST.yaml</c> marker file.
    ///
    /// <para>The default key grants <em>read-only</em> access only. For full unrestricted
    /// access, use the workspace token from the marker file or JWT Bearer authentication.</para>
    ///
    /// <para>This method is a no-op if <see cref="ApiKey"/> is already non-empty
    /// (i.e. it was seeded via <see cref="McpServerClientOptions.ApiKey"/> or set
    /// manually before calling this method).</para>
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The default API key that was fetched and applied.</returns>
    /// <exception cref="McpServerException">
    /// Thrown when the server returns a non-success status code (e.g. 503 if the
    /// workspace is not yet ready).
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the server response does not contain an <c>apiKey</c> property.
    /// </exception>
    /// <example>
    /// <code>
    /// var client = McpServerClientFactory.Create(new McpServerClientOptions
    /// {
    ///     BaseUrl = new Uri("http://localhost:7147")
    /// });
    /// await client.InitializeAsync();
    /// // client.ApiKey is now set; all sub-clients are ready.
    /// var items = await client.Todo.QueryAsync();
    /// </code>
    /// </example>
    public async Task<string> InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Skip if credentials were already provided (e.g. marker API key or OIDC bearer token).
        if (!string.IsNullOrWhiteSpace(_apiKey))
            return _apiKey;
        if (!string.IsNullOrWhiteSpace(_bearerToken))
            return string.Empty;

        var uri = new Uri($"{_options.BaseUrl.Scheme}://{_options.BaseUrl.Host}:{Port}/api-key");
        using var response = await _http.GetAsync(uri, cancellationToken);

        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(true);

        if (!response.IsSuccessStatusCode)
            throw new McpServerException(
                $"Failed to fetch default API key: HTTP {(int)response.StatusCode} — {content}",
                (int)response.StatusCode);

        using var doc = JsonDocument.Parse(content);
        if (!doc.RootElement.TryGetProperty("apiKey", out var keyElement) || keyElement.GetString() is not { } key)
            throw new InvalidOperationException("Server response did not contain an 'apiKey' property.");

        ApiKey = key;
        return key;
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
    /// GraphRAG endpoints — lifecycle, query, ingest, document, entity, and relationship operations.
    /// <para>See <see cref="GraphRagClient"/> for the full method list.</para>
    /// </summary>
    public GraphRagClient GraphRag { get; }

    /// <summary>
    /// Session log endpoints — submit, query, and append dialog items to session logs.
    /// <para>See <see cref="SessionLogClient"/> for the full method list.</para>
    /// </summary>
    public SessionLogClient SessionLog { get; }

    /// <summary>
    /// Memory endpoints - add, list, update, and remove agent memories.
    /// <para>See <see cref="MemoryClient"/> for the full method list.</para>
    /// </summary>
    public MemoryClient Memory { get; }

    /// <summary>
    /// GitHub integration endpoints — issues, pull requests, comments, and sync.
    /// <para>See <see cref="GitHubClient"/> for the full method list.</para>
    /// </summary>
    public GitHubClient GitHub { get; }

    /// <summary>
    /// Requirements endpoints — FR/TR/TEST CRUD, mapping, and document generation.
    /// <para>See <see cref="RequirementsClient"/> for the full method list.</para>
    /// </summary>
    public RequirementsClient Requirements { get; }

    /// <summary>
    /// Voice conversation endpoints — sessions, turns, transcripts, and interrupts.
    /// <para>See <see cref="VoiceClient"/> for the full method list.</para>
    /// </summary>
    public VoiceClient Voice { get; }

    /// <summary>
    /// Change-event SSE endpoints.
    /// <para>See <see cref="EventStreamClient"/> for the full method list.</para>
    /// </summary>
    public EventStreamClient Events { get; }

    /// <summary>
    /// Repository file endpoints — read, write, and list files in the workspace repository.
    /// <para>See <see cref="RepoClient"/> for the full method list.</para>
    /// </summary>
    public RepoClient Repo { get; }

    /// <summary>
    /// Desktop process launch endpoint — launch local programs on the interactive desktop through
    /// the authenticated workspace context.
    /// <para>See <see cref="DesktopClient"/> for the full method list.</para>
    /// </summary>
    public DesktopClient Desktop { get; }

    /// <summary>
    /// Tunnel management endpoints — list strategies, enable/disable, start, stop, restart.
    /// <para>See <see cref="TunnelClient"/> for the full method list.</para>
    /// </summary>
    public TunnelClient Tunnel { get; }

    /// <summary>
    /// Workspace management endpoints — list, create, update, delete, start, and stop workspaces.
    /// <para>See <see cref="WorkspaceClient"/> for the full method list.</para>
    /// </summary>
    public WorkspaceClient Workspace { get; }

    /// <summary>
    /// Admin configuration endpoints — read the effective flattened configuration and patch
    /// <c>appsettings.yaml</c>.
    /// <para>See <see cref="ConfigurationClient"/> for the full method list.</para>
    /// </summary>
    public ConfigurationClient Configuration { get; }

    /// <summary>
    /// Tool registry endpoints — CRUD, search, bucket management, and tool installation.
    /// <para>See <see cref="ToolRegistryClient"/> for the full method list.</para>
    /// </summary>
    public ToolRegistryClient Tools { get; }

    /// <summary>
    /// Public auth configuration endpoint for Director OIDC auto-discovery.
    /// <para>See <see cref="AuthConfigClient"/> for the full method list.</para>
    /// </summary>
    public AuthConfigClient AuthConfig { get; }

    /// <summary>
    /// Diagnostic endpoints for execution/appsettings path inspection.
    /// <para>See <see cref="DiagnosticClient"/> for the full method list.</para>
    /// </summary>
    public DiagnosticClient Diagnostic { get; }

    /// <summary>
    /// Prompt template management endpoints.
    /// <para>See <see cref="TemplateClient"/> for the full method list.</para>
    /// </summary>
    public TemplateClient Template { get; }

    /// <summary>
    /// Agent-pool runtime endpoints — lifecycle, queue operations, prompt resolution, and streams.
    /// <para>See <see cref="AgentPoolClient"/> for the full method list.</para>
    /// </summary>
    public AgentPoolClient AgentPool { get; }

    /// <summary>
    /// Agent-management endpoints.
    /// <para>See <see cref="AgentClient"/> for the full method list.</para>
    /// </summary>
    public AgentClient Agent { get; }

    /// <summary>
    /// Server health endpoint.
    /// <para>See <see cref="HealthClient"/> for the full method list.</para>
    /// </summary>
    public HealthClient Health { get; }

    /// <summary>
    /// Federation management endpoints — status, targets, routes, discovery, connection, and push.
    /// <para>See <see cref="FederationClient"/> for the full method list.</para>
    /// </summary>
    public FederationClient Federation { get; }

    /// <summary>
    /// Transaction keyserver endpoints — party registration, key lookup, manifest signing, and verification.
    /// <para>See <see cref="KeyServerClient"/> for the full method list.</para>
    /// </summary>
    public KeyServerClient KeyServer { get; }

    /// <summary>
    /// Transaction subscriber endpoints — encrypted diffgram commit, status, and abort operations.
    /// <para>See <see cref="SubscriberClient"/> for the full method list.</para>
    /// </summary>
    public SubscriberClient Subscriber { get; }

    /// <summary>
    /// Turn-transaction diagnostic endpoints — gate status, persisted pub/sub status, replay, and retention purge.
    /// <para>See <see cref="TurnTransactionsClient"/> for the full method list.</para>
    /// </summary>
    public TurnTransactionsClient TurnTransactions { get; }

    /// <summary>
    /// External brain-slot registry and invocation endpoints.
    /// <para>See <see cref="BrainSlotClient"/> for the full method list.</para>
    /// </summary>
    public BrainSlotClient BrainSlots { get; }

    /// <summary>
    /// FR-MCP-TRIAGE-001: Incidental bug triage endpoints for report intake, group status, flush, and retry.
    /// <para>See <see cref="TriageClient"/> for the full method list.</para>
    /// </summary>
    public TriageClient Triage { get; }

    /// <summary>
    /// FR-MCP-HELP-007: Agent Help conversation endpoints for MCP Server issue diagnosis.
    /// <para>See <see cref="AgentHelpClient"/> for the full method list.</para>
    /// </summary>
    public AgentHelpClient AgentHelp { get; }
}
