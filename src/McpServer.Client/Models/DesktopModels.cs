using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>
/// FR-MCP-047/TR-MCP-DESKTOP-001: Request payload for launching a local desktop process
/// through the MCP Server HTTP API.
/// </summary>
public sealed class DesktopLaunchRequest
{
    /// <summary>Full path to the executable to launch.</summary>
    [JsonPropertyName("executablePath")]
    public string ExecutablePath { get; set; } = string.Empty;

    /// <summary>Optional command-line arguments for the launched process.</summary>
    [JsonPropertyName("arguments")]
    public string? Arguments { get; set; }

    /// <summary>Optional working directory for the launched process.</summary>
    [JsonPropertyName("workingDirectory")]
    public string? WorkingDirectory { get; set; }

    /// <summary>Optional environment variables applied to the launched process.</summary>
    [JsonPropertyName("environmentVariables")]
    public Dictionary<string, string>? EnvironmentVariables { get; set; }

    /// <summary>Whether the process should be created without a visible window.</summary>
    [JsonPropertyName("createNoWindow")]
    public bool CreateNoWindow { get; set; }

    /// <summary>Window style for the launched process.</summary>
    [JsonPropertyName("windowStyle")]
    public string WindowStyle { get; set; } = "Normal";

    /// <summary>Whether the caller should wait for the process to exit.</summary>
    [JsonPropertyName("waitForExit")]
    public bool WaitForExit { get; set; }

    /// <summary>Optional timeout, in milliseconds, when waiting for process exit.</summary>
    [JsonPropertyName("timeoutMs")]
    public int? TimeoutMs { get; set; }
}

/// <summary>
/// FR-MCP-047/TR-MCP-DESKTOP-001: Result payload returned after a local desktop process
/// launch attempt.
/// </summary>
public sealed class DesktopLaunchResult
{
    /// <summary>Whether the launch attempt succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Process identifier for the launched process, when available.</summary>
    [JsonPropertyName("processId")]
    public int? ProcessId { get; set; }

    /// <summary>Exit code of the launched process when waiting for completion.</summary>
    [JsonPropertyName("exitCode")]
    public int? ExitCode { get; set; }

    /// <summary>Error message describing a failed launch attempt.</summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>Native or launcher-specific error code for a failed launch attempt.</summary>
    [JsonPropertyName("errorCode")]
    public int? ErrorCode { get; set; }
}
