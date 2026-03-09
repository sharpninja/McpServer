using McpServer.Client.Models;

namespace McpServer.AgentFramework.SessionLog;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-006: Parameters for updating session-level metadata on an active session-log workflow context.
/// Only non-<see langword="null"/> properties are applied; omitted properties leave the context unchanged.
/// </summary>
public sealed class SessionLogSessionUpdateRequest
{
    /// <summary>
    /// Gets or sets the updated session title, or <see langword="null"/> to leave unchanged.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the updated AI model identifier, or <see langword="null"/> to leave unchanged.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Gets or sets the updated session status, or <see langword="null"/> to leave unchanged.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the updated workspace metadata, or <see langword="null"/> to leave unchanged.
    /// </summary>
    public WorkspaceInfoDto? Workspace { get; set; }
}
