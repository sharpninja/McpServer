/// <summary>
/// TR-MCP-NUKE-019 AC-2: Production <see cref="IIntegrationLaneProjects"/>. The seven lane projects in their run order;
/// from QBrainAi.Support.Mcp.IntegrationTests only the classes RequirementScopeLayerReplIntegrationTests and
/// QuadBrainOllamaEndpointIntegrationTests, by class name (operator 2026-10-29: "Run the 2 classes, AC first
/// (Recommended)"; hostile validation finding R8B-F2); no other project joins the lane (QBrainAi.Acid.IntegrationTests and
/// QBrainAi.TransactionSecurity.IntegrationTests stay out until PLAN-SESSIONTRIAGEFIX-001 merges, TODO
/// IMPL-TXNINTEGRATION-001); a solution that lacks a lane project fails before any test runs.
/// </summary>
sealed class IntegrationLaneProjects : IIntegrationLaneProjects
{
    const string SupportIntegrationProject = "QBrainAi.Support.Mcp.IntegrationTests";

    static readonly string[] LaneProjects =
    [
        "QBrainAi.Support.Mcp.Tests",
        "QBrainAi.McpAgent.Tests",
        "QBrainAi.Repl.Core.Tests",
        "QBrainAi.QBAgent.Tests",
        "Build.Tests",
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
