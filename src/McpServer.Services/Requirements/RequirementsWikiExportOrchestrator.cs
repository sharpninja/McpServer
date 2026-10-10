using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.Extensions.DependencyInjection;

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
/// <param name="IncludeDump">FR-MCP-WIKIEXPORT-003: when true, write mcp-wiki-dump.json under the export root.</param>
public sealed record RequirementsWikiExportRequest(
    string OutputRootPath,
    DateTimeOffset GeneratedAtUtc,
    string? WorkspacePath,
    RequirementsOptions Options,
    IReadOnlyList<FrEntry> Functional,
    IReadOnlyList<TrEntry> Technical,
    IReadOnlyList<TestEntry> Testing,
    IReadOnlyList<FrTrMapping> Mappings,
    string? ExistingMatrixMarkdown,
    bool IncludeDump = false);

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
    private readonly IWikiDumpService? _wikiDumpService;
    private readonly IServiceScopeFactory? _scopeFactory;

    /// <summary>TR-MCP-WIKIEXPORT-003: Direct dump-service constructor used by tests.</summary>
    /// <param name="docFxWorkflowRunner">DocFX workflow runner.</param>
    /// <param name="wikiDumpService">Optional dump service used when no scope factory is present.</param>
    public RequirementsWikiExportOrchestrator(IRequirementsDocFxWorkflowRunner docFxWorkflowRunner, IWikiDumpService? wikiDumpService = null)
    {
        _docFxWorkflowRunner = docFxWorkflowRunner ?? throw new ArgumentNullException(nameof(docFxWorkflowRunner));
        _wikiDumpService = wikiDumpService;
    }

    /// <summary>TR-MCP-WIKIEXPORT-003: Host constructor. Resolves scoped <see cref="IWikiDumpService"/> per export.</summary>
    /// <param name="docFxWorkflowRunner">DocFX workflow runner.</param>
    /// <param name="scopeFactory">Scope factory used to resolve <see cref="IWikiDumpService"/> without a captive scoped dependency.</param>
    public RequirementsWikiExportOrchestrator(IRequirementsDocFxWorkflowRunner docFxWorkflowRunner, IServiceScopeFactory scopeFactory)
    {
        _docFxWorkflowRunner = docFxWorkflowRunner ?? throw new ArgumentNullException(nameof(docFxWorkflowRunner));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
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

        var result = await RequirementsDocumentExportWriter.WriteAsync(
            request.OutputRootPath,
            "wiki",
            "all",
            request.GeneratedAtUtc,
            documents,
            [RequirementsWikiDocumentRenderer.AzureFolder, RequirementsWikiDocumentRenderer.GitHubFolder],
            ct).ConfigureAwait(false);
        if (request.IncludeDump)
        {
            var dumpRequest = new McpServer.Support.Mcp.Models.WikiDumpExportRequest
            {
                OutputRoot = request.OutputRootPath,
                IncludeDump = true,
                WorkspacePath = request.WorkspacePath,
            };
            if (_scopeFactory is not null)
            {
                using var scope = _scopeFactory.CreateScope();
                var scoped = scope.ServiceProvider;
                if (!string.IsNullOrWhiteSpace(request.WorkspacePath))
                {
                    var workspaceContext = scoped.GetService<WorkspaceContext>();
                    if (workspaceContext is not null)
                        workspaceContext.WorkspacePath = request.WorkspacePath;
                }

                var dump = scoped.GetRequiredService<IWikiDumpService>();
                if (!string.IsNullOrWhiteSpace(request.WorkspacePath))
                    scoped.GetService<McpDbContext>()?.OverrideWorkspaceId(request.WorkspacePath);
                await dump.ExportAsync(dumpRequest, ct).ConfigureAwait(false);
            }
            else if (_wikiDumpService is not null)
            {
                await _wikiDumpService.ExportAsync(dumpRequest, ct).ConfigureAwait(false);
            }
        }

        return result;
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
