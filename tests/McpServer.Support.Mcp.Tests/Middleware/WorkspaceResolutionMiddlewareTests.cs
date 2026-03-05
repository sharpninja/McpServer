using McpServer.Support.Mcp.Middleware;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Middleware;

/// <summary>Unit tests for <see cref="WorkspaceResolutionMiddleware"/>.</summary>
public sealed class WorkspaceResolutionMiddlewareTests
{
    private const string WorkspaceA = @"C:\projects\alpha";
    private const string WorkspaceB = @"C:\projects\beta";

    private static WorkspaceResolutionMiddleware CreateMiddleware(RequestDelegate next)
        => new(next, NullLogger<WorkspaceResolutionMiddleware>.Instance);

    private static WorkspaceDto MakeDto(string path, bool isPrimary = false)
        => new()
        {
            WorkspacePath = path,
            Name = Path.GetFileName(path),
            TodoPath = "docs/todo.yaml",
            IsPrimary = isPrimary,
            StatusPrompt = "",
            ImplementPrompt = "",
            PlanPrompt = "",
        };

    private static IWorkspaceService CreateWorkspaceService(params WorkspaceDto[] workspaces)
    {
        var svc = Substitute.For<IWorkspaceService>();
        svc.ListAsync(Arg.Any<CancellationToken>())
            .Returns(new WorkspaceListResult(workspaces, workspaces.Length));
        foreach (var ws in workspaces)
        {
            svc.GetAsync(
                Arg.Is<string>(p => p != null && string.Equals(
                    Path.GetFullPath(p).TrimEnd(Path.DirectorySeparatorChar),
                    Path.GetFullPath(ws.WorkspacePath ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase)),
                Arg.Any<CancellationToken>())
                .Returns(ws);
        }
        return svc;
    }

    private static DefaultHttpContext CreateContext(string path, string method = "GET", string? workspaceHeader = null, string? apiKey = null)
    {
        var ctx = new DefaultHttpContext
        {
            Request = { Method = method, Path = path },
            Response = { Body = new MemoryStream() },
        };
        if (workspaceHeader is not null)
            ctx.Request.Headers[WorkspaceResolutionMiddleware.WorkspacePathHeader] = workspaceHeader;
        if (apiKey is not null)
            ctx.Request.Headers["X-Api-Key"] = apiKey;
        return ctx;
    }

    [Fact]
    public async Task XWorkspacePath_Header_ResolvesWorkspace()
    {
        var wsDto = MakeDto(WorkspaceA);
        var workspaceService = CreateWorkspaceService(wsDto);
        var tokenService = new WorkspaceTokenService();
        var wsContext = new WorkspaceContext();
        var nextCalled = false;
        var mw = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        var ctx = CreateContext("/mcpserver/todo", workspaceHeader: WorkspaceA);
        await mw.InvokeAsync(ctx, wsContext, tokenService, workspaceService);

        Assert.True(nextCalled);
        Assert.True(wsContext.IsResolved);
        Assert.Contains("alpha", wsContext.WorkspacePath!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task XWorkspacePath_Header_InvalidPath_Returns400()
    {
        var workspaceService = CreateWorkspaceService(); // no workspaces registered
        var tokenService = new WorkspaceTokenService();
        var wsContext = new WorkspaceContext();
        var nextCalled = false;
        var mw = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        var ctx = CreateContext("/mcpserver/todo", workspaceHeader: @"C:\nonexistent");
        await mw.InvokeAsync(ctx, wsContext, tokenService, workspaceService);

        Assert.False(nextCalled);
        Assert.Equal(400, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task ApiKey_ResolvesWorkspace_WhenNoHeader()
    {
        var wsDto = MakeDto(WorkspaceA);
        var workspaceService = CreateWorkspaceService(wsDto);
        var tokenService = new WorkspaceTokenService();
        var token = tokenService.GenerateToken(WorkspaceA);
        var wsContext = new WorkspaceContext();
        var nextCalled = false;
        var mw = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        var ctx = CreateContext("/mcpserver/todo", apiKey: token);
        await mw.InvokeAsync(ctx, wsContext, tokenService, workspaceService);

        Assert.True(nextCalled);
        Assert.True(wsContext.IsResolved);
        Assert.Contains("alpha", wsContext.WorkspacePath!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NoHeaderNoKey_PassesThroughWithoutResolution()
    {
        var wsDto = MakeDto(WorkspaceA, isPrimary: true);
        var workspaceService = CreateWorkspaceService(wsDto);
        var tokenService = new WorkspaceTokenService();
        var wsContext = new WorkspaceContext();
        var nextCalled = false;
        var mw = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        var ctx = CreateContext("/mcpserver/todo");
        await mw.InvokeAsync(ctx, wsContext, tokenService, workspaceService);

        Assert.True(nextCalled);
        Assert.False(wsContext.IsResolved);
    }

    [Fact]
    public async Task ApiKey_UnknownToken_PassesThroughWithoutResolution()
    {
        var wsDto = MakeDto(WorkspaceA, isPrimary: true);
        var workspaceService = CreateWorkspaceService(wsDto);
        var tokenService = new WorkspaceTokenService();
        var wsContext = new WorkspaceContext();
        var nextCalled = false;
        var mw = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        var ctx = CreateContext("/mcpserver/todo", apiKey: "unknown-token-abc");
        await mw.InvokeAsync(ctx, wsContext, tokenService, workspaceService);

        Assert.True(nextCalled);
        Assert.False(wsContext.IsResolved);
    }

    [Fact]
    public async Task HeaderTakesPriority_OverApiKey()
    {
        var wsDtoA = MakeDto(WorkspaceA);
        var wsDtoB = MakeDto(WorkspaceB);
        var workspaceService = CreateWorkspaceService(wsDtoA, wsDtoB);
        var tokenService = new WorkspaceTokenService();
        var tokenB = tokenService.GenerateToken(WorkspaceB);
        var wsContext = new WorkspaceContext();
        var nextCalled = false;
        var mw = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        // Header says workspace A, but API key is for workspace B → header wins
        var ctx = CreateContext("/mcpserver/todo", workspaceHeader: WorkspaceA, apiKey: tokenB);
        await mw.InvokeAsync(ctx, wsContext, tokenService, workspaceService);

        Assert.True(nextCalled);
        Assert.Contains("alpha", wsContext.WorkspacePath!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NonMcpRoute_SkipsResolution()
    {
        var workspaceService = Substitute.For<IWorkspaceService>();
        var tokenService = new WorkspaceTokenService();
        var wsContext = new WorkspaceContext();
        var nextCalled = false;
        var mw = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        var ctx = CreateContext("/health");
        await mw.InvokeAsync(ctx, wsContext, tokenService, workspaceService);

        Assert.True(nextCalled);
        Assert.False(wsContext.IsResolved);
        await workspaceService.DidNotReceive().ListAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task McpTransport_GetsResolution()
    {
        var wsDto = MakeDto(WorkspaceA);
        var workspaceService = CreateWorkspaceService(wsDto);
        var tokenService = new WorkspaceTokenService();
        var wsContext = new WorkspaceContext();
        var nextCalled = false;
        var mw = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        var ctx = CreateContext("/mcp-transport", workspaceHeader: WorkspaceA);
        await mw.InvokeAsync(ctx, wsContext, tokenService, workspaceService);

        Assert.True(nextCalled);
        Assert.True(wsContext.IsResolved);
    }

    [Fact]
    public async Task EmptyHeader_SkipsToApiKeyFallback()
    {
        var wsDto = MakeDto(WorkspaceA);
        var workspaceService = CreateWorkspaceService(wsDto);
        var tokenService = new WorkspaceTokenService();
        var token = tokenService.GenerateToken(WorkspaceA);
        var wsContext = new WorkspaceContext();
        var nextCalled = false;
        var mw = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        var ctx = CreateContext("/mcpserver/todo", workspaceHeader: "", apiKey: token);
        await mw.InvokeAsync(ctx, wsContext, tokenService, workspaceService);

        Assert.True(nextCalled);
        Assert.True(wsContext.IsResolved);
        Assert.Contains("alpha", wsContext.WorkspacePath!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DefaultToken_SetsIsDefaultKey()
    {
        var wsDto = MakeDto(WorkspaceA);
        var workspaceService = CreateWorkspaceService(wsDto);
        var tokenService = new WorkspaceTokenService();
        var defToken = tokenService.GenerateDefaultToken(WorkspaceA);
        var wsContext = new WorkspaceContext();
        var mw = CreateMiddleware(_ => Task.CompletedTask);

        var ctx = CreateContext("/mcpserver/todo", apiKey: defToken);
        await mw.InvokeAsync(ctx, wsContext, tokenService, workspaceService);

        Assert.True(wsContext.IsResolved);
        Assert.True(wsContext.IsDefaultKey);
    }
}
