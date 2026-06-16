namespace McpServer.McpAgent;

/// <summary>
/// FR-MCP-136/TR-MCP-AGENT-015: Stable public definition for the ACID tightly coupled
/// Microsoft Agent Framework hosted agent.
/// </summary>
public sealed class McpAcidAgentDefinition
{
    private static readonly string[] AllowedTools =
    [
        "mcp_session_bootstrap",
        "mcp_session_update",
        "mcp_session_turn_begin",
        "mcp_session_turn_update",
        "mcp_session_turn_complete",
        "mcp_session_query_history",
        "mcp_todo_query",
        "mcp_todo_get",
        "mcp_repo_read",
        "mcp_repo_list",
        "mcp_requirements_list_fr",
        "mcp_requirements_list_tr",
        "mcp_requirements_list_test",
        "mcp_requirements_get_fr",
        "mcp_requirements_get_tr",
        "mcp_requirements_get_test",
        "mcp_graphrag_list_documents",
        "mcp_graphrag_get_document_chunks",
        "mcp_graphrag_list_entities",
        "mcp_graphrag_get_entity",
        "mcp_graphrag_list_relationships",
        "mcp_graphrag_get_relationship",
    ];

    private static readonly string[] BlockedTools =
    [
        "mcp_todo_update",
        "mcp_todo_create",
        "mcp_todo_delete",
        "mcp_todo_plan",
        "mcp_todo_status",
        "mcp_todo_implementation",
        "mcp_repo_write",
        "mcp_desktop_launch",
        "mcp_powershell_session_create",
        "mcp_powershell_session_command",
        "mcp_powershell_session_close",
        "mcp_client_invoke",
        "mcp_graphrag_ingest_text",
        "mcp_graphrag_delete_document",
        "mcp_graphrag_create_entity",
        "mcp_graphrag_update_entity",
        "mcp_graphrag_delete_entity",
        "mcp_graphrag_create_relationship",
        "mcp_graphrag_update_relationship",
        "mcp_graphrag_delete_relationship",
    ];

    private static readonly string[] RequiredInvariants =
    [
        "RequireAuthentication",
        "RequireWorkspacePath",
        "RequireSessionTurnBoundary",
        "RequireDurableAudit",
        "RequireTransactionalMutations",
        "RequireSerializedToolInvocation",
        "AllowMultipleToolCalls=false",
        "FunctionInvokingChatClient.AllowConcurrentInvocation=false",
        "FailClosedUnsafeTools",
    ];

    private readonly HashSet<string> _allowedToolLookup;

    private McpAcidAgentDefinition()
    {
        _allowedToolLookup = new HashSet<string>(AllowedTools, StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the singleton ACID hosted-agent definition.
    /// </summary>
    public static McpAcidAgentDefinition Instance { get; } = new();

    /// <summary>
    /// Gets the stable Agent Framework metadata identifier.
    /// </summary>
    public string AgentId => McpHostedAgentDefaults.AcidAgentId;

    /// <summary>
    /// Gets the stable host-facing agent name.
    /// </summary>
    public string AgentName => McpHostedAgentDefaults.AcidAgentName;

    /// <summary>
    /// Gets the stable source type used for session-log and audit records.
    /// </summary>
    public string SourceType => McpHostedAgentDefaults.AcidSourceType;

    /// <summary>
    /// Gets the public description projected into Microsoft Agent Framework metadata.
    /// </summary>
    public string Description => McpHostedAgentDefaults.AcidAgentDescription;

    /// <summary>
    /// Gets the declared consistency model for this hosted-agent profile.
    /// </summary>
    public string ConsistencyModel => "ACID";

    /// <summary>
    /// Gets the declared coupling model for this hosted-agent profile.
    /// </summary>
    public string CouplingMode => "TightlyCoupled";

    /// <summary>
    /// Gets the Microsoft Agent Framework agent type extended by this profile.
    /// </summary>
    public string FrameworkAgentType => "Microsoft.Agents.AI.ChatClientAgent";

    /// <summary>
    /// Gets the built-in MCP tool names exposed to the model by default in ACID mode.
    /// </summary>
    public IReadOnlyList<string> AllowedToolNames => AllowedTools;

    /// <summary>
    /// Gets the built-in MCP tool names intentionally hidden by default in ACID mode.
    /// </summary>
    public IReadOnlyList<string> BlockedToolNames => BlockedTools;

    /// <summary>
    /// Gets the invariants required before a host can claim the ACID profile.
    /// </summary>
    public IReadOnlyList<string> Invariants => RequiredInvariants;

    /// <summary>
    /// Determines whether a built-in MCP tool is allowed in the ACID model-visible tool surface.
    /// </summary>
    /// <param name="toolName">The MCP tool name to evaluate.</param>
    /// <returns><see langword="true"/> when the tool is allowed by the ACID profile.</returns>
    public bool IsToolAllowed(string? toolName) =>
        !string.IsNullOrWhiteSpace(toolName) && _allowedToolLookup.Contains(toolName);
}
