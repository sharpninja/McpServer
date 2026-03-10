using McpServer.Client;
using McpServer.Client.Models;

namespace McpServer.McpAgent.SessionLog;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-007: Parameters for appending processing-dialog items to an active
/// turn within the session-log workflow.
/// </summary>
public sealed class SessionLogDialogAppendRequest
{
    /// <summary>
    /// Gets or sets the identifier of the request entry receiving the dialog items.
    /// </summary>
    public required string RequestId { get; set; }

    /// <summary>
    /// Gets or sets the dialog items to append. The workflow posts these items through
    /// <see cref="SessionLogClient.AppendDialogAsync"/>
    /// and mirrors them into the in-memory turn context.
    /// </summary>
    public IReadOnlyList<ProcessingDialogItemDto> Items { get; set; } = Array.Empty<ProcessingDialogItemDto>();
}
