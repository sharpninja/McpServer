using McpServer.McpAgent;
using McpServer.QBAgent;

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
}
