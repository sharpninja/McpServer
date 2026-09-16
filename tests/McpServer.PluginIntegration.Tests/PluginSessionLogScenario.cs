namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// FR-MCP-PLUGININT-001: one Session Log workflow integration catalog row.
/// </summary>
public sealed class PluginSessionLogScenario
{
    /// <summary>Stable catalog name (Codex, Claude Code, Claude Cowork, Copilot, Grok, Cline, Cline v2, OpenCode).</summary>
    public required string Name { get; init; }

    /// <summary>Plugin host kind for this row.</summary>
    public required PluginHostKind HostKind { get; init; }

    /// <summary>Pascal-Case agent source type.</summary>
    public required string AgentSourceType { get; init; }

    /// <summary>Sibling plugin repository folder name.</summary>
    public required string RepositoryName { get; init; }

    /// <summary>Workspace cache folder name under .mcpServer (TR-MCP-PLUGININT-001 AC1).</summary>
    public required string CacheFolder { get; init; }

    /// <summary>Repository-relative typed entrypoint path (TR-MCP-PLUGININT-001 AC1).</summary>
    public required string Entrypoint { get; init; }

    /// <summary>Required environment variable names for this host (TR-MCP-PLUGININT-001 AC1).</summary>
    public required IReadOnlyList<string> RequiredEnvironmentVariables { get; init; }

    /// <summary>Whether this catalog row is enabled.</summary>
    public required bool Enabled { get; init; }
}
