using System.Net;
using System.Net.Http.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>TR-PLANNED-013: Integration tests for SessionLogController endpoints (MVP-SUPPORT-011).</summary>
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
        var dto = CreateTestDto("Cursor", $"int-{Guid.NewGuid():N}");

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), dto).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task WhenPostingWithoutSourceTypeThenReturns400()
    {
        var dto = new UnifiedSessionLogDto { SourceType = null, SessionId = "test" };

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), dto).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WhenPostingWithoutSessionIdThenReturns400()
    {
        var dto = new UnifiedSessionLogDto { SourceType = "Cursor", SessionId = null };

        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), dto).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WhenGettingWithNoParamsThenReturns200WithArray()
    {
        // Submit a session first so there's data
        var dto = CreateTestDto("Copilot", $"get-{Guid.NewGuid():N}");
        await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), dto).ConfigureAwait(true);

        var response = await _client.GetAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative)).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SessionLogQueryResult>().ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
    }

    [Fact]
    public async Task WhenGettingByAgentThenReturnsOnlyMatchingSessions()
    {
        var id = Guid.NewGuid().ToString("N");
        await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), CreateTestDto("CursorFilter", $"f-{id}")).ConfigureAwait(true);
        await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), CreateTestDto("CopilotFilter", $"f2-{id}")).ConfigureAwait(true);

        var response = await _client.GetAsync(new Uri("/mcpserver/sessionlog?agent=CursorFilter", UriKind.Relative)).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SessionLogQueryResult>().ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.All(result.Items, item => Assert.Equal("CursorFilter", item.SourceType));
    }

    [Fact]
    public async Task WhenPostingSameSessionTwiceThenSessionIsUpserted()
    {
        var sessionId = $"upsert-{Guid.NewGuid():N}";
        var dto1 = CreateTestDto("Cursor", sessionId);
        dto1.Title = "Original";
        await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), dto1).ConfigureAwait(true);

        var dto2 = CreateTestDto("Cursor", sessionId);
        dto2.Title = "Updated";
        var response = await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), dto2).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Query to verify update
        var query = await _client.GetFromJsonAsync<SessionLogQueryResult>(
            new Uri($"/mcpserver/sessionlog?agent=Cursor", UriKind.Relative)).ConfigureAwait(true);
        var match = query?.Items.FirstOrDefault(i => i.SessionId == sessionId);
        Assert.NotNull(match);
        Assert.Equal("Updated", match!.Title);
    }

    [Fact]
    public async Task WhenAppendingDialogToValidEntryThenReturns200()
    {
        var sessionId = $"dialog-{Guid.NewGuid():N}";
        var dto = CreateTestDto("Cursor", sessionId);
        await _client.PostAsJsonAsync(new Uri("/mcpserver/sessionlog", UriKind.Relative), dto).ConfigureAwait(true);

        var items = new[]
        {
            new ProcessingDialogItemDto { Role = "model", Content = "Analyzing...", Category = "reasoning" }
        };

        var response = await _client.PostAsJsonAsync(
            new Uri($"/mcpserver/sessionlog/Cursor/{sessionId}/req-{sessionId}-1/dialog", UriKind.Relative), items).ConfigureAwait(true);

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
            new Uri("/mcpserver/sessionlog/Cursor/nonexistent/req-1/dialog", UriKind.Relative), items).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task WhenAppendingEmptyDialogArrayThenReturns400()
    {
        var items = Array.Empty<ProcessingDialogItemDto>();

        var response = await _client.PostAsJsonAsync(
            new Uri("/mcpserver/sessionlog/Cursor/any/req-1/dialog", UriKind.Relative), items).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

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
            EntryCount = 1,
            Entries =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = $"req-{sessionId}-1",
                    Timestamp = "2026-02-12T10:01:00Z",
                    QueryText = "Test query",
                    Response = "Test response",
                    Status = "completed"
                }
            ]
        };
    }
}
