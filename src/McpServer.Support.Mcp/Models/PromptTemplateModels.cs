// Copyright (c) 2025 McpServer Contributors. All rights reserved.

using System.Collections.Generic;

namespace McpServer.Support.Mcp.Models;

/// <summary>Represents a prompt template stored in the registry.</summary>
public sealed record PromptTemplate
{
    /// <summary>Unique kebab-case identifier (e.g. "marker-agent-instructions").</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable title.</summary>
    public required string Title { get; init; }

    /// <summary>Grouping category (e.g. "marker", "todo", "review", "custom").</summary>
    public required string Category { get; init; }

    /// <summary>Cross-cutting tags for filtering.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Optional description of the template's purpose.</summary>
    public string? Description { get; init; }

    /// <summary>Rendering engine identifier (default: "handlebars").</summary>
    public string Engine { get; init; } = "handlebars";

    /// <summary>Declared template variables with metadata.</summary>
    public IReadOnlyList<TemplateVariable> Variables { get; init; } = [];

    /// <summary>The template body content.</summary>
    public required string Content { get; init; }
}

/// <summary>Describes a variable accepted by a prompt template.</summary>
public sealed record TemplateVariable
{
    /// <summary>Variable name as used in the template (e.g. "baseUrl").</summary>
    public required string Name { get; init; }

    /// <summary>Optional description of the variable.</summary>
    public string? Description { get; init; }

    /// <summary>Whether the variable is required for rendering.</summary>
    public bool Required { get; init; }

    /// <summary>Example value for documentation or testing.</summary>
    public string? Example { get; init; }

    /// <summary>Default value used when the variable is not supplied.</summary>
    public string? DefaultValue { get; init; }
}

/// <summary>Request to create a new prompt template.</summary>
public sealed record PromptTemplateCreateRequest
{
    /// <summary>Unique kebab-case identifier. Required.</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable title. Required.</summary>
    public required string Title { get; init; }

    /// <summary>Grouping category. Required.</summary>
    public required string Category { get; init; }

    /// <summary>Template body content. Required.</summary>
    public required string Content { get; init; }

    /// <summary>Cross-cutting tags for filtering.</summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>Optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Rendering engine (default: "handlebars").</summary>
    public string? Engine { get; init; }

    /// <summary>Declared template variables.</summary>
    public IReadOnlyList<TemplateVariable>? Variables { get; init; }
}

/// <summary>Request to update an existing prompt template. Null fields are not changed.</summary>
public sealed record PromptTemplateUpdateRequest
{
    /// <summary>Updated title (null = no change).</summary>
    public string? Title { get; init; }

    /// <summary>Updated category (null = no change).</summary>
    public string? Category { get; init; }

    /// <summary>Updated content (null = no change).</summary>
    public string? Content { get; init; }

    /// <summary>Updated tags (null = no change).</summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>Updated description (null = no change).</summary>
    public string? Description { get; init; }

    /// <summary>Updated engine (null = no change).</summary>
    public string? Engine { get; init; }

    /// <summary>Updated variables (null = no change).</summary>
    public IReadOnlyList<TemplateVariable>? Variables { get; init; }
}

/// <summary>Result of a template query (list).</summary>
/// <param name="Items">Matching templates.</param>
/// <param name="TotalCount">Total number of matching templates.</param>
public sealed record PromptTemplateQueryResult(IReadOnlyList<PromptTemplate> Items, int TotalCount);

/// <summary>Result of a template mutation (create, update, delete).</summary>
/// <param name="Success">Whether the operation succeeded.</param>
/// <param name="Error">Error message if the operation failed.</param>
/// <param name="Item">The affected template, if applicable.</param>
public sealed record PromptTemplateMutationResult(bool Success, string? Error = null, PromptTemplate? Item = null);

/// <summary>Request to test/render a template with sample data.</summary>
public sealed record PromptTemplateTestRequest
{
    /// <summary>Variable values to pass to the template context.</summary>
    public Dictionary<string, object?>? Variables { get; init; }

    /// <summary>Optional inline template content (for testing without saving).</summary>
    public string? InlineTemplate { get; init; }
}

/// <summary>Result of a template test/render operation.</summary>
public sealed record PromptTemplateTestResult
{
    /// <summary>Whether rendering succeeded.</summary>
    public required bool Success { get; init; }

    /// <summary>The rendered output, if successful.</summary>
    public string? RenderedContent { get; init; }

    /// <summary>Error message, if rendering failed.</summary>
    public string? Error { get; init; }

    /// <summary>Required variables that were missing from the input.</summary>
    public IReadOnlyList<string>? MissingVariables { get; init; }
}

/// <summary>
/// FR-MCP-056: Request to resolve a stored template by id and variable dictionary.
/// </summary>
public sealed record PromptTemplateResolveRequest
{
    /// <summary>
    /// Variable values to pass to template rendering.
    /// </summary>
    public Dictionary<string, object?>? Values { get; init; }
}

/// <summary>
/// FR-MCP-056: Result of resolving a stored template by id.
/// </summary>
public sealed record PromptTemplateResolveResult
{
    /// <summary>
    /// Whether rendering succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Template identifier used for rendering.
    /// </summary>
    public string? TemplateId { get; init; }

    /// <summary>
    /// Populated prompt text.
    /// </summary>
    public string? Prompt { get; init; }

    /// <summary>
    /// Error text for failed resolutions.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Missing required variables when validation fails.
    /// </summary>
    public IReadOnlyList<string>? MissingVariables { get; init; }
}
