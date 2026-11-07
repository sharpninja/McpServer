namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 (AC-2): Contract cases and assertions for <see cref="IIntegrationLaneProjects"/>. The expected
/// selection comes from the acceptance criterion as replaced on 2026-10-29 (operator: "Run the 2 classes, AC first
/// (Recommended)"; hostile validation finding R8B-F2): QBrainAi.Support.Mcp.Tests, QBrainAi.McpAgent.Tests,
/// QBrainAi.Repl.Core.Tests, QBrainAi.QBAgent.Tests, Build.Tests, QBrainAi.Repl.IntegrationTests and
/// QBrainAi.Support.Mcp.IntegrationTests, in that order, and nothing else; from QBrainAi.Support.Mcp.IntegrationTests
/// only the classes RequirementScopeLayerReplIntegrationTests and QuadBrainOllamaEndpointIntegrationTests, by class
/// name, and every class of the other six; QBrainAi.Acid.IntegrationTests and
/// QBrainAi.TransactionSecurity.IntegrationTests stay out until PLAN-SESSIONTRIAGEFIX-001 merges (TODO
/// IMPL-TXNINTEGRATION-001), and the .Validation projects, the AI review projects and QBrainAi.PluginIntegration.Tests
/// are not lane projects. The solution row is the 48 project names of QBrainAi.sln as of plan section 22, slice A, in
/// solution order; the binding tests (IntegrationLaneProjectsTests) check that row against the real solution file and
/// the two class names against the project's sources. The same assertions run against the scripted selector (BDPv4
/// mock gate), wrong selectors (negative controls), and the production selector.
/// </summary>
internal static class IntegrationLaneProjectsContract
{
    /// <summary>The project from which AC-2 runs two classes only.</summary>
    internal const string SupportIntegrationProject = "QBrainAi.Support.Mcp.IntegrationTests";

    /// <summary>The seven lane projects AC-2 names, in run order.</summary>
    internal static readonly string[] LaneProjects =
    [
        "QBrainAi.Support.Mcp.Tests",
        "QBrainAi.McpAgent.Tests",
        "QBrainAi.Repl.Core.Tests",
        "QBrainAi.QBAgent.Tests",
        "Build.Tests",
        "QBrainAi.Repl.IntegrationTests",
        SupportIntegrationProject,
    ];

    /// <summary>The classes AC-2 runs from <see cref="SupportIntegrationProject"/>, in its order.</summary>
    internal static readonly string[] SupportIntegrationClasses = ["RequirementScopeLayerReplIntegrationTests", "QuadBrainOllamaEndpointIntegrationTests"];

    /// <summary>The integration projects AC-2 keeps out of the lane until PLAN-SESSIONTRIAGEFIX-001 merges.</summary>
    internal static readonly string[] HeldOutProjects =
    [
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

    /// <summary>The selection AC-2 requires for <see cref="Solution"/>: the seven lane projects, in run order.</summary>
    internal static readonly string[] SolutionSelection = [.. LaneProjects];

    /// <summary>
    /// The contract rows: a row name, the solution's project names, and the selection AC-2 requires: today's solution;
    /// a new .IntegrationTests project, which does not join the lane; a name that holds IntegrationTests without ending
    /// in it; and today's solution in reverse order. Every row selects the same seven projects in the same order.
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
    /// project, names exactly <see cref="SupportIntegrationClasses"/> for <see cref="SupportIntegrationProject"/> and no
    /// class for any other lane project, and refuses a solution that lacks any of the seven lane projects,
    /// naming the missing one.
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

        foreach (var project in LaneProjects)
        {
            string[] expected = project == SupportIntegrationProject ? SupportIntegrationClasses : [];
            var actual = selector.ClassesOf(project);
            Assert.True(expected.SequenceEqual(actual), $"Project '{project}': expected classes [{string.Join(", ", expected)}] but got [{string.Join(", ", actual)}].");
        }

        foreach (var required in LaneProjects)
        {
            var without = Solution.Where(project => project != required).ToArray();
            var refusal = Assert.Throws<InvalidOperationException>(() => selector.Select(without));
            Assert.Contains(required, refusal.Message, StringComparison.Ordinal);
        }
    }
}
