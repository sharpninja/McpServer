namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 (AC-2): Contract cases and assertions for <see cref="IIntegrationLaneProjects"/>. The expected
/// selection comes from the acceptance criterion: QBrainAi.Support.Mcp.Tests, QBrainAi.McpAgent.Tests,
/// QBrainAi.Repl.Core.Tests, QBrainAi.QBAgent.Tests, Build.Tests and QBrainAi.Repl.IntegrationTests, in that order, and
/// nothing else; QBrainAi.Support.Mcp.IntegrationTests, QBrainAi.Acid.IntegrationTests and
/// QBrainAi.TransactionSecurity.IntegrationTests stay out until PLAN-SESSIONTRIAGEFIX-001 merges (TODO
/// IMPL-TXNINTEGRATION-001), and the .Validation projects, the AI review projects and QBrainAi.PluginIntegration.Tests
/// are not lane projects. The solution row is the 48 project names of QBrainAi.sln as of plan section 22, slice A, in
/// solution order; the binding tests (IntegrationLaneProjectsTests) check that row against the real solution file. The
/// same assertions run against the scripted selector (BDPv4 mock gate), wrong selectors (negative controls), and the
/// production selector.
/// </summary>
internal static class IntegrationLaneProjectsContract
{
    /// <summary>The six lane projects AC-2 names, in run order.</summary>
    internal static readonly string[] LaneProjects =
    [
        "QBrainAi.Support.Mcp.Tests",
        "QBrainAi.McpAgent.Tests",
        "QBrainAi.Repl.Core.Tests",
        "QBrainAi.QBAgent.Tests",
        "Build.Tests",
        "QBrainAi.Repl.IntegrationTests",
    ];

    /// <summary>The integration projects AC-2 keeps out of the lane until PLAN-SESSIONTRIAGEFIX-001 merges.</summary>
    internal static readonly string[] HeldOutProjects =
    [
        "QBrainAi.Support.Mcp.IntegrationTests",
        "QBrainAi.Acid.IntegrationTests",
        "QBrainAi.TransactionSecurity.IntegrationTests",
    ];

    /// <summary>The project names of QBrainAi.sln as of slice A, in solution order.</summary>
    internal static readonly string[] Solution =
    [
        "QBrainAi.Common.AgentCli", "QBrainAi.ServiceDefaults", "QBrainAi.Support.Mcp", "QBrainAi.Support.Mcp.Tests",
        "QBrainAi.Support.Mcp.IntegrationTests", "QBrainAi.Client", "QBrainAi.Client.Tests", "QBrainAi.Cqrs.Mvvm",
        "QBrainAi.Cqrs", "QBrainAi.Cqrs.Tests", "QBrainAi.Launcher", "QBrainAi.Launcher.Tests", "QBrainAi.Storage",
        "QBrainAi.Services", "QBrainAi.GraphRag", "QBrainAi.Context.Validation", "QBrainAi.GitHub.Validation",
        "QBrainAi.Repo.Validation", "QBrainAi.SessionLog.Validation", "QBrainAi.Todo.Validation",
        "QBrainAi.ToolRegistry.Validation", "QBrainAi.Workspace.Validation", "QBrainAi.McpAgent",
        "QBrainAi.McpAgent.SampleHost", "QBrainAi.McpAgent.Tests", "QBrainAi.Storage.SqliteMigrations",
        "QBrainAi.Storage.PostgreSqlMigrations", "QBrainAi.Storage.SqlServerMigrations", "QBrainAi.Repl.Core",
        "QBrainAi.Repl.Host", "QBrainAi.Repl.Core.Tests", "QBrainAi.Repl.IntegrationTests", "_build", "Build.Tests",
        "QBrainAi.PlanReview.Tests", "QBrainAi.TransactionSecurity", "QBrainAi.KeyServer", "QBrainAi.Subscriber",
        "QBrainAi.TransactionSecurity.IntegrationTests", "QBrainAi.Acid.IntegrationTests", "QBrainAi.QBAgent",
        "QBrainAi.QBAgent.Tests", "QBrainAi.QBAgent.Tools", "QBrainAi.QBAgent.Skills", "QBrainAi.Review.Tests",
        "QBrainAi.SessionLog.Transcripts", "QBrainAi.PluginIntegration.Tests", "QBrainAi.ProcessTree.TestHelper",
    ];

    /// <summary>The selection AC-2 requires for <see cref="Solution"/>: the six lane projects, in run order.</summary>
    internal static readonly string[] SolutionSelection = [.. LaneProjects];

    /// <summary>
    /// The contract rows: a row name, the solution's project names, and the selection AC-2 requires: today's solution;
    /// a new .IntegrationTests project, which does not join the lane; a name that holds IntegrationTests without ending
    /// in it; and today's solution in reverse order. Every row selects the same six projects in the same order.
    /// </summary>
    internal static readonly (string Name, string[] Projects, string[] Expected)[] Rows =
    [
        ("the solution as of slice A", Solution, SolutionSelection),
        ("a new IntegrationTests project is not a lane project", [.. Solution, "QBrainAi.Zeta.IntegrationTests"], SolutionSelection),
        ("a name holding IntegrationTests without ending in it", [.. Solution, "QBrainAi.IntegrationTestsSupport"], SolutionSelection),
        ("the solution in reverse order", [.. Enumerable.Reverse(Solution)], SolutionSelection),
    ];

    /// <summary>
    /// Asserts that <paramref name="selector"/> gives the required selection on every row, never selects a held-out
    /// project, and refuses a solution that lacks Build.Tests or QBrainAi.Repl.IntegrationTests, naming the missing one.
    /// </summary>
    /// <param name="selector">Selector under test.</param>
    internal static void AssertContract(IIntegrationLaneProjects selector)
    {
        foreach (var (name, projects, expected) in Rows)
        {
            var actual = selector.Select(projects);
            Assert.True(expected.SequenceEqual(actual), $"Row '{name}': expected [{string.Join(", ", expected)}] but got [{string.Join(", ", actual)}].");
            var heldOut = actual.Intersect(HeldOutProjects, StringComparer.Ordinal).ToArray();
            Assert.True(heldOut.Length == 0, $"Row '{name}': held-out projects selected: [{string.Join(", ", heldOut)}].");
        }

        foreach (var required in new[] { "Build.Tests", "QBrainAi.Repl.IntegrationTests" })
        {
            var without = Solution.Where(project => project != required).ToArray();
            var refusal = Assert.Throws<InvalidOperationException>(() => selector.Select(without));
            Assert.Contains(required, refusal.Message, StringComparison.Ordinal);
        }
    }
}
