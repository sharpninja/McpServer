namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 (AC-2, AC-7): Contract cases and assertions for <see cref="IIntegrationLaneProjects"/>. The expected
/// selection comes from the acceptance criterion as replaced on 2026-10-29 (operator: "Run the 2 classes, AC first
/// (Recommended)"; hostile validation finding R8B-F2), with the integration cases of the MCP agent, REPL core and
/// QBAgent test projects moved into integration-owned projects (AC-7, slice S3), the build's integration cases moved
/// out of the infrastructure-owned Build.Tests (AC-7, slice S4) and the integration-owned
/// QBrainAi.Support.Mcp.EnvironmentIntegrationTests run after QBrainAi.Support.Mcp.Tests (AC-7, slice S5):
/// QBrainAi.Support.Mcp.Tests, QBrainAi.Support.Mcp.EnvironmentIntegrationTests, QBrainAi.McpAgent.IntegrationTests,
/// QBrainAi.QBAgent.IntegrationTests, NukeBuild.IntegrationTests, QBrainAi.Repl.IntegrationTests and
/// QBrainAi.Support.Mcp.IntegrationTests, in that order, and nothing else; from
/// QBrainAi.Support.Mcp.IntegrationTests only the classes RequirementScopeLayerReplIntegrationTests and
/// QuadBrainOllamaEndpointIntegrationTests, by class name, and every class of the other six; QBrainAi.Acid.IntegrationTests
/// and QBrainAi.TransactionSecurity.IntegrationTests stay out until PLAN-SESSIONTRIAGEFIX-001 merges (TODO
/// IMPL-TXNINTEGRATION-001), and the .Validation projects, the AI review projects, QBrainAi.PluginIntegration.Tests and
/// Build.Tests are not lane projects. The solution row is the 52 project names of QBrainAi.sln after the project renames
/// that follow slice S6 of plan section 27.45, in solution order; the integration half of IntegrationLaneProjectsTests
/// (NukeBuild.IntegrationTests) checks that row against the real solution file and the two class names against the
/// project's sources. The same assertions run against the scripted selector (BDPv4 mock gate), wrong selectors (negative
/// controls), and the production selector.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-07</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: slice S3 of hostile validation finding R14-F4 (operator 2026-11-06: "Per-owner projects (Recommended)"): the lane's QBrainAi.McpAgent.Tests, QBrainAi.Repl.Core.Tests and QBrainAi.QBAgent.Tests, which no longer hold an integration case, give way to QBrainAi.McpAgent.IntegrationTests and QBrainAi.QBAgent.IntegrationTests in their run positions (the REPL core cases joined QBrainAi.Repl.IntegrationTests); the solution row gains the two new projects (48 to 50 names).</description></item>
/// <item><term>2026-11-07</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: slice S4 of hostile validation finding R14-F4 (operator 2026-11-06: "Move them (Recommended)"): the infrastructure-owned Build.Tests, which no longer holds an integration case, gives way to the integration-owned Build.IntegrationTests in its run position; the solution row gains Build.IntegrationTests (50 to 51 names); this file is linked into Build.IntegrationTests, whose integration half of IntegrationLaneProjectsTests reads the real solution file.</description></item>
/// <item><term>2026-11-07</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: slice S5 of hostile validation finding R14-F4 (operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"): the integration-owned QBrainAi.Support.Mcp.Environment.IntegrationTests, which takes the Category=Integration cases of QBrainAi.Support.Mcp.Tests folder by folder, joins the lane immediately after QBrainAi.Support.Mcp.Tests, run whole with the lane's Category=Integration and host platform filter; QBrainAi.Support.Mcp.Tests stays in the lane until slice S8 empties its integration cases; the solution row gains the new project (51 to 52 names). Changed before the production selector, with the negative control for the lane of slice S4 (BDPv4 mock gate ug-r14f4-s5-mockgate-1).</description></item>
/// <item><term>2026-11-07</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: the project renames that follow slice S6 of hostile validation finding R14-F4 (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name."): QBrainAi.Support.Mcp.Environment.IntegrationTests becomes QBrainAi.Support.Mcp.EnvironmentIntegrationTests and Build.IntegrationTests becomes NukeBuild.IntegrationTests, each the name of its root namespace, in the lane and in the solution row, each in the same position; the row still holds 52 names and the assertions are unchanged. Changed before the production selector, with the negative control for the lane before the renames (BDPv4 mock gate ug-r14f4-rn-mockgate-1).</description></item>
/// </list>
/// </remarks>
internal static class IntegrationLaneProjectsContract
{
    /// <summary>The project from which AC-2 runs two classes only.</summary>
    internal const string SupportIntegrationProject = "QBrainAi.Support.Mcp.IntegrationTests";

    /// <summary>The seven lane projects, in run order.</summary>
    /// <remarks>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: QBrainAi.McpAgent.IntegrationTests and QBrainAi.QBAgent.IntegrationTests replace the three unit projects whose integration cases moved (slice S3).</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: Build.IntegrationTests replaces Build.Tests, whose integration cases moved (slice S4).</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: QBrainAi.Support.Mcp.Environment.IntegrationTests added immediately after QBrainAi.Support.Mcp.Tests, which keeps its run position while it still holds integration cases (slice S5).</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: QBrainAi.Support.Mcp.EnvironmentIntegrationTests and NukeBuild.IntegrationTests are QBrainAi.Support.Mcp.Environment.IntegrationTests and Build.IntegrationTests renamed to their root namespaces, in the same run positions (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name.").</para>
    /// </remarks>
    internal static readonly string[] LaneProjects =
    [
        "QBrainAi.Support.Mcp.Tests",
        "QBrainAi.Support.Mcp.EnvironmentIntegrationTests",
        "QBrainAi.McpAgent.IntegrationTests",
        "QBrainAi.QBAgent.IntegrationTests",
        "NukeBuild.IntegrationTests",
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

    /// <summary>The project names of QBrainAi.sln after the project renames that follow slice S6, in solution order.</summary>
    /// <remarks>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: the two integration-owned projects of slice S3 appended, in the order the solution lists them.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: Build.IntegrationTests appended (slice S4).</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: QBrainAi.Support.Mcp.Environment.IntegrationTests appended (slice S5).</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: Build.IntegrationTests and QBrainAi.Support.Mcp.Environment.IntegrationTests renamed in place to NukeBuild.IntegrationTests and QBrainAi.Support.Mcp.EnvironmentIntegrationTests, their root namespaces (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name."); still 52 names in the same order.</para>
    /// </remarks>
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
        "QBrainAi.McpAgent.IntegrationTests", "QBrainAi.QBAgent.IntegrationTests", "NukeBuild.IntegrationTests",
        "QBrainAi.Support.Mcp.EnvironmentIntegrationTests",
    ];

    /// <summary>The selection AC-2 and AC-7 require for <see cref="Solution"/>: the seven lane projects, in run order.</summary>
    internal static readonly string[] SolutionSelection = [.. LaneProjects];

    /// <summary>
    /// The contract rows: a row name, the solution's project names, and the selection AC-2 requires: today's solution;
    /// a new .IntegrationTests project, which does not join the lane; a name that holds IntegrationTests without ending
    /// in it; and today's solution in reverse order. Every row selects the same seven projects in the same order.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: the first row names the solution as of slice S3; the rows are otherwise unchanged.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: the first row names the solution as of slice S4; the rows are otherwise unchanged.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: the first row names the solution as of slice S5; the rows are otherwise unchanged.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: the first row names the solution after the project renames (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name."); the rows are otherwise unchanged.</para>
    /// </remarks>
    internal static readonly (string Name, string[] Projects, string[] Expected)[] Rows =
    [
        ("the solution after the project renames", Solution, SolutionSelection),
        ("a new IntegrationTests project is not a lane project", [.. Solution, "QBrainAi.Zeta.IntegrationTests"], SolutionSelection),
        ("a name holding IntegrationTests without ending in it", [.. Solution, "QBrainAi.IntegrationTestsSupport"], SolutionSelection),
        ("the solution in reverse order", [.. Enumerable.Reverse(Solution)], SolutionSelection),
    ];

    /// <summary>
    /// Asserts that <paramref name="selector"/> gives the required selection on every row, never selects a held-out
    /// project, names exactly <see cref="SupportIntegrationClasses"/> for <see cref="SupportIntegrationProject"/> and no
    /// class for any other lane project, and refuses a solution that lacks any of the lane projects,
    /// naming the missing one.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: unchanged assertions; the documentation no longer counts the lane projects, which slice S3 changed from seven to six.</remarks>
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
