using System.Text;
using System.Text.Json;
using SharpNinja.AiUnit.Review;
using Abstractions = Xunit.Abstractions;

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
