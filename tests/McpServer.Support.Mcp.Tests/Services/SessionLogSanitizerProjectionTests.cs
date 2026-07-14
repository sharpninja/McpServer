using System.Text.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TEST-MCP-SESSIONLOGSAN-001: DTO graph and recursive payload sanitizer projection coverage.</summary>
public sealed class SessionLogSanitizerProjectionTests
{
    private const string Secret = "hunter2";

    /// <summary>Session DTO projection redacts every string-bearing DTO section and leaves the source object unchanged.</summary>
    [Fact]
    public void SanitizeSessionLog_ClonesAndRedactsDtoGraphWithoutMutatingSource()
    {
        ISessionLogSanitizer sanitizer = CreateSanitizer();
        var source = CreateSessionLog();

        var sanitized = sanitizer.SanitizeSessionLog(source);

        Assert.NotNull(sanitized);
        Assert.NotSame(source, sanitized);
        Assert.NotSame(source.Workspace, sanitized!.Workspace);
        Assert.NotSame(source.Turns, sanitized.Turns);
        var sourceTurn = Assert.Single(source.Turns ?? []);
        var sanitizedTurn = Assert.Single(sanitized.Turns ?? []);
        Assert.NotSame(sourceTurn, sanitizedTurn);
        Assert.NotSame(sourceTurn.Actions, sanitizedTurn.Actions);
        Assert.NotSame(sourceTurn.ProcessingDialog, sanitizedTurn.ProcessingDialog);
        Assert.NotSame(sourceTurn.Commits, sanitizedTurn.Commits);
        Assert.Equal(source.TurnCount, sanitized.TurnCount);
        Assert.Equal(source.TotalTokens, sanitized.TotalTokens);
        Assert.Equal(sourceTurn.TokenCount, sanitizedTurn.TokenCount);
        Assert.Equal(sourceTurn.Score, sanitizedTurn.Score);
        Assert.Equal(sourceTurn.IsPremium, sanitizedTurn.IsPremium);

        var sanitizedJson = JsonSerializer.Serialize(sanitized);
        Assert.DoesNotContain(Secret, sanitizedJson, StringComparison.Ordinal);
        Assert.Contains("[REDACTED:secret-assignment]", sanitizedJson, StringComparison.Ordinal);

        var sourceJson = JsonSerializer.Serialize(source);
        Assert.Contains(Secret, sourceJson, StringComparison.Ordinal);
    }

    /// <summary>Query result projection preserves paging metadata while cloning and sanitizing page items.</summary>
    [Fact]
    public void SanitizeQueryResult_ClonesItemsAndPreservesPaginationMetadata()
    {
        ISessionLogSanitizer sanitizer = CreateSanitizer();
        var source = new SessionLogQueryResult
        {
            TotalCount = 17,
            Limit = 5,
            Offset = 10,
            Items = [CreateSessionLog()],
        };

        var sanitized = sanitizer.SanitizeQueryResult(source);

        Assert.NotSame(source, sanitized);
        Assert.Equal(source.TotalCount, sanitized.TotalCount);
        Assert.Equal(source.Limit, sanitized.Limit);
        Assert.Equal(source.Offset, sanitized.Offset);
        Assert.NotSame(source.Items, sanitized.Items);
        Assert.NotSame(source.Items[0], sanitized.Items[0]);
        Assert.DoesNotContain(Secret, JsonSerializer.Serialize(sanitized), StringComparison.Ordinal);
        Assert.Contains(Secret, JsonSerializer.Serialize(source), StringComparison.Ordinal);
    }

    /// <summary>Recursive payload projection handles JsonElement, dictionaries, arrays, lists, primitive values, and unsupported object references.</summary>
    [Fact]
    public void SanitizeSessionLog_RecursivelySanitizesPayloadContainersAndPreservesPrimitiveValues()
    {
        ISessionLogSanitizer sanitizer = CreateSanitizer();
        var unsupported = new UnsupportedPayload("leave-me-alone");
        var session = CreateSessionLog();
        var turn = Assert.Single(session.Turns ?? []);
        turn.RawContext = new Dictionary<string, object?>
        {
            ["json"] = JsonDocument.Parse("""{"token":"password=hunter2","items":["secret=hunter2",42,true,null,{"inner":"api_key=hunter2"}]}""").RootElement.Clone(),
            ["dictionary"] = new Dictionary<string, object?> { ["secret"] = "password=hunter2", ["count"] = 7 },
            ["array"] = new object?[] { "secret=hunter2", 9, false, null },
            ["list"] = new List<object?> { "api_key=hunter2", 11, true, null },
            ["unsupported"] = unsupported,
        };

        var sanitized = sanitizer.SanitizeSessionLog(session);
        var sanitizedTurn = Assert.Single(sanitized!.Turns ?? []);
        var rawContext = Assert.IsType<Dictionary<string, object?>>(sanitizedTurn.RawContext);

        Assert.Same(unsupported, rawContext["unsupported"]);
        Assert.Equal(7, Assert.IsType<Dictionary<string, object?>>(rawContext["dictionary"])["count"]);
        Assert.DoesNotContain(Secret, JsonSerializer.Serialize(rawContext), StringComparison.Ordinal);
        Assert.Contains("[REDACTED:secret-assignment]", JsonSerializer.Serialize(rawContext), StringComparison.Ordinal);
        Assert.IsType<JsonElement>(rawContext["json"]);
        Assert.IsType<object?[]>(rawContext["array"]);
        Assert.IsType<List<object?>>(rawContext["list"]);
    }

    /// <summary>Recursive payload projection fails closed for excessive nesting and cycles.</summary>
    [Fact]
    public void SanitizeSessionLog_RedactsPayloadCyclesAndExcessiveDepth()
    {
        ISessionLogSanitizer sanitizer = CreateSanitizer();
        var cyclic = new Dictionary<string, object?> { ["secret"] = "password=hunter2" };
        cyclic["self"] = cyclic;
        var session = CreateSessionLog();
        var turn = Assert.Single(session.Turns ?? []);
        turn.RawContext = new Dictionary<string, object?>
        {
            ["cycle"] = cyclic,
            ["deep"] = BuildDeepPayload(40, "password=hunter2"),
        };

        var sanitized = sanitizer.SanitizeSessionLog(session);
        var sanitizedTurn = Assert.Single(sanitized!.Turns ?? []);
        var json = JsonSerializer.Serialize(sanitizedTurn.RawContext);

        Assert.DoesNotContain(Secret, json, StringComparison.Ordinal);
        Assert.Contains("[REDACTED:payload-cycle]", json, StringComparison.Ordinal);
        Assert.Contains("[REDACTED:payload-depth]", json, StringComparison.Ordinal);
    }

    private static ISessionLogSanitizer CreateSanitizer()
    {
        return new SessionLogSanitizer(Microsoft.Extensions.Options.Options.Create(new SessionLogSanitizationOptions()));
    }

    private static UnifiedSessionLogDto CreateSessionLog()
    {
        return new UnifiedSessionLogDto
        {
            SourceType = "Codex",
            SessionId = "Codex-20260714T000000Z-sanitizer",
            AgentDefinitionId = $"agent password={Secret}",
            Title = $"title password={Secret}",
            Model = $"model password={Secret}",
            Started = "2026-07-14T00:00:00Z",
            LastUpdated = "2026-07-14T00:01:00Z",
            Status = "completed",
            TurnCount = 1,
            TotalTokens = 123,
            CursorSessionLabel = $"label password={Secret}",
            CopilotStatistics = new CopilotStatisticsDto
            {
                AverageSuccessScore = 0.95,
                TotalNetTokens = 42,
                TotalNetPremiumRequests = 1,
                CompletedCount = 1,
                InProgressCount = 0,
            },
            Workspace = new WorkspaceInfoDto
            {
                Project = $"project password={Secret}",
                TargetFramework = $"framework password={Secret}",
                Repository = $"repo password={Secret}",
                Branch = $"branch password={Secret}",
            },
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = "req-20260714T000000Z-sanitizer",
                    Timestamp = "2026-07-14T00:00:10Z",
                    QueryText = $"query password={Secret}",
                    QueryTitle = $"query title password={Secret}",
                    Response = $"response password={Secret}",
                    Interpretation = $"interpretation password={Secret}",
                    Status = "completed",
                    Model = $"turn model password={Secret}",
                    ModelProvider = $"provider password={Secret}",
                    TokenCount = 12,
                    FailureNote = $"failure password={Secret}",
                    Score = 0.9,
                    IsPremium = true,
                    Tags = [$"tag password={Secret}"],
                    ContextList = [$"context password={Secret}"],
                    DesignDecisions = [$"decision password={Secret}"],
                    RequirementsDiscovered = [$"FR-MCP-SESSIONLOGSAN-001 password={Secret}"],
                    FilesModified = [$"file password={Secret}"],
                    Blockers = [$"blocker password={Secret}"],
                    RawContext = new Dictionary<string, object?> { ["raw"] = $"password={Secret}" },
                    OriginalEntry = new Dictionary<string, object?> { ["original"] = $"password={Secret}" },
                    Actions =
                    [
                        new UnifiedActionDto
                        {
                            Order = 1,
                            Description = $"action password={Secret}",
                            Type = $"type password={Secret}",
                            Status = $"status password={Secret}",
                            FilePath = $"path password={Secret}",
                        },
                    ],
                    ProcessingDialog =
                    [
                        new ProcessingDialogItemDto
                        {
                            Timestamp = "2026-07-14T00:00:11Z",
                            Role = $"role password={Secret}",
                            Content = $"content password={Secret}",
                            Category = $"category password={Secret}",
                        },
                    ],
                    Commits =
                    [
                        new SessionLogCommitDto
                        {
                            Sha = "abc123",
                            Branch = $"commit branch password={Secret}",
                            Message = $"message password={Secret}",
                            Author = $"author password={Secret}",
                            Timestamp = "2026-07-14T00:00:12Z",
                            FilesChanged = [$"changed password={Secret}"],
                        },
                    ],
                },
            ],
        };
    }

    private static Dictionary<string, object?> BuildDeepPayload(int depth, object? terminal)
    {
        var current = new Dictionary<string, object?> { ["value"] = terminal };
        for (var index = 0; index < depth; index++)
        {
            current = new Dictionary<string, object?> { ["next"] = current };
        }

        return current;
    }

    private sealed record UnsupportedPayload(string Value);
}
