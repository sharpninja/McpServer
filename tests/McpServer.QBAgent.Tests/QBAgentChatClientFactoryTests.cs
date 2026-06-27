using McpServer.McpAgent;
using McpServer.QBAgent;
using Microsoft.Extensions.AI;

namespace McpServer.QBAgent.Tests;

/// <summary>
/// TEST-MCP-QBOPENAI-001 (slice 4): Verifies QBAgent builds its OpenAI-compatible chat client against the
/// QuadBrain <c>/v1</c> endpoint derived from the marker base URL.
/// </summary>
public sealed class QBAgentChatClientFactoryTests
{
    /// <summary>The endpoint appends <c>/v1</c> to the workspace base URL.</summary>
    [Fact]
    public void BuildEndpoint_AppendsV1()
        => Assert.Equal(new Uri("http://server:7147/v1"), QBAgentChatClientFactory.BuildEndpoint(new Uri("http://server:7147")));

    /// <summary>An already-<c>/v1</c> base URL is not doubled.</summary>
    [Fact]
    public void BuildEndpoint_AlreadyV1_NotDoubled()
        => Assert.Equal(new Uri("http://server:7147/v1"), QBAgentChatClientFactory.BuildEndpoint(new Uri("http://server:7147/v1")));

    /// <summary>A chat client is built from marker-bound options without throwing.</summary>
    [Fact]
    public void Create_BuildsChatClient()
    {
        using var client = QBAgentChatClientFactory.Create(new McpAgentOptions
        {
            BaseUrl = new Uri("http://payton-legion2:7147"),
            ApiKey = "marker-key",
        });

        Assert.NotNull(client);
    }

    /// <summary>QBAgent reports the public QuadBrain model, not an internal brain-slot provider model.</summary>
    [Fact]
    public void ModelId_IsPublicQuadBrainModel()
        => Assert.Equal("QuadBrain", QBAgentChatClientFactory.ModelId);

    /// <summary>FR-MCP-QBOPENAI-001 (G-014): an unreachable QuadBrain endpoint propagates a clear transport failure without hanging QBAgent.</summary>
    [Fact]
    public async Task Create_UnreachableEndpoint_PropagatesTransportFailure()
    {
        using var httpClient = new HttpClient(new ThrowingHandler());
        using var client = QBAgentChatClientFactory.Create(
            new McpAgentOptions { BaseUrl = new Uri("http://offline-host:7147"), ApiKey = "marker-key" },
            httpClient);

        var ex = await Record.ExceptionAsync(() => client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")]))
            .ConfigureAwait(true);
        Assert.NotNull(ex);
        Assert.Contains("QuadBrain endpoint unreachable", ex!.ToString(), StringComparison.Ordinal);
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("QuadBrain endpoint unreachable");
    }
}
