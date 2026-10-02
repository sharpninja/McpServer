using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using McpServer.Client;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 / TR-MCP-MEMORY-API-002:
/// MemoryClient methods mirror REST 1:1 for the additive verbs.
/// </summary>
public sealed class MemoryClientTests
{
    private static readonly McpServerClientOptions Options = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key",
        WorkspacePath = @"C:\projects\mcp-s5",
    };

    /// <summary>AC-TR-MCP-MEMORY-API-002-05: MemoryClient methods mirror REST 1:1.</summary>
    [Fact]
    public async Task Methods_MirrorRest()
    {
        var type = typeof(McpServer.Client.MemoryClient);
        var names = type.GetMethods().Select(method => method.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var method in MemoryS5Catalog.ClientMethods)
            Assert.Contains(method, names);

        var handler = new RecordingHandler(HttpStatusCode.OK, """{"statusCode":200,"items":[]}""");
        using var http = new HttpClient(handler);
        var client = new McpServer.Client.MemoryClient(http, Options);

        await InvokeClientAsync(client, "RememberAsync", "MemoryRememberRequest", new Dictionary<string, object?> { ["Content"] = "s5" })
            .ConfigureAwait(true);
        Assert.Equal("http://localhost:7147/mcpserver/memory/remember", handler.LastUri);
        Assert.Equal(HttpMethod.Post, handler.LastMethod);

        await InvokeClientAsync(client, "RecallAsync", "MemoryRecallRequest", new Dictionary<string, object?> { ["Query"] = "s5" })
            .ConfigureAwait(true);
        Assert.Equal("http://localhost:7147/mcpserver/memory/recall", handler.LastUri);

        await InvokeClientAsync(client, "ExploreAsync", "MemoryExploreRequest", new Dictionary<string, object?> { ["Query"] = "s5" })
            .ConfigureAwait(true);
        Assert.Equal("http://localhost:7147/mcpserver/memory/explore", handler.LastUri);

        await InvokeClientAsync(client, "ConsolidateAsync", "MemoryConsolidateRequest", new Dictionary<string, object?> { ["DryRun"] = true })
            .ConfigureAwait(true);
        Assert.Equal("http://localhost:7147/mcpserver/memory/consolidate", handler.LastUri);

        await InvokeClientAsync(client, "PromoteAsync", "MemoryPromoteRequest", new Dictionary<string, object?> { ["SourceKind"] = "sessionlog", ["SourceRef"] = "turn-1" })
            .ConfigureAwait(true);
        Assert.Equal("http://localhost:7147/mcpserver/memory/promote", handler.LastUri);

        var versions = type.GetMethod("ListVersionsAsync");
        Assert.NotNull(versions);
        await ((Task)versions!.Invoke(client, ["MEMORY-S5-001", TestContext.Current.CancellationToken])!).ConfigureAwait(true);
        Assert.Equal("http://localhost:7147/mcpserver/memory/MEMORY-S5-001/versions", handler.LastUri);

        var revert = type.GetMethod("RevertAsync");
        Assert.NotNull(revert);
        await ((Task)revert!.Invoke(client, ["MEMORY-S5-001", 1, TestContext.Current.CancellationToken])!).ConfigureAwait(true);
        Assert.Equal("http://localhost:7147/mcpserver/memory/MEMORY-S5-001/revert", handler.LastUri);
        Assert.Equal(HttpMethod.Post, handler.LastMethod);
    }

    private static async Task InvokeClientAsync(
        McpServer.Client.MemoryClient client,
        string methodName,
        string requestTypeName,
        IReadOnlyDictionary<string, object?> properties)
    {
        var requestType = typeof(McpServer.Client.MemoryClient).Assembly
            .GetTypes()
            .FirstOrDefault(type => type.Name == requestTypeName);
        Assert.NotNull(requestType);
        var request = Activator.CreateInstance(requestType!);
        Assert.NotNull(request);
        foreach (var pair in properties)
        {
            var property = requestType!.GetProperty(pair.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            Assert.NotNull(property);
            property!.SetValue(request, pair.Value);
        }

        var method = typeof(McpServer.Client.MemoryClient).GetMethods()
            .FirstOrDefault(item => item.Name == methodName && item.GetParameters().Length == 2);
        Assert.NotNull(method);
        await ((Task)method!.Invoke(client, [request, TestContext.Current.CancellationToken])!).ConfigureAwait(true);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public RecordingHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        public string? LastUri { get; private set; }

        public HttpMethod? LastMethod { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastUri = request.RequestUri?.ToString();
            LastMethod = request.Method;
            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json"),
            });
        }
    }
}
