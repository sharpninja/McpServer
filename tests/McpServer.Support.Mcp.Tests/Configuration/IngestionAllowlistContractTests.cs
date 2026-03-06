using Xunit;

namespace McpServer.Support.Mcp.Tests.Configuration;

/// <summary>
/// Validates ingestion allowlist and marker-template indexing contracts.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-087, FR-MCP-039, TR-MCP-CTX-001.
/// Test data uses repository files checked into source control so coverage assertions remain deterministic and traceable.
/// </remarks>
public sealed class IngestionAllowlistContractTests
{
    /// <summary>
    /// Verifies that <c>appsettings.yaml</c> includes required repository allowlist patterns for indexed projects.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-087, FR-MCP-039, TR-MCP-CTX-001.
    /// Test data: <c>src\McpServer.Support.Mcp\appsettings.yaml</c> content with expected glob values.
    /// This data is used to ensure configuration defaults preserve indexing coverage for CQRS source trees.
    /// </remarks>
    [Fact]
    public void AppSettingsYaml_ContainsRequiredRepoAllowlistPatterns()
    {
        var path = FindFileFromRepoRoot("src", "McpServer.Support.Mcp", "appsettings.yaml");
        var yaml = File.ReadAllText(path);

        Assert.Contains("src/McpServer.Cqrs/**/*.cs", yaml);
        Assert.Contains("src/McpServer.Cqrs.Mvvm/**/*.cs", yaml);
    }

    /// <summary>
    /// Verifies that the marker prompt template includes the Available Capabilities section and expected project entries.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-087, FR-MCP-039, TR-MCP-CTX-001.
    /// Test data: <c>templates\prompt-templates.yaml</c> and known capability bullet strings for indexed projects.
    /// This data is used to confirm marker prompt output advertises indexed libraries required for context retrieval.
    /// </remarks>
    [Fact]
    public void MarkerPromptTemplate_ContainsAvailableCapabilitiesSection()
    {
        var path = FindFileFromRepoRoot("templates", "prompt-templates.yaml");
        var content = File.ReadAllText(path);

        Assert.Contains("## Available Capabilities", content);
        Assert.Contains("- McpServer.Cqrs (CQRS framework)", content);
        Assert.Contains("- McpServer.Cqrs.Mvvm (MVVM support)", content);
        Assert.Contains("- McpServer.UI.Core (Core UI logic)", content);
        Assert.Contains("- McpServer.Director (Director CLI)", content);
    }

    /// <summary>
    /// Locates a repository file by walking upward from the test execution directory.
    /// </summary>
    /// <param name="segments">Path segments from repository root to the target file.</param>
    /// <returns>Absolute path to the requested file.</returns>
    /// <remarks>
    /// Test data: relative path segments for files under test.
    /// This helper is used so tests can resolve files reliably across local and CI run directories.
    /// </remarks>
    private static string FindFileFromRepoRoot(params string[] segments)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, Path.Combine(segments));
            if (File.Exists(candidate))
                return candidate;

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not locate file '{Path.Combine(segments)}' from '{AppContext.BaseDirectory}'.");
    }
}
