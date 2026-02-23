namespace McpServer.Common.Copilot;

/// <summary>TR-CLI-001: Options for invoking the Copilot CLI.</summary>
public sealed class CopilotClientOptions
{
    /// <summary>
    /// Path to the CLI agent binary. Defaults to "copilot" (GitHub Copilot CLI, must be on PATH).
    /// </summary>
    public string AgentPath { get; set; } = "copilot";

    /// <summary>
    /// Model to use for the agent via --model.
    /// Defaults to "auto".
    /// </summary>
    public string Model { get; set; } = "auto";

    /// <summary>
    /// When <c>true</c>, passes <c>--silent</c> to the Copilot CLI so only the
    /// agent response is emitted (no statistics or progress lines).
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool Silent { get; set; } = true;

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

    /// <summary>
    /// Windows user identity whose profile environment is loaded before spawning
    /// the CLI process. When running as a Windows service (<c>LocalSystem</c>), this
    /// ensures the spawned process inherits the user's <c>PATH</c> (so the CLI binary
    /// is discoverable) and profile directories (<c>USERPROFILE</c>, <c>APPDATA</c>,
    /// <c>LOCALAPPDATA</c>) so the CLI can access cached authentication tokens.
    /// Null or empty = inherit the current process environment.
    /// </summary>
    public string? RunAs { get; set; }
}
