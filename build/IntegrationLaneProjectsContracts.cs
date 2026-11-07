/// <summary>
/// TR-MCP-NUKE-019 AC-2: Selects the projects MigrationIntegrationTests runs from the solution's project names:
/// QBrainAi.Support.Mcp.Tests, QBrainAi.McpAgent.IntegrationTests, QBrainAi.QBAgent.IntegrationTests,
/// Build.IntegrationTests, QBrainAi.Repl.IntegrationTests and QBrainAi.Support.Mcp.IntegrationTests, in that order, and
/// names the classes the
/// lane runs from a project: from QBrainAi.Support.Mcp.IntegrationTests only RequirementScopeLayerReplIntegrationTests
/// and QuadBrainOllamaEndpointIntegrationTests, every class of the others. QBrainAi.Acid.IntegrationTests and
/// QBrainAi.TransactionSecurity.IntegrationTests are not lane projects until PLAN-SESSIONTRIAGEFIX-001 merges (TODO
/// IMPL-TXNINTEGRATION-001); the .Validation projects, the AI review projects and QBrainAi.PluginIntegration.Tests, which
/// has its own target, and the build's infrastructure tests (Build.Tests) are not lane projects.
/// </summary>
/// <remarks>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: the named selection follows slice S3 of hostile validation finding R14-F4 (the MCP agent and QBAgent integration-owned projects replace the three unit projects whose integration cases moved); the contract is unchanged.</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: the named selection follows slice S4 (Build.IntegrationTests replaces Build.Tests); the contract is unchanged.</para>
/// </remarks>
interface IIntegrationLaneProjects
{
    /// <summary>
    /// Returns the lane's projects, in run order, from <paramref name="solutionProjectNames"/>; throws
    /// <see cref="InvalidOperationException"/> naming any lane project the solution lacks.
    /// </summary>
    /// <param name="solutionProjectNames">The names of the solution's projects, in any order.</param>
    IReadOnlyList<string> Select(IReadOnlyList<string> solutionProjectNames);

    /// <summary>
    /// TR-MCP-NUKE-019 AC-2: Returns the classes of <paramref name="projectName"/> whose Category=Integration cases the
    /// lane runs, selected by class name, or an empty list when the lane runs the project's cases of every class.
    /// </summary>
    /// <param name="projectName">A lane project's name.</param>
    IReadOnlyList<string> ClassesOf(string projectName);
}
