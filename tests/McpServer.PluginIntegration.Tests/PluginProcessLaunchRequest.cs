namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// TR-MCP-PLUGININT-001 AC3: captured process launch inputs for a plugin host adapter.
/// </summary>
public sealed class PluginProcessLaunchRequest
{
    /// <summary>Executable file name (for example pwsh.exe or node).</summary>
    public required string Executable { get; init; }

    /// <summary>Argument list passed without shell parsing.</summary>
    public required IReadOnlyList<string> Arguments { get; init; }

    /// <summary>Stdin envelope forwarded to the host process.</summary>
    public required string StandardInput { get; init; }

    /// <summary>Per-process environment variable overrides.</summary>
    public required IReadOnlyDictionary<string, string> Environment { get; init; }

    /// <summary>Working directory for the host process.</summary>
    public required string WorkingDirectory { get; init; }

    /// <summary>Launch timeout forwarded to the process runner.</summary>
    public required TimeSpan Timeout { get; init; }
}
