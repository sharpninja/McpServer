/// <summary>
/// TR-MCP-NUKE-019 AC-2: Production <see cref="IIntegrationLaneProjects"/>. The six lane projects in their run order;
/// no other project joins the lane (QBrainAi.Support.Mcp.IntegrationTests, QBrainAi.Acid.IntegrationTests and
/// QBrainAi.TransactionSecurity.IntegrationTests stay out until PLAN-SESSIONTRIAGEFIX-001 merges, TODO
/// IMPL-TXNINTEGRATION-001); a solution that lacks a lane project fails before any test runs.
/// </summary>
sealed class IntegrationLaneProjects : IIntegrationLaneProjects
{
    static readonly string[] LaneProjects =
    [
        "QBrainAi.Support.Mcp.Tests",
        "QBrainAi.McpAgent.Tests",
        "QBrainAi.Repl.Core.Tests",
        "QBrainAi.QBAgent.Tests",
        "Build.Tests",
        "QBrainAi.Repl.IntegrationTests",
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
}
