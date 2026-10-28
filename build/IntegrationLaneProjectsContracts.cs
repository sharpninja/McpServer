/// <summary>
/// TR-MCP-NUKE-019 AC-2: Selects the projects MigrationIntegrationTests runs from the solution's project names:
/// QBrainAi.Support.Mcp.Tests, QBrainAi.McpAgent.Tests, QBrainAi.Repl.Core.Tests, QBrainAi.QBAgent.Tests, Build.Tests
/// and QBrainAi.Repl.IntegrationTests, in that order. QBrainAi.Support.Mcp.IntegrationTests,
/// QBrainAi.Acid.IntegrationTests and QBrainAi.TransactionSecurity.IntegrationTests are not lane projects until
/// PLAN-SESSIONTRIAGEFIX-001 merges (TODO IMPL-TXNINTEGRATION-001); the .Validation projects, the AI review projects and
/// QBrainAi.PluginIntegration.Tests, which has its own target, are not lane projects.
/// </summary>
interface IIntegrationLaneProjects
{
    /// <summary>
    /// Returns the lane's projects, in run order, from <paramref name="solutionProjectNames"/>; throws
    /// <see cref="InvalidOperationException"/> naming any lane project the solution lacks.
    /// </summary>
    /// <param name="solutionProjectNames">The names of the solution's projects, in any order.</param>
    IReadOnlyList<string> Select(IReadOnlyList<string> solutionProjectNames);
}
