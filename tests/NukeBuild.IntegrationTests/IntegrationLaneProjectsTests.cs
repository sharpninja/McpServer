using System.Text.RegularExpressions;
using NukeBuild.Tests;

namespace NukeBuild.IntegrationTests;

/// <summary>
/// TR-MCP-NUKE-019 (AC-2): Binds the <see cref="IntegrationLaneProjectsContract"/> cases and assertions, unchanged
/// from the BDPv4 mock gate, to the production <see cref="IntegrationLaneProjects"/>, and checks the contract's
/// solution row against the real QBrainAi.sln (read only), so the row cannot drift from the solution the lane selects
/// from. Build-infrastructure test, accountable through TR only.
/// </summary>
/// <remarks>
/// <para>2026-11-07, TR-MCP-NUKE-019 AC-2, AC-7; FR-MCP-BUILD-001: the integration half of IntegrationLaneProjectsTests, split from its contract binding in Build.Tests into the integration-owned Build.IntegrationTests (root namespace NukeBuild.IntegrationTests), so the infrastructure-owned Build.Tests holds no Category=Integration case (slice S4, hostile validation finding R14-F4; operator 2026-11-06: "Move them (Recommended)"). The three cases that read the checkout and the helpers only they use (ProjectLine, ReadSolutionProjectNames, FindRepositoryRoot) moved unchanged; IntegrationLaneProjectsContract is linked from Build.Tests, not copied; each half holds its own instance of the production selector; only the namespace changed.</para>
/// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: Build.IntegrationTests, the project the entry above names, is now NukeBuild.IntegrationTests, renamed to its root namespace (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name."); the linked contract names the renamed projects; the class and its cases are unchanged.</para>
/// </remarks>
public sealed class IntegrationLaneProjectsTests
{
    private static readonly Regex ProjectLine = new(
        "^Project\\(\"\\{[^}]+\\}\"\\) = \"(?<name>[^\"]+)\", \"(?<path>[^\"]+\\.csproj)\"",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private readonly IntegrationLaneProjects selector = new();

    /// <summary>TR-MCP-NUKE-019 AC-2: the contract's solution row is QBrainAi.sln's project list, in solution order.</summary>
    /// <remarks>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void SolutionRow_IsTheSolutionFile()
        => Assert.Equal(IntegrationLaneProjectsContract.Solution, ReadSolutionProjectNames());

    /// <summary>
    /// TR-MCP-NUKE-019 AC-2: on QBrainAi.sln the production selector gives the lane projects in run order
    /// (QBrainAi.Support.Mcp.IntegrationTests last) and neither of the two held-out .IntegrationTests projects.
    /// </summary>
    /// <remarks>
    /// <para>TR-MCP-NUKE-019: integration because this case resolves or reads the physical checkout.</para>
    /// <para>2026-11-07, FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-2, AC-7: unchanged case; the documentation no longer counts the lane projects, which slice S3 changed from seven to six.</para>
    /// </remarks>
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
