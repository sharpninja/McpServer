namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// TEST-MCP-PLUGININT-001 AC2: captured identifiers and receipts from a canonical plugin Session Log turn.
/// </summary>
public sealed class PluginSessionLogWorkflowResult
{
    /// <summary>True when the host adapter completed bootstrap/begin/append/complete.</summary>
    public required bool AdapterOperational { get; init; }

    /// <summary>Session id returned by the plugin workflow.</summary>
    public required string SessionId { get; init; }

    /// <summary>Turn request id returned by the plugin workflow.</summary>
    public required string RequestId { get; init; }

    /// <summary>Turn status after complete (expected completed).</summary>
    public required string Status { get; init; }

    /// <summary>Workspace cache path under .mcpServer/{agent}.</summary>
    public required string CachePath { get; init; }

    /// <summary>Plugin source SHA or version stamp captured for the row.</summary>
    public required string SourceSha { get; init; }

    /// <summary>
    /// True when PLUGIN_ROOT_OVERRIDE was rejected and cache stayed under workspace .mcpServer/{agent}.
    /// C-red-P14 stays false until the override guard is implemented.
    /// </summary>
    public bool PluginRootOverrideRejected { get; init; }

    /// <summary>
    /// True when the V4 failsafe pending path was inspected after the turn.
    /// C-red-P15 stays false until failsafe assertions are implemented.
    /// </summary>
    public bool FailsafePathVerified { get; init; }
}
