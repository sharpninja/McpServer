using System.Xml.Linq;
using Xunit;

namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// PLAN-PLUGINHANDOFF-001 P2: PluginIntegration project carries XML docs and nonparallel collection.
/// </summary>
[Collection("PluginSessionLog")]
[Trait("PluginInt", "Deterministic")]
public sealed class PluginIntegrationProjectTests
{
    /// <summary>
    /// The test project enables GenerateDocumentationFile and disables parallelization.
    /// </summary>
    [Fact]
    public void PluginIntegrationProject_HasXmlDocsAndNonparallelCollection()
    {
        var repoRoot = FindRepositoryRoot();
        var csprojPath = Path.Combine(repoRoot, "tests", "McpServer.PluginIntegration.Tests", "McpServer.PluginIntegration.Tests.csproj");
        var csproj = XDocument.Load(csprojPath);
        var generateDocs = csproj.Descendants("GenerateDocumentationFile").Select(e => e.Value).FirstOrDefault();
        Assert.Equal("true", generateDocs, StringComparer.OrdinalIgnoreCase);

        var collectionSource = File.ReadAllText(Path.Combine(repoRoot, "tests", "McpServer.PluginIntegration.Tests", "PluginSessionLogCollection.cs"));
        Assert.Contains("DisableParallelization = true", collectionSource, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "McpServer.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("McpServer.sln not found.");
    }
}
