namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 (AC-2): Binds the <see cref="IntegrationLaneClassFilterContract"/> cases and assertions, unchanged
/// from the BDPv4 mock gate, to the production <see cref="IntegrationLaneClassFilter"/>, and composes the lane's real
/// filters from the production host filter, project selector and class filter. Build-infrastructure test, accountable
/// through TR only.
/// </summary>
public sealed class IntegrationLaneClassFilterTests
{
    /// <summary>TR-MCP-NUKE-019 AC-2: the production class filter satisfies every composition row and every refusal.</summary>
    [Fact]
    public void ProductionClassFilter_SatisfiesTheContract()
        => IntegrationLaneClassFilterContract.AssertContract(new IntegrationLaneClassFilter());

    /// <summary>
    /// TR-MCP-NUKE-019 AC-2: on each host the production pieces compose, for QBrainAi.Support.Mcp.IntegrationTests, the
    /// filter of the contract's two-class row, and for every other lane project the host filter alone.
    /// </summary>
    /// <param name="host">Host the lane runs on: "linux" or "windows".</param>
    [Theory]
    [InlineData("linux")]
    [InlineData("windows")]
    public void LaneFilters_ComposeFromTheProductionPieces(string host)
    {
        var linux = host == "linux";
        var hostFilter = new IntegrationLaneFilter().Compose(linux ? IntegrationLaneHost.Linux : IntegrationLaneHost.Windows);
        var projects = new IntegrationLaneProjects();
        var classFilter = new IntegrationLaneClassFilter();
        var twoClassRow = linux ? "the two classes on Linux" : "the two classes on Windows";
        var expectedSupport = IntegrationLaneClassFilterContract.Rows.Single(row => row.Name == twoClassRow).Expected;

        foreach (var project in IntegrationLaneProjectsContract.LaneProjects)
        {
            var expected = project == IntegrationLaneProjectsContract.SupportIntegrationProject ? expectedSupport : hostFilter;
            Assert.Equal(expected, classFilter.Compose(hostFilter, projects.ClassesOf(project)));
        }
    }
}
