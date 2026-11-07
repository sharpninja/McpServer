using QBrainAi.Tests.Shared;

namespace QBrainAi.Support.Mcp.Tests.Configuration;

/// <summary>
/// TR-MCP-USECASE-018 (AC-006), TR-MCP-USECASE-019 (AC-008): binds <see cref="UnitSuitePlatformScanner"/>,
/// unchanged from its mock gate, to the C# sources of every project the Nuke <c>Test</c> target runs
/// (<c>build/Build.Test.cs</c>: test projects except integration, plugin-integration, review and Build.Tests
/// projects). A test that depends on the platform is an integration test, so the unit tests hold no
/// operating-system check, OS-platform attribute, xUnit skip, or real process-host binding. These tests
/// read the repository checkout, so they are integration tests on both hosts and run in each host's
/// MigrationIntegrationTests (hostile validation round 2, finding R2-F1); the scripted cases in
/// <see cref="UnitSuitePlatformScannerMockGateTests"/> stay unit tests.
/// </summary>
[Trait("Category", "Integration")]
public sealed class UnitSuitePlatformScannerTests
{
    private static readonly string[] ExpectedUnitProjects =
    [
        "QBrainAi.Client.Tests",
        "QBrainAi.Cqrs.Tests",
        "QBrainAi.Launcher.Tests",
        "QBrainAi.McpAgent.Tests",
        "QBrainAi.QBAgent.Tests",
        "QBrainAi.Repl.Core.Tests",
        "QBrainAi.Support.Mcp.Tests",
    ];

    /// <summary>No unit test class in the unit-suite projects depends on the platform.</summary>
    [Fact]
    public void UnitTestClasses_HaveNoPlatformDependence()
    {
        var testsRoot = FindTestsRoot();
        var findings = UnitProjects(testsRoot)
            .SelectMany(project => Directory.EnumerateFiles(project, "*.cs", SearchOption.AllDirectories))
            .Where(path => !IsBuildOutput(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .SelectMany(path => UnitSuitePlatformScanner.Scan(
                Path.GetRelativePath(testsRoot, path).Replace('\\', '/'),
                File.ReadAllText(path)))
            .Select(finding => finding.ToString())
            .ToArray();

        Assert.True(
            findings.Length == 0,
            "A test that depends on the platform is an integration test: tag the test method or its class [Trait(\"Category\", \"Integration\")] "
            + "(with [Trait(\"Platform\", ...)] when it applies to one host) or remove the dependence. Findings:"
            + Environment.NewLine + string.Join(Environment.NewLine, findings));
    }

    /// <summary>
    /// Pins the projects the guard scans to the seven the Nuke <c>Test</c> target ran when this was written
    /// (<c>build/Build.Test.cs:16-24</c>): a test project added to or removed from <c>tests/</c> changes the
    /// guard's selection and fails here. Limit: this test does not read <c>build/Build.Test.cs</c>, so a
    /// change confined to that target's selection leaves it green and must be mirrored here by hand.
    /// </summary>
    [Fact]
    public void GuardProjectSelection_MatchesTheUnitTestTarget()
    {
        var names = UnitProjects(FindTestsRoot()).Select(directory => new DirectoryInfo(directory).Name).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(ExpectedUnitProjects, names);
    }

    /// <summary>
    /// DRY: no source in the unit-suite projects holds a repository-root locator beside the shared
    /// <c>tests/Shared/TestRepositoryRoot.cs</c>, and the guard does find the shared locator (positive control on the
    /// real file).
    /// </summary>
    [Fact]
    public void UnitProjects_HoldNoRepositoryLocatorBesideTheSharedOne()
    {
        var testsRoot = FindTestsRoot();
        var findings = UnitProjects(testsRoot)
            .SelectMany(project => Directory.EnumerateFiles(project, "*.cs", SearchOption.AllDirectories))
            .Where(path => !IsBuildOutput(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .SelectMany(path => UnitSuitePlatformScanner.FindRepositoryLocators(File.ReadAllText(path))
                .Select(line => $"{Path.GetRelativePath(testsRoot, path).Replace('\\', '/')}:{line}"))
            .ToArray();

        Assert.True(
            findings.Length == 0,
            "Locate the repository with the shared TestRepositoryRoot (tests/Shared/TestRepositoryRoot.cs) instead of a "
            + "local walk up. Findings:" + Environment.NewLine + string.Join(Environment.NewLine, findings));
        Assert.NotEmpty(UnitSuitePlatformScanner.FindRepositoryLocators(
            File.ReadAllText(Path.Combine(testsRoot, "Shared", "TestRepositoryRoot.cs"))));
    }

    private static IEnumerable<string> UnitProjects(string testsRoot) =>
        Directory.EnumerateDirectories(testsRoot)
            .Where(directory =>
            {
                var name = new DirectoryInfo(directory).Name;
                return name.EndsWith(".Tests", StringComparison.Ordinal)
                    && !name.Contains("IntegrationTests", StringComparison.Ordinal)
                    && !name.Contains("PluginIntegration", StringComparison.Ordinal)
                    && !name.Contains("Review.Tests", StringComparison.Ordinal)
                    && !name.EndsWith("Build.Tests", StringComparison.Ordinal)
                    && Directory.EnumerateFiles(directory, "*.csproj").Any();
            });

    private static bool IsBuildOutput(string path)
    {
        var separator = Path.DirectorySeparatorChar;
        return path.Contains($"{separator}bin{separator}", StringComparison.OrdinalIgnoreCase)
            || path.Contains($"{separator}obj{separator}", StringComparison.OrdinalIgnoreCase);
    }

    private static string FindTestsRoot() => TestRepositoryRoot.Combine("tests");
}
