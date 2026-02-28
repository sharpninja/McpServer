using System;
using System.Collections.Generic;

namespace McpServer.VsExtension.McpTodo.Models;

/// <summary>Flat TODO item from MCP GET /mcpserver/todo.</summary>
public sealed class TodoFlatItem
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Section { get; set; } = "";
    public string Priority { get; set; } = "";
    public bool Done { get; set; }
    public string? Estimate { get; set; }
    public string? Note { get; set; }
    public IReadOnlyList<string>? Description { get; set; }
    public IReadOnlyList<string>? TechnicalDetails { get; set; }
    public IReadOnlyList<TodoFlatTask>? ImplementationTasks { get; set; }
    public string? CompletedDate { get; set; }
    public string? DoneSummary { get; set; }
    public string? Remaining { get; set; }
    public IReadOnlyList<string>? DependsOn { get; set; }
    public IReadOnlyList<string>? FunctionalRequirements { get; set; }
    public IReadOnlyList<string>? TechnicalRequirements { get; set; }
    public string? Reference { get; set; }
}

public sealed class TodoFlatTask
{
    public string Task { get; set; } = "";
    public bool Done { get; set; }
}

public sealed class TodoQueryResult
{
    public List<TodoFlatItem>? Items { get; set; }
    public int TotalCount { get; set; }
}

/// <summary>Body for PUT /mcpserver/todo/{id}.</summary>
public sealed class TodoUpdateBody
{
    public string? Title { get; set; }
    public string? Priority { get; set; }
    public string? Section { get; set; }
    public bool? Done { get; set; }
    public string? Estimate { get; set; }
    public IReadOnlyList<string>? Description { get; set; }
    public IReadOnlyList<string>? TechnicalDetails { get; set; }
    public IReadOnlyList<TodoFlatTask>? ImplementationTasks { get; set; }
    public string? Note { get; set; }
    public string? CompletedDate { get; set; }
    public string? DoneSummary { get; set; }
    public string? Remaining { get; set; }
    public IReadOnlyList<string>? DependsOn { get; set; }
    public IReadOnlyList<string>? FunctionalRequirements { get; set; }
    public IReadOnlyList<string>? TechnicalRequirements { get; set; }
}

public sealed class TodoMutationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public TodoFlatItem? Item { get; set; }
}
