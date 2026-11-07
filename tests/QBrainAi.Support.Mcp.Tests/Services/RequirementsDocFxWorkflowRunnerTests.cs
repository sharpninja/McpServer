using QBrainAi.Support.Mcp.Requirements;
using QBrainAi.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static QBrainAi.Support.Mcp.Tests.Services.RequirementsDocFxWorkflowRunnerTestsSupport;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>TEST-MCP-DOCFXWIKI-001: DocFX workflow runner execution, artifact, and path-safety coverage.</summary>
/// <remarks>2026-11-07, TEST-MCP-DOCFXWIKI-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 1 Category=Integration case (RunAsync_WhenOutputContainsReparsePointEscape_Throws) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the helpers both halves use (CreateConfig, CreateWorkflow, RecordingProcessRunner and TestWorkspace) moved once into Services/RequirementsDocFxWorkflowRunnerTestsSupport.cs beside this file, which the integration project links (DRY); the unit cases and the class attributes are unchanged.</remarks>
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
}
