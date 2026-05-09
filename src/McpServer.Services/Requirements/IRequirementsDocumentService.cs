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
    /// Generate all four requirements documents into a workspace folder.
    /// </summary>
    /// <param name="outputRootPath">Directory where the generated documents should be written.</param>
    /// <param name="generatedAtUtc">Optional export timestamp. Uses current UTC time when omitted.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Metadata for the workspace files written by the export.</returns>
    Task<RequirementsDocumentExportResult> GenerateAllAsync(string outputRootPath, DateTimeOffset? generatedAtUtc = null, CancellationToken ct = default);

    /// <summary>
    /// Generate Azure and GitHub wiki copies of all requirements documents into a workspace folder.
    /// </summary>
    /// <param name="outputRootPath">Directory where the generated wiki folders should be written.</param>
    /// <param name="generatedAtUtc">Optional manifest and file timestamp. Uses current UTC time when omitted.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Metadata for the workspace files written by the export.</returns>
    Task<RequirementsDocumentExportResult> GenerateWikiAsync(string outputRootPath, DateTimeOffset? generatedAtUtc = null, CancellationToken ct = default);
}
