using McpServer.Support.Mcp.Requirements.Models;

namespace McpServer.Support.Mcp.Requirements;

/// <summary>
/// FR-MCP-026: Extends <see cref="IRequirementsRepository"/> with document generation capabilities.
/// Parses the canonical requirements files into a typed in-memory model on startup and generates Markdown output.
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
    /// Generate all canonical requirements documents into a workspace folder.
    /// </summary>
    /// <param name="outputRootPath">Directory where the generated documents should be written.</param>
    /// <param name="generatedAtUtc">Optional export timestamp. Uses current UTC time when omitted.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Metadata for the workspace files written by the export.</returns>
    Task<RequirementsDocumentExportResult> GenerateAllAsync(string outputRootPath, DateTimeOffset? generatedAtUtc = null, CancellationToken ct = default);

    /// <summary>
    /// Generate Azure and GitHub wiki copies of all canonical requirements documents into a workspace folder.
    /// </summary>
    /// <param name="outputRootPath">Directory where the generated wiki folders should be written.</param>
    /// <param name="generatedAtUtc">Optional manifest and file timestamp. Uses current UTC time when omitted.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Metadata for the workspace files written by the export.</returns>
    Task<RequirementsDocumentExportResult> GenerateWikiAsync(string outputRootPath, DateTimeOffset? generatedAtUtc = null, CancellationToken ct = default);
}

/// <summary>
/// TR-MCP-TXN-001: Captures and restores requirements repository state for transaction rollback compensation.
/// </summary>
public interface IRequirementsCompensation
{
    /// <summary>Captures the current requirements repository state for rollback.</summary>
    Task<RequirementsCompensationSnapshot> CaptureRequirementsSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>Restores a previously captured requirements repository state.</summary>
    Task RestoreRequirementsSnapshotAsync(RequirementsCompensationSnapshot snapshot, CancellationToken cancellationToken = default);
}

/// <summary>
/// TR-MCP-TXN-001: Requirements repository snapshot used by transaction rollback compensation.
/// </summary>
public sealed record RequirementsCompensationSnapshot(
    IReadOnlyList<FrEntry> Functional,
    IReadOnlyList<TrEntry> Technical,
    IReadOnlyList<TestEntry> Testing,
    IReadOnlyList<FrTrMapping> Mappings,
    string Provider = "generic",
    object? State = null)
{
    /// <summary>An empty requirements repository snapshot.</summary>
    public static RequirementsCompensationSnapshot Empty { get; } = new([], [], [], []);
}
