using System.Net;
using System.Net.Http.Json;
using McpServer.Support.Mcp.Models;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>
/// FR-SUPPORT-010G: Integration coverage for the PATCH (additive) / PUT (replace)
/// / DELETE (remove) verb split on the session log controller.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SessionLogReplaceDeleteControllerTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private const string Agent = "ReplaceDelete";
    private readonly HttpClient _client;

    public SessionLogReplaceDeleteControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(_client, factory.Services);
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task PutTurn_ReplacesTurn_ClearsOmittedSections()
    {
        var sessionId = await SeedAsync().ConfigureAwait(true);

        var put = await _client.PutAsJsonAsync(TurnUri(sessionId), new UnifiedRequestEntryDto
        {
            RequestId = SeedRequestId,
            Status = "completed",
            Actions = [new UnifiedActionDto { Order = 0, Description = "kept", Status = "completed" }],
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var turn = await GetTurnAsync(sessionId).ConfigureAwait(true);
        Assert.Null(turn.Tags);
        Assert.Null(turn.Commits);
        Assert.Single(turn.Actions!);
    }

    [Fact]
    public async Task PatchTurn_IsAdditive_AppendsWithoutClobber()
    {
        var sessionId = await SeedAsync().ConfigureAwait(true);

        var patch = await _client.PatchAsJsonAsync(TurnUri(sessionId), new UnifiedRequestEntryDto
        {
            RequestId = SeedRequestId,
            Tags = ["added"],
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);

        var turn = await GetTurnAsync(sessionId).ConfigureAwait(true);
        Assert.Contains("added", turn.Tags!);
        Assert.Contains("t-a", turn.Tags!);          // original preserved
        Assert.Equal("seed query", turn.QueryText);  // omitted scalar preserved
    }

    [Fact]
    public async Task PutSection_ReplacesNamedSectionOnly()
    {
        var sessionId = await SeedAsync().ConfigureAwait(true);

        var put = await _client.PutAsJsonAsync(SectionUri(sessionId, "tags"), new UnifiedRequestEntryDto
        {
            RequestId = SeedRequestId,
            Tags = ["only"],
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var turn = await GetTurnAsync(sessionId).ConfigureAwait(true);
        Assert.Equal(new[] { "only" }, turn.Tags!.ToArray());
        Assert.NotNull(turn.Commits); // other sections untouched
    }

    [Fact]
    public async Task PutSection_UnknownSection_Returns400()
    {
        var sessionId = await SeedAsync().ConfigureAwait(true);

        var put = await _client.PutAsJsonAsync(SectionUri(sessionId, "bogus"), new UnifiedRequestEntryDto { RequestId = SeedRequestId }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
    }

    [Fact]
    public async Task DeleteSection_ClearsSection()
    {
        var sessionId = await SeedAsync().ConfigureAwait(true);

        var del = await _client.DeleteAsync(SectionUri(sessionId, "commits"), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, del.StatusCode);

        var turn = await GetTurnAsync(sessionId).ConfigureAwait(true);
        Assert.Null(turn.Commits);
    }

    [Fact]
    public async Task DeleteItem_RemovesSingleTagByValue()
    {
        var sessionId = await SeedAsync().ConfigureAwait(true);

        var del = await _client.DeleteAsync(new Uri($"{SectionUri(sessionId, "tags")}/items/{Uri.EscapeDataString("t-b")}", UriKind.Relative), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, del.StatusCode);

        var turn = await GetTurnAsync(sessionId).ConfigureAwait(true);
        Assert.Equal(new[] { "t-a" }, turn.Tags!.ToArray());
    }

    [Fact]
    public async Task DeleteTurn_RemovesTurn()
    {
        var sessionId = await SeedAsync().ConfigureAwait(true);

        var del = await _client.DeleteAsync(TurnUri(sessionId), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, del.StatusCode);

        var session = await GetSessionAsync(sessionId).ConfigureAwait(true);
        Assert.NotNull(session);
        Assert.True(session!.Turns is null or { Count: 0 });
    }

    [Fact]
    public async Task DeleteSession_RemovesSession_GetReturns404()
    {
        var sessionId = await SeedAsync().ConfigureAwait(true);

        var del = await _client.DeleteAsync(SessionUri(sessionId), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, del.StatusCode);

        var get = await _client.GetAsync(SessionUri(sessionId), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task DeleteSession_Missing_Returns404()
    {
        var del = await _client.DeleteAsync(SessionUri(BuildSessionId($"absent-{Guid.NewGuid():N}")), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.NotFound, del.StatusCode);
    }

    // ---- helpers ------------------------------------------------------------

    private const string SeedRequestId = "req-20260613T120000Z-001-seed";

    private static string BuildSessionId(string suffix) => $"{Agent}-20260613T120000Z-{suffix}";
    private static Uri SessionUri(string sessionId) => new($"/mcpserver/sessionlog/{Agent}/{sessionId}", UriKind.Relative);
    private static Uri TurnUri(string sessionId) => new($"/mcpserver/sessionlog/{Agent}/{sessionId}/{SeedRequestId}", UriKind.Relative);
    private static Uri SectionUri(string sessionId, string section) => new($"/mcpserver/sessionlog/{Agent}/{sessionId}/{SeedRequestId}/sections/{section}", UriKind.Relative);

    private async Task<string> SeedAsync()
    {
        var sessionId = BuildSessionId(Guid.NewGuid().ToString("N"));
        var dto = new UnifiedSessionLogDto
        {
            SourceType = Agent,
            SessionId = sessionId,
            Title = "Seed",
            Started = "2026-06-13T12:00:00Z",
            Status = "in_progress",
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = SeedRequestId,
                    Timestamp = "2026-06-13T12:00:00Z",
                    QueryText = "seed query",
                    Status = "in_progress",
                    Tags = ["t-a", "t-b"],
                    Commits = [new SessionLogCommitDto { Sha = "sha-1", Message = "m", Author = "p" }],
                    Actions = [new UnifiedActionDto { Order = 0, Description = "a", Status = "completed" }],
                    DesignDecisions = ["d-1"],
                }
            ]
        };
        var resp = await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), dto).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return sessionId;
    }

    private async Task<UnifiedSessionLogDto?> GetSessionAsync(string sessionId)
    {
        var resp = await _client.GetAsync(SessionUri(sessionId)).ConfigureAwait(true);
        if (resp.StatusCode == HttpStatusCode.NotFound)
            return null;
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        return await resp.Content.ReadFromJsonAsync<UnifiedSessionLogDto>().ConfigureAwait(true);
    }

    private async Task<UnifiedRequestEntryDto> GetTurnAsync(string sessionId)
    {
        var session = await GetSessionAsync(sessionId).ConfigureAwait(true);
        Assert.NotNull(session);
        Assert.NotNull(session!.Turns);
        return session.Turns!.Single(t => t.RequestId == SeedRequestId);
    }
}
