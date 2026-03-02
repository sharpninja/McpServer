using System;
using System.Net;
using System.Net.Http;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class ErrorHandlingTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key"
    };

    [Fact]
    public async System.Threading.Tasks.Task BadRequest_ThrowsMcpValidationException()
    {
        var handler = new MockHttpHandler(HttpStatusCode.BadRequest, """{"error":"Invalid request"}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var ex = await Assert.ThrowsAsync<McpValidationException>(() => client.QueryAsync());
        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("Invalid request", ex.Message);
    }

    [Fact]
    public async System.Threading.Tasks.Task Unauthorized_ThrowsMcpUnauthorizedException()
    {
        var handler = new MockHttpHandler(HttpStatusCode.Unauthorized, """{"error":"Invalid or missing API key."}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var ex = await Assert.ThrowsAsync<McpUnauthorizedException>(() => client.QueryAsync());
        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public async System.Threading.Tasks.Task NotFound_ThrowsMcpNotFoundException()
    {
        var handler = new MockHttpHandler(HttpStatusCode.NotFound, """{"error":"Item not found"}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var ex = await Assert.ThrowsAsync<McpNotFoundException>(() => client.GetAsync("NOPE"));
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async System.Threading.Tasks.Task Conflict_ThrowsMcpConflictException()
    {
        var handler = new MockHttpHandler(HttpStatusCode.Conflict, """{"error":"Already exists"}""");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var ex = await Assert.ThrowsAsync<McpConflictException>(() =>
            client.CreateAsync(new Models.TodoCreateRequest { Id = "X", Title = "T", Section = "s", Priority = "high" }));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async System.Threading.Tasks.Task ServerError_ThrowsMcpServerException()
    {
        var handler = new MockHttpHandler(HttpStatusCode.InternalServerError, "Internal error");
        using var http = new HttpClient(handler);
        var client = new TodoClient(http, DefaultOptions);

        var ex = await Assert.ThrowsAsync<McpServerException>(() => client.QueryAsync());
        Assert.Equal(500, ex.StatusCode);
    }
}
