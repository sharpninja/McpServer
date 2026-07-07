using System.Text;
using System.Text.Json;
using SharpNinja.AiUnit.Review;
using Abstractions = Xunit;

namespace McpServer.Review.Tests;

/// <summary>
/// aiUnit driven code and project reviews. The attributes cause the library to
/// execute the review (using appsettings.aiunit.json strategy) when the test runs.
/// The test bodies aggregate the resulting prompt + response into docs/reviews MD.
/// </summary>
public sealed class AiReviewTests
{
    private readonly Abstractions.ITestOutputHelper _output;

    // The prompts are supplied directly as attribute arguments (must be constants / literals).

    public AiReviewTests(Abstractions.ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Runs an aiUnit code review via the attribute. The test aggregates prompt and findings
    /// (from produced runlog) into a markdown file under docs/reviews.
    /// </summary>
    [Theory]
    [Trait("Category", "AiReview")]
    [AiCodeReview(@"
Review the implementation of the --agent CLI parameter support and per-agent cache isolation in the McpServer REPL and plugins.

Scope:
- src/McpServer.Repl.Host/Program.cs (CLI option, forwarding to resolver)
- src/McpServer.Repl.Host/MarkerFileClientOptionsResolver.cs (AgentOverride, GetCurrentAgent, VerifiedMarkerCacheEntry per agent)
- plugins/core/lib-sh/repl-invoke.sh, lib-ps/repl-invoke.ps1, lib-node/src/transport/repl-bridge.ts, repl-daemon.js, repl-persistent.sh (all must include --agent on every call)

Check for:
- Correct propagation of agent on every invocation
- Proper cache keying to prevent mixing Codex/Claude sessions
- Error handling, docs updates, requirements traceability (FR-MCP-REPL-008 etc.)
- No regressions in session log or trust bootstrap.

Return findings in the aiunit review format with severity, title, detail, recommendation, filePath, line.
")]
    public void CodeReview(string prompt, string responseJson)
    {
        // AiCodeReviewAttribute (via its data provider) supplies the prompt + the review result JSON.
        AggregateReviewToMarkdown("code", prompt, responseJson);
    }

    /// <summary>
    /// Runs an aiUnit project review via the attribute. Aggregates to MD under docs/reviews.
    /// </summary>
    [Theory]
    [Trait("Category", "AiReview")]
    [AiProjectReview(@"
Perform a full project review of the McpServer implementation focusing on the recent addition of --agent parameter support for REPL and plugins.

Review:
- CLI changes in Repl.Host
- Per-agent cache in resolver
- Enforcement in all plugin/core call sites (sh, ps, ts, daemon)
- Requirements and docs updates for FR-MCP-REPL-008 / TR-MCP-REPL-009
- Any impact on session logging, timeouts, trust bootstrap.

Provide structured findings with severity etc.
")]
    public void ProjectReview(string prompt, string responseJson)
    {
        // AiProjectReviewAttribute (via its data provider) supplies the prompt + the review result JSON.
        AggregateReviewToMarkdown("project", prompt, responseJson);
    }

    /// <summary>
    /// Runs an aiUnit governance review for warning suppression decisions and traceability.
    /// </summary>
    [Theory]
    [Trait("Category", "AiReview")]
    [AiProjectReview(@"
Review warning suppression governance for PLAN-WARNREMEDIATION-001.

Scope
- docs/Project requirement exports and live MCP requirements for FR-MCP-139, TR-MCP-QUALITY-001, TEST-MCP-AIUNIT-002
- PLAN-WARNREMEDIATION-001 TODO current decisions and implementation task state
- Directory.Build.props, project NoWarn entries, pragma warning directives, SuppressMessage attributes, editorconfig analyzer severity, and any broad warning bypasses
- tests/McpServer.Review.Tests/AiReviewTests.cs and build/Build.AiWarningSuppressionReview.cs

Approved decisions
- CA1416 may remain suppressed only for Windows only code paths with justification and review condition
- CA1819 may remain suppressed where array returning API is intentional and justified
- Current CA2227 suppressions may remain only for non observable JSON or YAML or options binding DTOs and EF navigation collections
- Observable collections must be repopulated in place rather than suppressed
- CA1308 is not approved and code must use invariant case insensitive comparison or explicit mapping rather than lowercase normalization
- CS8632 is not approved and every project must enable nullable annotations and remove CS8632 NoWarn entries
- TreatWarningsAsErrors false and stale ASP0019 suppressions are not approved and must remain removed

Completed remediation decisions to audit
- xUnit1051 is not approved and test projects must pass TestContext cancellation tokens to cancellable async APIs instead of suppressing the analyzer
- xUnit1041 is not approved and xUnit v3 tests must use supported fixture or ITestOutputHelper patterns instead of suppressing constructor injection diagnostics
- CA1812 is not approved and middleware or DI-only types must be made visible to analyzers through real construction or removed
- CA1848 is not approved and no editorconfig or project-level disable may remain for LoggerMessage guidance
- CA2000 is not approved and disposal warnings must be fixed or proven stale by removing the pragma and building clean
- CA1861 is not approved and constant array arguments must be hoisted rather than suppressed
- CA1062 is not approved and public migration methods must validate arguments rather than suppressing the rule
- CS0436 is not approved and stale type-conflict NoWarn entries must be removed
- CS0618 is not approved and obsolete APIs must be replaced with current APIs plus focused regression tests
- CA1055 is not approved and string return APIs must not advertise URI semantics
- NU5104 is not approved and stable packages must not depend on prerelease packages
- NU1901 and NU1903 are not approved and vulnerable package advisories must be resolved by package updates and a clean vulnerability scan

Acceptance criteria to audit
- Every suppression decision above is captured in TR-MCP-QUALITY-001 structured acceptance criteria
- TEST-MCP-AIUNIT-002 maps to TR-MCP-QUALITY-001 and has aiUnit prompt coverage
- PLAN-WARNREMEDIATION-001 lists approved suppressions separately from required code fixes and marks only validated work done
- No unapproved warning suppression or broad warning bypass is introduced or marked complete without build or test evidence
- Generated requirements documents and traceability mappings include the FR, TR, and TEST records

Return structured findings in the aiUnit review format with severity, title, detail, recommendation, filePath, and line.
Report no findings only if every item above is satisfied by durable artifacts.
")]
    public void WarningSuppressionGovernanceReview(string prompt, string responseJson)
    {
        // AiProjectReviewAttribute supplies the governance prompt and review result JSON.
        AggregateReviewToMarkdown("warning-suppression", prompt, responseJson);
    }

    private void AggregateReviewToMarkdown(string reviewType, string? suppliedPrompt = null, string? suppliedResponseJson = null)
    {
        var root = FindRepositoryRoot();

        var candidateDirs = new[]
        {
            Path.Combine(root, "artifacts", $"aiunit-{reviewType}-review"),
            Path.Combine(root, "tests", "McpServer.Review.Tests", "bin", "Debug", "net10.0", "aiunit-results"),
            Path.Combine(AppContext.BaseDirectory, "aiunit-results"),
            Path.Combine(root, "TestResults")
        };

        string? latestRunLog = null;
        foreach (var dir in candidateDirs)
        {
            if (!Directory.Exists(dir)) continue;
            var files = Directory.GetFiles(dir, $"*review*{reviewType}*.json", SearchOption.AllDirectories)
                .OrderByDescending(f => f).ToArray();
            if (files.Length > 0) { latestRunLog = files[0]; break; }
        }

        if (latestRunLog == null || !File.Exists(latestRunLog))
        {
            var fb = Path.Combine(root, "artifacts", $"aiunit-{reviewType}-review");
            if (Directory.Exists(fb))
            {
                latestRunLog = Directory.GetFiles(fb, $"aiunit-review-{reviewType}-*.json").OrderByDescending(x => x).FirstOrDefault();
            }
        }

        string prompt = suppliedPrompt ?? "prompt supplied to attribute";
        string findingsJson = suppliedResponseJson ?? "{}";

        if (string.IsNullOrWhiteSpace(suppliedResponseJson) && latestRunLog != null && File.Exists(latestRunLog))
        {
            var resultJson = File.ReadAllText(latestRunLog);
            using var document = JsonDocument.Parse(resultJson);
            var rootEl = document.RootElement;
            prompt = rootEl.TryGetProperty("prompt", out var p) ? (p.GetString() ?? prompt) : prompt;
            findingsJson = rootEl.TryGetProperty("findings", out var f) ? f.GetRawText() :
                           rootEl.TryGetProperty("resultJson", out var r) ? (r.GetString() ?? resultJson) : resultJson;
        }
        else if (string.IsNullOrWhiteSpace(suppliedResponseJson))
        {
            _output.WriteLine($"No runlog or supplied response for {reviewType}; writing MD using supplied prompt only.");
        }

        var reviewsDir = Path.Combine(root, "docs", "reviews");
        Directory.CreateDirectory(reviewsDir);

        var ts = DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmss.fffZ");
        var baseName = $"aiunit-review-{reviewType}-{ts}";
        var mdPath = Path.Combine(reviewsDir, baseName + ".md");

        var markdown = new StringBuilder()
            .Append("# aiUnit Review: ").AppendLine(reviewType)
            .AppendLine()
            .Append("- Run-log: `").Append(latestRunLog != null ? Path.GetFileName(latestRunLog) : "from attribute execution").AppendLine("`")
            .AppendLine()
            .AppendLine("## Prompt")
            .AppendLine()
            .AppendLine("```text")
            .AppendLine(prompt.TrimEnd())
            .AppendLine("```")
            .AppendLine()
            .AppendLine("## Response")
            .AppendLine()
            .AppendLine("```json")
            .AppendLine(findingsJson.TrimEnd())
            .AppendLine("```")
            .ToString();

        File.WriteAllText(mdPath, markdown);
        _output.WriteLine("aiUnit review markdown written by test: " + mdPath);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "docs", "Project")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        // fallback to current
        return Directory.GetCurrentDirectory();
    }
}
