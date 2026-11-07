using NSubstitute;
using QBrainAi.Client;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Tests.Storage;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// FR-MCP-FED-PATH-001 AC5 contract (operator 2026-10-29: "gh foreign-workspace AC (Recommended)"; holistic review findings SRC-1,
/// SRC-4 and SRC-5): how a GitHub CLI operation is planned for the active workspace. A workspace whose path belongs to the other
/// platform than the host never starts gh unless a repository is configured: without one the plan is a refusal that names the
/// workspace and the Mcp:GitHub:Repository setting; with one, gh runs with that repository and no working directory. A host-native
/// workspace keeps its working directory and its repository (the configured one, else the one its git configuration names). Unit
/// tests over simulated Linux and Windows hosts (the production normalizer over NSubstitute host doubles), so no row depends on the
/// running host.
/// </summary>
public sealed class GitHubCliInvocationPlanContractTests
{
    /// <summary>The configured repository the rows use.</summary>
    internal const string ConfiguredRepository = "owner/repo";

    /// <summary>The repository the scripted git configuration names for a native workspace.</summary>
    internal const string GitConfigRepository = "git/config";

    /// <summary>The setting a refusal names.</summary>
    internal const string RepositorySetting = "Mcp:GitHub:Repository";

    /// <summary>
    /// FR-MCP-FED-PATH-001 AC5 mock gate: every row holds for the scripted reference planner. Data: hand-written workspace paths and
    /// repository settings on simulated hosts; no process starts.
    /// </summary>
    [Fact]
    public void ScriptedReferencePlanner_SatisfiesTheContract()
    {
        foreach (var row in Rows)
            AssertPlan(row, ScriptedPlanner(row.WorkspacePath, row.ConfiguredRepository, SimulatedWorkspaceHosts.Real(row.Host), ScriptedGitConfig(row)));
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001 AC5 negative controls inside a passing harness: a planner that starts gh for a foreign workspace with no
    /// repository (the SRC-1 defect), one that drops the repository for a foreign workspace, one that refuses native workspaces,
    /// one whose refusal does not name the setting, and one that gives a foreign workspace a working directory are each rejected by
    /// at least one row. Data: the same rows.
    /// </summary>
    [Fact]
    public void WrongPlanners_AreRejectedInsidePassingHarness()
    {
        Planner[] wrong =
        [
            (workspace, configured, normalizer, gitConfig) =>
                IsForeign(workspace, normalizer) ? new GhPlan(false, null, null, configured) : ScriptedPlanner(workspace, configured, normalizer, gitConfig),
            (workspace, configured, normalizer, gitConfig) =>
                IsForeign(workspace, normalizer) && configured is not null ? new GhPlan(false, null, null, null) : ScriptedPlanner(workspace, configured, normalizer, gitConfig),
            (workspace, configured, normalizer, gitConfig) =>
                string.IsNullOrWhiteSpace(workspace) ? ScriptedPlanner(workspace, configured, normalizer, gitConfig) : new GhPlan(true, $"Refused '{workspace}': no {RepositorySetting}.", null, null),
            (workspace, configured, normalizer, gitConfig) =>
                ScriptedPlanner(workspace, configured, normalizer, gitConfig) is { Refused: true } refused
                    ? refused with { Refusal = $"GitHub CLI operation refused for workspace '{workspace}'." }
                    : ScriptedPlanner(workspace, configured, normalizer, gitConfig),
            (workspace, configured, normalizer, gitConfig) =>
                IsForeign(workspace, normalizer) && configured is not null ? new GhPlan(false, null, workspace, configured) : ScriptedPlanner(workspace, configured, normalizer, gitConfig),
        ];

        foreach (var planner in wrong)
        {
            Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            {
                foreach (var row in Rows)
                    AssertPlan(row, planner(row.WorkspacePath, row.ConfiguredRepository, SimulatedWorkspaceHosts.Real(row.Host), ScriptedGitConfig(row)));
            });
        }
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001 AC5 binding: every row holds for the production planner, GitHubCliService.PlanInvocation. Data: the same
    /// rows on the same simulated hosts.
    /// </summary>
    [Fact]
    public void ProductionPlanner_SatisfiesTheContract()
    {
        foreach (var row in Rows)
        {
            var plan = GitHubCliService.PlanInvocation(row.WorkspacePath, row.ConfiguredRepository, SimulatedWorkspaceHosts.Real(row.Host), ScriptedGitConfig(row));
            AssertPlan(row, new GhPlan(plan.Refused, plan.Refusal, plan.WorkingDirectory, plan.Repository));
        }
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001 AC5 binding through the service on a simulated host: for a foreign-platform workspace with no configured
    /// repository, ListIssuesAsync never starts gh and fails with a message that names the workspace and Mcp:GitHub:Repository.
    /// Data: a Windows drive workspace on the simulated Linux hub and a POSIX workspace on the simulated Windows host.
    /// </summary>
    /// <param name="host">The simulated host.</param>
    /// <param name="foreignWorkspace">A workspace path of the other platform.</param>
    [Theory]
    [InlineData(WorkspacePathPlatform.CaseSensitive, @"C:\foreign\workspace")]
    [InlineData(WorkspacePathPlatform.Windows, "/srv/foreign/workspace")]
    public async Task ListIssuesAsync_ForeignWorkspaceWithoutRepository_DoesNotStartGh(WorkspacePathPlatform host, string foreignWorkspace)
    {
        var processRunner = NSubstitute.Substitute.For<IProcessRunner>();
        var workspaceAccessor = TestWorkspaceAccessorHelper.Create(NSubstitute.Substitute.For<ITodoService>(), repoRoot: foreignWorkspace);
        var sut = new GitHubCliService(
            processRunner,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<GitHubCliService>.Instance,
            workspaceAccessor: workspaceAccessor,
            pathNormalizer: SimulatedWorkspaceHosts.Real(host));

        var result = await sut.ListIssuesAsync("open", 5, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Contains(workspaceAccessor.GetWorkspacePath()!, result.Error, StringComparison.Ordinal);
        Assert.Contains(RepositorySetting, result.Error, StringComparison.Ordinal);
        Assert.Empty(processRunner.ReceivedCalls());
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001 AC5 binding through the service on a simulated host: for a foreign-platform workspace with a configured
    /// repository, gh runs with --repo and no working directory. Data: the configured repository "owner/repo" on both simulated hosts.
    /// </summary>
    /// <param name="host">The simulated host.</param>
    /// <param name="foreignWorkspace">A workspace path of the other platform.</param>
    [Theory]
    [InlineData(WorkspacePathPlatform.CaseSensitive, @"C:\foreign\workspace")]
    [InlineData(WorkspacePathPlatform.Windows, "/srv/foreign/workspace")]
    public async Task ListIssuesAsync_ForeignWorkspaceWithRepository_RunsWithRepoAndNoWorkingDirectory(WorkspacePathPlatform host, string foreignWorkspace)
    {
        var processRunner = NSubstitute.Substitute.For<IProcessRunner>();
        processRunner.RunAsync("gh", NSubstitute.Arg.Any<string>(), NSubstitute.Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "[]", null));
        var options = NSubstitute.Substitute.For<Microsoft.Extensions.Options.IOptionsMonitor<QBrainAi.Support.Mcp.Options.GitHubIntegrationOptions>>();
        options.CurrentValue.Returns(new QBrainAi.Support.Mcp.Options.GitHubIntegrationOptions { Repository = ConfiguredRepository });
        var workspaceAccessor = TestWorkspaceAccessorHelper.Create(NSubstitute.Substitute.For<ITodoService>(), repoRoot: foreignWorkspace);
        var sut = new GitHubCliService(
            processRunner,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<GitHubCliService>.Instance,
            githubOptions: options,
            workspaceAccessor: workspaceAccessor,
            pathNormalizer: SimulatedWorkspaceHosts.Real(host));

        var result = await sut.ListIssuesAsync("open", 5, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        await processRunner.Received(1).RunAsync(
            "gh",
            NSubstitute.Arg.Is<string>(arguments => arguments != null && arguments.Contains("--repo " + ConfiguredRepository, StringComparison.Ordinal)),
            NSubstitute.Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await processRunner.DidNotReceive().RunAsync(NSubstitute.Arg.Any<ProcessRunRequest>(), NSubstitute.Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>A planned GitHub CLI invocation: a refusal with its message, or a run with its working directory and repository.</summary>
    internal sealed record GhPlan(bool Refused, string? Refusal, string? WorkingDirectory, string? Repository);

    /// <summary>A planner under the contract: workspace path, configured repository, path normalizer, git-configuration lookup.</summary>
    internal delegate GhPlan Planner(string? workspacePath, string? configuredRepository, IWorkspacePathNormalizer normalizer, Func<string, string?> gitConfigRepository);

    /// <summary>One contract row: the host, the workspace, the configured repository, what git configuration names, and the plan.</summary>
    internal sealed record Row(
        WorkspacePathPlatform Host,
        string? WorkspacePath,
        string? ConfiguredRepository,
        string? GitConfigNames,
        GhPlan Expected);

    /// <summary>FR-MCP-FED-PATH-001 AC5 rows over a simulated Linux hub and a simulated Windows host.</summary>
    internal static Row[] Rows { get; } =
    [
        new(WorkspacePathPlatform.CaseSensitive, @"C:\foreign\workspace", null, null, new GhPlan(true, null, null, null)),
        new(WorkspacePathPlatform.CaseSensitive, @"C:\foreign\workspace", ConfiguredRepository, null, new GhPlan(false, null, null, ConfiguredRepository)),
        new(WorkspacePathPlatform.CaseSensitive, "/srv/ws", null, GitConfigRepository, new GhPlan(false, null, "/srv/ws", GitConfigRepository)),
        new(WorkspacePathPlatform.CaseSensitive, "/srv/ws", ConfiguredRepository, GitConfigRepository, new GhPlan(false, null, "/srv/ws", ConfiguredRepository)),
        new(WorkspacePathPlatform.CaseSensitive, null, null, null, new GhPlan(false, null, null, null)),
        new(WorkspacePathPlatform.Windows, "/srv/foreign/workspace", null, null, new GhPlan(true, null, null, null)),
        new(WorkspacePathPlatform.Windows, "/srv/foreign/workspace", ConfiguredRepository, null, new GhPlan(false, null, null, ConfiguredRepository)),
        new(WorkspacePathPlatform.Windows, @"C:\ws", null, null, new GhPlan(false, null, @"C:\ws", null)),
    ];

    /// <summary>
    /// FR-MCP-FED-PATH-001 AC5: a refusal names the workspace and the setting and plans nothing to run; a run matches the row's
    /// working directory and repository exactly.
    /// </summary>
    /// <param name="row">The contract row.</param>
    /// <param name="plan">The planner's answer.</param>
    internal static void AssertPlan(Row row, GhPlan plan)
    {
        Assert.Equal(row.Expected.Refused, plan.Refused);
        if (row.Expected.Refused)
        {
            Assert.NotNull(plan.Refusal);
            Assert.Contains(row.WorkspacePath!, plan.Refusal, StringComparison.Ordinal);
            Assert.Contains(RepositorySetting, plan.Refusal, StringComparison.Ordinal);
            Assert.Null(plan.WorkingDirectory);
            Assert.Null(plan.Repository);
            return;
        }

        Assert.Null(plan.Refusal);
        Assert.Equal(row.Expected.WorkingDirectory, plan.WorkingDirectory);
        Assert.Equal(row.Expected.Repository, plan.Repository);
    }

    /// <summary>The scripted git-configuration lookup for a row: the row's repository for its own workspace, nothing otherwise.</summary>
    /// <param name="row">The contract row.</param>
    /// <returns>The lookup.</returns>
    internal static Func<string, string?> ScriptedGitConfig(Row row) =>
        path => string.Equals(path, row.WorkspacePath, StringComparison.Ordinal) ? row.GitConfigNames : null;

    private static bool IsForeign(string? workspace, IWorkspacePathNormalizer normalizer) =>
        !string.IsNullOrWhiteSpace(workspace) && !normalizer.IsHostNative(workspace);

    private static GhPlan ScriptedPlanner(string? workspace, string? configured, IWorkspacePathNormalizer normalizer, Func<string, string?> gitConfig)
    {
        if (string.IsNullOrWhiteSpace(workspace))
            return new GhPlan(false, null, null, configured);
        if (!normalizer.IsHostNative(workspace))
        {
            return configured is null
                ? new GhPlan(true, $"GitHub CLI operation refused: workspace '{workspace}' belongs to another platform and no {RepositorySetting} is configured.", null, null)
                : new GhPlan(false, null, null, configured);
        }

        return new GhPlan(false, null, workspace, configured ?? gitConfig(workspace));
    }
}
