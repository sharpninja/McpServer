using System.Text.RegularExpressions;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage.Entities;
using QBrainAi.Tests.Shared;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// P3: ReplayOfRunId and HandoffReviewState.Approved stay removed.
/// Reflection, schema, and source inventory fail if either dead contract is reintroduced.
/// </summary>
public sealed class HandoffDeadContractInventoryTests
{
    private static readonly Regex ApprovedEnumMember = new(
        @"enum\s+HandoffReviewState\s*\{[^}]*\bApproved\b",
        RegexOptions.CultureInvariant);

    /// <summary>P3: neither service nor client HandoffReviewState defines Approved.</summary>
    [Fact]
    public void HandoffReviewState_DoesNotDefineApproved()
    {
        Assert.DoesNotContain(Enum.GetNames<HandoffReviewState>(), name => string.Equals(name, "Approved", StringComparison.Ordinal));
        Assert.DoesNotContain(Enum.GetNames<QBrainAi.Client.Models.HandoffReviewState>(), name => string.Equals(name, "Approved", StringComparison.Ordinal));
        Assert.False(Enum.IsDefined(typeof(HandoffReviewState), 2));
        Assert.False(Enum.IsDefined(typeof(QBrainAi.Client.Models.HandoffReviewState), 2));
    }

    /// <summary>P3: the persisted run entity has no ReplayOfRunId column or property.</summary>
    [Fact]
    public void HandoffIngestionRunEntity_DoesNotExposeReplayOfRunId()
    {
        var names = typeof(HandoffIngestionRunEntity).GetProperties().Select(property => property.Name).ToArray();
        Assert.DoesNotContain(names, name => string.Equals(name, "ReplayOfRunId", StringComparison.Ordinal));
    }

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
