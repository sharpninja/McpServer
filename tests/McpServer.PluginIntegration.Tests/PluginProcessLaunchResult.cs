namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// TR-MCP-PLUGININT-001 AC3: captured process launch outputs for a plugin host adapter.
/// </summary>
public sealed class PluginProcessLaunchResult
{
    /// <summary>Process exit code.</summary>
    public required int ExitCode { get; init; }

    /// <summary>Captured standard output.</summary>
    public required string StandardOutput { get; init; }

    /// <summary>Captured standard error.</summary>
    public required string StandardError { get; init; }
}
