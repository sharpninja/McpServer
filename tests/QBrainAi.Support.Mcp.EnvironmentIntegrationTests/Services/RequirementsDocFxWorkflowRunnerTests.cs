using QBrainAi.Support.Mcp.Requirements;
using QBrainAi.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static QBrainAi.Support.Mcp.Tests.Services.RequirementsDocFxWorkflowRunnerTestsSupport;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>TEST-MCP-DOCFXWIKI-001: DocFX workflow runner execution, artifact, and path-safety coverage.</summary>
/// <remarks>2026-11-07, TEST-MCP-DOCFXWIKI-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/RequirementsDocFxWorkflowRunnerTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case moved unchanged with the class attributes; the helpers both halves use come from Services/RequirementsDocFxWorkflowRunnerTestsSupport.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class RequirementsDocFxWorkflowRunnerTests
{
    /// <summary>Runner rejects reparse points in staged output before they can escape the workspace.</summary>
    /// <remarks>Integration: creates a real reparse point in staged filesystem output (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
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
}
