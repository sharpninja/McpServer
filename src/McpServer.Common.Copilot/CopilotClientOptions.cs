namespace McpServer.Common.Copilot;

/// <summary>TR-CLI-001: Options for invoking the Copilot CLI.</summary>
public sealed class CopilotClientOptions
{
    /// <summary>
    /// Path to the CLI agent binary. Defaults to "agent" (must be on PATH).
    /// </summary>
    public string AgentPath { get; set; } = "agent";

    /// <summary>
    /// Model to use for the agent via --model.
    /// Defaults to "auto".
    /// </summary>
    public string Model { get; set; } = "auto";

    /// <summary>
    /// Output format passed to the agent via --output-format.
    /// Defaults to "text".
    /// </summary>
    public string OutputFormat { get; set; } = "text";

    /// <summary>
    /// Timeout for the CLI process. Defaults to 2 minutes.
    /// Set to <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> for no timeout.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Working directory for the spawned process.
    /// Defaults to <see cref="Environment.CurrentDirectory"/>.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Additional environment variables to pass to the spawned process.
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; } = [];
}
