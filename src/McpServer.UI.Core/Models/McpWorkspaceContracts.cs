using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.UI.Core.Models;

#pragma warning disable CS1591

public sealed class McpWorkspaceQueryResult
{
    [JsonPropertyName("items")]
    public List<McpWorkspaceItem> Items { get; set; } = new();

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

public sealed class McpWorkspaceItem
{
    [JsonPropertyName("workspacePath")]
    public string? WorkspacePath { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("todoPath")]
    public string? TodoPath { get; set; }

    [JsonPropertyName("dataDirectory")]
    public string? DataDirectory { get; set; }

    [JsonPropertyName("tunnelProvider")]
    public string? TunnelProvider { get; set; }

    [JsonPropertyName("isPrimary")]
    public bool? IsPrimary { get; set; }

    [JsonPropertyName("isEnabled")]
    public bool? IsEnabled { get; set; }

    [JsonPropertyName("dateTimeCreated")]
    public DateTimeOffset? DateTimeCreated { get; set; }

    [JsonPropertyName("dateTimeModified")]
    public DateTimeOffset? DateTimeModified { get; set; }

    [JsonPropertyName("runAs")]
    public string? RunAs { get; set; }

    [JsonPropertyName("promptTemplate")]
    public string? PromptTemplate { get; set; }

    [JsonPropertyName("statusPrompt")]
    public string? StatusPrompt { get; set; }

    [JsonPropertyName("implementPrompt")]
    public string? ImplementPrompt { get; set; }

    [JsonPropertyName("planPrompt")]
    public string? PlanPrompt { get; set; }

    [JsonPropertyName("bannedLicenses")]
    public List<string>? BannedLicenses { get; set; }

    [JsonPropertyName("bannedCountriesOfOrigin")]
    public List<string>? BannedCountriesOfOrigin { get; set; }

    [JsonPropertyName("bannedOrganizations")]
    public List<string>? BannedOrganizations { get; set; }

    [JsonPropertyName("bannedIndividuals")]
    public List<string>? BannedIndividuals { get; set; }
}

public sealed class McpWorkspaceCreateRequest
{
    [JsonPropertyName("workspacePath")]
    public string? WorkspacePath { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("todoPath")]
    public string? TodoPath { get; set; }

    [JsonPropertyName("dataDirectory")]
    public string? DataDirectory { get; set; }

    [JsonPropertyName("tunnelProvider")]
    public string? TunnelProvider { get; set; }

    [JsonPropertyName("runAs")]
    public string? RunAs { get; set; }

    [JsonPropertyName("isPrimary")]
    public bool? IsPrimary { get; set; }

    [JsonPropertyName("isEnabled")]
    public bool? IsEnabled { get; set; }

    [JsonPropertyName("promptTemplate")]
    public string? PromptTemplate { get; set; }

    [JsonPropertyName("statusPrompt")]
    public string? StatusPrompt { get; set; }

    [JsonPropertyName("implementPrompt")]
    public string? ImplementPrompt { get; set; }

    [JsonPropertyName("planPrompt")]
    public string? PlanPrompt { get; set; }

    [JsonPropertyName("bannedLicenses")]
    public List<string>? BannedLicenses { get; set; }

    [JsonPropertyName("bannedCountriesOfOrigin")]
    public List<string>? BannedCountriesOfOrigin { get; set; }

    [JsonPropertyName("bannedOrganizations")]
    public List<string>? BannedOrganizations { get; set; }

    [JsonPropertyName("bannedIndividuals")]
    public List<string>? BannedIndividuals { get; set; }
}

public sealed class McpWorkspaceUpdateRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("todoPath")]
    public string? TodoPath { get; set; }

    [JsonPropertyName("dataDirectory")]
    public string? DataDirectory { get; set; }

    [JsonPropertyName("tunnelProvider")]
    public string? TunnelProvider { get; set; }

    [JsonPropertyName("runAs")]
    public string? RunAs { get; set; }

    [JsonPropertyName("isPrimary")]
    public bool? IsPrimary { get; set; }

    [JsonPropertyName("isEnabled")]
    public bool? IsEnabled { get; set; }

    [JsonPropertyName("promptTemplate")]
    public string? PromptTemplate { get; set; }

    [JsonPropertyName("statusPrompt")]
    public string? StatusPrompt { get; set; }

    [JsonPropertyName("implementPrompt")]
    public string? ImplementPrompt { get; set; }

    [JsonPropertyName("planPrompt")]
    public string? PlanPrompt { get; set; }

    [JsonPropertyName("bannedLicenses")]
    public List<string>? BannedLicenses { get; set; }

    [JsonPropertyName("bannedCountriesOfOrigin")]
    public List<string>? BannedCountriesOfOrigin { get; set; }

    [JsonPropertyName("bannedOrganizations")]
    public List<string>? BannedOrganizations { get; set; }

    [JsonPropertyName("bannedIndividuals")]
    public List<string>? BannedIndividuals { get; set; }
}

public sealed class McpWorkspaceMutationResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("workspace")]
    public McpWorkspaceItem? Workspace { get; set; }
}

public sealed class McpWorkspaceProcessStatus
{
    [JsonPropertyName("isRunning")]
    public bool IsRunning { get; set; }

    [JsonPropertyName("pid")]
    public int? Pid { get; set; }

    [JsonPropertyName("uptime")]
    public string? Uptime { get; set; }

    [JsonPropertyName("port")]
    public int? Port { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public sealed class McpWorkspaceHealthResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public sealed class McpWorkspaceInitResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("filesCreated")]
    public List<string>? FilesCreated { get; set; }
}

public sealed class McpWorkspaceGlobalPromptResult
{
    [JsonPropertyName("template")]
    public string? Template { get; set; }

    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }
}

public sealed class McpWorkspaceGlobalPromptUpdateRequest
{
    [JsonPropertyName("template")]
    public string? Template { get; set; }
}

#pragma warning restore CS1591
