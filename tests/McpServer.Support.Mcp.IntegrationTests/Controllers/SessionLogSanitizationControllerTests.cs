using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>
/// TEST-MCP-SESSIONLOGSAN-001 / FR-MCP-SESSIONLOGSAN-001: HTTP query and GET sanitization
/// for a raw-secret fixture across the session-log DTO graph, plus query-semantics
/// (filter, TotalCount, order, Limit, Offset computed on raw data).
/// </summary>
[Trait("Category", "Integration")]
public sealed class SessionLogSanitizationControllerTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    /// <summary>Default provider-token pattern already detected by SessionLogSanitizer.</summary>
    internal const string Secret = "sk-test-sessionlog-secret-001";

    private const string RedactedToken = "[REDACTED:provider-token]";
    private const string SourceTypeS15 = "SanitizerS15";
    private const string SourceTypeS16 = "SanitizerS16";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    /// <summary>Creates an authenticated client against the shared integration host.</summary>
    /// <param name="factory">Web application factory fixture.</param>
    public SessionLogSanitizationControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(_client, factory.Services);
    }

    /// <inheritdoc />
    public void Dispose() => _client.Dispose();

    /// <summary>
    /// S15: POST a session with the default provider-token secret in every DTO section.
    /// Query and GET HTTP responses must replace the secret; SQLite rows remain raw.
    /// Fixture: CustomWebApplicationFactory. Validates TEST-MCP-SESSIONLOGSAN-001 / FR-MCP-SESSIONLOGSAN-001.
    /// </summary>
    [Fact]
    public async Task S15_QueryAndGetHttpResponses_ReplaceSecretsInEveryDtoSection_WhileDbRowsRemainUnsanitized()
    {
        var sessionId = BuildSessionId(SourceTypeS15, $"s15-{Guid.NewGuid():N}");
        var dto = CreateSecretSession(SourceTypeS15, sessionId, "2026-08-20T15:00:00Z");

        var post = await _client.PostAsJsonAsync(
            new Uri("/mcpserver/sessionlog", UriKind.Relative),
            dto,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var postBody = await post.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(post.StatusCode == HttpStatusCode.Created, $"S15 POST expected 201, got {(int)post.StatusCode}: {postBody}");

        var queryResponse = await _client.GetAsync(
            new Uri($"/mcpserver/sessionlog?agent={SourceTypeS15}&limit=100", UriKind.Relative),
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, queryResponse.StatusCode);
        var queryBody = await queryResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        var query = JsonSerializer.Deserialize<SessionLogQueryResult>(queryBody, JsonOptions);
        Assert.NotNull(query);
        var queried = Assert.Single(query!.Items, item => item.SessionId == sessionId);
        AssertHttpBodyRedactsSecret("query", queryBody);
        AssertDtoSectionsRedacted(queried);

        var getResponse = await _client.GetAsync(
            new Uri($"/mcpserver/sessionlog/{SourceTypeS15}/{sessionId}", UriKind.Relative),
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var getBody = await getResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        var fetched = JsonSerializer.Deserialize<UnifiedSessionLogDto>(getBody, JsonOptions);
        Assert.NotNull(fetched);
        Assert.Equal(sessionId, fetched!.SessionId);
        AssertHttpBodyRedactsSecret("get", getBody);
        AssertDtoSectionsRedacted(fetched);

        await AssertDatabaseStillContainsRawSecretAsync(sessionId).ConfigureAwait(true);
    }

    /// <summary>
    /// S16: a secret-containing raw record still participates in text filtering.
    /// TotalCount, LastUpdated order, Limit, and Offset are computed from raw rows
    /// and remain unchanged after outbound redaction.
    /// Fixture: CustomWebApplicationFactory. Validates TEST-MCP-SESSIONLOGSAN-001 / FR-MCP-SESSIONLOGSAN-001.
    /// </summary>
    [Fact]
    public async Task S16_QueryTextFilter_SecretContainingRawRecordStillParticipates_AndPagingMetadataUnchanged()
    {
        var nonce = Guid.NewGuid().ToString("N");
        var decoyId = BuildSessionId(SourceTypeS16, $"s16-decoy-{nonce}");
        var olderSecretId = BuildSessionId(SourceTypeS16, $"s16-older-{nonce}");
        var newerSecretId = BuildSessionId(SourceTypeS16, $"s16-newer-{nonce}");

        await PostSessionAsync(CreatePlainSession(SourceTypeS16, decoyId, "2026-08-20T12:00:00Z", $"harmless query without credentials {nonce}")).ConfigureAwait(true);
        await PostSessionAsync(WithQueryNonce(CreateSecretSession(SourceTypeS16, olderSecretId, "2026-08-20T13:00:00Z"), nonce)).ConfigureAwait(true);
        await PostSessionAsync(WithQueryNonce(CreateSecretSession(SourceTypeS16, newerSecretId, "2026-08-20T14:00:00Z"), nonce)).ConfigureAwait(true);

        var encodedFilter = Uri.EscapeDataString($"{Secret} {nonce}");
        var firstPageResponse = await _client.GetAsync(
            new Uri($"/mcpserver/sessionlog?agent={SourceTypeS16}&text={encodedFilter}&limit=1&offset=0", UriKind.Relative),
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, firstPageResponse.StatusCode);
        var firstPageBody = await firstPageResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        var firstPage = JsonSerializer.Deserialize<SessionLogQueryResult>(firstPageBody, JsonOptions);
        Assert.NotNull(firstPage);
        Assert.Equal(2, firstPage!.TotalCount);
        Assert.Equal(1, firstPage.Limit);
        Assert.Equal(0, firstPage.Offset);
        var firstItem = Assert.Single(firstPage.Items);
        Assert.Equal(newerSecretId, firstItem.SessionId);
        Assert.DoesNotContain(decoyId, firstPageBody, StringComparison.Ordinal);
        AssertHttpBodyRedactsSecret("s16-offset-0", firstPageBody);

        var secondPageResponse = await _client.GetAsync(
            new Uri($"/mcpserver/sessionlog?agent={SourceTypeS16}&text={encodedFilter}&limit=1&offset=1", UriKind.Relative),
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, secondPageResponse.StatusCode);
        var secondPageBody = await secondPageResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        var secondPage = JsonSerializer.Deserialize<SessionLogQueryResult>(secondPageBody, JsonOptions);
        Assert.NotNull(secondPage);
        Assert.Equal(2, secondPage!.TotalCount);
        Assert.Equal(1, secondPage.Limit);
        Assert.Equal(1, secondPage.Offset);
        var secondItem = Assert.Single(secondPage.Items);
        Assert.Equal(olderSecretId, secondItem.SessionId);
        Assert.DoesNotContain(decoyId, secondPageBody, StringComparison.Ordinal);
        AssertHttpBodyRedactsSecret("s16-offset-1", secondPageBody);
    }

    private async Task PostSessionAsync(UnifiedSessionLogDto dto)
    {
        var response = await _client.PostAsJsonAsync(
            new Uri("/mcpserver/sessionlog", UriKind.Relative),
            dto,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(response.StatusCode == HttpStatusCode.Created, $"POST {dto.SessionId} expected 201, got {(int)response.StatusCode}: {body}");
    }

    private async Task AssertDatabaseStillContainsRawSecretAsync(string sessionId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        db.OverrideWorkspaceId(_factory.WorkspacePath);

        var entity = await db.SessionLogs
            .IgnoreQueryFilters()
            .Include(session => session.Tags)
            .Include(session => session.Turns)
                .ThenInclude(turn => turn.Actions)
            .Include(session => session.Turns)
                .ThenInclude(turn => turn.Tags)
            .Include(session => session.Turns)
                .ThenInclude(turn => turn.ContextItems)
            .Include(session => session.Turns)
                .ThenInclude(turn => turn.ProcessingDialog)
            .Include(session => session.Turns)
                .ThenInclude(turn => turn.Commits)
                    .ThenInclude(commit => commit.Files)
            .Include(session => session.Turns)
                .ThenInclude(turn => turn.StringListItems)
            .AsSplitQuery()
            .SingleAsync(session => session.SessionId == sessionId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        AssertContainsSecret("session.Title", entity.Title);
        AssertContainsSecret("session.Model", entity.Model);
        AssertContainsSecret("session.CursorSessionLabel", entity.CursorSessionLabel);
        AssertContainsSecret("session.AgentSessionId", entity.AgentSessionId);
        AssertContainsSecret("session.AgentSessionTranscriptFile", entity.AgentSessionTranscriptFile);
        AssertContainsSecret("session.AgentExecutablePath", entity.AgentExecutablePath);
        AssertContainsSecret("session.AgentExecutableVersion", entity.AgentExecutableVersion);
        AssertContainsSecret("session.Project", entity.Project);
        AssertContainsSecret("session.TargetFramework", entity.TargetFramework);
        AssertContainsSecret("session.Repository", entity.Repository);
        AssertContainsSecret("session.Branch", entity.Branch);
        AssertContainsSecret("session.Tags", Assert.Single(entity.Tags).Tag);

        var turn = Assert.Single(entity.Turns);
        AssertContainsSecret("turn.QueryText", turn.QueryText);
        AssertContainsSecret("turn.QueryTitle", turn.QueryTitle);
        AssertContainsSecret("turn.Response", turn.Response);
        AssertContainsSecret("turn.Interpretation", turn.Interpretation);
        AssertContainsSecret("turn.FailureNote", turn.FailureNote);
        AssertContainsSecret("turn.Model", turn.Model);
        AssertContainsSecret("turn.ModelProvider", turn.ModelProvider);
        AssertContainsSecret("turn.PlanFile", turn.PlanFile);
        AssertContainsSecret("turn.RawContextJson", turn.RawContextJson);
        AssertContainsSecret("turn.OriginalEntryJson", turn.OriginalEntryJson);
        AssertContainsSecret("turn.Tags", Assert.Single(turn.Tags).Tag);
        AssertContainsSecret("turn.ContextItems", Assert.Single(turn.ContextItems).ContextItem);

        var action = Assert.Single(turn.Actions);
        AssertContainsSecret("action.Description", action.Description);
        AssertContainsSecret("action.Type", action.Type);
        AssertContainsSecret("action.Status", action.Status);
        AssertContainsSecret("action.FilePath", action.FilePath);

        var dialog = Assert.Single(turn.ProcessingDialog);
        AssertContainsSecret("dialog.Role", dialog.Role);
        AssertContainsSecret("dialog.Content", dialog.Content);
        AssertContainsSecret("dialog.Category", dialog.Category);

        var commit = Assert.Single(turn.Commits);
        AssertContainsSecret("commit.Branch", commit.Branch);
        AssertContainsSecret("commit.Message", commit.Message);
        AssertContainsSecret("commit.Author", commit.Author);
        AssertContainsSecret("commit.Files", Assert.Single(commit.Files).Path);

        AssertContainsSecret("designDecision", SingleListValue(turn, "DesignDecision"));
        AssertContainsSecret("requirement", SingleListValue(turn, "Requirement"));
        AssertContainsSecret("fileModified", SingleListValue(turn, "FileModified"));
        AssertContainsSecret("blocker", SingleListValue(turn, "Blocker"));
    }

    private static void AssertDtoSectionsRedacted(UnifiedSessionLogDto dto)
    {
        AssertDoesNotContainSecret("title", dto.Title);
        AssertDoesNotContainSecret("model", dto.Model);
        AssertDoesNotContainSecret("cursorSessionLabel", dto.CursorSessionLabel);
        AssertDoesNotContainSecret("agentSessionId", dto.AgentSessionId);
        AssertDoesNotContainSecret("agentSessionTranscriptFile", dto.AgentSessionTranscriptFile);
        AssertDoesNotContainSecret("agentExecutablePath", dto.AgentExecutablePath);
        AssertDoesNotContainSecret("agentExecutableVersion", dto.AgentExecutableVersion);
        Assert.NotNull(dto.Workspace);
        AssertDoesNotContainSecret("workspace.project", dto.Workspace!.Project);
        AssertDoesNotContainSecret("workspace.targetFramework", dto.Workspace.TargetFramework);
        AssertDoesNotContainSecret("workspace.repository", dto.Workspace.Repository);
        AssertDoesNotContainSecret("workspace.branch", dto.Workspace.Branch);
        AssertDoesNotContainSecret("session.tags", Assert.Single(dto.Tags ?? []));

        var turn = Assert.Single(dto.Turns ?? []);
        AssertDoesNotContainSecret("queryText", turn.QueryText);
        AssertDoesNotContainSecret("queryTitle", turn.QueryTitle);
        AssertDoesNotContainSecret("response", turn.Response);
        AssertDoesNotContainSecret("interpretation", turn.Interpretation);
        AssertDoesNotContainSecret("failureNote", turn.FailureNote);
        AssertDoesNotContainSecret("turn.model", turn.Model);
        AssertDoesNotContainSecret("modelProvider", turn.ModelProvider);
        AssertDoesNotContainSecret("planFile", turn.PlanFile);
        AssertDoesNotContainSecret("turn.tags", Assert.Single(turn.Tags ?? []));
        AssertDoesNotContainSecret("contextList", Assert.Single(turn.ContextList ?? []));
        AssertDoesNotContainSecret("designDecisions", Assert.Single(turn.DesignDecisions ?? []));
        AssertDoesNotContainSecret("requirementsDiscovered", Assert.Single(turn.RequirementsDiscovered ?? []));
        AssertDoesNotContainSecret("filesModified", Assert.Single(turn.FilesModified ?? []));
        AssertDoesNotContainSecret("blockers", Assert.Single(turn.Blockers ?? []));

        var action = Assert.Single(turn.Actions ?? []);
        AssertDoesNotContainSecret("action.description", action.Description);
        AssertDoesNotContainSecret("action.type", action.Type);
        AssertDoesNotContainSecret("action.status", action.Status);
        AssertDoesNotContainSecret("action.filePath", action.FilePath);

        var dialog = Assert.Single(turn.ProcessingDialog ?? []);
        AssertDoesNotContainSecret("dialog.role", dialog.Role);
        AssertDoesNotContainSecret("dialog.content", dialog.Content);
        AssertDoesNotContainSecret("dialog.category", dialog.Category);

        var commit = Assert.Single(turn.Commits ?? []);
        AssertDoesNotContainSecret("commit.branch", commit.Branch);
        AssertDoesNotContainSecret("commit.message", commit.Message);
        AssertDoesNotContainSecret("commit.author", commit.Author);
        AssertDoesNotContainSecret("commit.filesChanged", Assert.Single(commit.FilesChanged ?? []));

        Assert.Contains(RedactedToken, dto.Title, StringComparison.Ordinal);
        Assert.Contains(RedactedToken, turn.QueryText, StringComparison.Ordinal);
        Assert.Contains(RedactedToken, action.Description, StringComparison.Ordinal);
        Assert.Contains(RedactedToken, dialog.Content, StringComparison.Ordinal);
        Assert.Contains(RedactedToken, Assert.Single(turn.ContextList ?? []), StringComparison.Ordinal);
        Assert.Contains(RedactedToken, Assert.Single(turn.FilesModified ?? []), StringComparison.Ordinal);
        Assert.Contains(RedactedToken, Assert.Single(turn.Blockers ?? []), StringComparison.Ordinal);
        Assert.Contains(RedactedToken, Assert.Single(turn.RequirementsDiscovered ?? []), StringComparison.Ordinal);
        Assert.Contains(RedactedToken, commit.Message, StringComparison.Ordinal);
        Assert.Contains(RedactedToken, Assert.Single(dto.Tags ?? []), StringComparison.Ordinal);
    }

    private static void AssertHttpBodyRedactsSecret(string label, string body)
    {
        Assert.False(body.Contains(Secret, StringComparison.Ordinal), $"{label} HTTP body still contains the raw secret.");
        Assert.Contains(RedactedToken, body, StringComparison.Ordinal);
    }

    private static void AssertContainsSecret(string field, string? value)
    {
        Assert.False(string.IsNullOrWhiteSpace(value), $"{field} was empty in the database.");
        Assert.Contains(Secret, value, StringComparison.Ordinal);
    }

    private static void AssertDoesNotContainSecret(string field, string? value)
    {
        Assert.False(string.IsNullOrWhiteSpace(value), $"{field} was empty in the HTTP DTO.");
        Assert.DoesNotContain(Secret, value, StringComparison.Ordinal);
        Assert.Contains(RedactedToken, value, StringComparison.Ordinal);
    }

    private static string SingleListValue(SessionLogTurnEntity turn, string listType)
    {
        return Assert.Single(turn.StringListItems, item => item.ListType == listType).Value;
    }

    private static UnifiedSessionLogDto CreateSecretSession(string sourceType, string sessionId, string timestamp)
    {
        var marked = WithSecret;
        return new UnifiedSessionLogDto
        {
            SourceType = sourceType,
            SessionId = sessionId,
            Title = marked("title"),
            Model = marked("model"),
            Started = timestamp,
            LastUpdated = timestamp,
            Status = "completed",
            TurnCount = 1,
            CursorSessionLabel = marked("label"),
            AgentSessionId = marked("agent-session"),
            AgentSessionTranscriptFile = marked("transcript"),
            AgentExecutablePath = marked("executable"),
            AgentExecutableVersion = marked("version"),
            Tags = [marked("session-tag")],
            Workspace = new WorkspaceInfoDto
            {
                Project = marked("project"),
                TargetFramework = marked("net"),
                Repository = marked("repo"),
                Branch = marked("branch"),
            },
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = "req-20260820T150000Z-s15-secret-fixture",
                    Timestamp = timestamp,
                    QueryText = marked("query"),
                    QueryTitle = marked("query-title"),
                    Response = marked("response"),
                    Interpretation = marked("interpretation"),
                    Status = "completed",
                    FailureNote = marked("failure"),
                    Model = marked("turn-model"),
                    ModelProvider = marked("provider"),
                    PlanFile = $"docs/plans/{Secret}.md",
                    TodoId = "PLAN-SESSIONLOGREMEDIATE-001",
                    Tags = [marked("turn-tag")],
                    ContextList = [marked("context")],
                    DesignDecisions = [marked("decision")],
                    RequirementsDiscovered = [marked("FR-MCP-SESSIONLOGSAN-001")],
                    FilesModified = [$"docs/{Secret}.md"],
                    Blockers = [marked("blocker")],
                    RawContext = new Dictionary<string, object?> { ["raw"] = marked("raw") },
                    OriginalEntry = new Dictionary<string, object?> { ["original"] = marked("original") },
                    Actions =
                    [
                        new UnifiedActionDto
                        {
                            Order = 1,
                            Description = marked("action"),
                            Type = marked("type"),
                            Status = marked("status"),
                            FilePath = $"docs/{Secret}.cs",
                        },
                    ],
                    ProcessingDialog =
                    [
                        new ProcessingDialogItemDto
                        {
                            Timestamp = timestamp,
                            Role = marked("role"),
                            Content = marked("dialog"),
                            Category = marked("category"),
                        },
                    ],
                    Commits =
                    [
                        new SessionLogCommitDto
                        {
                            Sha = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                            Branch = marked("commit-branch"),
                            Message = marked("commit-message"),
                            Author = marked("author"),
                            Timestamp = timestamp,
                            FilesChanged = [$"docs/{Secret}.txt"],
                        },
                    ],
                },
            ],
        };
    }

    private static UnifiedSessionLogDto WithQueryNonce(UnifiedSessionLogDto dto, string nonce)
    {
        var turn = Assert.Single(dto.Turns ?? []);
        turn.QueryText = $"{turn.QueryText} {nonce}";
        return dto;
    }

    private static UnifiedSessionLogDto CreatePlainSession(string sourceType, string sessionId, string timestamp, string queryText)
    {
        return new UnifiedSessionLogDto
        {
            SourceType = sourceType,
            SessionId = sessionId,
            Title = "plain session",
            Model = "gpt-4",
            Started = timestamp,
            LastUpdated = timestamp,
            Status = "completed",
            TurnCount = 1,
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = "req-20260820T120000Z-s16-plain-decoy",
                    Timestamp = timestamp,
                    QueryText = queryText,
                    Response = "plain response",
                    Status = "completed",
                    PlanFile = "None",
                    TodoId = "None",
                },
            ],
        };
    }

    private static string WithSecret(string prefix) => $"{prefix} {Secret}";

    private static string BuildSessionId(string agent, string suffix)
    {
        var normalized = new string(suffix
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray())
            .Trim('-');
        return $"{agent}-20260820T150000Z-{normalized}";
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };
}
