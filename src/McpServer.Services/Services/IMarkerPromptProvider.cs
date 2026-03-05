namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-049, TR-MCP-TPL-001: Provides the global marker prompt template
/// from the combined prompt-templates.yaml via <see cref="IPromptTemplateService"/>,
/// with graceful fallback to the built-in default.
/// </summary>
public interface IMarkerPromptProvider
{
    /// <summary>
    /// Returns the marker prompt template loaded from
    /// the <c>default-marker-prompt</c> entry in <c>prompt-templates.yaml</c>,
    /// or <see langword="null"/> when not found (caller falls back to
    /// <see cref="MarkerFileService.DefaultPromptTemplate"/>).
    /// </summary>
    Task<string?> GetGlobalPromptTemplateAsync(CancellationToken cancellationToken = default);
}
