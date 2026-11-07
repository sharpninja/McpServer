using QBrainAi.Support.Mcp.Requirements;
using QBrainAi.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-DOCFXWIKI-001: the helpers that both halves of RequirementsDocFxWorkflowRunnerTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-DOCFXWIKI-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from RequirementsDocFxWorkflowRunnerTests (CreateConfig, CreateWorkflow, RecordingProcessRunner and TestWorkspace) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal.</remarks>
internal static class RequirementsDocFxWorkflowRunnerTestsSupport
{
    internal static RequirementsWikiExportConfig CreateConfig(string workspaceRoot, params RequirementsDocFxWorkflow[] workflows) =>
        new()
        {
            ConfigPath = Path.Combine(workspaceRoot, "docs", "wiki.yaml"),
            WorkspaceRoot = workspaceRoot,
            Documents = [],
            Navigation = [],
            DocFxWorkflows = workflows,
            DocumentsById = new Dictionary<string, RequirementsWikiExportDocument>(StringComparer.OrdinalIgnoreCase)
        };

    internal static RequirementsDocFxWorkflow CreateWorkflow(
        string workspaceRoot,
        string id = "docs",
        IReadOnlyList<string>? arguments = null,
        string workingDirectory = "docs/docfx",
        string outputRoot = "docs/docfx/_site",
        string targetRoot = "api",
        IReadOnlyList<string>? platforms = null,
        int timeoutSeconds = 120)
    {
        return new RequirementsDocFxWorkflow
        {
            Id = id,
            Executable = "dotnet",
            Arguments = arguments ?? ["docfx", "docfx.json"],
            WorkingDirectory = workingDirectory,
            WorkingDirectoryPath = Path.GetFullPath(Path.Combine(workspaceRoot, workingDirectory)),
            OutputRoot = outputRoot,
            OutputRootPath = Path.GetFullPath(Path.Combine(workspaceRoot, outputRoot)),
            TargetRoot = targetRoot,
            Platforms = new HashSet<string>(platforms ?? ["github", "azure"], StringComparer.OrdinalIgnoreCase),
            TimeoutSeconds = timeoutSeconds
        };
    }

    internal sealed class RecordingProcessRunner : IProcessRunner
    {
        private readonly Queue<Func<ProcessRunRequest, CancellationToken, ProcessRunResult>> _handlers = new();

        public List<ProcessRunRequest> Requests { get; } = [];

        public List<CancellationToken> CancellationTokens { get; } = [];

        public void Enqueue(Func<ProcessRunRequest, CancellationToken, ProcessRunResult> handler) => _handlers.Enqueue(handler);

        public Task<ProcessRunResult> RunAsync(string fileName, string arguments, CancellationToken ct = default)
            => RunAsync(new ProcessRunRequest(fileName, arguments), ct);

        public Task<ProcessRunResult> RunAsync(ProcessRunRequest request, CancellationToken ct = default)
        {
            Requests.Add(request);
            CancellationTokens.Add(ct);
            var handler = _handlers.Count == 0
                ? static (_, _) => new ProcessRunResult(0, "ok", null)
                : _handlers.Dequeue();
            return Task.FromResult(handler(request, ct));
        }
    }

    internal sealed class TestWorkspace : IDisposable
    {
        public TestWorkspace()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mcp-docfx-runner-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
            Directory.CreateDirectory(System.IO.Path.Combine(Path, "docs", "docfx"));
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
