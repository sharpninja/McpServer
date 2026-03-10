using System.Text.Json.Serialization;

namespace McpServer.Support.Mcp.Models;

/// <summary>
/// Runtime lifecycle state for a managed agent process.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentProcessStatus
{
    /// <summary>
    /// The process is starting but has not yet been confirmed as running.
    /// </summary>
    Starting,

    /// <summary>
    /// The process is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// The process has stopped normally.
    /// </summary>
    Stopped,

    /// <summary>
    /// The process terminated with a failure.
    /// </summary>
    Failed,
}

/// <summary>
/// Runtime information for an agent process associated with a workspace.
/// </summary>
public sealed class AgentProcessInfo
{
    /// <summary>
    /// Gets or sets the operating-system process identifier when available.
    /// </summary>
    [JsonPropertyName("processId")]
    public int? ProcessId { get; set; }

    /// <summary>
    /// Gets or sets the logical agent identifier.
    /// </summary>
    [JsonPropertyName("agentId")]
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the workspace path that owns the process.
    /// </summary>
    [JsonPropertyName("workspacePath")]
    public string WorkspacePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when the process started.
    /// </summary>
    [JsonPropertyName("startedAt")]
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the current runtime status.
    /// </summary>
    [JsonPropertyName("status")]
    public AgentProcessStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the process exit code when the process has exited.
    /// </summary>
    [JsonPropertyName("exitCode")]
    public int? ExitCode { get; set; }

    /// <summary>
    /// Gets or sets the effective working directory used for process launch.
    /// </summary>
    [JsonPropertyName("workDirectory")]
    public string? WorkDirectory { get; set; }

    /// <summary>
    /// Gets or sets a human-readable error message when launch or execution fails.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}
