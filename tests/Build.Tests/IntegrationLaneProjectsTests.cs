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

    /// <summary>TR-MCP-NUKE-019 AC-2: the production selector satisfies every row and all seven missing-project refusals.</summary>
    [Fact]
    public void ProductionSelector_SatisfiesTheContract()
        => IntegrationLaneProjectsContract.AssertContract(selector);

    /// <summary>TR-MCP-NUKE-019 AC-2: the contract's solution row is QBrainAi.sln's project list, in solution order.</summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void SolutionRow_IsTheSolutionFile()
        => Assert.Equal(IntegrationLaneProjectsContract.Solution, ReadSolutionProjectNames());

    /// <summary>
    /// TR-MCP-NUKE-019 AC-2: on QBrainAi.sln the production selector gives the seven lane projects in run order
    /// (QBrainAi.Support.Mcp.IntegrationTests last) and neither of the two held-out .IntegrationTests projects.
    /// </summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void ProductionSelector_OnTheSolutionFile_SelectsTheLaneProjects()
        => Assert.Equal(IntegrationLaneProjectsContract.SolutionSelection, selector.Select(ReadSolutionProjectNames()));

    /// <summary>
    /// TR-MCP-NUKE-019 AC-2 (hostile validation finding R8B-F2): each class the lane runs from
    /// QBrainAi.Support.Mcp.IntegrationTests is declared in exactly one source file of that project (read only), and
    /// that file tags it Category=Integration, so a renamed or untagged class cannot leave the lane silently empty.
    /// </summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void SupportIntegrationClasses_AreDeclaredOnceAndTaggedIntegration()
    {
        var projectFolder = Path.Combine(FindRepositoryRoot(), "tests", IntegrationLaneProjectsContract.SupportIntegrationProject);
        var sources = Directory.GetFiles(projectFolder, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(path => (Path: path, Text: File.ReadAllText(path)))
            .ToArray();

        foreach (var className in selector.ClassesOf(IntegrationLaneProjectsContract.SupportIntegrationProject))
        {
            var declaration = new Regex($@"\bclass\s+{Regex.Escape(className)}\b", RegexOptions.CultureInvariant);
            var declaring = sources.Where(source => declaration.IsMatch(source.Text)).ToArray();
            var file = Assert.Single(declaring).Path;
            Assert.Contains("[Trait(\"Category\", \"Integration\")]", File.ReadAllText(file), StringComparison.Ordinal);
        }
    }

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
