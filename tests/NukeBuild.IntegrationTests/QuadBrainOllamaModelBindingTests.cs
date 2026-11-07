using QBrainAi.Build.Shared;

namespace NukeBuild.IntegrationTests;

/// <summary>
/// TR-MCP-NUKE-016 AC-001 binding: the shared model rule <see cref="QuadBrainOllamaModel.Select"/> satisfies the rows of
/// <c>QuadBrainOllamaModelContractTests</c>, and both consumers, InstallOllama's RequiredOllamaModel and
/// QuadBrainOllamaEndpointIntegrationTests, choose the model through it and read neither variable another way.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-02</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-016 AC-001 (appended: one rule both use): added after mock gate ug-r10-mockgate-t-1, for hostile validation finding R9-F5 (operator 2026-11-02: "Strengthen 5 ACs, fix all 8 (Recommended)").</description></item>
/// <item><term>2026-11-07</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-016 AC-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from its unit case in Build.Tests into the integration-owned Build.IntegrationTests (root namespace NukeBuild.IntegrationTests), so the infrastructure-owned Build.Tests holds no Category=Integration case (slice S4, hostile validation finding R14-F4; operator 2026-11-06: "Move them (Recommended)"). BothConsumers_UseTheSharedRule and FindRepositoryRoot, which only it uses, moved unchanged; the halves share no private helper; the summary's reference to QuadBrainOllamaModelContractTests, which stays in Build.Tests, is now text instead of a cref.</description></item>
/// <item><term>2026-11-07</term><description>FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: Build.IntegrationTests, the project the entry above names, is now NukeBuild.IntegrationTests, renamed to its root namespace (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name."); the class and its cases are unchanged.</description></item>
/// </list>
/// </remarks>
public sealed class QuadBrainOllamaModelBindingTests
{
    /// <summary>
    /// Both consumers call the shared rule with the two variables it names, neither spells a variable name itself, and
    /// the integration project links the rule's source file.
    /// </summary>
    /// <remarks>TR-MCP-NUKE-016 AC-001: integration because this case reads the physical checkout.</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void BothConsumers_UseTheSharedRule()
    {
        var root = FindRepositoryRoot();
        var consumers = new[]
        {
            Path.Combine(root, "build", "Build.TestDependencies.cs"),
            Path.Combine(root, "tests", "QBrainAi.Support.Mcp.IntegrationTests", "Controllers", "QuadBrainOllamaEndpointIntegrationTests.cs"),
        };
        foreach (var path in consumers)
        {
            var source = File.ReadAllText(path);
            Assert.Contains("QuadBrainOllamaModel.Select(", source, StringComparison.Ordinal);
            Assert.Contains("QuadBrainOllamaModel.CanonicalVariable", source, StringComparison.Ordinal);
            Assert.Contains("QuadBrainOllamaModel.LegacyVariable", source, StringComparison.Ordinal);
            Assert.DoesNotContain("\"" + QuadBrainOllamaModel.CanonicalVariable + "\"", source, StringComparison.Ordinal);
            Assert.DoesNotContain("\"" + QuadBrainOllamaModel.LegacyVariable + "\"", source, StringComparison.Ordinal);
        }

        var project = File.ReadAllText(Path.Combine(root, "tests", "QBrainAi.Support.Mcp.IntegrationTests", "QBrainAi.Support.Mcp.IntegrationTests.csproj"));
        Assert.Contains(@"..\..\build\QuadBrainOllamaModel.cs", project, StringComparison.Ordinal);
    }

    /// <summary>TR-MCP-NUKE-016 AC-001: the checkout root, found from the test output folder.</summary>
    /// <returns>The folder that holds QBrainAi.sln.</returns>
    /// <exception cref="DirectoryNotFoundException">No parent folder holds QBrainAi.sln.</exception>
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
