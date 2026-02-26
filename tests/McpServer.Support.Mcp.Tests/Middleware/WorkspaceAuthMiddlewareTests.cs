using McpServer.Support.Mcp.Middleware;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Middleware;

/// <summary>Unit tests for <see cref="WorkspaceAuthMiddleware"/> default key behavior.</summary>
public sealed class WorkspaceAuthMiddlewareTests
{
    private const string WorkspacePath = @"C:\projects\test";

    private static WorkspaceTokenService CreateTokenService()
    {
        var svc = new WorkspaceTokenService();
        svc.GenerateToken(WorkspacePath);
        svc.GenerateDefaultToken(WorkspacePath);
        return svc;
    }

    private static IConfiguration CreateConfig()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mcp:RepoRoot"] = WorkspacePath,
            })
            .Build();
    }

    private static WorkspaceContext CreateWorkspaceContext()
    {
        return new WorkspaceContext { WorkspacePath = WorkspacePath };
    }

    private static DefaultHttpContext CreateContext(string method, string path, string? apiKey)
    {
        var ctx = new DefaultHttpContext
        {
            Request = { Method = method, Path = path },
            Response = { Body = new MemoryStream() }
        };
        if (apiKey is not null)
            ctx.Request.Headers["X-Api-Key"] = apiKey;
        return ctx;
    }

    [Fact]
    public async Task FullToken_AllowsWriteOnNonTodoRoute()
    {
        var tokenService = CreateTokenService();
        var fullToken = tokenService.GetToken(WorkspacePath)!;
        var nextCalled = false;
        var middleware = new WorkspaceAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = CreateContext("POST", "/mcp/sync/run", fullToken);

        await middleware.InvokeAsync(ctx, tokenService, CreateConfig(), CreateWorkspaceContext());

        Assert.True(nextCalled);
        Assert.Equal(200, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task DefaultToken_AllowsReadOnNonTodoRoute()
    {
        var tokenService = CreateTokenService();
        var defaultToken = tokenService.GetDefaultToken(WorkspacePath)!;
        var nextCalled = false;
        var middleware = new WorkspaceAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = CreateContext("GET", "/mcp/context/search", defaultToken);

        await middleware.InvokeAsync(ctx, tokenService, CreateConfig(), CreateWorkspaceContext());

        Assert.True(nextCalled);
        Assert.True((bool)ctx.Items[WorkspaceAuthMiddleware.IsDefaultKeyItem]!);
    }

    [Fact]
    public async Task DefaultToken_AllowsWriteOnTodoRoute()
    {
        var tokenService = CreateTokenService();
        var defaultToken = tokenService.GetDefaultToken(WorkspacePath)!;
        var nextCalled = false;
        var middleware = new WorkspaceAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = CreateContext("POST", "/mcp/todo", defaultToken);

        await middleware.InvokeAsync(ctx, tokenService, CreateConfig(), CreateWorkspaceContext());

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task DefaultToken_DeniesWriteOnNonTodoRoute()
    {
        var tokenService = CreateTokenService();
        var defaultToken = tokenService.GetDefaultToken(WorkspacePath)!;
        var nextCalled = false;
        var middleware = new WorkspaceAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = CreateContext("POST", "/mcp/sync/run", defaultToken);

        await middleware.InvokeAsync(ctx, tokenService, CreateConfig(), CreateWorkspaceContext());

        Assert.False(nextCalled);
        Assert.Equal(403, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task DefaultToken_DeniesDeleteOnNonTodoRoute()
    {
        var tokenService = CreateTokenService();
        var defaultToken = tokenService.GetDefaultToken(WorkspacePath)!;
        var nextCalled = false;
        var middleware = new WorkspaceAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = CreateContext("DELETE", "/mcp/repo/test.txt", defaultToken);

        await middleware.InvokeAsync(ctx, tokenService, CreateConfig(), CreateWorkspaceContext());

        Assert.False(nextCalled);
        Assert.Equal(403, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task DefaultToken_AllowsDeleteOnTodoRoute()
    {
        var tokenService = CreateTokenService();
        var defaultToken = tokenService.GetDefaultToken(WorkspacePath)!;
        var nextCalled = false;
        var middleware = new WorkspaceAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = CreateContext("DELETE", "/mcp/todo/MVP-APP-001", defaultToken);

        await middleware.InvokeAsync(ctx, tokenService, CreateConfig(), CreateWorkspaceContext());

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvalidToken_Returns401()
    {
        var tokenService = CreateTokenService();
        var nextCalled = false;
        var middleware = new WorkspaceAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = CreateContext("GET", "/mcp/todo", "totally-wrong-token");

        await middleware.InvokeAsync(ctx, tokenService, CreateConfig(), CreateWorkspaceContext());

        Assert.False(nextCalled);
        Assert.Equal(401, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task NonMcpRoute_PassesThroughWithoutAuth()
    {
        var tokenService = CreateTokenService();
        var nextCalled = false;
        var middleware = new WorkspaceAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = CreateContext("GET", "/health", null);

        await middleware.InvokeAsync(ctx, tokenService, CreateConfig(), CreateWorkspaceContext());

        Assert.True(nextCalled);
    }
}
