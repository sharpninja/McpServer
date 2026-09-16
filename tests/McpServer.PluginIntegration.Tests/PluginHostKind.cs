namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// FR-MCP-PLUGININT-001: host kind for each catalog row.
/// </summary>
public enum PluginHostKind
{
    /// <summary>Codex CLI plugin host.</summary>
    Codex = 0,

    /// <summary>Claude Code plugin host.</summary>
    ClaudeCode = 1,

    /// <summary>Claude Cowork plugin host.</summary>
    ClaudeCowork = 2,

    /// <summary>Copilot plugin host.</summary>
    Copilot = 3,

    /// <summary>Grok plugin host.</summary>
    Grok = 4,

    /// <summary>Cline v1 MCP-stdio host.</summary>
    Cline = 5,

    /// <summary>Cline v2 SDK host.</summary>
    ClineV2 = 6,

    /// <summary>OpenCode SDK host.</summary>
    OpenCode = 7,
}
