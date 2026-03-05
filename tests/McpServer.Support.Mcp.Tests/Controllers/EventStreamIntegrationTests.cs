using System.Net;
using System.Net.Http.Headers;
using System.Text;
using McpServer.Support.Mcp.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>Integration tests for SSE change event stream endpoint.</summary>
public sealed class EventStreamIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EventStreamIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        TestAuthHelper.AddAuthHeader(_client, factory.Services);
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task StreamEndpoint_ReturnsEventStreamContentType()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/mcpserver/events");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("text/event-stream", response.Content.Headers.ContentType?.MediaType, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StreamEndpoint_WithCategoryFilter_EmitsFilteredEvent()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/mcpserver/events?category=todo");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(true);
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

        // Allow subscription pipeline to fully attach before publishing.
        await Task.Delay(150, cts.Token).ConfigureAwait(true);
        var bus = _factory.Services.GetRequiredService<IChangeEventBus>();
        await bus.PublishAsync(new ChangeEvent
        {
            Category = ChangeEventCategories.Todo,
            Action = ChangeEventActions.Updated,
            EntityId = "EVENT-STREAM-001",
            ResourceUri = "mcp://workspace/todo/EVENT-STREAM-001"
        }, cts.Token).ConfigureAwait(true);

        var sawTodoEvent = false;
        var sawEntityId = false;
        while (!cts.IsCancellationRequested && (!sawTodoEvent || !sawEntityId))
        {
            var line = await reader.ReadLineAsync(cts.Token).ConfigureAwait(true);
            if (line is null)
                break;

            if (line.StartsWith("event: ", StringComparison.OrdinalIgnoreCase) &&
                line.Contains("todo", StringComparison.OrdinalIgnoreCase))
            {
                sawTodoEvent = true;
            }

            if (line.StartsWith("data: ", StringComparison.OrdinalIgnoreCase) &&
                line.Contains("\"entityId\":\"EVENT-STREAM-001\"", StringComparison.Ordinal))
            {
                sawEntityId = true;
            }
        }

        Assert.True(sawTodoEvent);
        Assert.True(sawEntityId);
    }

    [Fact]
    public async Task StreamEndpoint_WithCategoryFilter_DoesNotEmitNonMatchingEvents()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/mcpserver/events?category=todo");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(true);
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

        // Allow subscription pipeline to fully attach before publishing.
        await Task.Delay(150, cts.Token).ConfigureAwait(true);

        var bus = _factory.Services.GetRequiredService<IChangeEventBus>();
        await bus.PublishAsync(new ChangeEvent
        {
            Category = ChangeEventCategories.GitHub,
            Action = ChangeEventActions.Updated,
            EntityId = "EVENT-STREAM-NONMATCH-001",
            ResourceUri = "mcp://workspace/github/EVENT-STREAM-NONMATCH-001"
        }, cts.Token).ConfigureAwait(true);

        await bus.PublishAsync(new ChangeEvent
        {
            Category = ChangeEventCategories.Todo,
            Action = ChangeEventActions.Updated,
            EntityId = "EVENT-STREAM-MATCH-001",
            ResourceUri = "mcp://workspace/todo/EVENT-STREAM-MATCH-001"
        }, cts.Token).ConfigureAwait(true);

        var sawTodoEvent = false;
        var sawTodoEntity = false;
        var sawGithubEvent = false;
        var sawGithubEntity = false;

        while (!cts.IsCancellationRequested && (!sawTodoEvent || !sawTodoEntity))
        {
            var line = await reader.ReadLineAsync(cts.Token).ConfigureAwait(true);
            if (line is null)
                break;

            if (line.StartsWith("event: ", StringComparison.OrdinalIgnoreCase))
            {
                if (line.Contains("todo", StringComparison.OrdinalIgnoreCase))
                    sawTodoEvent = true;
                if (line.Contains("github", StringComparison.OrdinalIgnoreCase))
                    sawGithubEvent = true;
            }

            if (line.StartsWith("data: ", StringComparison.OrdinalIgnoreCase))
            {
                if (line.Contains("\"entityId\":\"EVENT-STREAM-MATCH-001\"", StringComparison.Ordinal))
                    sawTodoEntity = true;
                if (line.Contains("\"entityId\":\"EVENT-STREAM-NONMATCH-001\"", StringComparison.Ordinal))
                    sawGithubEntity = true;
            }
        }

        Assert.True(sawTodoEvent);
        Assert.True(sawTodoEntity);
        Assert.False(sawGithubEvent);
        Assert.False(sawGithubEntity);
    }
}
