using McpServer.Support.Mcp.Notifications;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-017 / TR-MCP-MEMORY-API-002: Canonical S5 verb, route, and description catalog.
/// </summary>
public static class MemorySurfaceCatalog
{
    /// <summary>Additive MCP verbs for remember/recall/explore/consolidate/promote/revert.</summary>
    public static readonly IReadOnlyList<string> NewVerbs =
    [
        "memory_remember",
        "memory_recall",
        "memory_explore",
        "memory_consolidate",
        "memory_promote",
        "memory_revert",
    ];

    /// <summary>Compat CRUD verbs retained beside the new set.</summary>
    public static readonly IReadOnlyList<string> CompatVerbs =
    [
        "memory_add",
        "memory_list",
        "memory_update",
        "memory_remove",
    ];

    /// <summary>Agent-oriented description for <c>memory_remember</c>.</summary>
    public const string RememberDescription =
        "Remember a durable multi-layer memory when the operator or agent wants a fact, decision, preference, procedure, or entity to persist across sessions. Use this instead of memory_add when title, type, tags, confidence, or provenance matter.";

    /// <summary>Agent-oriented description for <c>memory_recall</c>.</summary>
    public const string RecallDescription =
        "Recall memories by meaning or keyword when you need ranked guidance for the current question. Use recall instead of list when the query is semantic; do not invent memories that recall does not return.";

    /// <summary>Agent-oriented description for <c>memory_explore</c>.</summary>
    public const string ExploreDescription =
        "Explore related memories from a seed id or query when you need neighborhood context. Use explore after recall when you want linked neighbors, not a fresh ranked search.";

    /// <summary>Agent-oriented description for <c>memory_consolidate</c>.</summary>
    public const string ConsolidateDescription =
        "Consolidate near-duplicate memories when an operator asks for sleep/merge maintenance. Default is dry-run; only write when explicitly requested.";

    /// <summary>Agent-oriented description for <c>memory_promote</c>.</summary>
    public const string PromoteDescription =
        "Promote a session-log or context source into memory when the operator explicitly wants that text remembered. Do not auto-promote completed turns.";

    /// <summary>Agent-oriented description for <c>memory_revert</c>.</summary>
    public const string RevertDescription =
        "Revert a memory to a prior version when the current content is wrong and a snapshot should be restored. History is preserved.";
}

/// <summary>
/// TR-MCP-MEMORY-API-002-06: Documented additionalProperties policy for new memory MCP tools.
/// Unknown required-breaking fields are rejected because additional properties are not allowed.
/// </summary>
public static class MemoryMcpSchemaPolicy
{
    /// <summary>New memory tool schemas set additionalProperties to false.</summary>
    public const bool AdditionalPropertiesAllowed = false;
}

/// <summary>
/// FR-MCP-MEMORY-017-08: Checklist guard used by CI and S5 tests.
/// </summary>
public static class MemoryPluginVerbChecklist
{
    /// <summary>Returns required verbs that are missing from <paramref name="skillText"/>.</summary>
    public static IReadOnlyList<string> Validate(string skillText, IReadOnlyList<string> requiredVerbs)
    {
        ArgumentNullException.ThrowIfNull(skillText);
        ArgumentNullException.ThrowIfNull(requiredVerbs);
        return requiredVerbs
            .Where(verb => skillText.IndexOf(verb, StringComparison.Ordinal) < 0)
            .ToList();
    }
}

/// <summary>
/// TR-MCP-MEMORY-API-002-07: Optional context pack source <c>memories</c>.
/// Off unless the caller requests it; when on, only the provided Effective set is used.
/// </summary>
public static class MemoryContextSource
{
    /// <summary>Opt-in source key.</summary>
    public const string Name = "memories";

    /// <summary>Default is off.</summary>
    public const bool DefaultEnabled = false;

    /// <summary>Returns Effective memories only when <paramref name="sources"/> includes <see cref="Name"/>.</summary>
    public static IReadOnlyList<MemoryItem> FilterEffective(
        IReadOnlyList<MemoryItem> effective,
        IReadOnlyList<string>? sources)
    {
        ArgumentNullException.ThrowIfNull(effective);
        if (!IsRequested(sources))
            return [];

        return effective;
    }

    /// <summary>True when the caller opted into the memories source.</summary>
    public static bool IsRequested(IReadOnlyList<string>? sources)
        => sources is not null
           && sources.Any(source => string.Equals(source, Name, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// TR-MCP-MEMORY-API-002-08: Filters memory SSE events to the caller workspace.
/// </summary>
public static class MemorySseWorkspaceFilter
{
    /// <summary>Allows connection events always; memory events only when workspace matches.</summary>
    public static bool Allow(ChangeEvent changeEvent, string? workspacePath)
    {
        ArgumentNullException.ThrowIfNull(changeEvent);
        if (!string.Equals(changeEvent.Category, "memory", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(changeEvent.Category, ChangeEventCategories.Connection, StringComparison.OrdinalIgnoreCase)
            && changeEvent.Category?.StartsWith("memory.", StringComparison.OrdinalIgnoreCase) != true)
        {
            return true;
        }

        if (string.Equals(changeEvent.Category, ChangeEventCategories.Connection, StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.IsNullOrWhiteSpace(changeEvent.WorkspacePath) || string.IsNullOrWhiteSpace(workspacePath))
            return false;

        return string.Equals(changeEvent.WorkspacePath, workspacePath, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// FR-MCP-MEMORY-014: REST body for <c>memory_revert</c>.
/// </summary>
public sealed record MemoryRevertRequest
{
    /// <summary>Snapshot number to restore.</summary>
    public int VersionNumber { get; init; }
}

/// <summary>
/// TR-MCP-MEMORY-API-002-02: Shared type/title/status/detail envelope for new memory endpoints.
/// </summary>
public static class MemoryErrorEnvelope
{
    /// <summary>Builds the standard classified payload.</summary>
    public static object Create(string title, string detail, int status)
        => new
        {
            type = "https://httpstatuses.io/" + status,
            title,
            status,
            detail,
            code = title,
            message = detail,
            retryable = false,
            error = title,
        };
}
