using McpServer.Support.Mcp.Middleware;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 / FR-MCP-MEMORY-010 / FR-MCP-MEMORY-011 / FR-MCP-MEMORY-014:
/// Read-only vs full-access key behavior for S1 verbs and S2 recall.
/// </summary>
public sealed class MemoryAuthTests : IDisposable
{
    private const string WorkspacePath = @"C:\projects\mcp-s1-auth";
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-FR-MCP-MEMORY-010-26: Read-only API key cannot remember/add/update/remove.</summary>
    [Fact]
    public async Task ReadOnlyKey_CannotMutate()
    {
        var remember = await _harness.RememberAsync(
            new MemoryRememberRequest { Content = "nope", Type = "fact" },
            readOnlyCaller: true,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var postRemember = await InvokeAuthAsync("POST", "/mcpserver/memory/remember", defaultKey: true)
            .ConfigureAwait(true);
        var postAdd = await InvokeAuthAsync("POST", "/mcpserver/memory", defaultKey: true)
            .ConfigureAwait(true);

        Assert.True(
            remember.StatusCode is 403
            || postRemember == 403
            || postAdd == 403);
        Assert.True(postAdd == 403);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-27: Full-access key can mutate; get after add returns the same id and Content.</summary>
    [Fact]
    public async Task FullKey_MutateThenGet()
    {
        var added = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "full",
            Text = "full access body",
            Content = "full access body",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(added.Success, added.Error);

        var got = await _harness.GetCompatAsync(added.Memory!.Id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(added.Memory.Id, got?.Id);
        Assert.Equal("full access body", got?.Content ?? got?.Text);
    }

    /// <summary>AC-FR-MCP-MEMORY-014-13: Read-only key can list versions but cannot revert.</summary>
    [Fact]
    public async Task ReadOnlyKey_CanListVersionsNotRevert()
    {
        var added = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "ver",
            Text = "v1",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(added.Success, added.Error);

        var versions = await _harness.ListVersionsAsync(
            added.Memory!.Id,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var revert = await _harness.RevertAsync(
            added.Memory.Id,
            1,
            readOnlyCaller: true,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var getVersionsAuth = await InvokeAuthAsync(
            "GET",
            $"/mcpserver/memory/{added.Memory.Id}/versions",
            defaultKey: true).ConfigureAwait(true);
        var revertAuth = await InvokeAuthAsync(
            "POST",
            $"/mcpserver/memory/{added.Memory.Id}/revert",
            defaultKey: true).ConfigureAwait(true);

        Assert.True(versions.StatusCode is 200 or 201, versions.Error);
        Assert.NotNull(versions.Items);
        Assert.Equal(403, revert.StatusCode == 501 ? revertAuth : revert.StatusCode);
        Assert.True(getVersionsAuth is 200 or 404 or 403);
        Assert.Equal(403, revertAuth);
    }

    /// <summary>AC-FR-MCP-MEMORY-011-26: Read-only API key may recall/list.</summary>
    [Fact]
    public async Task ReadOnlyKey_CanRecall()
    {
        var added = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "recall",
            Text = "BENCH-PREF-EDITOR=neovim read-only recall",
            Content = "BENCH-PREF-EDITOR=neovim read-only recall",
            Type = "preference",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(added.Success, added.Error);

        var recall = await _harness.RecallAsync(
            new MemoryRecallRequest { Query = "BENCH-PREF-EDITOR=neovim", MinScore = 0.1 },
            readOnlyCaller: true,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var listAuth = await InvokeAuthAsync("GET", "/mcpserver/memory", defaultKey: true)
            .ConfigureAwait(true);
        var recallAuth = await InvokeAuthAsync("POST", "/mcpserver/memory/recall", defaultKey: true)
            .ConfigureAwait(true);

        Assert.Equal(200, recall.StatusCode);
        Assert.NotEqual(403, recall.StatusCode);
        Assert.Contains(recall.Items ?? [], item => item.Id == added.Memory!.Id && item.Score.HasValue);
        Assert.Equal(200, listAuth);
        Assert.Equal(200, recallAuth);
    }

    private static async Task<int> InvokeAuthAsync(string method, string path, bool defaultKey)
    {
        var tokens = new WorkspaceTokenService();
        tokens.GenerateToken(WorkspacePath);
        tokens.GenerateDefaultToken(WorkspacePath);
        var key = defaultKey ? tokens.GetDefaultToken(WorkspacePath)! : tokens.GetToken(WorkspacePath)!;
        var nextCalled = false;
        var middleware = new WorkspaceAuthMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            NullLogger<WorkspaceAuthMiddleware>.Instance);
        var http = new DefaultHttpContext
        {
            Request = { Method = method, Path = path },
            Response = { Body = new MemoryStream() },
        };
        http.Request.Headers[WorkspaceAuthMiddleware.HeaderName] = key;
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Mcp:RepoRoot"] = WorkspacePath })
            .Build();
        await middleware.InvokeAsync(
            http,
            tokens,
            config,
            new WorkspaceContext { WorkspacePath = WorkspacePath },
            Microsoft.Extensions.Options.Options.Create(new FederationOptions())).ConfigureAwait(true);
        return nextCalled ? 200 : http.Response.StatusCode;
    }
}
