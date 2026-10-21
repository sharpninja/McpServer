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
/// </remarks>
public sealed class IngestionAllowlistContractTests
{
    /// <summary>
    /// Verifies that <c>appsettings.yaml</c> includes required repository allowlist patterns for indexed projects.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-087, FR-MCP-039, TR-MCP-CTX-001.
    /// Test data: <c>src\QBrainAi.Support.Mcp\appsettings.yaml</c> content with expected glob values.
    /// This data is used to ensure configuration defaults preserve indexing coverage for CQRS source trees.
    /// </remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void AppSettingsYaml_ContainsRequiredRepoAllowlistPatterns()
    {
        var path = FindFileFromRepoRoot("src", "QBrainAi.Support.Mcp", "appsettings.yaml");
        var yaml = File.ReadAllText(path);

        Assert.Contains("src/QBrainAi.Cqrs/**/*.cs", yaml);
        Assert.Contains("src/QBrainAi.Cqrs.Mvvm/**/*.cs", yaml);
    }

    /// <summary>
    /// Verifies that the marker prompt template includes the Available Capabilities section and repo-local project entries.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-087, FR-MCP-039, TR-MCP-CTX-001.
    /// Test data: <c>templates\prompt-templates.yaml</c> and known capability bullet strings for indexed repo-local projects.
    /// This data is used to confirm marker prompt output advertises indexed libraries that still live in this repository.
    /// </remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void MarkerPromptTemplate_ContainsAvailableCapabilitiesSection()
    {
        var path = FindFileFromRepoRoot("templates", "prompt-templates.yaml");
        var content = File.ReadAllText(path);

        Assert.Contains("## Available Capabilities", content);
        Assert.Contains("- QBrainAi.Cqrs (CQRS framework)", content);
        Assert.Contains("- QBrainAi.Cqrs.Mvvm (MVVM support)", content);
        Assert.DoesNotContain("- QBrainAi.UI.Core (Core UI logic)", content);
        Assert.DoesNotContain("- QBrainAi.Director (Director CLI)", content);
    }

    /// <summary>
    /// Verifies that the marker prompt template carries the Byrd V3 validation gate.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: Byrd Development Process V4 validation discipline.
    /// Test data: <c>templates\prompt-templates.yaml</c>.
    /// This data is used to ensure generated marker prompts tell agents that the
    /// iteration cannot advance unless the executed test gate has no failures
    /// and no skips.
    /// </remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void MarkerPromptTemplate_ContainsByrdV4HundredPercentTestGate()
    {
        var path = FindFileFromRepoRoot("templates", "prompt-templates.yaml");
        var content = File.ReadAllText(path);

        Assert.Contains("Byrd Development Process V4", content);
        Assert.Contains("100% test success", content);
        Assert.Contains("zero failed tests and zero skipped tests", content);
        Assert.Contains("Tests are the progress ledger", content);
    }

    /// <summary>
    /// Verifies that the marker prompt template carries the decision-complete planning handoff standard.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-104, TR-MCP-TPL-007, TEST-MCP-137.
    /// Test data: <c>templates\prompt-templates.yaml</c>.
    /// This data is used to ensure generated marker prompts tell frontier planning
    /// agents to produce requirements-backed, TDD-first plans that lower-cost
    /// implementation agents can execute without resolving unstated decisions.
    /// </remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void MarkerPromptTemplate_ContainsDecisionCompletePlanHandoffGuidance()
    {
        var path = FindFileFromRepoRoot("templates", "prompt-templates.yaml");
        var content = File.ReadAllText(path);

        Assert.Contains("## Planning Standard", content);
        Assert.Contains("frontier-model handoff artifacts", content);
        Assert.Contains("less expensive implementation model", content);
        Assert.Contains("decision-complete", content);
        Assert.Contains("Every plan must capture requirements before implementation", content);
        Assert.Contains("FR/TR/TEST", content);
        Assert.Contains("TDD unit tests", content);
        Assert.Contains("expected red state", content);
    }

    /// <summary>
    /// Verifies that the marker prompt template instructs agents to use Agent Help for QBrain.AI issues.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-HELP-008, TEST-MCP-HELP-SEC-007.
    /// </remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void MarkerPromptTemplate_ContainsAgentHelpSection()
    {
        var path = FindFileFromRepoRoot("templates", "prompt-templates.yaml");
        var content = File.ReadAllText(path);

        Assert.Contains("## Agent Help (QBrain.AI issues)", content);
        Assert.Contains("agent_help_create_session", content);
        Assert.Contains("workflow.agenthelp.createSession", content);
        Assert.Contains("/qbrainai/agent-help/session", content);
    }

    /// <summary>
    /// Verifies that the marker prompt template carries hub-and-spoke federation diagnostics.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-103, PLAN-FEDERATION-001 agent/operator surface.
    /// </remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void MarkerPromptTemplate_ContainsFederationTopologyDiagnostics()
    {
        var path = FindFileFromRepoRoot("templates", "prompt-templates.yaml");
        var content = File.ReadAllText(path);

        Assert.Contains("## Federation Topology", content);
        Assert.Contains("/qbrainai/federation/status", content);
        Assert.Contains("role", content);
        Assert.Contains("configuredRole", content);
        Assert.Contains("hubBaseUrl", content);
        Assert.Contains("proxyId", content);
        Assert.Contains("proxyCount", content);
        Assert.Contains("hostedWorkspaceCount", content);
        Assert.Contains("queueDepth", content);
        Assert.Contains("fanoutDepth", content);
        Assert.Contains("conflictCount", content);
        Assert.Contains("staleReadStatus", content);
        Assert.Contains("LocalProxy", content);
        Assert.Contains("queued writes", content);
    }

    /// <summary>
    /// Verifies that deployment appsettings carry the hub-and-spoke federation configuration shape.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-103, TR-MCP-FEDERATION-001.
    /// Test data: <c>src\QBrainAi.Support.Mcp\appsettings.yaml</c>.
    /// This data is used by service deployment, so the checked-in template must
    /// expose the role, hub, proxy, queue, sync, signing, and local-execution keys
    /// needed to configure Hub and LocalProxy machines without hand-inventing YAML.
    /// </remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void AppSettingsTemplate_ContainsHubSpokeFederationShape()
    {
        var path = FindFileFromRepoRoot("src", "QBrainAi.Support.Mcp", "appsettings.yaml");
        var content = File.ReadAllText(path);

        Assert.Contains("  Federation:", content);
        Assert.Contains("    Enabled: false", content);
        Assert.Contains("    Role: Standalone", content);
        Assert.Contains("    HubBaseUrl:", content);
        Assert.Contains("    HubAccessToken:", content);
        Assert.Contains("    ProxyId:", content);
        Assert.Contains("    EnrollmentToken:", content);
        Assert.Contains("    Queue:", content);
        Assert.Contains("      MaxReplayAttempts: 10", content);
        Assert.Contains("      MaxBodyBytes: 1048576", content);
        Assert.Contains("    Sync:", content);
        Assert.Contains("      HeartbeatSeconds: 30", content);
        Assert.Contains("      ReplayIntervalSeconds: 15", content);
        Assert.Contains("      FanoutIntervalSeconds: 15", content);
        Assert.Contains("    Signing:", content);
        Assert.Contains("      SharedSecret:", content);
        Assert.Contains("      EnvelopeTtlSeconds: 300", content);
        Assert.Contains("    LocalExecution:", content);
        Assert.Contains("      AllowedMethods:", content);
        Assert.Contains("      - desktop_launch", content);
    }

    /// <summary>
    /// Verifies active test source files do not hide incomplete work with xUnit skip mechanics.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: Byrd Development Process V4 validation discipline; TR-MCP-USECASE-018 (AC-006),
    /// TR-MCP-USECASE-019 (AC-008): platform-specific tests neither skip nor silently return.
    /// Test data: all C# source files under the repository <c>tests</c> folder, parsed with the C# compiler so
    /// scripted sources that mention a skip inside a string or comment are not counted. Skip forms are found
    /// by <see cref="UnitSuitePlatformScanner.FindSkipLines"/>, the one definition both guards share. The test
    /// reads the repository checkout, so it is an integration test on both hosts (hostile validation round 2,
    /// finding R2-F1: no unit test reads files outside its test output).
    /// This data is used to make progress visible: active validation suites must
    /// pass or fail directly instead of silently skipping unfinished behavior.
    /// </remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void TestSources_DoNotDeclareSkippedXunitTests()
    {
        var testsRoot = FindDirectoryFromRepoRoot("tests");
        var offenders = new List<string>();

        foreach (var path in Directory.EnumerateFiles(testsRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
                path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var line in UnitSuitePlatformScanner.FindSkipLines(File.ReadAllText(path)))
            {
                offenders.Add($"{Path.GetRelativePath(testsRoot, path)}:{line}");
            }
        }

        offenders = offenders.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();

        Assert.True(
            offenders.Count == 0,
            "Byrd V4 requires test gates to show direct progress; remove skip mechanics or move deferred work to MCP TODO state. Offenders: " + string.Join(", ", offenders));
    }

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

    /// <summary>
    /// Locates a repository file under the checkout root through the shared <see cref="TestRepositoryRoot"/>, which
    /// resolves only for an integration test (TR-MCP-USECASE-018 AC-006).
    /// </summary>
    /// <param name="segments">Path segments from repository root to the target file.</param>
    /// <returns>Absolute path to the requested file.</returns>
    private static string FindFileFromRepoRoot(params string[] segments) => TestRepositoryRoot.Combine(segments);

    private static string FindDirectoryFromRepoRoot(params string[] segments) => TestRepositoryRoot.Combine(segments);

}
