using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TEST-MCP-DOCFXWIKI-001: DocFX workflow runner execution, artifact, and path-safety coverage.</summary>
public sealed class RequirementsDocFxWorkflowRunnerTests
{
    /// <summary>Runner uses structured process arguments, working directory, timeout cancellation, and cleans staged output.</summary>
    [Fact]
    public async Task RunAsync_UsesStructuredProcessRequestAndCleansOutputRoot()
    {
        using var workspace = new TestWorkspace();
        string[] arguments = ["docfx", "metadata file.json", "literal\"quote", "a&b|c;d", "$(not-a-shell)"];
        var workflow = CreateWorkflow(workspace.Path, arguments: arguments);
        var processRunner = new RecordingProcessRunner();
        processRunner.Enqueue((_, _) =>
        {
            Directory.CreateDirectory(workflow.OutputRootPath);
            File.WriteAllText(Path.Combine(workflow.OutputRootPath, "index.html"), "<html>ok</html>");
            return new ProcessRunResult(0, "generated", null);
        });
        var runner = new RequirementsDocFxWorkflowRunner(processRunner, NullLogger<RequirementsDocFxWorkflowRunner>.Instance);

        var documents = await runner.RunAsync(CreateConfig(workspace.Path, workflow), TestContext.Current.CancellationToken).ConfigureAwait(true);

        var request = Assert.Single(processRunner.Requests);
        Assert.Equal("dotnet", request.FileName);
        Assert.Empty(request.Arguments);
        Assert.Equal(arguments, request.ArgumentList);
        Assert.Equal(workflow.WorkingDirectoryPath, request.WorkingDirectory);
        Assert.True(Assert.Single(processRunner.CancellationTokens).CanBeCanceled);
        Assert.False(Directory.Exists(workflow.OutputRootPath));
        Assert.Equal(["azure/api/index.html", "github/api/index.html"], documents.Select(static item => item.RelativePath).ToArray());
    }

    /// <summary>Runner reports bounded stdout and stderr when the DocFX process exits with a failure code.</summary>
    [Fact]
    public async Task RunAsync_WhenProcessExitsNonZero_ThrowsBoundedDiagnostics()
    {
        using var workspace = new TestWorkspace();
        var workflow = CreateWorkflow(workspace.Path, id: "api-docs");
        var processRunner = new RecordingProcessRunner();
        processRunner.Enqueue((_, _) => new ProcessRunResult(2, new string('o', 5000), "docfx failed"));
        var runner = new RequirementsDocFxWorkflowRunner(processRunner, NullLogger<RequirementsDocFxWorkflowRunner>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            runner.RunAsync(CreateConfig(workspace.Path, workflow), TestContext.Current.CancellationToken)).ConfigureAwait(true);

        Assert.Contains("api-docs", ex.Message, StringComparison.Ordinal);
        Assert.Contains("docfx failed", ex.Message, StringComparison.Ordinal);
        Assert.True(ex.Message.Length < 5000);
    }

    /// <summary>Runner fails when a successful workflow does not produce the configured output root.</summary>
    [Fact]
    public async Task RunAsync_WhenOutputRootMissing_Throws()
    {
        using var workspace = new TestWorkspace();
        var workflow = CreateWorkflow(workspace.Path);
        var processRunner = new RecordingProcessRunner();
        processRunner.Enqueue((_, _) => new ProcessRunResult(0, "ok", null));
        var runner = new RequirementsDocFxWorkflowRunner(processRunner, NullLogger<RequirementsDocFxWorkflowRunner>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            runner.RunAsync(CreateConfig(workspace.Path, workflow), TestContext.Current.CancellationToken)).ConfigureAwait(true);

        Assert.Contains("output root", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(Directory.Exists(workflow.OutputRootPath));
    }

    /// <summary>Runner rejects unsupported binary artifacts instead of publishing unreadable wiki documents.</summary>
    [Fact]
    public async Task RunAsync_WhenOutputContainsUnsupportedFile_Throws()
    {
        using var workspace = new TestWorkspace();
        var workflow = CreateWorkflow(workspace.Path);
        var processRunner = new RecordingProcessRunner();
        processRunner.Enqueue((_, _) =>
        {
            Directory.CreateDirectory(workflow.OutputRootPath);
            File.WriteAllBytes(Path.Combine(workflow.OutputRootPath, "logo.png"), [1, 2, 3]);
            return new ProcessRunResult(0, "ok", null);
        });
        var runner = new RequirementsDocFxWorkflowRunner(processRunner, NullLogger<RequirementsDocFxWorkflowRunner>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            runner.RunAsync(CreateConfig(workspace.Path, workflow), TestContext.Current.CancellationToken)).ConfigureAwait(true);

        Assert.Contains("unsupported", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(Directory.Exists(workflow.OutputRootPath));
    }

    /// <summary>Runner rejects reparse points in staged output before they can escape the workspace.</summary>
    [Fact]
    public async Task RunAsync_WhenOutputContainsReparsePointEscape_Throws()
    {
        using var workspace = new TestWorkspace();
        using var external = new TestWorkspace();
        var workflow = CreateWorkflow(workspace.Path);
        var processRunner = new RecordingProcessRunner();
        processRunner.Enqueue((_, _) =>
        {
            Directory.CreateDirectory(workflow.OutputRootPath);
            Directory.CreateSymbolicLink(Path.Combine(workflow.OutputRootPath, "external"), external.Path);
            return new ProcessRunResult(0, "ok", null);
        });
        var runner = new RequirementsDocFxWorkflowRunner(processRunner, NullLogger<RequirementsDocFxWorkflowRunner>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            runner.RunAsync(CreateConfig(workspace.Path, workflow), TestContext.Current.CancellationToken)).ConfigureAwait(true);

        Assert.Contains("reparse", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(Directory.Exists(workflow.OutputRootPath));
    }

    /// <summary>Runner emits deterministic, platform-prefixed wiki artifacts with normalized separators and content types.</summary>
    [Fact]
    public async Task RunAsync_ProjectsFilesInDeterministicOrder()
    {
        using var workspace = new TestWorkspace();
        var workflow = CreateWorkflow(workspace.Path);
        var processRunner = new RecordingProcessRunner();
        processRunner.Enqueue((_, _) =>
        {
            Directory.CreateDirectory(Path.Combine(workflow.OutputRootPath, "nested"));
            File.WriteAllText(Path.Combine(workflow.OutputRootPath, "b.html"), "b");
            File.WriteAllText(Path.Combine(workflow.OutputRootPath, "a.md"), "a");
            File.WriteAllText(Path.Combine(workflow.OutputRootPath, "nested", "c.css"), "c");
            return new ProcessRunResult(0, "ok", null);
        });
        var runner = new RequirementsDocFxWorkflowRunner(processRunner, NullLogger<RequirementsDocFxWorkflowRunner>.Instance);

        var documents = await runner.RunAsync(CreateConfig(workspace.Path, workflow), TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(
            [
                "azure/api/a.md",
                "azure/api/b.html",
                "azure/api/nested/c.css",
                "github/api/a.md",
                "github/api/b.html",
                "github/api/nested/c.css"
            ],
            documents.Select(static item => item.RelativePath).ToArray());
        Assert.Equal(["text/markdown", "text/html", "text/css", "text/markdown", "text/html", "text/css"], documents.Select(static item => item.ContentType).ToArray());
    }

    /// <summary>Runner rejects duplicate canonical publication paths across configured workflows.</summary>
    [Fact]
    public async Task RunAsync_WhenWorkflowsProduceDuplicatePublicationPaths_Throws()
    {
        using var workspace = new TestWorkspace();
        var first = CreateWorkflow(workspace.Path, id: "first", outputRoot: "docs/first-site");
        var second = CreateWorkflow(workspace.Path, id: "second", outputRoot: "docs/second-site");
        var processRunner = new RecordingProcessRunner();
        processRunner.Enqueue((_, _) =>
        {
            Directory.CreateDirectory(first.OutputRootPath);
            File.WriteAllText(Path.Combine(first.OutputRootPath, "index.html"), "first");
            return new ProcessRunResult(0, "ok", null);
        });
        processRunner.Enqueue((_, _) =>
        {
            Directory.CreateDirectory(second.OutputRootPath);
            File.WriteAllText(Path.Combine(second.OutputRootPath, "index.html"), "second");
            return new ProcessRunResult(0, "ok", null);
        });
        var runner = new RequirementsDocFxWorkflowRunner(processRunner, NullLogger<RequirementsDocFxWorkflowRunner>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            runner.RunAsync(CreateConfig(workspace.Path, first, second), TestContext.Current.CancellationToken)).ConfigureAwait(true);

        Assert.Contains("duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static RequirementsWikiExportConfig CreateConfig(string workspaceRoot, params RequirementsDocFxWorkflow[] workflows) =>
        new()
        {
            ConfigPath = Path.Combine(workspaceRoot, "docs", "wiki.yaml"),
            WorkspaceRoot = workspaceRoot,
            Documents = [],
            Navigation = [],
            DocFxWorkflows = workflows,
            DocumentsById = new Dictionary<string, RequirementsWikiExportDocument>(StringComparer.OrdinalIgnoreCase)
        };

    private static RequirementsDocFxWorkflow CreateWorkflow(
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

    private sealed class RecordingProcessRunner : IProcessRunner
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

    private sealed class TestWorkspace : IDisposable
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
