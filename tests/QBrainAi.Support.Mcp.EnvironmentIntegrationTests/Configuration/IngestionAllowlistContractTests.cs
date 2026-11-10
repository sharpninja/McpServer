using System.Text.RegularExpressions;
using QBrainAi.Tests.Shared;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Configuration;

/// <summary>
/// Validates ingestion allowlist and marker-template indexing contracts.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-087, FR-MCP-039, TR-MCP-CTX-001.
/// Test data uses repository files checked into source control so coverage assertions remain deterministic and traceable.
/// <para>2026-11-07, TEST-MCP-087; FR-MCP-039; TR-MCP-CTX-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Configuration/IngestionAllowlistContractTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 8 Category=Integration cases and the helpers only they use (FindFileFromRepoRoot and FindDirectoryFromRepoRoot) moved unchanged with the class attributes; only the namespace changed.</para>
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
    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
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
    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
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
    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
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
    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
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
    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
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
    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
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
    /// Requirement coverage: FR-MCP-103, TR-MCP-FEDERATION-001 (historical).
    /// Test data: <c>src\QBrainAi.Support.Mcp\appsettings.yaml</c>.
    /// This data is used by service deployment, so the checked-in template must
    /// expose the role, hub, proxy, queue, sync, signing, and local-execution keys
    /// needed to configure Hub and LocalProxy machines without hand-inventing YAML.
    /// </remarks>
    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
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
    /// Locates a repository file under the checkout root through the shared <see cref="TestRepositoryRoot"/>, which
    /// resolves only for an integration test (TR-MCP-USECASE-018 AC-006).
    /// </summary>
    /// <param name="segments">Path segments from repository root to the target file.</param>
    /// <returns>Absolute path to the requested file.</returns>
    private static string FindFileFromRepoRoot(params string[] segments) => TestRepositoryRoot.Combine(segments);

    private static string FindDirectoryFromRepoRoot(params string[] segments) => TestRepositoryRoot.Combine(segments);

}
