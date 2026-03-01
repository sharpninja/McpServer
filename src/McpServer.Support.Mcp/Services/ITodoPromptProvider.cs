namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-049, TR-MCP-TPL-001: Provides TODO prompt templates from external
/// YAML files via <see cref="IPromptTemplateService"/>, with fallback to
/// <see cref="TodoPromptDefaults"/> built-in constants.
/// </summary>
public interface ITodoPromptProvider
{
    /// <summary>Returns the status prompt template.</summary>
    Task<string> GetStatusPromptAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the implement prompt template.</summary>
    Task<string> GetImplementPromptAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the plan prompt template.</summary>
    Task<string> GetPlanPromptAsync(CancellationToken cancellationToken = default);
}
