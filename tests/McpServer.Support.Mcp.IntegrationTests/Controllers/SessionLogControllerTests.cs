using System.Linq;
using System.Net;
using System.Net.Http.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>TR-PLANNED-CORE-013: Integration tests for SessionLogController endpoints (MVP-SUPPORT-011).</summary>
[Trait("Category", "Integration")]
public sealed class SessionLogControllerTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly HttpClient _client;

    public SessionLogControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(_client, factory.Services);
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task WhenPostingValidSessionThenReturns201Created()
    {
        var dto = CreateTestDto("Cursor", BuildSessionId("Cursor", $"int-{Guid.NewGuid():N}"));

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task WhenPostingWithoutSourceTypeThenReturns400()
    {
        var dto = new UnifiedSessionLogDto { SourceType = null, SessionId = BuildSessionId("Cursor", "test") };

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WhenPostingWithoutSessionIdThenReturns400()
    {
        var dto = new UnifiedSessionLogDto { SourceType = "Cursor", SessionId = null };

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WhenGettingWithNoParamsThenReturns200WithArray()
    {
        // Submit a session first so there's data
        var dto = CreateTestDto("Copilot", BuildSessionId("Copilot", $"get-{Guid.NewGuid():N}"));
        await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var response = await _client.GetAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SessionLogQueryResult>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
    }

    [Fact]
    public async Task WhenGettingByAgentThenReturnsOnlyMatchingSessions()
    {
        var id = Guid.NewGuid().ToString("N");
        await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), CreateTestDto("CursorFilter", BuildSessionId("CursorFilter", $"f-{id}")), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), CreateTestDto("CopilotFilter", BuildSessionId("CopilotFilter", $"f2-{id}")), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var response = await _client.GetAsync(new Uri("/mcpserver/sessionlog?agent=CursorFilter", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SessionLogQueryResult>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.All(result.Items, item => Assert.Equal("CursorFilter", item.SourceType));
    }

    [Fact]
    public async Task WhenPostingSameSessionTwiceThenSessionIsUpserted()
    {
        var sessionId = BuildSessionId("Cursor", $"upsert-{Guid.NewGuid():N}");
        var dto1 = CreateTestDto("Cursor", sessionId);
        dto1.Title = "Original";
        await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), dto1, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var dto2 = CreateTestDto("Cursor", sessionId);
        dto2.Title = "Updated";
        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), dto2, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Query to verify update
        var query = await _client.GetFromJsonAsync<SessionLogQueryResult>(
            new Uri($"/mcpserver/sessionlog?agent=Cursor", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var match = query?.Items.FirstOrDefault(i => i.SessionId == sessionId);
        Assert.NotNull(match);
        Assert.Equal("Updated", match!.Title);
    }

    [Fact]
    public async Task WhenAppendingDialogToValidEntryThenReturns200()
    {
        var sessionId = BuildSessionId("Cursor", $"dialog-{Guid.NewGuid():N}");
        var dto = CreateTestDto("Cursor", sessionId);
        await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var items = new[]
        {
            new ProcessingDialogItemDto { Role = "model", Content = "Analyzing...", Category = "reasoning" }
        };

        var response = await _client.PostAsJsonAsync(
            new Uri($"/mcpserver/sessionlog/Cursor/{sessionId}/req-20260212T100100Z-entry-001/dialog", UriKind.Relative), items, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WhenAppendingDialogToNonexistentEntryThenReturns404()
    {
        var items = new[]
        {
            new ProcessingDialogItemDto { Role = "model", Content = "test" }
        };

        var response = await _client.PostAsJsonAsync(
            new Uri("/mcpserver/sessionlog/Cursor/Cursor-20260304T113901Z-nonexistent/req-20260304T113901Z-001/dialog", UriKind.Relative), items, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task WhenAppendingEmptyDialogArrayThenReturns400()
    {
        var items = Array.Empty<ProcessingDialogItemDto>();

        var response = await _client.PostAsJsonAsync(
            new Uri("/mcpserver/sessionlog/Cursor/Cursor-20260304T113901Z-any/req-20260304T113901Z-001/dialog", UriKind.Relative), items, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WhenPostingWithInvalidSessionIdFormatThenReturns400()
    {
        var dto = CreateTestDto("Cursor", "cursor-invalid");
        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WhenPostingWithInvalidRequestIdFormatThenReturns400()
    {
        var dto = CreateTestDto("Cursor", BuildSessionId("Cursor", "bad-request-id"));
        dto.Turns!.Single().RequestId = "req-bad";
        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WhenAppendingDialogWithInvalidIdsThenReturns400()
    {
        var items = new[]
        {
            new ProcessingDialogItemDto { Role = "model", Content = "test" }
        };

        var response = await _client.PostAsJsonAsync(
            new Uri("/mcpserver/sessionlog/Cursor/not-a-session/req-1/dialog", UriKind.Relative), items, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// FR-SUPPORT-012: Body-binding failure returns RFC 7807 ProblemDetails with
    /// the offending JSON path; the response must not echo the action parameter
    /// name (<c>dto</c>) which misleads callers into thinking a wrapper is required.
    /// </summary>
    [Fact]
    public async Task WhenPostingMalformedWorkspaceFieldThenReturnsProblemDetailsWithoutDtoKey()
    {
        var raw = "{\"sourceType\":\"Cursor\",\"sessionId\":\"Cursor-20260516T120000Z-bad-ws\",\"workspace\":\"not-an-object\"}";
        using var content = new StringContent(raw, System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), content, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.DoesNotContain("\"dto\"", body, StringComparison.Ordinal);
        Assert.Contains("workspace", body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR-SUPPORT-012: Domain validation (missing SourceType) returns ProblemDetails
    /// not the legacy <c>{"error":"..."}</c> plain object.
    /// </summary>
    [Fact]
    public async Task WhenPostingMissingSourceTypeThenReturnsProblemDetails()
    {
        var dto = new UnifiedSessionLogDto { SourceType = null, SessionId = BuildSessionId("Cursor", "no-source") };

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Contains("sourceType", body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR-SUPPORT-013: <c>GET /mcpserver/sessionlog/{agent}/{sessionId}</c> returns
    /// the just-created record so a POST/GET round-trip via REST works without
    /// scanning the list endpoint.
    /// </summary>
    [Fact]
    public async Task WhenPostingThenGetBySessionIdReturnsRecord()
    {
        var sessionId = BuildSessionId("Cursor", $"get-by-id-{Guid.NewGuid():N}");
        var dto = CreateTestDto("Cursor", sessionId);
        await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var response = await _client.GetAsync(
            new Uri($"/mcpserver/sessionlog/Cursor/{sessionId}", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await response.Content.ReadFromJsonAsync<UnifiedSessionLogDto>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(fetched);
        Assert.Equal(sessionId, fetched!.SessionId);
    }

    /// <summary>
    /// FR-SUPPORT-013: <c>GET</c> by sessionId returns 404 when the session is
    /// not found.
    /// </summary>
    [Fact]
    public async Task WhenGettingMissingSessionIdThenReturns404()
    {
        var response = await _client.GetAsync(
            new Uri("/mcpserver/sessionlog/Cursor/Cursor-20260101T000000Z-absent", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// FR-SUPPORT-013: <c>POST /mcpserver/sessionlog/{agent}/{sessionId}/turn</c>
    /// appends a turn to an existing session and the turn is retrievable.
    /// </summary>
    [Fact]
    public async Task WhenPostingTurnViaRestThenTurnIsRetrievable()
    {
        var sessionId = BuildSessionId("Cursor", $"turn-rest-{Guid.NewGuid():N}");
        var dto = CreateTestDto("Cursor", sessionId);
        await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var newTurn = new UnifiedRequestEntryDto
        {
            RequestId = "req-20260516T120000Z-via-rest",
            Timestamp = "2026-05-16T12:00:00Z",
            QueryText = "appended turn",
            Interpretation = "per-turn route preserves structured fields",
            Status = "completed",
            PlanFile = "None",
            TodoId = "None",
            Tags = ["rest"],
            ContextList = ["tests/McpServer.Support.Mcp.IntegrationTests/Controllers/SessionLogControllerTests.cs"],
            Actions =
            [
                new UnifiedActionDto
                {
                    Order = 1,
                    Description = "Recorded REST turn append",
                    Type = "session_turn",
                    Status = "completed",
                    FilePath = "tests/McpServer.Support.Mcp.IntegrationTests/Controllers/SessionLogControllerTests.cs"
                }
            ]
        };

        var response = await _client.PostAsJsonAsync(
            new Uri($"/mcpserver/sessionlog/Cursor/{sessionId}/turn", UriKind.Relative), newTurn, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var fetched = await _client.GetFromJsonAsync<UnifiedSessionLogDto>(
            new Uri($"/mcpserver/sessionlog/Cursor/{sessionId}", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(fetched);
        Assert.NotNull(fetched!.Turns);
        var appended = Assert.Single(fetched.Turns!, t => t.RequestId == "req-20260516T120000Z-via-rest");
        Assert.Equal("per-turn route preserves structured fields", appended.Interpretation);
        Assert.Equal("rest", Assert.Single(appended.Tags!));
        Assert.Equal(
            "tests/McpServer.Support.Mcp.IntegrationTests/Controllers/SessionLogControllerTests.cs",
            Assert.Single(appended.ContextList!));
        var action = Assert.Single(appended.Actions!);
        Assert.Equal(1, action.Order);
        Assert.Equal("Recorded REST turn append", action.Description);
    }

    /// <summary>
    /// FR-SUPPORT-013: Closing a turn through the REST turn endpoint for a Quad-Brain
    /// ACID agent session (SourceType <c>QBAgent</c>) requires at least one decision,
    /// action, or commit item so audit-empty completions are rejected.
    /// </summary>
    [Fact]
    public async Task WhenAcidAgentClosingTurnWithoutComplianceItemsThenReturns400()
    {
        const string qbAgentSourceType = "QBAgent";
        var sessionId = BuildSessionId(qbAgentSourceType, $"turn-close-validation-{Guid.NewGuid():N}");
        var dto = CreateTestDto(qbAgentSourceType, sessionId);
        await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var emptyClose = new UnifiedRequestEntryDto
        {
            RequestId = "req-20260516T120100Z-empty-close",
            Timestamp = "2026-05-16T12:01:00Z",
            QueryText = "close without compliance items",
            Status = "completed",
            PlanFile = "None",
            TodoId = "None"
        };

        var response = await _client.PostAsJsonAsync(
            new Uri($"/mcpserver/sessionlog/{qbAgentSourceType}/{sessionId}/turn", UriKind.Relative), emptyClose, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Contains("no decision, action, or commit items", body, StringComparison.Ordinal);
        Assert.Contains("Compliance with Session Logging Requirements is not optional.", body, StringComparison.Ordinal);
    }

    /// <summary>
    /// FR-SUPPORT-013: A standard (non-Quad-Brain) agent session closes a turn through
    /// the REST turn endpoint without any decision/action/commit evidence and succeeds;
    /// the ACID compliance gate must not leak into the standard session-log endpoints.
    /// </summary>
    [Fact]
    public async Task WhenStandardAgentClosingTurnWithoutComplianceItemsThenSucceeds()
    {
        var sessionId = BuildSessionId("Cursor", $"turn-close-standard-{Guid.NewGuid():N}");
        var dto = CreateTestDto("Cursor", sessionId);
        await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var emptyClose = new UnifiedRequestEntryDto
        {
            RequestId = "req-20260516T120200Z-empty-close-ok",
            Timestamp = "2026-05-16T12:02:00Z",
            QueryText = "standard close without compliance items",
            Status = "completed",
            PlanFile = "None",
            TodoId = "None"
        };

        var response = await _client.PostAsJsonAsync(
            new Uri($"/mcpserver/sessionlog/Cursor/{sessionId}/turn", UriKind.Relative), emptyClose, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.True(response.IsSuccessStatusCode, $"Expected success but got {(int)response.StatusCode}.");
    }

    /// <summary>
    /// FR-SUPPORT-010G: PUT on a turn route is now a supported verb (REPLACE). A
    /// valid requestId replaces the turn and returns 200; the bare <c>/turn</c>
    /// append route remains POST-only (PUT there resolves to a turn whose id is the
    /// literal "turn", which fails requestId validation with 400, not 405).
    /// </summary>
    [Fact]
    public async Task WhenPuttingTurnWithValidRequestIdThenReplacesAndReturns200()
    {
        var sessionId = BuildSessionId("Cursor", $"turn-verb-{Guid.NewGuid():N}");
        var dto = CreateTestDto("Cursor", sessionId);
        var requestId = dto.Turns!.Single().RequestId!;
        await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var content = JsonContent.Create(new UnifiedRequestEntryDto
        {
            RequestId = requestId,
            Status = "completed",
            PlanFile = "None",
            TodoId = "None",
            Actions = [new UnifiedActionDto { Order = 0, Description = "replace", Status = "completed" }],
        });
        using var request = new HttpRequestMessage(HttpMethod.Put,
            new Uri($"/mcpserver/sessionlog/Cursor/{sessionId}/{requestId}", UriKind.Relative))
        { Content = content };

        var response = await _client.SendAsync(request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// BUG-APPVISIBILITY-001: API callers switching workspaces must see only the
    /// session logs for the requested workspace on list and get routes.
    /// </summary>
    [Fact]
    public async Task WhenTwoWorkspacesQuerySessionLogsThenEachWorkspaceSeesOnlyItsOwnRows()
    {
        var secondaryWorkspacePath = Path.Combine(
            Path.GetTempPath(),
            $"mcp-support-integration-secondary-{Guid.NewGuid():N}",
            "workspace");
        var secondaryDataPath = Path.Combine(Path.GetTempPath(), $"mcp-support-integration-secondary-data-{Guid.NewGuid():N}");
        SeedMinimalWorkspaceFiles(secondaryWorkspacePath);
        Directory.CreateDirectory(secondaryDataPath);

        try
        {
            var overrides = new Dictionary<string, string?>
            {
                { "Mcp:Workspaces:1:WorkspacePath", secondaryWorkspacePath },
                { "Mcp:Workspaces:1:Name", "support-integration-secondary" },
                { "Mcp:Workspaces:1:TodoPath", Path.Combine(secondaryWorkspacePath, "docs", "Project", "TODO.yaml") },
                { "Mcp:Workspaces:1:DataDirectory", secondaryDataPath },
                { "Mcp:Workspaces:1:IsPrimary", "false" },
                { "Mcp:Workspaces:1:IsEnabled", "true" },
            };

            using var factory = new CustomWebApplicationFactory(null, overrides);
            using var primaryClient = factory.CreateClient();
            using var secondaryClient = factory.CreateClient();
            AddWorkspaceAuth(primaryClient, factory.Services, factory.WorkspacePath);
            AddWorkspaceAuth(secondaryClient, factory.Services, secondaryWorkspacePath);

            var primarySessionId = BuildSessionId("Codex", $"primary-visible-{Guid.NewGuid():N}");
            var secondarySessionId = BuildSessionId("Cursor", $"secondary-visible-{Guid.NewGuid():N}");

            var primaryPost = await primaryClient.PostAsJsonAsync(
                new Uri("/mcpserver/sessionlog", UriKind.Relative),
                CreateTestDto("Codex", primarySessionId), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            var secondaryPost = await secondaryClient.PostAsJsonAsync(
                new Uri("/mcpserver/sessionlog", UriKind.Relative),
                CreateTestDto("Cursor", secondarySessionId), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal(HttpStatusCode.Created, primaryPost.StatusCode);
            Assert.Equal(HttpStatusCode.Created, secondaryPost.StatusCode);

            var primaryList = await primaryClient.GetFromJsonAsync<SessionLogQueryResult>(
                new Uri("/mcpserver/sessionlog?limit=20", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            var secondaryList = await secondaryClient.GetFromJsonAsync<SessionLogQueryResult>(
                new Uri("/mcpserver/sessionlog?limit=20", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.NotNull(primaryList);
            Assert.Contains(primaryList!.Items, item => item.SessionId == primarySessionId);
            Assert.DoesNotContain(primaryList.Items, item => item.SessionId == secondarySessionId);
            Assert.NotNull(secondaryList);
            Assert.Contains(secondaryList!.Items, item => item.SessionId == secondarySessionId);
            Assert.DoesNotContain(secondaryList.Items, item => item.SessionId == primarySessionId);

            var secondaryFromPrimary = await primaryClient.GetAsync(
                new Uri($"/mcpserver/sessionlog/Cursor/{secondarySessionId}", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            var primaryFromSecondary = await secondaryClient.GetAsync(
                new Uri($"/mcpserver/sessionlog/Codex/{primarySessionId}", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal(HttpStatusCode.NotFound, secondaryFromPrimary.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, primaryFromSecondary.StatusCode);
        }
        finally
        {
            TryDeleteDirectory(secondaryWorkspacePath);
            TryDeleteDirectory(secondaryDataPath);
            TryDeleteDirectory(Path.GetDirectoryName(secondaryWorkspacePath));
        }
    }

    /// <summary>
    /// BUG-SESSIONLOG-WS-005: the workspace-stamp repair endpoint is reachable,
    /// idempotent, and reports the number of re-stamped rows.
    /// </summary>
    [Fact]
    public async Task WhenPostingRepairWorkspaceStampsThenReturns200WithCount()
    {
        var dto = CreateTestDto("Cursor", BuildSessionId("Cursor", $"repair-{Guid.NewGuid():N}"));
        await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), dto, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var response = await _client.PostAsync(new Uri("/mcpserver/sessionlog/repair-workspace-stamps", UriKind.Relative), null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RepairWorkspaceStampsResult>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(body);
        Assert.True(body!.Repaired >= 0);

        var second = await _client.PostAsync(new Uri("/mcpserver/sessionlog/repair-workspace-stamps", UriKind.Relative), null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondBody = await second.Content.ReadFromJsonAsync<RepairWorkspaceStampsResult>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(0, secondBody!.Repaired);
    }

    private sealed record RepairWorkspaceStampsResult(int Repaired);

    #region Phase 1a - stateless session lifecycle (FR-SUPPORT-014)

    /// <summary>
    /// FR-SUPPORT-014: open is an idempotent ensure-session keyed by
    /// (agent, sessionId); calling it twice yields one session and 200 both times.
    /// </summary>
    [Fact]
    public async Task OpenSession_Twice_IsIdempotent()
    {
        var sessionId = BuildSessionId("ClaudeCode", $"open-{Guid.NewGuid():N}");
        var body = new { title = "Lifecycle open", model = "claude-fable-5" };

        var first = await _client.PostAsJsonAsync(LifecycleUri($"ClaudeCode/{sessionId}/open"), body, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var second = await _client.PostAsJsonAsync(LifecycleUri($"ClaudeCode/{sessionId}/open"), body, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var query = await _client.GetFromJsonAsync<SessionLogQueryResult>(
            new Uri($"/mcpserver/sessionlog?agent=ClaudeCode", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Single(query!.Items, s => s.SessionId == sessionId);
    }

    /// <summary>
    /// FR-SUPPORT-014: begin creates an in_progress turn keyed by
    /// (agent, sessionId, requestId) with no in-process server state.
    /// </summary>
    [Fact]
    public async Task BeginTurn_CreatesInProgressTurn()
    {
        var sessionId = BuildSessionId("ClaudeCode", $"begin-{Guid.NewGuid():N}");
        await OpenSessionAsync(sessionId).ConfigureAwait(true);
        var requestId = NewRequestId("begin");

        var response = await _client.PostAsJsonAsync(
            LifecycleUri($"ClaudeCode/{sessionId}/{requestId}/begin"),
            new { queryTitle = "Begin turn", queryText = "lifecycle begin", planFile = "None", todoId = "None" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var fetched = await _client.GetFromJsonAsync<UnifiedSessionLogDto>(
            new Uri($"/mcpserver/sessionlog/ClaudeCode/{sessionId}", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var turn = Assert.Single(fetched!.Turns!);
        Assert.Equal(requestId, turn.RequestId);
        Assert.Equal("in_progress", turn.Status);
        Assert.Equal("None", turn.PlanFile);
        Assert.Equal("None", turn.TodoId);
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-002 / AC-TR-MCP-SESSIONLOG-006-006: None/None begin round-trips.</summary>
    [Fact]
    public async Task BeginTurn_NoneNone_Returns201_AndGetReturnsNone()
    {
        var sessionId = BuildSessionId("ClaudeCode", $"begin-none-{Guid.NewGuid():N}");
        await OpenSessionAsync(sessionId).ConfigureAwait(true);
        var requestId = NewRequestId("begin-none");
        var response = await _client.PostAsJsonAsync(
            LifecycleUri($"ClaudeCode/{sessionId}/{requestId}/begin"),
            new { queryTitle = "None pair", queryText = "none", planFile = "None", todoId = "None" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var fetched = await _client.GetFromJsonAsync<UnifiedSessionLogDto>(
            new Uri($"/mcpserver/sessionlog/ClaudeCode/{sessionId}", UriKind.Relative),
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var turn = Assert.Single(fetched!.Turns!);
        Assert.Equal("None", turn.PlanFile);
        Assert.Equal("None", turn.TodoId);
    }

    /// <summary>AC-FR-MCP-SESSIONLOGCTX-001-005: query todoId filter is exact.</summary>
    [Fact]
    public async Task Query_FilterByTodoId_ReturnsOnlyMatches()
    {
        var sessionId = BuildSessionId("ClaudeCode", $"q-todo-{Guid.NewGuid():N}");
        var todoId = $"ISSUE-{Random.Shared.Next(100000, 999999)}";
        await OpenSessionAsync(sessionId).ConfigureAwait(true);
        var requestId = NewRequestId("q-todo");
        var begin = await _client.PostAsJsonAsync(
            LifecycleUri($"ClaudeCode/{sessionId}/{requestId}/begin"),
            new { queryTitle = "filter", planFile = "None", todoId },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, begin.StatusCode);
        var stored = await _client.GetFromJsonAsync<UnifiedSessionLogDto>(
            new Uri($"/mcpserver/sessionlog/ClaudeCode/{sessionId}", UriKind.Relative),
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(todoId, Assert.Single(stored!.Turns!).TodoId);

        var hit = await _client.GetFromJsonAsync<SessionLogQueryResult>(
            new Uri($"/mcpserver/sessionlog?todoId={Uri.EscapeDataString(todoId)}&limit=200", UriKind.Relative),
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Contains(hit!.Items, item => item.SessionId == sessionId);
        Assert.All(hit.Items, item => Assert.Contains(item.Turns ?? [], turn => turn.TodoId == todoId));

        var miss = await _client.GetFromJsonAsync<SessionLogQueryResult>(
            new Uri("/mcpserver/sessionlog?todoId=PLAN-MISS-001&limit=50", UriKind.Relative),
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.DoesNotContain(miss!.Items, item => item.SessionId == sessionId);
    }

    /// <summary>FR-MCP-SESSIONLOGCTX-001: begin without planFile/todoId is 400.</summary>
    [Fact]
    public async Task BeginTurn_MissingFields_Returns400()
    {
        var sessionId = BuildSessionId("ClaudeCode", $"begin-miss-{Guid.NewGuid():N}");
        await OpenSessionAsync(sessionId).ConfigureAwait(true);
        var response = await _client.PostAsJsonAsync(
            LifecycleUri($"ClaudeCode/{sessionId}/{NewRequestId("begin-miss")}/begin"),
            new { queryTitle = "Begin turn", queryText = "lifecycle begin" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>FR-SUPPORT-014: begin on a missing session maps to 404.</summary>
    [Fact]
    public async Task BeginTurn_SessionMissing_Returns404()
    {
        var sessionId = BuildSessionId("ClaudeCode", $"missing-{Guid.NewGuid():N}");
        var response = await _client.PostAsJsonAsync(
            LifecycleUri($"ClaudeCode/{sessionId}/{NewRequestId("orphan")}/begin"),
            new { queryTitle = "x" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// FR-SUPPORT-014: complete merges the payload onto the existing turn and
    /// finalizes it; audit evidence (decision/action/commit) satisfies the
    /// terminal-turn compliance gate, and omitted fields (queryText) survive.
    /// </summary>
    [Fact]
    public async Task CompleteTurn_WithEvidence_FinalizesTurn()
    {
        var sessionId = BuildSessionId("ClaudeCode", $"complete-{Guid.NewGuid():N}");
        await OpenSessionAsync(sessionId).ConfigureAwait(true);
        var requestId = NewRequestId("complete");
        await _client.PostAsJsonAsync(
            LifecycleUri($"ClaudeCode/{sessionId}/{requestId}/begin"),
            new { queryTitle = "Work", queryText = "do work", planFile = "None", todoId = "None" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var response = await _client.PostAsJsonAsync(
            LifecycleUri($"ClaudeCode/{sessionId}/{requestId}/complete"),
            new UnifiedRequestEntryDto
            {
                Response = "done",
                DesignDecisions = ["Decision: lifecycle endpoints are stateless."]
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await _client.GetFromJsonAsync<UnifiedSessionLogDto>(
            new Uri($"/mcpserver/sessionlog/ClaudeCode/{sessionId}", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var turn = Assert.Single(fetched!.Turns!);
        Assert.Equal("completed", turn.Status);
        Assert.Equal("done", turn.Response);
        Assert.Equal("do work", turn.QueryText);
    }

    /// <summary>
    /// FR-SUPPORT-014: a standard (non-Quad-Brain) agent completes a turn without any
    /// decision/action/commit evidence and the turn finalizes successfully. The ACID
    /// compliance gate must not leak into the standard lifecycle endpoints.
    /// </summary>
    [Fact]
    public async Task CompleteTurn_StandardAgentWithoutEvidence_Succeeds()
    {
        var sessionId = BuildSessionId("ClaudeCode", $"completeok-{Guid.NewGuid():N}");
        await OpenSessionAsync(sessionId).ConfigureAwait(true);
        var requestId = NewRequestId("noevidence");
        await _client.PostAsJsonAsync(
            LifecycleUri($"ClaudeCode/{sessionId}/{requestId}/begin"),
            new { queryTitle = "Work", planFile = "None", todoId = "None" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var response = await _client.PostAsJsonAsync(
            LifecycleUri($"ClaudeCode/{sessionId}/{requestId}/complete"),
            new UnifiedRequestEntryDto { Response = "done" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// FR-SUPPORT-014: a Quad-Brain ACID agent (SourceType <c>QBAgent</c>) completing
    /// a turn without any decision/action/commit evidence is rejected by the terminal-turn
    /// compliance gate with 400.
    /// </summary>
    [Fact]
    public async Task CompleteTurn_AcidAgentWithoutEvidence_Returns400()
    {
        const string qbAgentSourceType = "QBAgent";
        var sessionId = BuildSessionId(qbAgentSourceType, $"complete400-{Guid.NewGuid():N}");
        await OpenSessionAsync(sessionId, qbAgentSourceType).ConfigureAwait(true);
        var requestId = NewRequestId("noevidence");
        await _client.PostAsJsonAsync(
            LifecycleUri($"{qbAgentSourceType}/{sessionId}/{requestId}/begin"),
            new { queryTitle = "Work", planFile = "None", todoId = "None" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var response = await _client.PostAsJsonAsync(
            LifecycleUri($"{qbAgentSourceType}/{sessionId}/{requestId}/complete"),
            new UnifiedRequestEntryDto { Response = "done" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// FR-SUPPORT-014: fail finalizes the turn as failed, records the failure
    /// note, and still honors the compliance gate via supplied evidence.
    /// </summary>
    [Fact]
    public async Task FailTurn_SetsFailedStatusWithNote()
    {
        var sessionId = BuildSessionId("ClaudeCode", $"fail-{Guid.NewGuid():N}");
        await OpenSessionAsync(sessionId).ConfigureAwait(true);
        var requestId = NewRequestId("fail");
        await _client.PostAsJsonAsync(
            LifecycleUri($"ClaudeCode/{sessionId}/{requestId}/begin"),
            new { queryTitle = "Doomed work", planFile = "None", todoId = "None" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var response = await _client.PostAsJsonAsync(
            LifecycleUri($"ClaudeCode/{sessionId}/{requestId}/fail"),
            new UnifiedRequestEntryDto
            {
                FailureNote = "dependency missing",
                DesignDecisions = ["Decision: abort; dependency missing."]
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await _client.GetFromJsonAsync<UnifiedSessionLogDto>(
            new Uri($"/mcpserver/sessionlog/ClaudeCode/{sessionId}", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var turn = Assert.Single(fetched!.Turns!);
        Assert.Equal("failed", turn.Status);
        Assert.Equal("dependency missing", turn.FailureNote);
    }

    private async Task OpenSessionAsync(string sessionId, string sourceType = "ClaudeCode")
    {
        var response = await _client.PostAsJsonAsync(
            LifecycleUri($"{sourceType}/{sessionId}/open"),
            new { title = "Lifecycle test session", model = "claude-fable-5" }).ConfigureAwait(true);
        response.EnsureSuccessStatusCode();
    }

    private static Uri LifecycleUri(string suffix) =>
        new($"/mcpserver/sessionlog/{suffix}", UriKind.Relative);

    private static string NewRequestId(string slug) =>
        $"req-{DateTime.UtcNow:yyyyMMddTHHmmss}Z-{slug}-{Guid.NewGuid().ToString("N")[..12]}";

    #endregion

    private static UnifiedSessionLogDto CreateTestDto(string sourceType, string sessionId)
    {
        return new UnifiedSessionLogDto
        {
            SourceType = sourceType,
            SessionId = sessionId,
            Title = "Integration Test Session",
            Model = "gpt-4",
            Started = "2026-02-12T10:00:00Z",
            LastUpdated = "2026-02-12T12:00:00Z",
            Status = "completed",
            TurnCount = 1,
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = "req-20260212T100100Z-entry-001",
                    Timestamp = "2026-02-12T10:01:00Z",
                    QueryText = "Test query",
                    Response = "Test response",
                    Status = "completed",
                    PlanFile = "None",
                    TodoId = "None"
                }
            ]
        };
    }

    private static string BuildSessionId(string agent, string suffix)
    {
        var normalized = new string((suffix ?? string.Empty)
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray())
            .Trim('-');
        if (string.IsNullOrWhiteSpace(normalized))
            normalized = "session";
        return $"{agent}-20260304T113901Z-{normalized}";
    }

    private static void AddWorkspaceAuth(HttpClient client, IServiceProvider services, string workspacePath)
    {
        using var scope = services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<WorkspaceTokenService>();
        var token = tokenService.GetToken(workspacePath) ?? tokenService.GenerateToken(workspacePath);

        client.DefaultRequestHeaders.Remove("X-Api-Key");
        client.DefaultRequestHeaders.Add("X-Api-Key", token);
        client.DefaultRequestHeaders.Remove("X-Workspace-Path");
        client.DefaultRequestHeaders.Add("X-Workspace-Path", workspacePath);
    }

    private static void SeedMinimalWorkspaceFiles(string workspacePath)
    {
        var projectPath = Path.Combine(workspacePath, "docs", "Project");
        Directory.CreateDirectory(projectPath);
        File.WriteAllText(Path.Combine(projectPath, "TODO.yaml"), """
            mvp-app:
              high-priority: []
            """);
    }

    private static void TryDeleteDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }
}
