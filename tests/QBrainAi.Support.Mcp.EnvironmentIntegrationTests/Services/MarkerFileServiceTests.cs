using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Requirements;
using QBrainAi.Support.Mcp.Services;
using Xunit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// Tests prompt composition, template context construction, and marker-file output behavior in <see cref="MarkerFileService"/>.
/// </summary>
/// <remarks>
/// Requirement coverage: FR-MCP-018, FR-MCP-050, TR-MCP-TPL-005.
/// Test data uses deterministic URLs, workspace DTO samples, and template strings so rendering and marker YAML output
/// can be validated without external dependencies.
/// <para>2026-11-07, FR-MCP-018; FR-MCP-050; TR-MCP-TPL-005; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/MarkerFileServiceTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case and the helpers only it uses (ReadDocumentedCanonicalKeyOrder and FindFileFromRepoRoot) moved unchanged with the class attributes; only the namespace changed.</para>
/// </remarks>
public sealed class MarkerFileServiceTests
{
    /// <summary>
    /// Reads the documented canonical payload key order out of <c>docs/REPL-AGENT-GUIDE.md</c>.
    /// </summary>
    /// <returns>The documented field names, in documented order.</returns>
    /// <remarks>
    /// Test data: the <c>```text</c> block that follows the "The canonical key order is:" heading in
    /// the REPL agent guide, which is the prose contract external verifiers implement against.
    /// </remarks>
    private static string[] ReadDocumentedCanonicalKeyOrder()
    {
        var path = FindFileFromRepoRoot("docs", "REPL-AGENT-GUIDE.md");
        var text = File.ReadAllText(path).ReplaceLineEndings("\n");
        var headingIndex = text.IndexOf("The canonical key order is:", StringComparison.Ordinal);
        Assert.True(headingIndex >= 0, $"'The canonical key order is:' heading not found in {path}.");

        var blockStart = text.IndexOf("```text", headingIndex, StringComparison.Ordinal);
        Assert.True(blockStart >= 0, $"Canonical key order code fence not found in {path}.");
        blockStart = text.IndexOf('\n', blockStart) + 1;
        var blockEnd = text.IndexOf("```", blockStart, StringComparison.Ordinal);
        Assert.True(blockEnd > blockStart, $"Canonical key order code fence is not closed in {path}.");

        var keys = new List<string>();
        foreach (var rawLine in text[blockStart..blockEnd].Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            Assert.EndsWith(@"\n", line, StringComparison.Ordinal);
            var separator = line.IndexOf('=', StringComparison.Ordinal);
            Assert.True(separator > 0, $"Documented payload line '{line}' is not in key=value form.");
            keys.Add(line[..separator]);
        }

        return [.. keys];
    }

    /// <summary>
    /// Locates a repository file through the shared <see cref="QBrainAi.Tests.Shared.TestRepositoryRoot"/> (integration
    /// tests only).
    /// </summary>
    /// <param name="segments">Repo-relative path segments of the file to locate.</param>
    /// <returns>The absolute path of the located file.</returns>
    private static string FindFileFromRepoRoot(params string[] segments) => QBrainAi.Tests.Shared.TestRepositoryRoot.Combine(segments);

    /// <summary>
    /// Verifies the documented canonical key order in <c>docs/REPL-AGENT-GUIDE.md</c> matches
    /// <see cref="MarkerFileService.SignaturePayloadFields"/> exactly, so the prose spec external
    /// verifiers implement cannot silently drift from the code.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-140, TR-MCP-SEC-005.
    /// Test data: the canonical key order code fence in <c>docs/REPL-AGENT-GUIDE.md</c>.
    /// This data is used to bind the published verifier contract to the authoritative field array.
    /// </remarks>
    /// <remarks>Integration: reads the live verifier documentation from the checkout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void SignaturePayloadFields_MatchesDocumentedCanonicalKeyOrder()
    {
        var documented = ReadDocumentedCanonicalKeyOrder();

        Assert.Equal(MarkerFileService.SignaturePayloadFields, documented);
    }
}
