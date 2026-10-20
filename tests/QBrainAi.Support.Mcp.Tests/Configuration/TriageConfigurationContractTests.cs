using QBrainAi.Tests.Shared;

namespace QBrainAi.Support.Mcp.Tests.Configuration;

/// <summary>
/// Verifies shipped triage configuration uses the same Grok one-shot strategy as Agent Help.
/// </summary>
public sealed class TriageConfigurationContractTests
{
    [Trait("Category", "Integration")]
    [Theory]
    [InlineData("appsettings.yaml")]
    [InlineData("appsettings.Staging.yaml")]
    [InlineData("src", "QBrainAi.Support.Mcp", "appsettings.yaml")]
    [InlineData("src", "QBrainAi.Support.Mcp", "appsettings.Staging.yaml")]
    public void AppSettings_PrimaryTriageAgentUsesGrokCli(params string[] segments)
    {
        var path = FindFileFromRepoRoot(segments);
        var primaryTriage = ExtractPrimaryTriageSection(File.ReadAllText(path));

        Assert.Contains("AgentPath: grok", primaryTriage, StringComparison.Ordinal);
        Assert.Contains("ExecutionStrategy: grok-cli", primaryTriage, StringComparison.Ordinal);
        Assert.DoesNotContain("codex.cmd", primaryTriage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ExecutionStrategy: codex-cli", primaryTriage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AgentPath: cline", primaryTriage, StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractPrimaryTriageSection(string yaml)
    {
        var triageIndex = yaml.IndexOf("Triage:", StringComparison.Ordinal);
        Assert.True(triageIndex >= 0, "appsettings file must contain a Triage section.");

        var section = yaml[triageIndex..];
        var fallbackIndex = section.IndexOf("  FallbackOnTimeout:", StringComparison.Ordinal);
        if (fallbackIndex >= 0)
            section = section[..fallbackIndex];

        var secondaryIndex = section.IndexOf("  Secondary:", StringComparison.Ordinal);
        if (secondaryIndex >= 0)
            section = section[..secondaryIndex];

        return section;
    }

    private static string FindFileFromRepoRoot(params string[] segments) => TestRepositoryRoot.Combine(segments);
}
