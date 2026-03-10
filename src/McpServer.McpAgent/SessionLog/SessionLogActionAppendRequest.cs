using McpServer.Client.Models;

namespace McpServer.McpAgent.SessionLog;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-007: Parameters for appending one or more ordered actions to an active
/// turn within the session-log workflow.
/// </summary>
public sealed class SessionLogActionAppendRequest
{
    /// <summary>
    /// Gets or sets the identifier of the request entry receiving the appended actions.
    /// </summary>
    public required string RequestId { get; set; }

    /// <summary>
    /// Gets or sets the actions to append in the supplied order. The workflow normalizes
    /// <see cref="UnifiedActionDto.Order"/> values so they continue from the current turn state.
    /// </summary>
    public IReadOnlyList<UnifiedActionDto> Actions { get; set; } = Array.Empty<UnifiedActionDto>();
}
