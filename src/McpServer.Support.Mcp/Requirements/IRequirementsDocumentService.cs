using McpServer.Support.Mcp.Requirements.Models;

namespace McpServer.Support.Mcp.Requirements;

/// <summary>
/// FR-MCP-026: Extends <see cref="IRequirementsRepository"/> with document generation capabilities.
/// Parses all four canonical files into a typed in-memory model on startup and generates Markdown output.
/// </summary>
public interface IRequirementsDocumentService : IRequirementsRepository
{
    /// <summary>
    /// Generate a single requirements document as Markdown.
    /// </summary>
    /// <param name="docType">The document type to generate (must not be <see cref="RequirementsDocType.All"/>).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Tuple of (markdownContent, mimeType).</returns>
    Task<(string Content, string MimeType)> GenerateDocumentAsync(RequirementsDocType docType, CancellationToken ct = default);

    /// <summary>
    /// Generate all four requirements documents as a ZIP archive.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="MemoryStream"/> containing the ZIP archive.</returns>
    Task<MemoryStream> GenerateAllAsync(CancellationToken ct = default);
}