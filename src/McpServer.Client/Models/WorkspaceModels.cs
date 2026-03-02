using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>A workspace DTO.</summary>
public sealed class WorkspaceDto
{
    /// <summary>Absolute path to the workspace root.</summary>
    [JsonPropertyName("workspacePath")]
    public string WorkspacePath { get; set; } = string.Empty;

    /// <summary>Human-readable workspace name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Path to the TODO file.</summary>
    [JsonPropertyName("todoPath")]
    public string TodoPath { get; set; } = string.Empty;

    /// <summary>Override data directory.</summary>
    [JsonPropertyName("dataDirectory")]
    public string? DataDirectory { get; set; }

    /// <summary>Tunnel provider (ngrok, cloudflare, frp, or null).</summary>
    [JsonPropertyName("tunnelProvider")]
    public string? TunnelProvider { get; set; }

    /// <summary>Whether this is the primary workspace.</summary>
    [JsonPropertyName("isPrimary")]
    public bool IsPrimary { get; set; }

    /// <summary>Whether this workspace auto-starts.</summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; }

    /// <summary>Creation timestamp.</summary>
    [JsonPropertyName("dateTimeCreated")]
    public DateTimeOffset DateTimeCreated { get; set; }

    /// <summary>Last modification timestamp.</summary>
    [JsonPropertyName("dateTimeModified")]
    public DateTimeOffset DateTimeModified { get; set; }

    /// <summary>Windows identity for the workspace process.</summary>
    [JsonPropertyName("runAs")]
    public string? RunAs { get; set; }

    /// <summary>Optional markdown prompt template appended to the global marker prompt.</summary>
    [JsonPropertyName("promptTemplate")]
    public string? PromptTemplate { get; set; }

    /// <summary>Effective Copilot status prompt (custom override or built-in default).</summary>
    [JsonPropertyName("statusPrompt")]
    public string StatusPrompt { get; set; } = string.Empty;

    /// <summary>Effective Copilot implement prompt (custom override or built-in default).</summary>
    [JsonPropertyName("implementPrompt")]
    public string ImplementPrompt { get; set; } = string.Empty;

    /// <summary>Effective Copilot plan prompt (custom override or built-in default).</summary>
    [JsonPropertyName("planPrompt")]
    public string PlanPrompt { get; set; } = string.Empty;

    /// <summary>SPDX license identifiers banned in this workspace.</summary>
    [JsonPropertyName("bannedLicenses")]
    public List<string> BannedLicenses { get; set; } = [];

    /// <summary>ISO country codes banned as dependency origin in this workspace.</summary>
    [JsonPropertyName("bannedCountriesOfOrigin")]
    public List<string> BannedCountriesOfOrigin { get; set; } = [];

    /// <summary>Organization/company names banned in this workspace.</summary>
    [JsonPropertyName("bannedOrganizations")]
    public List<string> BannedOrganizations { get; set; } = [];

    /// <summary>Individual names/handles banned in this workspace.</summary>
    [JsonPropertyName("bannedIndividuals")]
    public List<string> BannedIndividuals { get; set; } = [];
}

/// <summary>Request to create a workspace.</summary>
public sealed class WorkspaceCreateRequest
{
    /// <summary>Absolute path to the workspace root.</summary>
    [JsonPropertyName("workspacePath")]
    public string WorkspacePath { get; set; } = string.Empty;

    /// <summary>Workspace name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>TODO file path.</summary>
    [JsonPropertyName("todoPath")]
    public string? TodoPath { get; set; }

    /// <summary>Override data directory.</summary>
    [JsonPropertyName("dataDirectory")]
    public string? DataDirectory { get; set; }

    /// <summary>Tunnel provider.</summary>
    [JsonPropertyName("tunnelProvider")]
    public string? TunnelProvider { get; set; }

    /// <summary>Windows identity.</summary>
    [JsonPropertyName("runAs")]
    public string? RunAs { get; set; }

    /// <summary>Mark as primary workspace.</summary>
    [JsonPropertyName("isPrimary")]
    public bool IsPrimary { get; set; }

    /// <summary>Enable auto-start.</summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>Optional markdown prompt template appended to the global marker prompt.</summary>
    [JsonPropertyName("promptTemplate")]
    public string? PromptTemplate { get; set; }

    /// <summary>Override for the Copilot status prompt. Null = use built-in default.</summary>
    [JsonPropertyName("statusPrompt")]
    public string? StatusPrompt { get; set; }

    /// <summary>Override for the Copilot implement prompt. Null = use built-in default.</summary>
    [JsonPropertyName("implementPrompt")]
    public string? ImplementPrompt { get; set; }

    /// <summary>Override for the Copilot plan prompt. Null = use built-in default.</summary>
    [JsonPropertyName("planPrompt")]
    public string? PlanPrompt { get; set; }

    /// <summary>Initial banned licenses for this workspace.</summary>
    [JsonPropertyName("bannedLicenses")]
    public List<string>? BannedLicenses { get; set; }

    /// <summary>Initial banned countries of origin for this workspace.</summary>
    [JsonPropertyName("bannedCountriesOfOrigin")]
    public List<string>? BannedCountriesOfOrigin { get; set; }

    /// <summary>Initial banned organizations for this workspace.</summary>
    [JsonPropertyName("bannedOrganizations")]
    public List<string>? BannedOrganizations { get; set; }

    /// <summary>Initial banned individuals for this workspace.</summary>
    [JsonPropertyName("bannedIndividuals")]
    public List<string>? BannedIndividuals { get; set; }
}

/// <summary>Request to update a workspace.</summary>
public sealed class WorkspaceUpdateRequest
{
    /// <summary>Updated name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Updated TODO path.</summary>
    [JsonPropertyName("todoPath")]
    public string? TodoPath { get; set; }

    /// <summary>Updated data directory.</summary>
    [JsonPropertyName("dataDirectory")]
    public string? DataDirectory { get; set; }

    /// <summary>Updated tunnel provider.</summary>
    [JsonPropertyName("tunnelProvider")]
    public string? TunnelProvider { get; set; }

    /// <summary>Updated Windows identity.</summary>
    [JsonPropertyName("runAs")]
    public string? RunAs { get; set; }

    /// <summary>Updated primary flag.</summary>
    [JsonPropertyName("isPrimary")]
    public bool? IsPrimary { get; set; }

    /// <summary>Updated enabled flag.</summary>
    [JsonPropertyName("isEnabled")]
    public bool? IsEnabled { get; set; }

    /// <summary>Updated workspace prompt template (null = no change, empty string = remove).</summary>
    [JsonPropertyName("promptTemplate")]
    public string? PromptTemplate { get; set; }

    /// <summary>Updated status prompt (null = no change, empty string = revert to default).</summary>
    [JsonPropertyName("statusPrompt")]
    public string? StatusPrompt { get; set; }

    /// <summary>Updated implement prompt (null = no change, empty string = revert to default).</summary>
    [JsonPropertyName("implementPrompt")]
    public string? ImplementPrompt { get; set; }

    /// <summary>Updated plan prompt (null = no change, empty string = revert to default).</summary>
    [JsonPropertyName("planPrompt")]
    public string? PlanPrompt { get; set; }

    /// <summary>Updated banned licenses (null = no change, empty list = clear all).</summary>
    [JsonPropertyName("bannedLicenses")]
    public List<string>? BannedLicenses { get; set; }

    /// <summary>Updated banned countries of origin (null = no change, empty list = clear all).</summary>
    [JsonPropertyName("bannedCountriesOfOrigin")]
    public List<string>? BannedCountriesOfOrigin { get; set; }

    /// <summary>Updated banned organizations (null = no change, empty list = clear all).</summary>
    [JsonPropertyName("bannedOrganizations")]
    public List<string>? BannedOrganizations { get; set; }

    /// <summary>Updated banned individuals (null = no change, empty list = clear all).</summary>
    [JsonPropertyName("bannedIndividuals")]
    public List<string>? BannedIndividuals { get; set; }
}

/// <summary>Result of reading the global marker prompt template.</summary>
public sealed class GlobalPromptResult
{
    /// <summary>The resolved prompt template text.</summary>
    [JsonPropertyName("template")]
    public string Template { get; set; } = string.Empty;

    /// <summary>Whether the built-in default template is in use.</summary>
    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }
}

/// <summary>Request to update the global marker prompt template.</summary>
public sealed class GlobalPromptUpdateRequest
{
    /// <summary>The new global prompt template. Send null or empty to revert to default.</summary>
    [JsonPropertyName("template")]
    public string? Template { get; set; }
}

/// <summary>Result of listing workspaces.</summary>
public sealed class WorkspaceListResult
{
    /// <summary>Workspaces.</summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<WorkspaceDto> Items { get; set; } = [];

    /// <summary>Total count.</summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

/// <summary>Result of a workspace mutation.</summary>
public sealed class WorkspaceMutationResult
{
    /// <summary>Whether the operation succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Error message.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>The affected workspace.</summary>
    [JsonPropertyName("workspace")]
    public WorkspaceDto? Workspace { get; set; }
}

/// <summary>Result of workspace initialization.</summary>
public sealed class WorkspaceInitResult
{
    /// <summary>Whether init succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Error message.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Files created during init.</summary>
    [JsonPropertyName("filesCreated")]
    public IReadOnlyList<string>? FilesCreated { get; set; }
}

/// <summary>Workspace process status.</summary>
public sealed class WorkspaceProcessStatus
{
    /// <summary>Whether the workspace host is running.</summary>
    [JsonPropertyName("isRunning")]
    public bool IsRunning { get; set; }

    /// <summary>Process ID.</summary>
    [JsonPropertyName("pid")]
    public int? Pid { get; set; }

    /// <summary>Uptime.</summary>
    [JsonPropertyName("uptime")]
    public string? Uptime { get; set; }

    /// <summary>Listening port.</summary>
    [JsonPropertyName("port")]
    public int? Port { get; set; }

    /// <summary>Error message.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
