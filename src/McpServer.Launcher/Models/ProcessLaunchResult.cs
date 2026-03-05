using System.Text.Json.Serialization;

namespace McpServer.Launcher.Models;

/// <summary>
/// Result of a desktop process launch attempt.
/// </summary>
public sealed class ProcessLaunchResult
{
    /// <summary>
    /// Whether the process was launched successfully.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Process ID of the launched process (null on failure).
    /// </summary>
    [JsonPropertyName("processId")]
    public int? ProcessId { get; set; }

    /// <summary>
    /// Exit code of the process. Only set when <see cref="ProcessLaunchRequest.WaitForExit"/> is true.
    /// </summary>
    [JsonPropertyName("exitCode")]
    public int? ExitCode { get; set; }

    /// <summary>
    /// Error message when the launch fails.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Win32 error code on failure.
    /// </summary>
    [JsonPropertyName("errorCode")]
    public int? ErrorCode { get; set; }
}
