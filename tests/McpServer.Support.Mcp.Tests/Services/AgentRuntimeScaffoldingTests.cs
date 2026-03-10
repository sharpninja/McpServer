using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Focused regression tests for MVP-MCP-005 runtime scaffolding.
/// </summary>
public sealed class AgentRuntimeScaffoldingTests
{
    [Fact]
    public async Task NoneIsolationStrategy_ReturnsOriginalWorkspacePath()
    {
        var strategy = new NoneAgentIsolationStrategy();
        var workspacePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "mcp-agent-runtime-none"));

        var result = await strategy.PrepareWorkDirectoryAsync(workspacePath, "planner").ConfigureAwait(true);

        Assert.Equal(workspacePath, result);
    }

    [Fact]
    public async Task WorktreeIsolationStrategy_UsesWorkspaceAsWorkingDirectoryForGit()
    {
        var processRunner = Substitute.For<IProcessRunner>();
        processRunner.RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProcessRunResult(0, string.Empty, null)));

        var workspacePath = Path.Combine(Path.GetTempPath(), $"mcp-worktree-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspacePath);
        await File.WriteAllTextAsync(Path.Combine(workspacePath, MarkerFileService.MarkerFileName), "marker").ConfigureAwait(true);

        var strategy = new WorktreeAgentIsolationStrategy(
            processRunner,
            Options.Create(new AgentProcessManagerOptions()),
            NullLogger<WorktreeAgentIsolationStrategy>.Instance);

        try
        {
            _ = await strategy.PrepareWorkDirectoryAsync(workspacePath, "planner").ConfigureAwait(true);

            await processRunner.Received(1).RunAsync(
                Arg.Is<ProcessRunRequest>(request =>
                    request.FileName == "git"
                    && request.WorkingDirectory == Path.GetFullPath(workspacePath)
                    && request.Arguments.Contains("worktree add", StringComparison.Ordinal)),
                Arg.Any<CancellationToken>()).ConfigureAwait(true);
        }
        finally
        {
            if (Directory.Exists(workspacePath))
                Directory.Delete(workspacePath, recursive: true);
        }
    }

    [Fact]
    public async Task CloneIsolationStrategy_UsesWorkspaceAsWorkingDirectoryForGit()
    {
        var processRunner = Substitute.For<IProcessRunner>();
        processRunner.RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProcessRunResult(0, string.Empty, null)));

        var workspacePath = Path.Combine(Path.GetTempPath(), $"mcp-clone-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspacePath);
        await File.WriteAllTextAsync(Path.Combine(workspacePath, MarkerFileService.MarkerFileName), "marker").ConfigureAwait(true);

        var strategy = new CloneAgentIsolationStrategy(
            processRunner,
            Options.Create(new AgentProcessManagerOptions()),
            NullLogger<CloneAgentIsolationStrategy>.Instance);

        try
        {
            _ = await strategy.PrepareWorkDirectoryAsync(workspacePath, "planner").ConfigureAwait(true);

            await processRunner.Received(1).RunAsync(
                Arg.Is<ProcessRunRequest>(request =>
                    request.FileName == "git"
                    && request.WorkingDirectory == Path.GetFullPath(workspacePath)
                    && request.Arguments.Contains("clone --depth 1", StringComparison.Ordinal)),
                Arg.Any<CancellationToken>()).ConfigureAwait(true);
        }
        finally
        {
            if (Directory.Exists(workspacePath))
                Directory.Delete(workspacePath, recursive: true);
        }
    }

    [Fact]
    public async Task DirectBranchStrategy_UsesSuppliedWorkingDirectory()
    {
        var processRunner = Substitute.For<IProcessRunner>();
        processRunner.RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProcessRunResult(0, "main", null)));

        var workDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "mcp-branch-direct"));
        var strategy = new DirectAgentBranchStrategy(processRunner);

        var branch = await strategy.PrepareBranchAsync(workDirectory, "planner").ConfigureAwait(true);

        Assert.Equal("main", branch);
        await processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(request =>
                request.FileName == "git"
                && request.WorkingDirectory == workDirectory
                && request.Arguments == "rev-parse --abbrev-ref HEAD"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task FeatureBranchStrategy_CreatesAndRestoresBranchInSuppliedWorkingDirectory()
    {
        var processRunner = Substitute.For<IProcessRunner>();
        processRunner.RunAsync(
                Arg.Is<ProcessRunRequest>(request => request.Arguments == "rev-parse --abbrev-ref HEAD"),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProcessRunResult(0, "develop", null)));
        processRunner.RunAsync(
                Arg.Is<ProcessRunRequest>(request => request.Arguments.StartsWith("checkout -b ", StringComparison.Ordinal)),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProcessRunResult(0, string.Empty, null)));
        processRunner.RunAsync(
                Arg.Is<ProcessRunRequest>(request => request.Arguments == "checkout \"develop\""),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProcessRunResult(0, string.Empty, null)));

        var workDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "mcp-branch-feature"));
        var strategy = new FeatureAgentBranchStrategy(processRunner, NullLogger<FeatureAgentBranchStrategy>.Instance);

        var branch = await strategy.PrepareBranchAsync(workDirectory, "planner").ConfigureAwait(true);
        Assert.StartsWith("agent/planner/", branch, StringComparison.Ordinal);

        await strategy.FinalizeBranchAsync(workDirectory, "planner").ConfigureAwait(true);

        await processRunner.Received().RunAsync(
            Arg.Is<ProcessRunRequest>(request => request.WorkingDirectory == workDirectory),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task MarkerFileService_BuildTemplateContext_IncludesAgentAdditions()
    {
        var additions = new List<(string AgentId, string Content)>
        {
            ("planner", "Plan-only guidance"),
            ("coder", "Code-only guidance"),
        };

        var context = MarkerFileService.BuildTemplateContext(
            "http://localhost:7147",
            "abc123",
            workspace: null,
            workspacePath: "C:/repo",
            workspaceName: "repo",
            agentAdditions: additions);

        Assert.True(context.TryGetValue("agentAdditions", out var raw));
        var items = Assert.IsAssignableFrom<IEnumerable<object>>(raw);
        Assert.Equal(2, items.Count());
    }
}
