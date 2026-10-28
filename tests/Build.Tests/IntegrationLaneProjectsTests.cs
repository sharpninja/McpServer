using System.Text.RegularExpressions;

namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 (AC-2): Binds the <see cref="IntegrationLaneProjectsContract"/> cases and assertions, unchanged
/// from the BDPv4 mock gate, to the production <see cref="IntegrationLaneProjects"/>, and checks the contract's
/// solution row against the real QBrainAi.sln (read only), so the row cannot drift from the solution the lane selects
/// from. Build-infrastructure test, accountable through TR only.
/// </summary>
public sealed class IntegrationLaneProjectsTests
{
    private static readonly Regex ProjectLine = new(
        "^Project\\(\"\\{[^}]+\\}\"\\) = \"(?<name>[^\"]+)\", \"(?<path>[^\"]+\\.csproj)\"",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private readonly IntegrationLaneProjects selector = new();

    /// <summary>TR-MCP-NUKE-019 AC-2: the production selector satisfies every contract row and both refusals.</summary>
    [Fact]
    public void ProductionSelector_SatisfiesTheContract()
        => IntegrationLaneProjectsContract.AssertContract(selector);

    /// <summary>TR-MCP-NUKE-019 AC-2: the contract's solution row is QBrainAi.sln's project list, in solution order.</summary>
    [Fact]
    public void SolutionRow_IsTheSolutionFile()
        => Assert.Equal(IntegrationLaneProjectsContract.Solution, ReadSolutionProjectNames());

    /// <summary>
    /// TR-MCP-NUKE-019 AC-2: on QBrainAi.sln the production selector gives the six lane projects in run order
    /// (QBrainAi.Repl.IntegrationTests last) and none of the three held-out .IntegrationTests projects.
    /// </summary>
    [Fact]
    public void ProductionSelector_OnTheSolutionFile_SelectsTheLaneProjects()
        => Assert.Equal(IntegrationLaneProjectsContract.SolutionSelection, selector.Select(ReadSolutionProjectNames()));

    private static string[] ReadSolutionProjectNames()
    {
        var solution = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "QBrainAi.sln"));
        return ProjectLine.Matches(solution).Select(match => match.Groups["name"].Value).ToArray();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "QBrainAi.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root containing QBrainAi.sln.");
    }
}
