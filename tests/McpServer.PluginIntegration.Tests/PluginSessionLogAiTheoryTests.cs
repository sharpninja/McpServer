using System.Text.Json;
using Xunit;

namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// PLAN-PLUGINHANDOFF-001 C-red-P16: companion AiTheory rows for TEST-MCP-PLUGININT-001 AC3.
/// Fixture: isolated MCP host plus the catalog production entrypoint. The redacted receipt
/// is built from the canonical plugin turn result, not from catalog metadata alone.
/// Category=AiReview keeps live aiUnit rows on PluginSessionLogIntegration, not default Nuke Test.
/// </summary>
[Collection("PluginSessionLog")]
[Trait("PluginInt", "AI")]
[Trait("Category", "AiReview")]
public sealed class PluginSessionLogAiTheoryTests
{
    /// <summary>
    /// TEST-MCP-PLUGININT-001 AC3: companion evaluator must invoke SharpNinja.aiUnit,
    /// not a local JsonDocument success parser.
    /// </summary>
    [Fact]
    public void AiStrategyFixture_InvokesSharpNinjaAiUnitNotLocalJsonParser()
    {
        var path = Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "McpServer.PluginIntegration.Tests",
            "AiStrategyFixture.cs");
        var source = File.ReadAllText(path);
        Assert.DoesNotContain("JsonDocument.Parse", source, StringComparison.Ordinal);
        Assert.Contains("SharpNinja.AiUnit", source, StringComparison.Ordinal);
        Assert.Contains("SendAsync", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// P16 red: each agent row requires AiStrategyFixture JSON fields valid, missingFields, and contradictions.
    /// </summary>
    /// <param name="hostKind">One of the eight catalog host kinds.</param>
    [Theory(Timeout = 240000)]
    [InlineData(PluginHostKind.Codex)]
    [InlineData(PluginHostKind.ClaudeCode)]
    [InlineData(PluginHostKind.ClaudeCowork)]
    [InlineData(PluginHostKind.Copilot)]
    [InlineData(PluginHostKind.Grok)]
    [InlineData(PluginHostKind.Cline)]
    [InlineData(PluginHostKind.ClineV2)]
    [InlineData(PluginHostKind.OpenCode)]
    public async Task AiTheory_Agent_RequiresValidJsonFields(PluginHostKind hostKind)
    {
        var scenario = PluginSessionLogCatalog.LoadAndValidate(FindRepositoryRoot())
            .Single(row => row.HostKind == hostKind);
        await using var fixture = new PluginIntegrationServerFixture();
        await fixture.StartAsync(TestContext.Current.CancellationToken);
        var result = await new PluginSessionLogWorkflowAdapter().ExecuteCanonicalTurnAsync(
            scenario,
            fixture,
            TestContext.Current.CancellationToken);
        var receipt = new Dictionary<string, string>
        {
            ["agent"] = scenario.AgentSourceType,
            ["cacheFolder"] = scenario.CacheFolder,
            ["entrypoint"] = scenario.Entrypoint,
            ["status"] = result.Status,
            ["sessionId"] = result.SessionId,
            ["requestId"] = result.RequestId,
            ["sourceSha"] = result.SourceSha,
        };
        var redactedReceipt = JsonSerializer.Serialize(receipt);
        Assert.Contains(result.SessionId, redactedReceipt, StringComparison.Ordinal);
        var evaluation = await AiStrategyFixture.EvaluateAsync(redactedReceipt, TestContext.Current.CancellationToken);
        Assert.NotNull(evaluation);
        Assert.True(
            evaluation.Valid,
            hostKind + " receipt was not valid. missing=" + string.Join(",", evaluation.MissingFields)
            + " contradictions=" + string.Join(",", evaluation.Contradictions));
        Assert.NotNull(evaluation.MissingFields);
        Assert.Empty(evaluation.MissingFields);
        Assert.NotNull(evaluation.Contradictions);
        Assert.Empty(evaluation.Contradictions);
    }

    /// <summary>
    /// P17 green: the required model <c>valid</c> field is false even when arrays are empty.
    /// </summary>
    [Fact]
    public void AiTheory_ParseAiUnitPayload_ValidFalseEmptyArrays_IsInvalid()
    {
        var evaluation = AiStrategyFixture.ParseAiUnitPayload(
            "{\"valid\":false,\"missingFields\":[],\"contradictions\":[]}");
        Assert.False(evaluation.Valid);
        Assert.Empty(evaluation.MissingFields);
        Assert.Empty(evaluation.Contradictions);
    }

    /// <summary>
    /// P17 green: invalid JSON and deterministicFailure=true stay Valid=false. AI output never overrides deterministic failure.
    /// </summary>
    [Fact]
    public async Task AiTheory_RejectsInvalidJson_DoesNotOverrideDeterministicFailure()
    {
        var invalid = await AiStrategyFixture.EvaluateAsync("{not-json", TestContext.Current.CancellationToken);
        Assert.False(invalid.Valid);
        Assert.Contains("json", invalid.MissingFields);
        Assert.Contains("invalid-json", invalid.Contradictions);

        var missing = await AiStrategyFixture.EvaluateAsync("{\"agent\":\"Codex\"}", TestContext.Current.CancellationToken);
        Assert.False(missing.Valid);
        Assert.Contains("cacheFolder", missing.MissingFields);
        Assert.Contains("entrypoint", missing.MissingFields);
        Assert.Contains("status", missing.MissingFields);

        var deterministic = await AiStrategyFixture.EvaluateAsync(
            "{\"agent\":\"Codex\",\"cacheFolder\":\"codex\",\"entrypoint\":\"Invoke-CodexMcpPlugin.ps1\",\"status\":\"completed\",\"deterministicFailure\":true}",
            TestContext.Current.CancellationToken);
        Assert.False(deterministic.Valid);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "McpServer.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("McpServer.sln not found.");
    }
}
