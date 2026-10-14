// SPDX-License-Identifier: Apache-2.0
using System.Net;
using System.Net.Http.Json;
using QBrainAi.Support.Mcp.Models;
using Xunit;

namespace QBrainAi.Support.Mcp.IntegrationTests.Controllers;

/// <summary>
/// FR-SUPPORT-010G (PLAN-SESSIONTRIAGEFIX-001 A8, triage-report-ed3b0381): through the real ASP.NET Core pipeline of the in-memory
/// server (a temporary instance), a turn's string-list item whose text contains a slash is deleted by DELETE
/// .../sections/{section}/items?itemKey=..., while the path-segment route, which keeps %2F encoded in the route value, matches no
/// item. Data: one session per test with a turn whose design decisions are "FR/TR/TEST decision" and "keep".
/// </summary>
[Trait("Category", "Integration")]
public sealed class SessionLogDeleteItemRoutingTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private const string Agent = "ClaudeCode";
    private const string RequestId = "req-20261013T120100Z-a8-routing";
    private const string SlashKey = "FR/TR/TEST decision";
    private readonly HttpClient _client;

    /// <summary>Creates an authorized client on the shared in-memory server.</summary>
    public SessionLogDeleteItemRoutingTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(_client, factory.Services);
    }

    /// <inheritdoc />
    public void Dispose() => _client.Dispose();

    /// <summary>
    /// A8: the query-string route deletes exactly the item whose key contains slashes and answers 200; the other item stays.
    /// </summary>
    [Fact]
    public async Task DeleteByQuery_KeyWithSlashes_DeletesThatItemOnly()
    {
        var sessionId = await SubmitSessionAsync("query").ConfigureAwait(true);

        using var response = await _client.DeleteAsync(
                new Uri($"{TurnUri(sessionId)}/sections/designDecisions/items?itemKey={Uri.EscapeDataString(SlashKey)}", UriKind.Relative),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new[] { "keep" }, await ReadDecisionsAsync(sessionId).ConfigureAwait(true));
    }

    /// <summary>
    /// A8 assertion proof and the original defect: a key that matches no item, and the slash key sent as an encoded path segment,
    /// both delete nothing and do not answer 200; both items stay.
    /// </summary>
    [Fact]
    public async Task UnknownKeyAndEncodedPathSegment_DeleteNothing()
    {
        var sessionId = await SubmitSessionAsync("negative").ConfigureAwait(true);

        using var unknown = await _client.DeleteAsync(
                new Uri($"{TurnUri(sessionId)}/sections/designDecisions/items?itemKey={Uri.EscapeDataString("no such decision")}", UriKind.Relative),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using var pathSegment = await _client.DeleteAsync(
                new Uri($"{TurnUri(sessionId)}/sections/designDecisions/items/{Uri.EscapeDataString(SlashKey)}", UriKind.Relative),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.NotEqual(HttpStatusCode.OK, unknown.StatusCode);
        Assert.NotEqual(HttpStatusCode.OK, pathSegment.StatusCode);
        Assert.Equal(new[] { SlashKey, "keep" }, await ReadDecisionsAsync(sessionId).ConfigureAwait(true));
    }

    private async Task<string> SubmitSessionAsync(string suffix)
    {
        var sessionId = $"{Agent}-20261013T120000Z-a8-{suffix}-{Guid.NewGuid():N}";
        var dto = new UnifiedSessionLogDto
        {
            SourceType = Agent,
            SessionId = sessionId,
            Title = "A8 routing",
            Model = "claude-opus-5-5",
            Started = "2026-10-13T12:00:00Z",
            LastUpdated = "2026-10-13T12:01:00Z",
            Status = "in_progress",
            TurnCount = 1,
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = RequestId,
                    Timestamp = "2026-10-13T12:01:00Z",
                    QueryText = "A8 routing",
                    Status = "in_progress",
                    PlanFile = "None",
                    TodoId = "None",
                    DesignDecisions = [SlashKey, "keep"],
                },
            ],
        };
        using var response = await _client.PostAsJsonAsync(new Uri("/qbrainai/sessionlog", UriKind.Relative), dto, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return sessionId;
    }

    private async Task<string[]> ReadDecisionsAsync(string sessionId)
    {
        var session = await _client.GetFromJsonAsync<UnifiedSessionLogDto>(
                new Uri($"/qbrainai/sessionlog/{Agent}/{sessionId}", UriKind.Relative), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.NotNull(session);
        return [.. Assert.Single(session.Turns!).DesignDecisions ?? []];
    }

    private static string TurnUri(string sessionId) => $"/qbrainai/sessionlog/{Agent}/{sessionId}/{RequestId}";
}
