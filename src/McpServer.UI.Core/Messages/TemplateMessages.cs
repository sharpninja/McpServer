using McpServer.Cqrs;

namespace McpServer.UI.Core.Messages;

/// <summary>Query to list prompt templates with optional filters.</summary>
public sealed record ListTemplatesQuery : IQuery<ListTemplatesResult>
{
    /// <summary>Optional exact category filter.</summary>
    public string? Category { get; init; }

    /// <summary>Optional tag filter (any match).</summary>
    public string? Tag { get; init; }

    /// <summary>Optional keyword search across id, title, description.</summary>
    public string? Keyword { get; init; }
}

/// <summary>Result of a template list query.</summary>
public sealed record ListTemplatesResult(IReadOnlyList<TemplateListItem> Items, int TotalCount);

/// <summary>List-friendly template summary.</summary>
public sealed record TemplateListItem(
    string Id,
    string Title,
    string Category,
    IReadOnlyList<string> Tags,
    string? Description);

/// <summary>Query to load a single template by ID.</summary>
public sealed record GetTemplateQuery(string TemplateId) : IQuery<TemplateDetail?>;

/// <summary>Detailed template view including content and variables.</summary>
public sealed record TemplateDetail(
    string Id,
    string Title,
    string Category,
    IReadOnlyList<string> Tags,
    string? Description,
    string Engine,
    IReadOnlyList<TemplateVariableDetail> Variables,
    string Content);

/// <summary>Detail view of a template variable.</summary>
public sealed record TemplateVariableDetail(
    string Name,
    string? Description,
    bool Required,
    string? Example,
    string? DefaultValue);

/// <summary>Typed result of a template create/update/delete mutation.</summary>
public sealed record TemplateMutationOutcome(
    bool Success,
    string? Error,
    TemplateDetail? Item);

/// <summary>Query to test/render a template with sample data.</summary>
public sealed record TestTemplateQuery : IQuery<TemplateTestOutcome>
{
    /// <summary>Template ID to test (null if testing inline).</summary>
    public string? TemplateId { get; init; }

    /// <summary>Inline template content (for testing without saving).</summary>
    public string? InlineTemplate { get; init; }

    /// <summary>JSON-encoded variable values for the template context.</summary>
    public required string VariablesJson { get; init; }
}

/// <summary>Typed result of a template test/render operation.</summary>
public sealed record TemplateTestOutcome(
    bool Success,
    string? RenderedContent,
    string? Error,
    IReadOnlyList<string>? MissingVariables);

/// <summary>Command to create a new prompt template.</summary>
public sealed record CreateTemplateCommand : ICommand<TemplateMutationOutcome>
{
    /// <summary>Template ID (required).</summary>
    public required string Id { get; init; }

    /// <summary>Title (required).</summary>
    public required string Title { get; init; }

    /// <summary>Category (required).</summary>
    public required string Category { get; init; }

    /// <summary>Template body content (required).</summary>
    public required string Content { get; init; }

    /// <summary>Optional tags.</summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>Optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Optional engine override (default: handlebars).</summary>
    public string? Engine { get; init; }

    /// <summary>Optional variable definitions as JSON.</summary>
    public string? VariablesJson { get; init; }
}

/// <summary>Command to update an existing prompt template.</summary>
public sealed record UpdateTemplateCommand : ICommand<TemplateMutationOutcome>
{
    /// <summary>ID of the template to update.</summary>
    public required string TemplateId { get; init; }

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

    /// <summary>Updated variables as JSON (null = no change).</summary>
    public string? VariablesJson { get; init; }
}

/// <summary>Command to delete a prompt template.</summary>
public sealed record DeleteTemplateCommand(string TemplateId) : ICommand<TemplateMutationOutcome>;
