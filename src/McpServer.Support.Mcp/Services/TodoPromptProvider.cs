namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-049, TR-MCP-TPL-001: Loads TODO prompt templates from
/// <see cref="IPromptTemplateService"/> by well-known IDs, falling back to
/// <see cref="TodoPromptDefaults"/> built-in constants when not found.
/// </summary>
public sealed class TodoPromptProvider : ITodoPromptProvider
{
    /// <summary>Well-known template ID for the status prompt.</summary>
    internal const string StatusPromptId = "todo-status-prompt";

    /// <summary>Well-known template ID for the implement prompt.</summary>
    internal const string ImplementPromptId = "todo-implement-prompt";

    /// <summary>Well-known template ID for the plan prompt.</summary>
    internal const string PlanPromptId = "todo-plan-prompt";

    private readonly IPromptTemplateService _templateService;
    private readonly ILogger<TodoPromptProvider> _logger;

    /// <summary>Initializes a new instance of the <see cref="TodoPromptProvider"/> class.</summary>
    public TodoPromptProvider(IPromptTemplateService templateService, ILogger<TodoPromptProvider> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GetStatusPromptAsync(CancellationToken cancellationToken = default)
    {
        return await GetTemplateOrDefaultAsync(StatusPromptId, TodoPromptDefaults.StatusPrompt, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> GetImplementPromptAsync(CancellationToken cancellationToken = default)
    {
        return await GetTemplateOrDefaultAsync(ImplementPromptId, TodoPromptDefaults.ImplementPrompt, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> GetPlanPromptAsync(CancellationToken cancellationToken = default)
    {
        return await GetTemplateOrDefaultAsync(PlanPromptId, TodoPromptDefaults.PlanPrompt, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> GetTemplateOrDefaultAsync(string templateId, string fallback, CancellationToken cancellationToken)
    {
        try
        {
            var template = await _templateService.GetByIdAsync(templateId, cancellationToken).ConfigureAwait(false);
            if (template is not null && !string.IsNullOrWhiteSpace(template.Content))
            {
                _logger.LogDebug("Loaded TODO prompt template '{Id}' from template store", templateId);
                return template.Content;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to load TODO prompt template '{Id}': {Error}", templateId, ex.ToString());
        }

        _logger.LogDebug("Using built-in default for TODO prompt '{Id}'", templateId);
        return fallback;
    }
}
