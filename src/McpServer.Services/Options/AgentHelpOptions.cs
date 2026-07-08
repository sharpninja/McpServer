using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Options;

/// <summary>
/// FR-MCP-HELP-001: Configuration for Agent Help conversation orchestration.
/// TR-MCP-HELP-001: Options bound from the <see cref="SectionName"/> configuration section.
/// </summary>
public sealed class AgentHelpOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "AgentHelp";

    /// <summary>
    /// Enables Agent Help session endpoints and services.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default model identifier passed to compatible CLI agents.
    /// </summary>
    public string HelperModel { get; set; } = "gpt-5.3-codex";

    /// <summary>
    /// Default execution strategy used when callers do not explicitly choose one.
    /// Supported values are <c>one-shot-cli</c>, <c>copilot-cli</c>, <c>codex-cli</c>, and <c>hosted-mcp-agent</c>.
    /// </summary>
    public string DefaultExecutionStrategy { get; set; } = AgentExecutionStrategyNames.OneShotCli;

    /// <summary>
    /// Optional API key injected into the underlying agent/model process.
    /// When set, the key is exposed through <see cref="ModelApiKeyEnvironmentVariableName"/>.
    /// </summary>
    public string? ModelApiKey { get; set; }

    /// <summary>
    /// Environment variable name used to pass <see cref="ModelApiKey"/> to the underlying agent/model process.
    /// </summary>
    public string ModelApiKeyEnvironmentVariableName { get; set; } = "OPENAI_API_KEY";

    /// <summary>
    /// Working directory for helper agents. Empty means use the host content root or session workspace path.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Relative directory under the workspace data root where append-only help transcripts are stored.
    /// </summary>
    public string TranscriptDirectory { get; set; } = "agent-help/transcripts";

    /// <summary>
    /// Relative directory under the workspace data root where guard incidents are stored as JSON files.
    /// </summary>
    public string IncidentDirectory { get; set; } = "agent-help/incidents";

    /// <summary>
    /// When <see langword="true"/>, inbound prompts are evaluated by <see cref="Services.AgentHelp.AgentHelpInboundGuard"/>.
    /// </summary>
    public bool GuardEnabled { get; set; } = true;

    /// <summary>
    /// When <see langword="true"/>, corpus bootstrap is attempted when a help session is created.
    /// </summary>
    public bool CorpusBootstrapEnabled { get; set; } = true;

    /// <summary>
    /// Maximum characters of seeded context injected into helper prompts.
    /// </summary>
    public int MaxContextCharacters { get; set; } = 12_000;

    /// <summary>
    /// Maximum indexed search chunks to merge into the seeded context pack.
    /// </summary>
    public int ContextSearchChunkLimit { get; set; } = 8;

    /// <summary>
    /// TR-MCP-GRAPHRAG-GLOBAL-001: When true, Agent Help queries the host-global GraphRAG corpus before pinned filesystem paths.
    /// </summary>
    public bool PreferGlobalGraphRag { get; set; } = true;

    /// <summary>
    /// Pinned document path tokens seeded into every Agent Help context pack.
    /// Tokens may be absolute paths or scoped relative paths:
    /// <c>workspace:</c> (workspace root), <c>data:</c> (effective MCP data folder), <c>install:</c> (server content root).
    /// Unscoped tokens default to <c>workspace:</c>.
    /// </summary>
    public List<string> PinnedPaths { get; set; } = [];

    /// <summary>
    /// Maximum number of turns retained in the in-memory session registry per session.
    /// </summary>
    public int MaxTurnsPerSession { get; set; } = 50;

    /// <summary>
    /// Minutes of inactivity before a help session is eligible for cleanup.
    /// </summary>
    public TimeSpan SessionIdleTimeoutMinutes { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// When <see langword="true"/> and no execution strategy is available, the service echoes a deterministic helper response for tests.
    /// </summary>
    public bool UseEchoHelperFallback { get; set; } = true;
}