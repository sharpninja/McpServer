using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>Integration tests for the MCP Streamable HTTP transport endpoint.</summary>
[Trait("Category", "Integration")]
public sealed class McpTransportTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    /// <summary>Initializes a new instance of the <see cref="McpTransportTests"/> class.</summary>
    public McpTransportTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
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

        var response = await _client.SendAsync(request, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Contains("\"result\"", body, StringComparison.Ordinal);
        Assert.Contains("serverInfo", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task McpTransport_PostWithoutAcceptHeader_ReturnsNotAcceptable()
    {
        var initRequest = new { jsonrpc = "2.0", id = 1, method = "initialize", @params = new { protocolVersion = "2025-03-26", capabilities = new { }, clientInfo = new { name = "t", version = "1" } } };
        var content = new StringContent(JsonSerializer.Serialize(initRequest), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/mcp-transport", content, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Without proper Accept header, MCP Streamable HTTP returns 406 Not Acceptable.
        Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
    }

    [Fact]
    public async Task McpTransport_ExistingRestEndpoints_StillWork()
    {
        var response = await _client.GetAsync("/health", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task McpTransport_GraphRagStatusTool_ReturnsResultPayload()
    {
        await InitializeMcpAsync().ConfigureAwait(true);

        var call = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/call",
            @params = new
            {
                name = "graphrag_status",
                arguments = new
                {
                    workspacePath = @"E:\github\McpServer"
                }
            }
        };

        var body = await SendMcpRequestAsync(call).ConfigureAwait(true);
        Assert.Contains("\"result\"", body, StringComparison.Ordinal);
        Assert.Contains("GraphRoot", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task McpTransport_GraphRagIndexAndQueryTools_ReturnExpectedFields()
    {
        await InitializeMcpAsync().ConfigureAwait(true);

        var indexCall = new
        {
            jsonrpc = "2.0",
            id = 3,
            method = "tools/call",
            @params = new
            {
                name = "graphrag_index",
                arguments = new
                {
                    workspacePath = @"E:\github\McpServer",
                    force = false
                }
            }
        };
        var indexBody = await SendMcpRequestAsync(indexCall).ConfigureAwait(true);
        Assert.Contains("IsIndexed", indexBody, StringComparison.Ordinal);

        var queryCall = new
        {
            jsonrpc = "2.0",
            id = 4,
            method = "tools/call",
            @params = new
            {
                name = "graphrag_query",
                arguments = new
                {
                    query = "auth",
                    workspacePath = @"E:\github\McpServer",
                    mode = "local",
                    maxChunks = 5
                }
            }
        };
        var queryBody = await SendMcpRequestAsync(queryCall).ConfigureAwait(true);
        Assert.Contains("fallbackUsed", queryBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("answer", queryBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task McpTransport_ContextIngestWebsiteTool_ReturnsStructuredResult()
    {
        await InitializeMcpAsync().ConfigureAwait(true);

        var call = new
        {
            jsonrpc = "2.0",
            id = 5,
            method = "tools/call",
            @params = new
            {
                name = "context_ingest_website",
                arguments = new
                {
                    workspacePath = @"E:\github\McpServer",
                    url = "http://localhost/test",
                    maxPages = 1,
                    maxDepth = 0,
                    maxBytesPerPage = 4096
                }
            }
        };

        var body = await SendMcpRequestAsync(call).ConfigureAwait(true);
        Assert.Contains("urlResults", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("status", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task McpTransport_ContextIngestWebsiteTool_MissingUrl_ReturnsErrorPayload()
    {
        await InitializeMcpAsync().ConfigureAwait(true);

        var call = new
        {
            jsonrpc = "2.0",
            id = 6,
            method = "tools/call",
            @params = new
            {
                name = "context_ingest_website",
                arguments = new
                {
                    workspacePath = @"E:\github\McpServer"
                }
            }
        };

        var body = await SendMcpRequestAsync(call).ConfigureAwait(true);
        Assert.Contains("error", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task McpTransport_ToolsList_IncludesTranscriptTools()
    {
        await InitializeMcpAsync().ConfigureAwait(true);

        var body = await SendMcpRequestAsync(new
        {
            jsonrpc = "2.0",
            id = 7,
            method = "tools/list"
        }).ConfigureAwait(true);

        Assert.Contains("sessionlog_ingest_path", body, StringComparison.Ordinal);
        Assert.Contains("sessionlog_normalize_path", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task McpTransport_SessionLogIngestPathTool_PersistsRealFixture()
    {
        await InitializeMcpAsync().ConfigureAwait(true);
        var relativePath = Path.Combine("transcripts", Guid.NewGuid().ToString("N"), "codex", "session.jsonl");
        CopyRealFixtureToWorkspace("codex/session.jsonl", relativePath);

        var body = await SendMcpRequestAsync(new
        {
            jsonrpc = "2.0",
            id = 8,
            method = "tools/call",
            @params = new
            {
                name = "sessionlog_ingest_path",
                arguments = new
                {
                    workspacePath = _factory.WorkspacePath,
                    path = relativePath,
                    agent = "Codex",
                    source = "Codex",
                    recursive = false,
                    strict = true,
                    persist = true
                }
            }
        }).ConfigureAwait(true);

        Assert.Contains("codex-real-fixture-session", body, StringComparison.Ordinal);
        Assert.Contains("sessionLogId:", body, StringComparison.Ordinal);
        Assert.Contains("persisted", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task McpTransport_SessionLogNormalizePathTool_WritesArtifactsWithoutPersistence()
    {
        await InitializeMcpAsync().ConfigureAwait(true);
        var relativePath = Path.Combine("transcripts", Guid.NewGuid().ToString("N"), "codex", "session.jsonl");
        CopyRealFixtureToWorkspace("codex/session.jsonl", relativePath);

        var body = await SendMcpRequestAsync(new
        {
            jsonrpc = "2.0",
            id = 9,
            method = "tools/call",
            @params = new
            {
                name = "sessionlog_normalize_path",
                arguments = new
                {
                    workspacePath = _factory.WorkspacePath,
                    path = relativePath,
                    agent = "Codex",
                    targetProfile = "Grok",
                    source = "Codex",
                    recursive = false,
                    strict = true
                }
            }
        }).ConfigureAwait(true);

        Assert.Contains("codex-real-fixture-session", body, StringComparison.Ordinal);
        Assert.Contains("compatibilityArtifactPath", body, StringComparison.Ordinal);
        Assert.Contains(".jsonl", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("persisted", body, StringComparison.OrdinalIgnoreCase);
    }

    private async Task InitializeMcpAsync()
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

        var body = await SendMcpRequestAsync(initRequest).ConfigureAwait(true);
        Assert.Contains("serverInfo", body, StringComparison.Ordinal);
    }

    private void CopyRealFixtureToWorkspace(string sourceRelativePath, string workspaceRelativePath)
    {
        var destination = Path.Combine(_factory.WorkspacePath, workspaceRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Copy(ResolveRealFixturePath(sourceRelativePath), destination, overwrite: true);
    }

    private static string ResolveRealFixturePath(string relativePath)
    {
        var path = Path.Combine(CustomWebApplicationFactory.ResolveSolutionRoot(), "tests", "McpServer.Support.Mcp.Tests", "Fixtures", "Transcripts", "real", relativePath);
        if (!File.Exists(path))
            throw new FileNotFoundException("Missing real transcript fixture.", path);
        return path;
    }

    private async Task<string> SendMcpRequestAsync(object payload)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp-transport");
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        var response = await _client.SendAsync(request).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync().ConfigureAwait(true);
    }
}
