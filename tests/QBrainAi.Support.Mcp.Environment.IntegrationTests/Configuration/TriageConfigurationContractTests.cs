using QBrainAi.Tests.Shared;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Configuration;

/// <summary>
/// Verifies shipped triage configuration uses the same Grok one-shot strategy as Agent Help.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole from QBrainAi.Support.Mcp.Tests (Configuration/TriageConfigurationContractTests.cs) into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S6, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); the shared TestRepositoryRoot locator is linked into this assembly; only the namespace changed.</remarks>
public sealed class TriageConfigurationContractTests
{
    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
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
