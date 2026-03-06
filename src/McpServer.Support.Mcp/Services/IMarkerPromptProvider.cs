namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-049, TR-MCP-TPL-001: Provides the global marker prompt template
/// from the combined prompt-templates.yaml via <see cref="IPromptTemplateService"/>.
/// Throws a critical exception if the template is missing.
/// </summary>
public interface IMarkerPromptProvider
{
    /// <summary>
    /// Returns the marker prompt template loaded from
    /// the <c>default-marker-prompt</c> entry in <c>prompt-templates.yaml</c>.
    /// Throws <see cref="InvalidOperationException"/> if the template is not found.
    /// </summary>
    Task<string> GetGlobalPromptTemplateAsync(CancellationToken cancellationToken = default);
}
