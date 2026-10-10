using System.Net;
using System.Net.Http;
using System.Text.Json;
using McpServer.Client.Models;
using Xunit;

namespace McpServer.Client.Tests;

/// <summary>
/// Overlay G4: typed-client hostile-review contracts hit submit/status/get/query only.
/// TEST-MCP-HOSTILEREVIEW-006.
/// </summary>
public sealed class HostileReviewClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key",
    };

    /// <summary>TEST-MCP-HOSTILEREVIEW-006: SubmitAsync posts the shared submit contract.</summary>
    [Fact]
    public async Task SubmitAsync_PostsSubmitContract()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"success":true,"httpStatus":200,"requestId":"hr-1","status":"Queued"}""");
        using var http = new HttpClient(handler);
        var client = new HostileReviewClient(http, DefaultOptions);

        var result = await client.SubmitAsync(new HostileReviewSubmitRequest
        {
            TargetType = "code",
            Mode = "adversarial",
            ScopeStatement = "review",
            RequestingAgent = "g4",
            Links = [new HostileReviewArtifactLink { ArtifactType = "todo", ArtifactId = "MCP-HOSTILEREVIEW-001" }],
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/hostile-review/submit", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains("\"targetType\":\"code\"", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.DoesNotContain("SerializedPayload", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.DoesNotContain("repair", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
    }

    /// <summary>TEST-MCP-HOSTILEREVIEW-006: StatusAsync and GetAsync are read-only GETs.</summary>
    [Fact]
    public async Task StatusAndGetAsync_UseReadOnlyUrls()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"success":true,"httpStatus":200,"requestId":"hr-1","status":"Queued"}""");
        using var http = new HttpClient(handler);
        var client = new HostileReviewClient(http, DefaultOptions);

        var status = await client.StatusAsync("hr-1", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("hr-1", status.RequestId);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/hostile-review/hr-1/status", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);

        var got = await client.GetAsync("hr-1", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("/mcpserver/hostile-review/hr-1", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    /// <summary>TEST-MCP-HOSTILEREVIEW-006: QueryAsync posts AND filters and never hits repair/apply.</summary>
    [Fact]
    public async Task QueryAsync_PostsAndFilters()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """[{"success":true,"requestId":"hr-1","status":"Succeeded","requestingAgent":"g4"}]""");
        using var http = new HttpClient(handler);
        var client = new HostileReviewClient(http, DefaultOptions);

        var rows = await client.QueryAsync(new HostileReviewQueryRequest
        {
            Model = "gpt-6-astra",
            Effort = "xhigh",
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(rows);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/hostile-review/query", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains("\"model\":\"gpt-6-astra\"", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.DoesNotContain("repair", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.DoesNotContain("apply", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
    }

    /// <summary>TEST-MCP-HOSTILEREVIEW-006: public contract types round-trip through the client JSON context.</summary>
    [Fact]
    public void HostileReviewContracts_RoundTripThroughClientJsonContext()
    {
        var original = new HostileReviewResult
        {
            Success = true,
            RequestId = "hr-1",
            Status = "Queued",
            Links = [new HostileReviewArtifactLink { ArtifactType = "todo", ArtifactId = "MCP-HOSTILEREVIEW-001" }],
        };

        var json = JsonSerializer.Serialize(original, McpClientJsonContext.Default.HostileReviewResult);
        var restored = JsonSerializer.Deserialize(json, McpClientJsonContext.Default.HostileReviewResult);

        Assert.NotNull(restored);
        Assert.Equal(original.RequestId, restored!.RequestId);
        Assert.Equal("todo", restored.Links[0].ArtifactType);
        Assert.DoesNotContain("RAW-BODY", json, StringComparison.Ordinal);
        Assert.DoesNotContain("SerializedPayload", json, StringComparison.Ordinal);
    }
}
