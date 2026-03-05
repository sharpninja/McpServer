using Xunit;

namespace McpServer.Support.Mcp.Tests.Configuration;

/// <summary>
/// Validates the ingestion allowlist contract for indexed project coverage.
/// </summary>
public sealed class IngestionAllowlistContractTests
{
    [Fact]
    public void AppSettingsYaml_ContainsRequiredRepoAllowlistPatterns()
    {
        var path = FindFileFromRepoRoot("src", "McpServer.Support.Mcp", "appsettings.yaml");
        var yaml = File.ReadAllText(path);

        Assert.Contains("src/McpServer.Cqrs/**/*.cs", yaml);
        Assert.Contains("src/McpServer.Cqrs.Mvvm/**/*.cs", yaml);
        Assert.Contains("src/McpServer.UI.Core/**/*.cs", yaml);
        Assert.Contains("src/McpServer.Director/**/*.cs", yaml);
    }

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
