using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client;
using Xunit;

namespace McpServer.Client.Tests;

/// <summary>
/// TEST-MCP-PRODUCT-004: ProductClient posts to /mcpserver/products.
/// Phase 3 red until the client is implemented.
/// </summary>
public sealed class ProductClientTests
{
    private const string CallerWorkspaceId = "ws-caller";
    private const string OwnerWorkspaceId = "ws-owner";
    private const string MemberWorkspaceId = "ws-member";

    /// <summary>CreateAsync posts the key and deserializes ownerWorkspaceId.</summary>
    [Fact]
    public async Task CreateAsync_PostsProductsEndpoint()
    {
        var handler = new StubHandler(CallerWorkspaceId);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:7147/") };
        var client = new ProductClient(http, new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
            WorkspacePath = CallerWorkspaceId,
        });

        var result = await client.CreateAsync(
            new CreateProductRequest { Key = "PROD-MCPSERVER", Name = "McpServer" },
            CancellationToken.None);

        Assert.Equal("/mcpserver/products", handler.Path);
        Assert.Equal("PROD-MCPSERVER", result.Key);
        Assert.Equal(CallerWorkspaceId, result.OwnerWorkspaceId);
    }

    /// <summary>
    /// FR-MCP-PRODUCT-002: RemoveMemberAsync must deserialize the DELETE body.
    /// A follow-up GET after self-leave is 404 and is not the shipped contract.
    /// </summary>
    [Fact]
    public async Task RemoveMemberAsync_DeserializesDeleteBody_DoesNotGetAfterLeave()
    {
        var handler = new LeaveHandler(OwnerWorkspaceId);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:7147/") };
        var client = new ProductClient(http, new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
            WorkspacePath = MemberWorkspaceId,
        });

        var result = await client.RemoveMemberAsync("PROD-MCPSERVER", MemberWorkspaceId, CancellationToken.None);

        Assert.Equal(1, handler.RequestCount);
        Assert.Equal(HttpMethod.Delete, handler.LastMethod);
        Assert.Equal("/mcpserver/products/PROD-MCPSERVER/members/" + Uri.EscapeDataString(MemberWorkspaceId), handler.Path);
        Assert.Equal("PROD-MCPSERVER", result.Key);
        Assert.Equal(OwnerWorkspaceId, result.OwnerWorkspaceId);
        Assert.DoesNotContain(MemberWorkspaceId, result.MemberWorkspaceIds);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _ownerWorkspaceId;
        public string? Path { get; private set; }

        public StubHandler(string ownerWorkspaceId) => _ownerWorkspaceId = ownerWorkspaceId;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Path = request.RequestUri!.AbsolutePath;
            var json = $"{{\"key\":\"PROD-MCPSERVER\",\"name\":\"McpServer\",\"ownerWorkspaceId\":{JsonSerializer.Serialize(_ownerWorkspaceId)}}}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class LeaveHandler : HttpMessageHandler
    {
        private readonly string _ownerWorkspaceId;
        public int RequestCount { get; private set; }
        public string? Path { get; private set; }
        public HttpMethod? LastMethod { get; private set; }

        public LeaveHandler(string ownerWorkspaceId) => _ownerWorkspaceId = ownerWorkspaceId;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            Path = request.RequestUri!.AbsolutePath;
            LastMethod = request.Method;
            if (request.Method == HttpMethod.Delete)
            {
                var owner = JsonSerializer.Serialize(_ownerWorkspaceId);
                var json = $"{{\"key\":\"PROD-MCPSERVER\",\"name\":\"Shared\",\"ownerWorkspaceId\":{owner},\"memberWorkspaceIds\":[{owner}]}}";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("""{"error":"404: not a member"}""", Encoding.UTF8, "application/json"),
            });
        }
    }
}
