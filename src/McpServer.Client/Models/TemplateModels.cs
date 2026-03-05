using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>A prompt template item.</summary>
public sealed class TemplateItem
{
    /// <summary>Unique kebab-case identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Grouping category.</summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>Cross-cutting tags.</summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>Optional description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Rendering engine identifier.</summary>
    [JsonPropertyName("engine")]
    public string Engine { get; set; } = "handlebars";

    /// <summary>Declared template variables.</summary>
    [JsonPropertyName("variables")]
    public IReadOnlyList<TemplateVariableItem> Variables { get; set; } = [];

    /// <summary>Template body content.</summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>A template variable definition.</summary>
public sealed class TemplateVariableItem
{
    /// <summary>Variable name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Whether the variable is required.</summary>
    [JsonPropertyName("required")]
    public bool Required { get; set; }

    /// <summary>Example value.</summary>
    [JsonPropertyName("example")]
    public string? Example { get; set; }

    /// <summary>Default value when not supplied.</summary>
    [JsonPropertyName("defaultValue")]
    public string? DefaultValue { get; set; }
}

/// <summary>Request to create a template.</summary>
public sealed class TemplateCreateRequest
{
    /// <summary>Unique identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Category.</summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>Template body content.</summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>Tags.</summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; set; }

    /// <summary>Description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Rendering engine.</summary>
    [JsonPropertyName("engine")]
    public string? Engine { get; set; }

    /// <summary>Variable definitions.</summary>
    [JsonPropertyName("variables")]
    public IReadOnlyList<TemplateVariableItem>? Variables { get; set; }
}

/// <summary>Request to update a template.</summary>
public sealed class TemplateUpdateRequest
{
    /// <summary>Updated title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Updated category.</summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    /// <summary>Updated content.</summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>Updated tags.</summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; set; }

    /// <summary>Updated description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Updated engine.</summary>
    [JsonPropertyName("engine")]
    public string? Engine { get; set; }

    /// <summary>Updated variables.</summary>
    [JsonPropertyName("variables")]
    public IReadOnlyList<TemplateVariableItem>? Variables { get; set; }
}

/// <summary>Result of a template query.</summary>
public sealed class TemplateQueryResult
{
    /// <summary>Matching templates.</summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<TemplateItem> Items { get; set; } = [];

    /// <summary>Total count of matching templates.</summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

/// <summary>Result of a template mutation.</summary>
public sealed class TemplateMutationResult
{
    /// <summary>Whether the operation succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Error message on failure.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>The affected template.</summary>
    [JsonPropertyName("item")]
    public TemplateItem? Item { get; set; }
}

/// <summary>Request to test/render a template.</summary>
public sealed class TemplateTestRequest
{
    /// <summary>Variable values for the template context.</summary>
    [JsonPropertyName("variables")]
    public Dictionary<string, object?>? Variables { get; set; }

    /// <summary>Inline template content (for testing without saving).</summary>
    [JsonPropertyName("inlineTemplate")]
    public string? InlineTemplate { get; set; }
}

/// <summary>Result of a template test/render operation.</summary>
public sealed class TemplateTestResult
{
    /// <summary>Whether rendering succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Rendered output.</summary>
    [JsonPropertyName("renderedContent")]
    public string? RenderedContent { get; set; }

    /// <summary>Error message on failure.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Required variables that were missing.</summary>
    [JsonPropertyName("missingVariables")]
    public IReadOnlyList<string>? MissingVariables { get; set; }
}

/// <summary>Request to resolve a stored template by ID and variable dictionary.</summary>
public sealed class TemplateResolveRequest
{
    /// <summary>Variable values to pass to template rendering.</summary>
    [JsonPropertyName("values")]
    public Dictionary<string, object?>? Values { get; set; }
}

/// <summary>Result of resolving a stored template by ID.</summary>
public sealed class TemplateResolveResult
{
    /// <summary>Whether rendering succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Template identifier used for rendering.</summary>
    [JsonPropertyName("templateId")]
    public string? TemplateId { get; set; }

    /// <summary>Populated prompt text.</summary>
    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }

    /// <summary>Error text for failed resolutions.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Missing required variables when validation fails.</summary>
    [JsonPropertyName("missingVariables")]
    public IReadOnlyList<string>? MissingVariables { get; set; }
}
