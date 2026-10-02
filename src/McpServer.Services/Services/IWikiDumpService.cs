using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>FR-MCP-WIKIEXPORT-003/004/005: Wiki dump export and hydration.</summary>
public interface IWikiDumpService
{
    /// <summary>Export a workspace dump when IncludeDump is true.</summary>
    Task<WikiDumpExportResult> ExportAsync(WikiDumpExportRequest request, CancellationToken cancellationToken = default);

    /// <summary>Import a dump into a destination workspace. TODOs come from dump rows, not todo.yaml.</summary>
    Task<WikiDumpImportResult> ImportAsync(WikiDumpImportRequest request, CancellationToken cancellationToken = default);
}
