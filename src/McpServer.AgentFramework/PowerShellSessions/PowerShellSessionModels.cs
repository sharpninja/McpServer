using System;
using System.Text.Json.Serialization;

namespace McpServer.AgentFramework.PowerShellSessions;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-007: Result returned when a hosted agent creates a local
/// PowerShell session directly inside the current .NET process.
/// </summary>
public sealed class PowerShellSessionCreateResult
{
    /// <summary>Whether the session was created successfully.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>The generated session identifier when creation succeeds.</summary>
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    /// <summary>The current working directory for the created session.</summary>
    [JsonPropertyName("currentLocation")]
    public string? CurrentLocation { get; set; }

    /// <summary>The UTC timestamp recorded when the session was created.</summary>
    [JsonPropertyName("createdAtUtc")]
    public DateTimeOffset? CreatedAtUtc { get; set; }

    /// <summary>Error text describing a failed session-creation attempt.</summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-007: Result returned after executing a command inside a hosted
/// in-process PowerShell session.
/// </summary>
public sealed class PowerShellSessionCommandResult
{
    /// <summary>Whether the command completed without PowerShell errors.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>The targeted PowerShell session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    /// <summary>Formatted pipeline output captured from the command.</summary>
    [JsonPropertyName("output")]
    public string Output { get; set; } = string.Empty;

    /// <summary>Formatted error-stream output captured from the command.</summary>
    [JsonPropertyName("errorOutput")]
    public string? ErrorOutput { get; set; }

    /// <summary>Formatted warning-stream output captured from the command.</summary>
    [JsonPropertyName("warningOutput")]
    public string? WarningOutput { get; set; }

    /// <summary>Formatted information-stream output captured from the command.</summary>
    [JsonPropertyName("informationOutput")]
    public string? InformationOutput { get; set; }

    /// <summary>Formatted verbose-stream output captured from the command.</summary>
    [JsonPropertyName("verboseOutput")]
    public string? VerboseOutput { get; set; }

    /// <summary>Formatted debug-stream output captured from the command.</summary>
    [JsonPropertyName("debugOutput")]
    public string? DebugOutput { get; set; }

    /// <summary>Whether PowerShell reported any error records while running the command.</summary>
    [JsonPropertyName("hadErrors")]
    public bool HadErrors { get; set; }

    /// <summary>The current session working directory after the command finishes.</summary>
    [JsonPropertyName("currentLocation")]
    public string? CurrentLocation { get; set; }
}

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-007: Result returned when a hosted agent closes a local
/// in-process PowerShell session.
/// </summary>
public sealed class PowerShellSessionCloseResult
{
    /// <summary>Whether the targeted session was closed successfully.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>The targeted PowerShell session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    /// <summary>Error text describing a failed close attempt.</summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}
