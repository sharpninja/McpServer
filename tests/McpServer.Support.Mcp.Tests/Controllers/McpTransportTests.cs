using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>Integration tests for the MCP Streamable HTTP transport endpoint.</summary>
public sealed class McpTransportTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>Initializes a new instance of the <see cref="McpTransportTests"/> class.</summary>
    public McpTransportTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task McpTransport_PostInitialize_ReturnsOk()
    {
        var initRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2025-03-26",
                capabilities = new { },
                clientInfo = new { name = "test-client", version = "1.0.0" }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp-transport");
        request.Content = new StringContent(
            JsonSerializer.Serialize(initRequest),
            Encoding.UTF8,
            "application/json");
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        var response = await _client.SendAsync(request).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
        Assert.Contains("\"result\"", body, StringComparison.Ordinal);
        Assert.Contains("serverInfo", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task McpTransport_PostWithoutAcceptHeader_ReturnsNotAcceptable()
    {
        var initRequest = new { jsonrpc = "2.0", id = 1, method = "initialize", @params = new { protocolVersion = "2025-03-26", capabilities = new { }, clientInfo = new { name = "t", version = "1" } } };
        var content = new StringContent(JsonSerializer.Serialize(initRequest), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/mcp-transport", content).ConfigureAwait(true);

        // Without proper Accept header, MCP Streamable HTTP returns 406 Not Acceptable.
        Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
    }

    [Fact]
    public async Task McpTransport_ExistingRestEndpoints_StillWork()
    {
        var response = await _client.GetAsync("/health").ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
