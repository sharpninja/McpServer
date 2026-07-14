using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements.Models;

namespace McpServer.Support.Mcp.Requirements;

/// <summary>TR-MCP-DOCFXWIKI-001: Request data required to export requirements wiki documents through the shared orchestrator.</summary>
/// <param name="OutputRootPath">Target wiki export root.</param>
/// <param name="GeneratedAtUtc">Normalized generation timestamp.</param>
/// <param name="WorkspacePath">Active workspace root, when known.</param>
/// <param name="Options">Requirements options used for paths and wiki config lookup.</param>
/// <param name="Functional">Functional requirement entries.</param>
/// <param name="Technical">Technical requirement entries.</param>
/// <param name="Testing">Testing requirement entries.</param>
/// <param name="Mappings">FR/TR/TEST traceability mappings.</param>
/// <param name="ExistingMatrixMarkdown">Existing matrix markdown used to preserve status rows.</param>
public sealed record RequirementsWikiExportRequest(
    string OutputRootPath,
    DateTimeOffset GeneratedAtUtc,
    string? WorkspacePath,
    RequirementsOptions Options,
    IReadOnlyList<FrEntry> Functional,
    IReadOnlyList<TrEntry> Technical,
    IReadOnlyList<TestEntry> Testing,
    IReadOnlyList<FrTrMapping> Mappings,
    string? ExistingMatrixMarkdown);

/// <summary>TR-MCP-DOCFXWIKI-001: Shared service that owns requirements wiki export orchestration.</summary>
public interface IRequirementsWikiExportOrchestrator
{
    /// <summary>Exports a wiki using canonical requirements, optional DocFX output, and the atomic export writer.</summary>
    /// <param name="request">Wiki export request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Export result returned by the atomic writer.</returns>
    Task<RequirementsDocumentExportResult> ExportAsync(RequirementsWikiExportRequest request, CancellationToken ct = default);
}

/// <summary>TR-MCP-DOCFXWIKI-001: Default requirements wiki export orchestrator.</summary>
internal sealed class RequirementsWikiExportOrchestrator : IRequirementsWikiExportOrchestrator
{
    private readonly IRequirementsDocFxWorkflowRunner _docFxWorkflowRunner;

    public RequirementsWikiExportOrchestrator(IRequirementsDocFxWorkflowRunner docFxWorkflowRunner)
    {
        _docFxWorkflowRunner = docFxWorkflowRunner ?? throw new ArgumentNullException(nameof(docFxWorkflowRunner));
    }

    public async Task<RequirementsDocumentExportResult> ExportAsync(RequirementsWikiExportRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();

        var config = RequirementsWikiExportConfigLoader.Load(request.WorkspacePath, request.Options);
        var docFxDocuments = config?.DocFxWorkflows.Count > 0
            ? await _docFxWorkflowRunner.RunAsync(config, ct).ConfigureAwait(false)
            : [];
        var documents = RequirementsWikiDocumentRenderer.RenderWikiFiles(
            request.Functional,
            request.Technical,
            request.Testing,
            request.Mappings,
            request.GeneratedAtUtc,
            request.ExistingMatrixMarkdown,
            config,
            docFxDocuments);

        return await RequirementsDocumentExportWriter.WriteAsync(
            request.OutputRootPath,
            "wiki",
            "all",
            request.GeneratedAtUtc,
            documents,
            [RequirementsWikiDocumentRenderer.AzureFolder, RequirementsWikiDocumentRenderer.GitHubFolder],
            ct).ConfigureAwait(false);
    }
}

/// <summary>TR-MCP-DOCFXWIKI-001: Fallback runner used only when non-DI tests construct services without DocFX support.</summary>
internal sealed class DisabledRequirementsDocFxWorkflowRunner : IRequirementsDocFxWorkflowRunner
{
    public Task<IReadOnlyList<RequirementsRenderedDocument>> RunAsync(RequirementsWikiExportConfig config, CancellationToken ct = default)
    {
        throw new InvalidOperationException("DocFX wiki workflows require IRequirementsDocFxWorkflowRunner to be registered.");
    }
}
