using System.Text.RegularExpressions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Configuration;

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
    /// Test data: <c>src\McpServer.Support.Mcp\appsettings.yaml</c> content with expected glob values.
    /// This data is used to ensure configuration defaults preserve indexing coverage for CQRS source trees.
    /// </remarks>
    [Fact]
    public void AppSettingsYaml_ContainsRequiredRepoAllowlistPatterns()
    {
        var path = FindFileFromRepoRoot("src", "McpServer.Support.Mcp", "appsettings.yaml");
        var yaml = File.ReadAllText(path);

        Assert.Contains("src/McpServer.Cqrs/**/*.cs", yaml);
        Assert.Contains("src/McpServer.Cqrs.Mvvm/**/*.cs", yaml);
    }

    /// <summary>
    /// Verifies that the marker prompt template includes the Available Capabilities section and expected project entries.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-087, FR-MCP-039, TR-MCP-CTX-001.
    /// Test data: <c>templates\prompt-templates.yaml</c> and known capability bullet strings for indexed projects.
    /// This data is used to confirm marker prompt output advertises indexed libraries required for context retrieval.
    /// </remarks>
    [Fact]
    public void MarkerPromptTemplate_ContainsAvailableCapabilitiesSection()
    {
        var path = FindFileFromRepoRoot("templates", "prompt-templates.yaml");
        var content = File.ReadAllText(path);

        Assert.Contains("## Available Capabilities", content);
        Assert.Contains("- McpServer.Cqrs (CQRS framework)", content);
        Assert.Contains("- McpServer.Cqrs.Mvvm (MVVM support)", content);
        Assert.Contains("- McpServer.UI.Core (Core UI logic)", content);
        Assert.Contains("- McpServer.Director (Director CLI)", content);
    }

    /// <summary>
    /// Verifies that the marker prompt template carries the Byrd V3 validation gate.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: Byrd Development Process V3 validation discipline.
    /// Test data: <c>templates\prompt-templates.yaml</c>.
    /// This data is used to ensure generated marker prompts tell agents that the
    /// iteration cannot advance unless the executed test gate has no failures
    /// and no skips.
    /// </remarks>
    [Fact]
    public void MarkerPromptTemplate_ContainsByrdV3HundredPercentTestGate()
    {
        var path = FindFileFromRepoRoot("templates", "prompt-templates.yaml");
        var content = File.ReadAllText(path);

        Assert.Contains("Byrd Development Process V3", content);
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
    /// Verifies that the marker prompt template carries hub-and-spoke federation diagnostics.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: FR-MCP-103, PLAN-FEDERATION-001 agent/operator surface.
    /// Test data: <c>templates\prompt-templates.yaml</c>.
    /// This data is used to ensure generated markers tell agents how to identify
    /// hub/proxy topology and stale local state before using MCP endpoints.
    /// </remarks>
    [Fact]
    public void MarkerPromptTemplate_ContainsFederationTopologyDiagnostics()
    {
        var path = FindFileFromRepoRoot("templates", "prompt-templates.yaml");
        var content = File.ReadAllText(path);

        Assert.Contains("## Federation Topology", content);
        Assert.Contains("/mcpserver/federation/status", content);
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
    /// Test data: <c>src\McpServer.Support.Mcp\appsettings.yaml</c>.
    /// This data is used by service deployment, so the checked-in template must
    /// expose the role, hub, proxy, queue, sync, signing, and local-execution keys
    /// needed to configure Hub and LocalProxy machines without hand-inventing YAML.
    /// </remarks>
    [Fact]
    public void AppSettingsTemplate_ContainsHubSpokeFederationShape()
    {
        var path = FindFileFromRepoRoot("src", "McpServer.Support.Mcp", "appsettings.yaml");
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
    /// Requirement coverage: Byrd Development Process V3 validation discipline.
    /// Test data: all C# source files under the repository <c>tests</c> folder.
    /// This data is used to make progress visible: active validation suites must
    /// pass or fail directly instead of silently skipping unfinished behavior.
    /// </remarks>
    [Fact]
    public void TestSources_DoNotDeclareSkippedXunitTests()
    {
        var testsRoot = FindDirectoryFromRepoRoot("tests");
        var skipPattern = new Regex(
            @"\[(?:Fact|Theory)\s*\([^\]]*\bSkip\s*=|\b" + "Skip" + @"Exception\b|\bAssert\." + "Skip" + @"\b|\b" + "Skip" + @"pable(?:Fact|Theory)\b",
            RegexOptions.CultureInvariant | RegexOptions.Singleline);

        var offenders = Directory
            .EnumerateFiles(testsRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .SelectMany(path =>
            {
                var content = File.ReadAllText(path);
                return skipPattern
                    .Matches(content)
                    .Select(match => $"{Path.GetRelativePath(testsRoot, path)}:{LineNumber(content, match.Index)}");
            })
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            "Byrd V3 requires test gates to show direct progress; remove skip mechanics or move deferred work to MCP TODO state. Offenders: " + string.Join(", ", offenders));
    }

    /// <summary>
    /// Locates a repository file by walking upward from the test execution directory.
    /// </summary>
    /// <param name="segments">Path segments from repository root to the target file.</param>
    /// <returns>Absolute path to the requested file.</returns>
    /// <remarks>
    /// Test data: relative path segments for files under test.
    /// This helper is used so tests can resolve files reliably across local and CI run directories.
    /// </remarks>
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

    private static string FindDirectoryFromRepoRoot(params string[] segments)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, Path.Combine(segments));
            if (Directory.Exists(candidate))
                return candidate;

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate directory '{Path.Combine(segments)}' from '{AppContext.BaseDirectory}'.");
    }

    private static int LineNumber(string content, int index)
        => content.Take(index).Count(c => c == '\n') + 1;
}
