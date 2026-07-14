using System.Reflection;
using System.Text.Json;
using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Tests.Models;

/// <summary>
/// TEST-MCP-AIUNIT-002: Verifies unified session-log JSON DTO contracts keep array
/// compatibility while CA1002 public <c>List&lt;T&gt;</c> exposures are removed.
/// </summary>
public sealed class UnifiedSessionLogDtoContractTests
{
    /// <summary>
    /// TEST-MCP-AIUNIT-002: Reflects over the public unified session-log DTO surface
    /// to prove W8 removes public <c>List&lt;T&gt;</c> properties from JSON models.
    /// </summary>
    [Fact]
    public void UnifiedSessionLogModels_DoNotExposePublicListProperties()
    {
        Type[] modelTypes =
        [
            typeof(UnifiedSessionLogDto),
            typeof(UnifiedRequestEntryDto),
            typeof(SessionLogCommitDto),
        ];

        var publicListProperties = modelTypes
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .Where(property => property.PropertyType.IsGenericType)
            .Where(property => property.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            .Select(property => $"{property.DeclaringType!.Name}.{property.Name}")
            .ToArray();

        Assert.Empty(publicListProperties);
    }

    /// <summary>
    /// TEST-MCP-AIUNIT-002: Deserializes and serializes JSON arrays used by the unified
    /// session-log DTO so contract remediation preserves API/schema compatibility.
    /// </summary>
    [Fact]
    public void UnifiedSessionLogDto_RoundTripsJsonArrayProperties()
    {
        const string json = """
            {
              "sourceType": "Codex",
              "sessionId": "Codex-20260712T111500Z-test",
              "turns": [
                {
                  "requestId": "req-20260712T111500Z-test",
                  "queryText": "Run tests",
                  "actions": [ { "type": "test", "status": "completed" } ],
                  "tags": [ "transcript-import" ],
                  "contextList": [ "ctx" ],
                  "processingDialog": [ { "role": "model", "content": "thinking" } ],
                  "commits": [ { "sha": "abc", "filesChanged": [ "src/File.cs" ] } ],
                  "designDecisions": [ "decision" ],
                  "requirementsDiscovered": [ "FR-MCP-139" ],
                  "filesModified": [ "src/File.cs" ],
                  "blockers": [ "none" ]
                }
              ]
            }
            """;

        var dto = JsonSerializer.Deserialize<UnifiedSessionLogDto>(json);

        Assert.NotNull(dto);
        var turn = Assert.Single(dto!.Turns ?? []);
        Assert.Equal("Run tests", turn.QueryText);
        Assert.Single(turn.Actions ?? []);
        Assert.Contains("transcript-import", turn.Tags ?? []);
        Assert.Contains("ctx", turn.ContextList ?? []);
        Assert.Single(turn.ProcessingDialog ?? []);
        var commit = Assert.Single(turn.Commits ?? []);
        Assert.Contains("src/File.cs", commit.FilesChanged ?? []);
        Assert.Contains("decision", turn.DesignDecisions ?? []);
        Assert.Contains("FR-MCP-139", turn.RequirementsDiscovered ?? []);
        Assert.Contains("src/File.cs", turn.FilesModified ?? []);
        Assert.Contains("none", turn.Blockers ?? []);

        var roundTripped = JsonSerializer.Serialize(dto);
        Assert.Contains("\"turns\"", roundTripped, StringComparison.Ordinal);
        Assert.Contains("\"filesChanged\"", roundTripped, StringComparison.Ordinal);
    }
}
