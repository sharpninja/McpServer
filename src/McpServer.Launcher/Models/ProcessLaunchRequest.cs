using System.Text.Json.Serialization;

namespace McpServer.Launcher.Models;

/// <summary>
/// Deserialized input payload for desktop process launch.
/// </summary>
public sealed class ProcessLaunchRequest
{
    /// <summary>
    /// Full path to the executable to launch. Required.
    /// </summary>
    [JsonPropertyName("executablePath")]
    public string ExecutablePath { get; set; } = string.Empty;

    /// <summary>
    /// Command-line arguments for the process.
    /// </summary>
    [JsonPropertyName("arguments")]
    public string? Arguments { get; set; }

    /// <summary>
    /// Working directory for the new process.
    /// </summary>
    [JsonPropertyName("workingDirectory")]
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Environment variables to set for the new process.
    /// </summary>
    [JsonPropertyName("environmentVariables")]
    public Dictionary<string, string>? EnvironmentVariables { get; set; }

    /// <summary>
    /// Whether to create the process without a visible window.
    /// </summary>
    [JsonPropertyName("createNoWindow")]
    public bool CreateNoWindow { get; set; }

    /// <summary>
    /// Window style for the new process.
    /// </summary>
    [JsonPropertyName("windowStyle")]
    public WindowStyleOption WindowStyle { get; set; } = WindowStyleOption.Normal;

    /// <summary>
    /// Whether to wait for the process to exit before returning.
    /// </summary>
    [JsonPropertyName("waitForExit")]
    public bool WaitForExit { get; set; }

    /// <summary>
    /// Timeout in milliseconds when waiting for exit. Only used when <see cref="WaitForExit"/> is true.
    /// </summary>
    [JsonPropertyName("timeoutMs")]
    public int? TimeoutMs { get; set; }
}
