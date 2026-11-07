/// <summary>
/// TR-MCP-NUKE-019 AC-2: Production <see cref="IIntegrationLaneProjects"/>. The six lane projects in their run order;
/// from QBrainAi.Support.Mcp.IntegrationTests only the classes RequirementScopeLayerReplIntegrationTests and
/// QuadBrainOllamaEndpointIntegrationTests, by class name (operator 2026-10-29: "Run the 2 classes, AC first
/// (Recommended)"; hostile validation finding R8B-F2); no other project joins the lane (QBrainAi.Acid.IntegrationTests and
/// QBrainAi.TransactionSecurity.IntegrationTests stay out until PLAN-SESSIONTRIAGEFIX-001 merges, TODO
/// IMPL-TXNINTEGRATION-001); a solution that lacks a lane project fails before any test runs.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-07</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: slice S3 of hostile validation finding R14-F4 (operator 2026-11-06: "Per-owner projects (Recommended)"): the integration cases of QBrainAi.McpAgent.Tests, QBrainAi.Repl.Core.Tests and QBrainAi.QBAgent.Tests moved to QBrainAi.McpAgent.IntegrationTests, QBrainAi.Repl.IntegrationTests and QBrainAi.QBAgent.IntegrationTests, so the three unit projects, which would now select no case, leave the lane and the two new integration-owned projects take their run positions, run whole with the lane's Category=Integration and host platform filter; the mock gate ug-r14f4-s3-mockgate-1 ran the changed contract first.</description></item>
/// <item><term>2026-11-07</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: slice S4 of hostile validation finding R14-F4 (operator 2026-11-06: "Move them (Recommended)"): the build's integration cases moved from the infrastructure-owned Build.Tests to the integration-owned Build.IntegrationTests, so Build.Tests, which would now select no case, leaves the lane and Build.IntegrationTests takes its run position, run whole with the lane's Category=Integration and host platform filter and built by the lane (Compile leaves it out); the mock gate ug-r14f4-s4-mockgate-1 ran the changed contract first.</description></item>
/// <item><term>2026-11-07</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: slice S5 of hostile validation finding R14-F4 (operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"): the integration-owned QBrainAi.Support.Mcp.Environment.IntegrationTests, which takes the Category=Integration cases of QBrainAi.Support.Mcp.Tests folder by folder, runs immediately after QBrainAi.Support.Mcp.Tests, whole, with the lane's Category=Integration and host platform filter; QBrainAi.Support.Mcp.Tests keeps its run position until slice S8 empties its integration cases; the mock gate ug-r14f4-s5-mockgate-1 ran the changed contract first.</description></item>
/// <item><term>2026-11-07</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: the project renames that follow slice S6 of hostile validation finding R14-F4 (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name."): QBrainAi.Support.Mcp.Environment.IntegrationTests is now QBrainAi.Support.Mcp.EnvironmentIntegrationTests and Build.IntegrationTests is now NukeBuild.IntegrationTests, each named for its root namespace, in the same run positions; the mock gate ug-r14f4-rn-mockgate-1 ran the changed contract first.</description></item>
/// <item><term>2026-11-07</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: slice S8 of hostile validation finding R14-F4 (operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"): the last Category=Integration cases of QBrainAi.Support.Mcp.Tests moved to QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned QBrainAi.Support.Mcp.Tests, which would now select no case, leaves the lane; the six integration-owned projects keep their run order; the mock gate ug-r14f4-s8-mockgate-1 ran the changed contract first.</description></item>
/// </list>
/// </remarks>
sealed class IntegrationLaneProjects : IIntegrationLaneProjects
{
    const string SupportIntegrationProject = "QBrainAi.Support.Mcp.IntegrationTests";

    static readonly string[] LaneProjects =
    [
        "QBrainAi.Support.Mcp.EnvironmentIntegrationTests",
        "QBrainAi.McpAgent.IntegrationTests",
        "QBrainAi.QBAgent.IntegrationTests",
        "NukeBuild.IntegrationTests",
        "QBrainAi.Repl.IntegrationTests",
        SupportIntegrationProject,
    ];

    static readonly string[] SupportIntegrationClasses =
    [
        "RequirementScopeLayerReplIntegrationTests",
        "QuadBrainOllamaEndpointIntegrationTests",
    ];

    /// <inheritdoc />
    public IReadOnlyList<string> Select(IReadOnlyList<string> solutionProjectNames)
    {
        ArgumentNullException.ThrowIfNull(solutionProjectNames);
        var missing = LaneProjects.Where(project => !solutionProjectNames.Contains(project, StringComparer.Ordinal)).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException("MigrationIntegrationTests needs lane projects the solution lacks: " + string.Join(", ", missing));

        return [.. LaneProjects];
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ClassesOf(string projectName)
        => string.Equals(projectName, SupportIntegrationProject, StringComparison.Ordinal) ? [.. SupportIntegrationClasses] : [];
}

/// <summary>
/// TR-MCP-NUKE-019 AC-7: the build's test-project role declarations, the one place that says which role owns each test
/// project (declared by the build, never in requirement text), next to the lane composition above. Unit-owned: the seven
/// projects Test runs. Integration-owned: QBrainAi.Support.Mcp.IntegrationTests, QBrainAi.Repl.IntegrationTests, the
/// two projects the lane holds out (QBrainAi.TransactionSecurity.IntegrationTests, QBrainAi.Acid.IntegrationTests),
/// QBrainAi.McpAgent.IntegrationTests, QBrainAi.QBAgent.IntegrationTests, NukeBuild.IntegrationTests and
/// QBrainAi.Support.Mcp.EnvironmentIntegrationTests. Infrastructure-owned: Build.Tests. Not declared, so offered to neither selection: the .Validation projects, the AI
/// review projects and QBrainAi.PluginIntegration.Tests, which has its own target. The Test and MigrationIntegrationTests
/// targets consult these declarations (<see cref="RequireRole"/>, <see cref="RoleOf"/>); <see cref="TestProjectOwnership"/>
/// judges a selection from them and a supplied case inventory.
/// </summary>
/// <remarks>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added in slice S2 for hostile validation finding R14-F4 (operator 2026-11-06: "Separate projects + AC (Recommended)"; survey: project ownership is decided by the tests' categories and the build). The declarations describe the layout as of da11faa2; slices S3 to S8 add the new integration-owned projects as they create them.</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S3 declares QBrainAi.McpAgent.IntegrationTests and QBrainAi.QBAgent.IntegrationTests integration-owned (appended in solution order), the projects that took the integration cases of the MCP agent and QBAgent test projects.</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S4 declares Build.IntegrationTests integration-owned (appended in solution order), the project that took the build's integration cases; Build.Tests stays infrastructure-owned.</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: slice S5 declares QBrainAi.Support.Mcp.Environment.IntegrationTests integration-owned (appended in solution order), the project that takes the integration cases of QBrainAi.Support.Mcp.Tests; QBrainAi.Support.Mcp.Tests stays unit-owned.</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the project renames that follow slice S6 (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name."): the integration-owned Build.IntegrationTests and QBrainAi.Support.Mcp.Environment.IntegrationTests are declared under their new names, NukeBuild.IntegrationTests and QBrainAi.Support.Mcp.EnvironmentIntegrationTests, with the same role and position.</para>
/// </remarks>
static class TestProjectRoles
{
    /// <summary>The declarations, in solution order within each role.</summary>
    /// <remarks>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: two integration-owned projects added (slice S3).</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: Build.IntegrationTests added (slice S4).</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: QBrainAi.Support.Mcp.Environment.IntegrationTests added (slice S5).</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: Build.IntegrationTests and QBrainAi.Support.Mcp.Environment.IntegrationTests renamed in place to NukeBuild.IntegrationTests and QBrainAi.Support.Mcp.EnvironmentIntegrationTests (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name.").</para>
    /// </remarks>
    static readonly (string Project, TestProjectRole Role)[] Declarations =
    [
        ("QBrainAi.Support.Mcp.Tests", TestProjectRole.Unit),
        ("QBrainAi.Client.Tests", TestProjectRole.Unit),
        ("QBrainAi.Cqrs.Tests", TestProjectRole.Unit),
        ("QBrainAi.Launcher.Tests", TestProjectRole.Unit),
        ("QBrainAi.McpAgent.Tests", TestProjectRole.Unit),
        ("QBrainAi.Repl.Core.Tests", TestProjectRole.Unit),
        ("QBrainAi.QBAgent.Tests", TestProjectRole.Unit),
        ("QBrainAi.Support.Mcp.IntegrationTests", TestProjectRole.Integration),
        ("QBrainAi.Repl.IntegrationTests", TestProjectRole.Integration),
        ("QBrainAi.TransactionSecurity.IntegrationTests", TestProjectRole.Integration),
        ("QBrainAi.Acid.IntegrationTests", TestProjectRole.Integration),
        ("QBrainAi.McpAgent.IntegrationTests", TestProjectRole.Integration),
        ("QBrainAi.QBAgent.IntegrationTests", TestProjectRole.Integration),
        ("NukeBuild.IntegrationTests", TestProjectRole.Integration),
        ("QBrainAi.Support.Mcp.EnvironmentIntegrationTests", TestProjectRole.Integration),
        ("Build.Tests", TestProjectRole.Infrastructure),
    ];

    /// <summary>TR-MCP-NUKE-019 AC-7: every declared project and its role.</summary>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added.</remarks>
    internal static IReadOnlyList<(string Project, TestProjectRole Role)> Declared => Declarations;

    /// <summary>TR-MCP-NUKE-019 AC-7: the declared role of <paramref name="projectName"/>.</summary>
    /// <param name="projectName">A test project name.</param>
    /// <returns>The declared role.</returns>
    /// <exception cref="InvalidOperationException">The build declares no role for the project; the message names it.</exception>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added; MigrationIntegrationTests logs each lane project's role through it.</remarks>
    internal static TestProjectRole RoleOf(string projectName)
    {
        ArgumentNullException.ThrowIfNull(projectName);
        foreach (var (project, role) in Declarations)
        {
            if (string.Equals(project, projectName, StringComparison.Ordinal))
                return role;
        }

        throw new InvalidOperationException($"The build declares no test-project role for '{projectName}'; declare it in TestProjectRoles (build/IntegrationLaneProjects.cs).");
    }

    /// <summary>
    /// TR-MCP-NUKE-019 AC-7: refuses, before any test runs, a selection whose projects are not all declared with
    /// <paramref name="role"/>, naming each such project and its declared role (or none). Test calls it with the unit role
    /// over the projects its name rules select, so a test project its rules pick up but the build does not declare
    /// unit-owned fails Test instead of running as a unit project.
    /// </summary>
    /// <param name="role">The role every selected project must be declared with.</param>
    /// <param name="selectedProjects">The selected project names.</param>
    /// <param name="selection">The target or selection named in the message.</param>
    /// <exception cref="InvalidOperationException">A selected project is undeclared or declared with another role.</exception>
    /// <remarks>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: added; at the layout of da11faa2 the seven projects Test selects are all declared unit-owned, so no case changes.</remarks>
    internal static void RequireRole(TestProjectRole role, IReadOnlyList<string> selectedProjects, string selection)
    {
        ArgumentNullException.ThrowIfNull(selectedProjects);
        var wrong = new List<string>();
        foreach (var name in selectedProjects)
        {
            var declared = Declarations.Where(entry => string.Equals(entry.Project, name, StringComparison.Ordinal)).Select(entry => (TestProjectRole?)entry.Role).FirstOrDefault();
            if (declared != role)
                wrong.Add($"{name} (declared {(declared is null ? "no role" : declared.ToString())})");
        }

        if (wrong.Count > 0)
            throw new InvalidOperationException($"{selection} selects test projects the build does not declare {role}-owned: {string.Join(", ", wrong)}; declare each in TestProjectRoles (build/IntegrationLaneProjects.cs).");
    }
}
