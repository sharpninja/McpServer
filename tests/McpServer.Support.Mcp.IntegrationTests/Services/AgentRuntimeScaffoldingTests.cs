using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Services;

/// <summary>
/// Integration tests for MVP-MCP-005 runtime scaffolding strategies (isolation and branch management).
/// Validates agent isolation (none, worktree, clone) and branch strategies (direct, feature) with real
/// file system interactions and mocked process execution.
/// </summary>
public sealed class AgentRuntimeScaffoldingTests
{
    /// <summary>
    /// Verifies that <see cref="NoneAgentIsolationStrategy"/> returns the original workspace path unchanged.
    /// Tests MVP-MCP-005: no-op isolation passes the workspace through as-is.
    /// </summary>
    [Fact]
    public async Task NoneIsolationStrategy_ReturnsOriginalWorkspacePath()
    {
        var strategy = new NoneAgentIsolationStrategy();
        var workspacePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "mcp-agent-runtime-none"));

        var result = await strategy.PrepareWorkDirectoryAsync(workspacePath, "planner").ConfigureAwait(true);

        Assert.Equal(workspacePath, result);
    }

    /// <summary>
    /// Verifies that <see cref="WorktreeAgentIsolationStrategy"/> calls git worktree add using the workspace
    /// path as the working directory. Tests MVP-MCP-005: worktree isolation invokes git in the workspace root.
    /// </summary>
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
            Microsoft.Extensions.Options.Options.Create(new AgentProcessManagerOptions()),
            NullLogger<WorktreeAgentIsolationStrategy>.Instance);

        try
        {
            _ = await strategy.PrepareWorkDirectoryAsync(workspacePath, "planner").ConfigureAwait(true);

            await processRunner.Received(1).RunAsync(
                Arg.Is<ProcessRunRequest>(request =>
                    request != null
                    && request.FileName == "git"
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

    /// <summary>
    /// Verifies that <see cref="CloneAgentIsolationStrategy"/> calls git clone using the workspace path
    /// as the working directory. Tests MVP-MCP-005: clone isolation invokes git clone in the workspace root.
    /// </summary>
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
            Microsoft.Extensions.Options.Options.Create(new AgentProcessManagerOptions()),
            NullLogger<CloneAgentIsolationStrategy>.Instance);

        try
        {
            _ = await strategy.PrepareWorkDirectoryAsync(workspacePath, "planner").ConfigureAwait(true);

            await processRunner.Received(1).RunAsync(
                Arg.Is<ProcessRunRequest>(request =>
                    request != null
                    && request.FileName == "git"
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

    /// <summary>
    /// Verifies that <see cref="CloneAgentIsolationStrategy.CleanupAsync(string, string, CancellationToken)"/>
    /// removes clone directories even when Git pack files are marked read-only on Windows.
    /// Tests MVP-MCP-005 runtime cleanup using a clone layout that matches the live stop-path failure.
    /// </summary>
    [Fact]
    public async Task CloneIsolationStrategy_CleanupAsync_RemovesReadOnlyGitArtifacts()
    {
        var workspacePath = Path.Combine(Path.GetTempPath(), $"mcp-clone-cleanup-{Guid.NewGuid():N}");
        var clonePath = Path.Combine(workspacePath, ".agents", "planner-clone");
        var gitPackPath = Path.Combine(clonePath, ".git", "objects", "pack");
        Directory.CreateDirectory(gitPackPath);

        var packIndexPath = Path.Combine(gitPackPath, "pack-test.idx");
        await File.WriteAllTextAsync(packIndexPath, "idx").ConfigureAwait(true);
        File.SetAttributes(packIndexPath, File.GetAttributes(packIndexPath) | FileAttributes.ReadOnly);

        var strategy = new CloneAgentIsolationStrategy(
            Substitute.For<IProcessRunner>(),
            Microsoft.Extensions.Options.Options.Create(new AgentProcessManagerOptions()),
            NullLogger<CloneAgentIsolationStrategy>.Instance);

        try
        {
            await strategy.CleanupAsync(workspacePath, "planner").ConfigureAwait(true);

            Assert.False(Directory.Exists(clonePath));
        }
        finally
        {
            DeleteDirectoryIfPresent(workspacePath);
        }
    }

    /// <summary>
    /// Verifies that <see cref="DirectAgentBranchStrategy"/> calls git rev-parse using the supplied working
    /// directory and returns the branch name from stdout. Tests MVP-MCP-005: direct strategy reads the current branch.
    /// </summary>
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
                request != null
                && request.FileName == "git"
                && request.WorkingDirectory == workDirectory
                && request.Arguments == "rev-parse --abbrev-ref HEAD"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// Verifies that <see cref="FeatureAgentBranchStrategy"/> creates a feature branch, returns its name,
    /// and restores the original branch on finalize. Tests MVP-MCP-005: feature strategy checkout/restore lifecycle.
    /// </summary>
    [Fact]
    public async Task FeatureBranchStrategy_CreatesAndRestoresBranchInSuppliedWorkingDirectory()
    {
        var processRunner = Substitute.For<IProcessRunner>();
        processRunner.RunAsync(
                Arg.Is<ProcessRunRequest>(request => request != null && request.Arguments == "rev-parse --abbrev-ref HEAD"),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProcessRunResult(0, "develop", null)));
        processRunner.RunAsync(
                Arg.Is<ProcessRunRequest>(request => request != null && request.Arguments.StartsWith("checkout -b ", StringComparison.Ordinal)),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProcessRunResult(0, string.Empty, null)));
        processRunner.RunAsync(
                Arg.Is<ProcessRunRequest>(request => request != null && request.Arguments == "checkout \"develop\""),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProcessRunResult(0, string.Empty, null)));

        var workDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "mcp-branch-feature"));
        var strategy = new FeatureAgentBranchStrategy(processRunner, NullLogger<FeatureAgentBranchStrategy>.Instance);

        var branch = await strategy.PrepareBranchAsync(workDirectory, "planner").ConfigureAwait(true);
        Assert.StartsWith("agent/planner/", branch, StringComparison.Ordinal);

        await strategy.FinalizeBranchAsync(workDirectory, "planner").ConfigureAwait(true);

        await processRunner.Received().RunAsync(
            Arg.Is<ProcessRunRequest>(request => request != null && request.WorkingDirectory == workDirectory),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// Verifies that <see cref="MarkerFileService.BuildTemplateContext"/> includes agent-specific additions
    /// when a list of additions is provided. Tests MVP-MCP-005: template context carries per-agent content.
    /// </summary>
    [Fact]
    public void MarkerFileService_BuildTemplateContext_IncludesAgentAdditions()
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

    private static void DeleteDirectoryIfPresent(string path)
    {
        if (!Directory.Exists(path))
            return;

        var root = new DirectoryInfo(path);
        foreach (var entry in root.EnumerateFileSystemInfos(
                     "*",
                     new EnumerationOptions
                     {
                         RecurseSubdirectories = true,
                         AttributesToSkip = 0
                     }))
        {
            if ((entry.Attributes & FileAttributes.ReadOnly) != 0)
                entry.Attributes &= ~FileAttributes.ReadOnly;
        }

        if ((root.Attributes & FileAttributes.ReadOnly) != 0)
            root.Attributes &= ~FileAttributes.ReadOnly;

        Directory.Delete(path, recursive: true);
    }
}
