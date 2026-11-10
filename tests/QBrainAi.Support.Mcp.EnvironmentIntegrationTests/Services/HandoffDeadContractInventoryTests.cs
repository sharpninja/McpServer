using System.Text.RegularExpressions;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage.Entities;
using QBrainAi.Tests.Shared;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// P3: ReplayOfRunId and HandoffReviewState.Approved stay removed.
/// Reflection, schema, and source inventory fail if either dead contract is reintroduced.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/HandoffDeadContractInventoryTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case and the helpers only it uses (ApprovedEnumMember and FindRepoRoot) moved unchanged with the class attributes; only the namespace changed.</remarks>
public sealed class HandoffDeadContractInventoryTests
{
    private static readonly Regex ApprovedEnumMember = new(
        @"enum\s+HandoffReviewState\s*\{[^}]*\bApproved\b",
        RegexOptions.CultureInvariant);

    /// <summary>P3: source, snapshots, and migrations do not reintroduce the dead contracts.</summary>
    /// <remarks>Integration: reads live source, schema snapshots and migration files from the checkout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void SourceAndSchema_DoNotReintroduceReplayOfRunIdOrApproved()
    {
        var root = FindRepoRoot();
        var hits = new List<string>();
        var scanRoots = new[]
        {
            Path.Combine(root, "src"),
            Path.Combine(root, "tests"),
        };
        foreach (var scanRoot in scanRoots)
        {
            foreach (var file in Directory.EnumerateFiles(scanRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains("HandoffDeadContractInventoryTests.cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                var text = File.ReadAllText(file);
                if (text.Contains("ReplayOfRunId", StringComparison.Ordinal))
                    hits.Add($"{file}: ReplayOfRunId");
                if (ApprovedEnumMember.IsMatch(text) || text.Contains("HandoffReviewState.Approved", StringComparison.Ordinal))
                    hits.Add($"{file}: HandoffReviewState.Approved");
            }
        }

        Assert.True(hits.Count == 0, string.Join(Environment.NewLine, hits));
    }

    private static string FindRepoRoot() => TestRepositoryRoot.Resolve();
}
