using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>Diagnostic result for <c>/mcpserver/diagnostic/execution-path</c>.</summary>
public sealed class DiagnosticExecutionPathResult
{
    /// <summary>Current process executable path.</summary>
    [JsonPropertyName("processPath")]
    public string? ProcessPath { get; set; }

    /// <summary>Application base directory.</summary>
    [JsonPropertyName("baseDirectory")]
    public string? BaseDirectory { get; set; }
}

/// <summary>Diagnostic result for <c>/mcpserver/diagnostic/appsettings-path</c>.</summary>
public sealed class DiagnosticAppSettingsPathResult
{
    /// <summary>Hosting environment name.</summary>
    [JsonPropertyName("environmentName")]
    public string? EnvironmentName { get; set; }

    /// <summary>Resolved ASP.NET content root.</summary>
    [JsonPropertyName("contentRootPath")]
    public string? ContentRootPath { get; set; }

    /// <summary>Appsettings file candidates in load order.</summary>
    [JsonPropertyName("files")]
    public IReadOnlyList<DiagnosticPathFileEntry> Files { get; set; } = [];
}

/// <summary>Single appsettings file path candidate.</summary>
public sealed class DiagnosticPathFileEntry
{
    /// <summary>Candidate path.</summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>Whether the file exists on disk.</summary>
    [JsonPropertyName("exists")]
    public bool Exists { get; set; }
}
