using System.Text;
using System.Text.Json;
using SharpNinja.AiUnit.Review;

namespace McpServer.PlanReview.Tests;

/// <summary>
/// TEST-MCP-164: aiUnit plan review gate for PLAN-TURNTRANSACTIONS-001.
/// </summary>
public sealed class PlanTransactionReviewTests
{
    private const string ReviewArtifactRelativePath = "artifacts/aiunit-plan-review/aiunit-review-plan-20260612T060729.901Z.json";

    private readonly ITestOutputHelper _output;

    /// <summary>Initializes a new instance of the <see cref="PlanTransactionReviewTests"/> class.</summary>
    /// <param name="output">xUnit output helper.</param>
    public PlanTransactionReviewTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// FR-MCP-126 and TR-MCP-TXNAIUNIT-001: Reviews the transaction plan and
    /// fails when aiUnit reports critical or high findings.
    /// </summary>
    [Fact]
    public void PLAN_TURNTRANSACTIONS_001_HasNoCriticalOrHighPlanFindings()
    {
        var runLogPath = Path.Combine(FindRepositoryRoot(), ReviewArtifactRelativePath);
        Assert.True(File.Exists(runLogPath), "aiUnit run-log file must exist: " + runLogPath);

        var resultJson = File.ReadAllText(runLogPath);
        _output.WriteLine("aiUnit prompt:");
        using var document = JsonDocument.Parse(resultJson);
        var root = document.RootElement;
        var prompt = RequiredString(root, "prompt");
        _output.WriteLine(prompt);
        _output.WriteLine("aiUnit resultJson:");
        _output.WriteLine(resultJson);

        Assert.Equal("aiunit.review.runlog.v1", RequiredString(root, "schemaVersion"));
        var reviewType = RequiredString(root, "reviewType");
        Assert.Equal("plan", reviewType);
        Assert.Contains("PLAN-TURNTRANSACTIONS-001", prompt, StringComparison.Ordinal);
        Assert.Contains("FR-MCP-118 through FR-MCP-128", prompt, StringComparison.Ordinal);
        Assert.Contains("TEST-MCP-158 through TEST-MCP-173", prompt, StringComparison.Ordinal);

        Assert.True(root.TryGetProperty("findings", out var findings), "aiUnit run-log must include findings evidence.");

        // Persist the prompt and response (findings) to a timestamped markdown artifact under docs/reviews so
        // every run leaves a durable, human-readable record. Written before the blocking assertion below so the
        // artifact is produced even when the review fails the gate.
        var savedReview = SaveReviewArtifactMarkdown(
            Path.Combine(FindRepositoryRoot(), "docs", "reviews"),
            Path.GetFileNameWithoutExtension(runLogPath),
            reviewType,
            runLogPath,
            prompt,
            findings.GetRawText());
        _output.WriteLine("aiUnit review markdown: " + savedReview);
        Assert.Equal(AiReviewFindingsSchema.SchemaVersion, RequiredString(findings, "schemaVersion"));
        Assert.Equal("plan", RequiredString(findings, "reviewType"));
        Assert.NotEqual("error", RequiredString(findings, "status"));

        var blocking = findings
            .GetProperty("findings")
            .EnumerateArray()
            .Where(IsCriticalOrHigh)
            .Select(DescribeFinding)
            .ToArray();

        Assert.Empty(blocking);

        _output.WriteLine("aiUnit runLog.path: " + runLogPath);
        Assert.StartsWith("aiunit-review-plan-", Path.GetFileName(runLogPath), StringComparison.Ordinal);
        Assert.EndsWith(".json", runLogPath, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "docs", "Project")) &&
                Directory.Exists(Path.Combine(directory.FullName, "artifacts", "aiunit-plan-review")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from " + AppContext.BaseDirectory);
    }

    /// <summary>
    /// Writes the review prompt and response (findings JSON) to a timestamped markdown file under docs/reviews.
    /// The file name carries the aiUnit run-log timestamp so the artifact is traceable and re-running the same
    /// review overwrites rather than accumulates. Returns the absolute path written.
    /// </summary>
    private static string SaveReviewArtifactMarkdown(
        string reviewsDirectory,
        string runLogBaseName,
        string reviewType,
        string runLogPath,
        string prompt,
        string responseJson)
    {
        Directory.CreateDirectory(reviewsDirectory);
        var path = Path.Combine(reviewsDirectory, runLogBaseName + ".md");

        var markdown = new StringBuilder()
            .Append("# aiUnit Review: ").AppendLine(reviewType)
            .AppendLine()
            .Append("- Run-log: `").Append(Path.GetFileName(runLogPath)).AppendLine("`")
            .Append("- Source: `").Append(runLogPath).AppendLine("`")
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
            .AppendLine(responseJson.TrimEnd())
            .AppendLine("```")
            .ToString();

        File.WriteAllText(path, markdown);
        return path;
    }

    private static bool IsCriticalOrHigh(JsonElement finding)
    {
        if (!finding.TryGetProperty("severity", out var severityElement) ||
            severityElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var severity = severityElement.GetString();
        return string.Equals(severity, "critical", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(severity, "high", StringComparison.OrdinalIgnoreCase);
    }

    private static string DescribeFinding(JsonElement finding)
    {
        var severity = OptionalString(finding, "severity");
        var title = OptionalString(finding, "title");
        var detail = OptionalString(finding, "detail");
        return severity + ": " + title + " - " + detail;
    }

    private static string RequiredString(JsonElement element, string propertyName)
    {
        Assert.True(element.TryGetProperty(propertyName, out var property), propertyName + " is required.");
        Assert.Equal(JsonValueKind.String, property.ValueKind);
        var value = property.GetString();
        Assert.False(string.IsNullOrWhiteSpace(value), propertyName + " must not be empty.");
        return value!;
    }

    private static string OptionalString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
}
