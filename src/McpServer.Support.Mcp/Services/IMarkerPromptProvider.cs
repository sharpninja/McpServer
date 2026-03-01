namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-049, TR-MCP-TPL-001: Provides the global marker prompt template
/// from an external YAML file, with graceful fallback to the built-in default.
/// </summary>
public interface IMarkerPromptProvider
{
    /// <summary>
    /// Returns the marker prompt template loaded from
    /// <c>templates/default-marker-prompt.hbs.yaml</c>, or <see langword="null"/>
    /// when the file is missing or unreadable (caller falls back to
    /// <see cref="MarkerFileService.DefaultPromptTemplate"/>).
    /// </summary>
    Task<string?> GetGlobalPromptTemplateAsync(CancellationToken cancellationToken = default);
}
