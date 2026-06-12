using System.Text.Json;
using SharpNinja.AiUnit.Review;
using Xunit.Abstractions;

namespace McpServer.PlanReview.Tests;

/// <summary>
/// TEST-MCP-164: aiUnit plan review gate for PLAN-TURNTRANSACTIONS-001.
/// </summary>
public sealed class PlanTransactionReviewTests
{
    private readonly ITestOutputHelper _output;

    /// <summary>Initializes a new instance of the <see cref="PlanTransactionReviewTests"/> class.</summary>
    /// <param name="output">xUnit output helper.</param>
    public PlanTransactionReviewTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// FR-MCP-124 and TR-MCP-TXNAIUNIT-001: Reviews the transaction plan and
    /// fails when aiUnit reports critical or high findings.
    /// </summary>
    /// <param name="prompt">Effective aiUnit prompt.</param>
    /// <param name="resultJson">aiUnit review findings JSON.</param>
    [Theory]
    [AiPlanReview(
        "Review PLAN-TURNTRANSACTIONS-001 in F:\\GitHub\\McpServer. " +
        "Scope: docs/Project/Quad-Model-Transactional-Diffgram-Plan.md, " +
        "docs/Project/TurnTransactions-Architecture-Round1.md, " +
        "docs/Project/TurnTransactions-Design-Round2.md, " +
        "docs/Project/Functional-Requirements.md FR-MCP-118 through FR-MCP-128, " +
        "docs/Project/Technical-Requirements.md TR-MCP-KEYSERVER-001 through TR-MCP-TXNDESIGN-001, " +
        "docs/Project/Testing-Requirements.md TEST-MCP-158 through TEST-MCP-173, " +
        "and the current transaction-security implementation/tests. " +
        "Do not edit files. Treat explicitly documented deferred work as non-blocking for this review unless it hides an untracked safety, correctness, or validation gap. " +
        "Flag critical/high only for issues that should block continuing the next PLAN-TURNTRANSACTIONS-001 slice.",
        Agent = "claude")]
    public void PLAN_TURNTRANSACTIONS_001_HasNoCriticalOrHighPlanFindings(
        string prompt,
        string resultJson)
    {
        _output.WriteLine("aiUnit prompt:");
        _output.WriteLine(prompt);
        _output.WriteLine("aiUnit resultJson:");
        _output.WriteLine(resultJson);

        using var document = JsonDocument.Parse(resultJson);
        var root = document.RootElement;
        Assert.Equal(AiReviewFindingsSchema.SchemaVersion, RequiredString(root, "schemaVersion"));
        Assert.Equal("plan", RequiredString(root, "reviewType"));
        Assert.NotEqual("error", RequiredString(root, "status"));

        var blocking = root
            .GetProperty("findings")
            .EnumerateArray()
            .Where(IsCriticalOrHigh)
            .Select(DescribeFinding)
            .ToArray();

        Assert.Empty(blocking);

        Assert.True(root.TryGetProperty("runLog", out var runLog), "aiUnit result must include runLog evidence.");
        var runLogPath = RequiredString(runLog, "path");
        _output.WriteLine("aiUnit runLog.path: " + runLogPath);
        Assert.True(File.Exists(runLogPath), "aiUnit run-log file must exist: " + runLogPath);
        Assert.StartsWith("aiunit-review-plan-", Path.GetFileName(runLogPath), StringComparison.Ordinal);
        Assert.EndsWith(".json", runLogPath, StringComparison.OrdinalIgnoreCase);
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
