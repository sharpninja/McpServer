using System.Text.RegularExpressions;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// BDP v4 step 2: the ratchet evaluation proven against scripted in-memory sources, including each
/// failure path the Real fixture can report.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-FED-PATH-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: unchanged; the ratchet contract it derives from moved into Storage/WorkspacePathGetFullPathRatchetContract.cs beside this file, which the integration project links, and the repository-scanning fixture moved to the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4).</remarks>
public sealed class WorkspacePathGetFullPathRatchetMockDataTests : WorkspacePathGetFullPathRatchetContractTests
{
    private const string Call = "var x = Path.GetFullPath(p);\n";

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, int> Counts() => Count(CompliantSources());

    /// <summary>An identity file with a direct call is reported.</summary>
    [Fact]
    public void Evaluator_IdentityFileWithCall_IsReported()
    {
        var sources = CompliantSources().Append(KeyValuePair.Create(IdentityFiles[0], Call)).ToList();

        Assert.Equal([$"{IdentityFiles[0]}: 1"], IdentityOffenders(Count(sources)));
    }

    /// <summary>An allowlisted file above its reviewed count is reported.</summary>
    [Fact]
    public void Evaluator_AllowlistedFileAboveCount_IsReported()
    {
        var (file, allowed) = HostNativeAllowlist.First();
        var sources = CompliantSources()
            .Where(pair => pair.Key != file)
            .Append(KeyValuePair.Create(file, string.Concat(Enumerable.Repeat(Call, allowed + 1))))
            .ToList();

        Assert.Equal([$"{file}: {allowed + 1} (allowed {allowed})"], AllowlistViolations(Count(sources)));
    }

    /// <summary>A file not on the allowlist that starts calling Path.GetFullPath is reported.</summary>
    [Fact]
    public void Evaluator_NewUnlistedFile_IsReported()
    {
        var sources = CompliantSources().Append(KeyValuePair.Create("src/New/Unreviewed.cs", Call)).ToList();

        Assert.Equal(["src/New/Unreviewed.cs: 1 (allowed 0)"], AllowlistViolations(Count(sources)));
    }

    /// <summary>A count that goes down stays within the ratchet.</summary>
    [Fact]
    public void Evaluator_AllowlistedFileBelowCount_IsAccepted()
    {
        var (file, _) = HostNativeAllowlist.First(pair => pair.Value > 1);
        var sources = CompliantSources()
            .Where(pair => pair.Key != file)
            .Append(KeyValuePair.Create(file, Call))
            .ToList();

        Assert.Empty(AllowlistViolations(Count(sources)));
    }

    /// <summary>
    /// Scripted compliant tree: identity files without calls, a sample of allowlisted files at
    /// exactly their reviewed counts, and an unrelated file without calls.
    /// </summary>
    private static IEnumerable<KeyValuePair<string, string>> CompliantSources() =>
        IdentityFiles.Select(file => KeyValuePair.Create(file, "// routes paths through IWorkspacePathNormalizer\n"))
            .Concat(HostNativeAllowlist.Take(5).Select(pair => KeyValuePair.Create(pair.Key, string.Concat(Enumerable.Repeat(Call, pair.Value)))))
            .Append(KeyValuePair.Create("src/Other/NoPaths.cs", "class NoPaths { }\n"));
}
