using McpServer.Client.Models;

namespace McpServer.McpAgent.SessionLog;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-006: Parameters for bootstrapping a new session-log workflow context.
/// </summary>
public sealed class SessionLogBootstrapRequest
{
    /// <summary>
    /// Gets or sets an optional caller-supplied session identifier. When <see langword="null"/>,
    /// a canonical identifier is generated via <see cref="IMcpSessionIdentifierFactory.CreateSessionId"/>.
    /// When supplied, the value must pass <see cref="IMcpSessionIdentifierFactory.TryValidateSessionId"/>.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets or sets an optional suffix seed used when generating a canonical session identifier.
    /// When <see langword="null"/>, the workflow falls back to <see cref="Model"/>,
    /// then <see cref="Title"/>, then <c>session</c>.
    /// </summary>
    public string? SessionIdSuffix { get; set; }

    /// <summary>
    /// Gets or sets a human-readable title for the session.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the AI model identifier used for this session.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Gets or sets the initial session status. Defaults to <c>in_progress</c>.
    /// </summary>
    public string Status { get; set; } = "in_progress";

    /// <summary>
    /// Gets or sets optional workspace metadata to include in the session log.
    /// </summary>
    public WorkspaceInfoDto? Workspace { get; set; }

    /// <summary>
    /// Gets or sets an explicit session start time as an ISO 8601 string.
    /// When <see langword="null"/>, the current UTC time is used.
    /// </summary>
    public string? Started { get; set; }
}
