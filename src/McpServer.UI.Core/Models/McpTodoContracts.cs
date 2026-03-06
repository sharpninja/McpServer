using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.UI.Core.Models;

#pragma warning disable CS1591

// ── Query / List ────────────────────────────────────────────────────────────

public sealed class McpTodoQueryResult
{
    [JsonPropertyName("items")]
    public List<McpTodoFlatItem> Items { get; set; } = new();

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

// ── Core item ───────────────────────────────────────────────────────────────

public sealed class McpTodoFlatItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("section")]
    public string Section { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty;

    [JsonPropertyName("done")]
    public bool Done { get; set; }

    [JsonPropertyName("estimate")]
    public string? Estimate { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("description")]
    public List<string>? Description { get; set; }

    [JsonPropertyName("technicalDetails")]
    public List<string>? TechnicalDetails { get; set; }

    [JsonPropertyName("implementationTasks")]
    public List<McpTodoFlatTask>? ImplementationTasks { get; set; }

    [JsonPropertyName("completedDate")]
    public string? CompletedDate { get; set; }

    [JsonPropertyName("doneSummary")]
    public string? DoneSummary { get; set; }

    [JsonPropertyName("remaining")]
    public string? Remaining { get; set; }

    [JsonPropertyName("priorityNote")]
    public string? PriorityNote { get; set; }

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("dependsOn")]
    public List<string>? DependsOn { get; set; }

    [JsonPropertyName("functionalRequirements")]
    public List<string>? FunctionalRequirements { get; set; }

    [JsonPropertyName("technicalRequirements")]
    public List<string>? TechnicalRequirements { get; set; }
}

// ── Sub-task ────────────────────────────────────────────────────────────────

public sealed class McpTodoFlatTask
{
    [JsonPropertyName("task")]
    public string? Task { get; set; }

    [JsonPropertyName("done")]
    public bool Done { get; set; }
}

// ── Create request ──────────────────────────────────────────────────────────

public sealed class McpTodoCreateRequest
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("section")]
    public string Section { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty;

    [JsonPropertyName("estimate")]
    public string? Estimate { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("remaining")]
    public string? Remaining { get; set; }

    [JsonPropertyName("description")]
    public List<string>? Description { get; set; }

    [JsonPropertyName("technicalDetails")]
    public List<string>? TechnicalDetails { get; set; }

    [JsonPropertyName("implementationTasks")]
    public List<McpTodoFlatTask>? ImplementationTasks { get; set; }

    [JsonPropertyName("dependsOn")]
    public List<string>? DependsOn { get; set; }

    [JsonPropertyName("functionalRequirements")]
    public List<string>? FunctionalRequirements { get; set; }

    [JsonPropertyName("technicalRequirements")]
    public List<string>? TechnicalRequirements { get; set; }
}

// ── Update request ──────────────────────────────────────────────────────────

public sealed class McpTodoUpdateRequest
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    [JsonPropertyName("section")]
    public string? Section { get; set; }

    [JsonPropertyName("done")]
    public bool? Done { get; set; }

    [JsonPropertyName("estimate")]
    public string? Estimate { get; set; }

    [JsonPropertyName("description")]
    public List<string>? Description { get; set; }

    [JsonPropertyName("technicalDetails")]
    public List<string>? TechnicalDetails { get; set; }

    [JsonPropertyName("implementationTasks")]
    public List<McpTodoFlatTask>? ImplementationTasks { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("completedDate")]
    public string? CompletedDate { get; set; }

    [JsonPropertyName("doneSummary")]
    public string? DoneSummary { get; set; }

    [JsonPropertyName("remaining")]
    public string? Remaining { get; set; }

    [JsonPropertyName("dependsOn")]
    public List<string>? DependsOn { get; set; }

    [JsonPropertyName("functionalRequirements")]
    public List<string>? FunctionalRequirements { get; set; }

    [JsonPropertyName("technicalRequirements")]
    public List<string>? TechnicalRequirements { get; set; }
}

// ── Mutation result ─────────────────────────────────────────────────────────

public sealed class McpTodoMutationResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("item")]
    public McpTodoFlatItem? Item { get; set; }
}

// ── Requirements analysis ───────────────────────────────────────────────────

public sealed class McpRequirementsAnalysisResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("functionalRequirements")]
    public List<string>? FunctionalRequirements { get; set; }

    [JsonPropertyName("technicalRequirements")]
    public List<string>? TechnicalRequirements { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("copilotResponse")]
    public string? CopilotResponse { get; set; }
}

#pragma warning restore CS1591
