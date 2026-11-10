using System.Text.RegularExpressions;
using QBrainAi.Tests.Shared;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Configuration;

/// <summary>
/// Validates ingestion allowlist and marker-template indexing contracts.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-087, FR-MCP-039, TR-MCP-CTX-001.
/// Test data uses repository files checked into source control so coverage assertions remain deterministic and traceable.
/// <para>2026-11-07, TEST-MCP-087; FR-MCP-039; TR-MCP-CTX-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 8 Category=Integration cases and the helpers only they use (FindFileFromRepoRoot and FindDirectoryFromRepoRoot) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the unit cases and the class attributes are unchanged.</para>
/// </remarks>
public sealed class IngestionAllowlistContractTests
{
    /// <summary>
    /// TR-MCP-USECASE-018 (AC-006), BDPv4 mock gate for <see cref="UnitSuitePlatformScanner.FindSkipLines"/>:
    /// scripted sources yield exactly the lines that hold a skip form in code (never inside a string, comment
    /// or <c>nameof</c>), including an attribute whose arguments span lines, a qualified or listed attribute,
    /// the xUnit v3 conditional skip properties, a data-row skip, a <c>using static</c> import, and a skip in a
    /// conditional-compilation branch. Each scripted source is a C# file (statements are top-level statements
    /// and an attribute is followed by the member it decorates), the shape the guard reads.
    /// </summary>
    /// <param name="source">Scripted source; lines are separated by "\n".</param>
    /// <param name="expectedLines">Comma-separated 1-based lines the guard must count; empty for none.</param>
    [Theory]
    [InlineData("Assert.SkipUnless(OperatingSystem.IsWindows(), \"w\");", "1")]
    [InlineData("Assert.SkipWhen(true, \"w\");", "1")]
    [InlineData("Assert.Skip(\"s\");", "1")]
    [InlineData("[Fact(Skip = \"later\")]\npublic void A() { }", "1")]
    [InlineData("[Theory(DisplayName = \"x\", Skip = \"later\")]\npublic void A(int x) { }", "1")]
    [InlineData("throw new SkipException(\"x\");", "1")]
    [InlineData("[SkippableFact]\npublic void A() { }", "1")]
    [InlineData("[Fact(\n    Skip = \"later\")]\npublic void A() { }", "1")]
    [InlineData("[Fact]\npublic void A() { }\n[Theory(DisplayName = nameof(B),\n    SkipUnless = nameof(Ready),\n    Skip = \"x\")]\npublic void B(int x) { }", "3")]
    [InlineData("[Xunit.Fact(Skip = \"later\")]\npublic void A() { }", "1")]
    [InlineData("[global::Xunit.FactAttribute(Skip = \"x\")]\npublic void A() { }", "1")]
    [InlineData("[Trait(\"a\", \"b\"), Theory(Skip = \"x\")]\npublic void A(int x) { }", "1")]
    [InlineData("[Theory]\n[InlineData(1, Skip = \"row\")]\npublic void A(int x) { }", "2")]
    [InlineData("[SkippableFactAttribute]\npublic void A() { }", "1")]
    [InlineData("[Fact(SkipExceptions = new[] { typeof(InvalidOperationException) })]\npublic void A() { }", "1")]
    [InlineData("Skip.If(true);\nSkip.IfNot(false);", "1,2")]
    [InlineData("throw new Xunit.Sdk.SkipException(\"x\");", "1")]
    [InlineData("using static Xunit.Assert;\nSkip(\"s\");", "2")]
    [InlineData("#if WINDOWS\nAssert.Skip(\"w\");\n#endif", "2")]
    [InlineData("Assert.Contains(\"skip=3\", query);", "")]
    [InlineData("var text = \"Assert.SkipUnless(true)\";", "")]
    [InlineData("// Assert.Skip(\"x\");", "")]
    [InlineData("Assert.Skipped(result);", "")]
    [InlineData("var name = nameof(SkipException);", "")]
    [InlineData("[Fact(Timeout = 10)]\npublic void A() { }", "")]
    [InlineData("[Fact(DisplayName = \"Skip = later\")]\npublic void A() { }", "")]
    [InlineData("[Fact]\npublic void Skip() { var Skip = 1; }", "")]
    public void FindSkipLines_FindsEverySkipFormOutsideLiterals(string source, string expectedLines)
    {
        var expected = expectedLines.Length == 0
            ? Array.Empty<int>()
            : expectedLines.Split(',').Select(value => int.Parse(value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        Assert.Equal(expected, UnitSuitePlatformScanner.FindSkipLines(source));
    }

    /// <summary>
    /// TR-MCP-USECASE-018 (AC-006) negative controls: the single-line patterns this guard used before
    /// hostile validation round 1 (finding F3) missed a skip attribute split across lines and a qualified
    /// skip attribute, and the pattern used before 2026-10-19 also missed <c>Assert.SkipUnless</c> and
    /// <c>Assert.SkipWhen</c>; text matching counts a skip that only appears inside a string, which the
    /// compiler-parsed definition does not.
    /// </summary>
    [Fact]
    public void SkipGuardNegativeControls_AreRejectedInsidePassingHarness()
    {
        var original = new Regex(
            @"\[(?:Fact|Theory)\s*\([^\]]*\bSkip\s*=|\bSkipException\b|\bAssert\.Skip\b|\bSkippable(?:Fact|Theory)\b",
            RegexOptions.CultureInvariant);
        var roundOne = new Regex(
            @"\[(?:Fact|Theory)\s*\([^\]]*\bSkip\s*=|\bSkipException\b|\bAssert\s*\.\s*Skip(?:When|Unless)?\b|\bSkippable(?:Fact|Theory)\b",
            RegexOptions.CultureInvariant);
        Assert.DoesNotMatch(original, "Assert.SkipUnless(OperatingSystem.IsWindows(), \"w\");");
        Assert.DoesNotMatch(original, "Assert.SkipWhen(true, \"w\");");
        foreach (var line in "[Fact(\n    Skip = \"later\")]".Split('\n'))
        {
            Assert.DoesNotMatch(roundOne, line);
        }

        Assert.DoesNotMatch(roundOne, "[Xunit.Fact(Skip = \"later\")]");
        Assert.Equal(new[] { 1 }, UnitSuitePlatformScanner.FindSkipLines("[Xunit.Fact(Skip = \"later\")]\npublic void A() { }"));
        Assert.Matches(roundOne, "var text = \"Assert.SkipUnless(true)\";");
        Assert.Empty(UnitSuitePlatformScanner.FindSkipLines("var text = \"Assert.SkipUnless(true)\";"));
    }

}
