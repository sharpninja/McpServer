using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Middleware;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Middleware;

/// <summary>Unit tests for <see cref="WorkspaceAuthMiddleware"/> default-key behavior.</summary>
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

    private static IOptions<FederationOptions> CreateFederationOptions(Action<FederationOptions>? configure = null)
    {
        var options = new FederationOptions();
        configure?.Invoke(options);
        return Microsoft.Extensions.Options.Options.Create(options);
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
        var middleware = new WorkspaceAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, NullLogger<WorkspaceAuthMiddleware>.Instance);
        var ctx = CreateContext("POST", "/mcpserver/repo/file", fullToken);

        await middleware.InvokeAsync(ctx, tokenService, CreateConfig(), CreateWorkspaceContext(), CreateFederationOptions());

        Assert.True(nextCalled);
        Assert.Equal(200, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task DefaultToken_AllowsReadOnNonTodoRoute()
    {
        var tokenService = CreateTokenService();
        var defaultToken = tokenService.GetDefaultToken(WorkspacePath)!;
        var nextCalled = false;
        var middleware = new WorkspaceAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, NullLogger<WorkspaceAuthMiddleware>.Instance);
        var ctx = CreateContext("GET", "/mcpserver/context/search", defaultToken);

        await middleware.InvokeAsync(ctx, tokenService, CreateConfig(), CreateWorkspaceContext(), CreateFederationOptions());

        Assert.True(nextCalled);
        Assert.True((bool)ctx.Items[WorkspaceAuthMiddleware.IsDefaultKeyItem]!);
    }

    [Fact]
    public async Task DefaultToken_DeniesWriteOnTodoRoute()
    {
        var tokenService = CreateTokenService();
        var defaultToken = tokenService.GetDefaultToken(WorkspacePath)!;
        var nextCalled = false;
        var middleware = new WorkspaceAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, NullLogger<WorkspaceAuthMiddleware>.Instance);
        var ctx = CreateContext("POST", "/mcpserver/todo", defaultToken);

        await middleware.InvokeAsync(ctx, tokenService, CreateConfig(), CreateWorkspaceContext(), CreateFederationOptions());
        Assert.False(nextCalled);
        Assert.Equal(403, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task DefaultToken_DeniesWriteOnNonTodoRoute()
    {
        var tokenService = CreateTokenService();
        var defaultToken = tokenService.GetDefaultToken(WorkspacePath)!;
        var nextCalled = false;
        var middleware = new WorkspaceAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, NullLogger<WorkspaceAuthMiddleware>.Instance);
        var ctx = CreateContext("POST", "/mcpserver/repo/file", defaultToken);

        await middleware.InvokeAsync(ctx, tokenService, CreateConfig(), CreateWorkspaceContext(), CreateFederationOptions());

        Assert.False(nextCalled);
        Assert.Equal(403, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task DefaultToken_DeniesDeleteOnNonTodoRoute()
    {
        var tokenService = CreateTokenService();
        var defaultToken = tokenService.GetDefaultToken(WorkspacePath)!;
        var nextCalled = false;
        var middleware = new WorkspaceAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, NullLogger<WorkspaceAuthMiddleware>.Instance);
        var ctx = CreateContext("DELETE", "/mcpserver/repo/test.txt", defaultToken);

        await middleware.InvokeAsync(ctx, tokenService, CreateConfig(), CreateWorkspaceContext(), CreateFederationOptions());

        Assert.False(nextCalled);
        Assert.Equal(403, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task DefaultToken_DeniesDeleteOnTodoRoute()
    {
        var tokenService = CreateTokenService();
        var defaultToken = tokenService.GetDefaultToken(WorkspacePath)!;
        var nextCalled = false;
        var middleware = new WorkspaceAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, NullLogger<WorkspaceAuthMiddleware>.Instance);
        var ctx = CreateContext("DELETE", "/mcpserver/todo/MVP-APP-001", defaultToken);

        await middleware.InvokeAsync(ctx, tokenService, CreateConfig(), CreateWorkspaceContext(), CreateFederationOptions());

        Assert.False(nextCalled);
        Assert.Equal(403, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task InvalidToken_Returns401()
    {
        var tokenService = CreateTokenService();
        var nextCalled = false;
        var middleware = new WorkspaceAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, NullLogger<WorkspaceAuthMiddleware>.Instance);
        var ctx = CreateContext("GET", "/mcpserver/todo", "totally-wrong-token");

        await middleware.InvokeAsync(ctx, tokenService, CreateConfig(), CreateWorkspaceContext(), CreateFederationOptions());

        Assert.False(nextCalled);
        Assert.Equal(401, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task HubAccessToken_AllowsFederationHubRequest()
    {
        var tokenService = new WorkspaceTokenService();
        var nextCalled = false;
        var middleware = new WorkspaceAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, NullLogger<WorkspaceAuthMiddleware>.Instance);
        var ctx = CreateContext("POST", "/mcpserver/todo", "hub-secret");

        await middleware.InvokeAsync(
            ctx,
            tokenService,
            CreateConfig(),
            CreateWorkspaceContext(),
            CreateFederationOptions(options =>
            {
                options.Enabled = true;
                options.Role = FederationRole.Hub;
                options.HubAccessToken = "hub-secret";
            }));

        Assert.True(nextCalled);
        Assert.Equal(200, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task NonMcpRoute_PassesThroughWithoutAuth()
    {
        var tokenService = CreateTokenService();
        var nextCalled = false;
        var middleware = new WorkspaceAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, NullLogger<WorkspaceAuthMiddleware>.Instance);
        var ctx = CreateContext("GET", "/health", null);

        await middleware.InvokeAsync(ctx, tokenService, CreateConfig(), CreateWorkspaceContext(), CreateFederationOptions());

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task ReadsWorkspaceFromContext_InsteadOfConfig()
    {
        // WorkspaceContext points to WorkspacePath, config points elsewhere.
        // Auth middleware should use the context workspace path.
        var tokenService = new WorkspaceTokenService();
        tokenService.GenerateToken(WorkspacePath);
        var fullToken = tokenService.GetToken(WorkspacePath)!;
        var configOther = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Mcp:RepoRoot"] = @"C:\other" })
            .Build();
        var wsContext = new WorkspaceContext { WorkspacePath = WorkspacePath };
        var nextCalled = false;
        var middleware = new WorkspaceAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, NullLogger<WorkspaceAuthMiddleware>.Instance);
        var ctx = CreateContext("POST", "/mcpserver/repo/file", fullToken);

        await middleware.InvokeAsync(ctx, tokenService, configOther, wsContext, CreateFederationOptions());

        Assert.True(nextCalled, "Should accept token validated against WorkspaceContext path, not config path");
    }

    [Fact]
    public async Task MissingWorkspaceToken_Returns503()
    {
        var tokenService = new WorkspaceTokenService();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mcp:ApiKey"] = "",
                ["Mcp:RepoRoot"] = WorkspacePath,
            })
            .Build();
        var wsContext = new WorkspaceContext { WorkspacePath = WorkspacePath };
        var nextCalled = false;
        var middleware = new WorkspaceAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, NullLogger<WorkspaceAuthMiddleware>.Instance);
        var ctx = CreateContext("POST", "/mcpserver/repo/file", null);

        await middleware.InvokeAsync(ctx, tokenService, config, wsContext, CreateFederationOptions());

        Assert.False(nextCalled);
        Assert.Equal(503, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task EmptyWorkspaceContext_Returns503()
    {
        var tokenService = new WorkspaceTokenService();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var wsContext = new WorkspaceContext();
        var nextCalled = false;
        var middleware = new WorkspaceAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, NullLogger<WorkspaceAuthMiddleware>.Instance);
        var ctx = CreateContext("GET", "/mcpserver/context/search", null);

        await middleware.InvokeAsync(ctx, tokenService, config, wsContext, CreateFederationOptions());

        Assert.False(nextCalled);
        Assert.Equal(503, ctx.Response.StatusCode);
    }

    // --- FR-MCP-132 / TR-MCP-AUTH-010: unknown/stale/missing key on a workspace-independent
    //     route is an authentication outcome (401), not a startup readiness 503, once the
    //     auth-token subsystem is initialized. -----------------------------------------------

    private static IConfiguration CreateConfigWithRepoRoot(string repoRoot)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Mcp:RepoRoot"] = repoRoot })
            .Build();

    /// <summary>
    /// TEST-MCP-AUTH-010: An unknown API key with no resolved workspace (workspace-independent
    /// route, no X-Workspace-Path) where the Mcp:RepoRoot fallback path has no seeded token, but
    /// the subsystem IS initialized, returns 401 - not the legacy "token not initialized" 503.
    /// </summary>
    [Fact]
    public async Task UnknownApiKey_Unresolved_Initialized_Returns401()
    {
        var tokenService = new WorkspaceTokenService();
        tokenService.GenerateToken(@"C:\real\workspace"); // initialized, but for a different path
        var nextCalled = false;
        var middleware = new WorkspaceAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, NullLogger<WorkspaceAuthMiddleware>.Instance);
        var ctx = CreateContext("GET", "/mcpserver/todo", "stale-or-wrong-key");

        await middleware.InvokeAsync(
            ctx,
            tokenService,
            CreateConfigWithRepoRoot(@"C:\different\repo\root"),
            new WorkspaceContext { WorkspacePath = null },
            CreateFederationOptions());

        Assert.False(nextCalled);
        Assert.Equal(401, ctx.Response.StatusCode);
    }

    /// <summary>
    /// TEST-MCP-AUTH-010: A missing API key under the same unresolved/initialized condition returns 401.
    /// </summary>
    [Fact]
    public async Task NoApiKey_Unresolved_Initialized_Returns401()
    {
        var tokenService = new WorkspaceTokenService();
        tokenService.GenerateToken(@"C:\real\workspace");
        var nextCalled = false;
        var middleware = new WorkspaceAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, NullLogger<WorkspaceAuthMiddleware>.Instance);
        var ctx = CreateContext("GET", "/mcpserver/todo", null);

        await middleware.InvokeAsync(
            ctx,
            tokenService,
            CreateConfigWithRepoRoot(@"C:\different\repo\root"),
            new WorkspaceContext { WorkspacePath = null },
            CreateFederationOptions());

        Assert.False(nextCalled);
        Assert.Equal(401, ctx.Response.StatusCode);
    }

    /// <summary>
    /// TEST-MCP-AUTH-010: An empty fallback repo root is still a credential failure, not startup 503,
    /// once the token subsystem is initialized.
    /// </summary>
    [Fact]
    public async Task EmptyRepoRoot_Unresolved_Initialized_Returns401()
    {
        var tokenService = new WorkspaceTokenService();
        tokenService.GenerateToken(@"C:\real\workspace");
        var nextCalled = false;
        var middleware = new WorkspaceAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, NullLogger<WorkspaceAuthMiddleware>.Instance);
        var ctx = CreateContext("GET", "/mcpserver/todo", null);

        await middleware.InvokeAsync(
            ctx,
            tokenService,
            CreateConfigWithRepoRoot(string.Empty),
            new WorkspaceContext { WorkspacePath = null },
            CreateFederationOptions());

        Assert.False(nextCalled);
        Assert.Equal(401, ctx.Response.StatusCode);
    }

    /// <summary>
    /// TEST-MCP-AUTH-011: When the auth-token subsystem is genuinely not initialized, the 503
    /// readiness response must carry a Retry-After header.
    /// </summary>
    [Fact]
    public async Task SubsystemNotInitialized_Returns503WithRetryAfter()
    {
        var tokenService = new WorkspaceTokenService(); // no tokens -> not initialized
        var nextCalled = false;
        var middleware = new WorkspaceAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, NullLogger<WorkspaceAuthMiddleware>.Instance);
        var ctx = CreateContext("GET", "/mcpserver/todo", "anything");

        await middleware.InvokeAsync(
            ctx,
            tokenService,
            CreateConfigWithRepoRoot(@"C:\different\repo\root"),
            new WorkspaceContext { WorkspacePath = null },
            CreateFederationOptions());

        Assert.False(nextCalled);
        Assert.Equal(503, ctx.Response.StatusCode);
        Assert.True(ctx.Response.Headers.ContainsKey("Retry-After"),
            "A 503 readiness response must include a Retry-After header.");
    }
}
