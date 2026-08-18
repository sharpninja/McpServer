using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MsOptions = Microsoft.Extensions.Options.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-HANDOFF-006: WorkspaceServiceAccessor must push/pop scoped workspace overrides
/// so nested and parallel STDIO calls cannot leak a workspace path.
/// </summary>
public sealed class WorkspaceServiceAccessorTests
{
    /// <summary>Nested PushWorkspace restores the prior override when the inner scope disposes.</summary>
    [Fact]
    public void PushWorkspace_NestedScopes_RestorePriorOverride()
    {
        var accessor = CreateAccessor(@"F:\outer-default");
        using (accessor.PushWorkspace(@"F:\first"))
        {
            Assert.Equal(Path.GetFullPath(@"F:\first"), accessor.GetWorkspacePath());
            using (accessor.PushWorkspace(@"F:\second"))
            {
                Assert.Equal(Path.GetFullPath(@"F:\second"), accessor.GetWorkspacePath());
            }

            Assert.Equal(Path.GetFullPath(@"F:\first"), accessor.GetWorkspacePath());
        }

        Assert.Equal(Path.GetFullPath(@"F:\outer-default"), accessor.GetWorkspacePath());
    }

    /// <summary>Parallel async flows must not observe each other's workspace override.</summary>
    [Fact]
    public async Task PushWorkspace_ParallelCalls_DoNotLeakWorkspace()
    {
        var accessor = CreateAccessor(@"F:\default-ws");
        var observed = new string[2];
        var ct = TestContext.Current.CancellationToken;
        await Task.WhenAll(
            Task.Run(async () =>
            {
                using (accessor.PushWorkspace(@"F:\alpha"))
                {
                    await Task.Yield();
                    observed[0] = accessor.GetWorkspacePath();
                    await Task.Delay(25, ct);
                    Assert.Equal(Path.GetFullPath(@"F:\alpha"), accessor.GetWorkspacePath());
                }
            }, ct),
            Task.Run(async () =>
            {
                using (accessor.PushWorkspace(@"F:\beta"))
                {
                    await Task.Yield();
                    observed[1] = accessor.GetWorkspacePath();
                    await Task.Delay(25, ct);
                    Assert.Equal(Path.GetFullPath(@"F:\beta"), accessor.GetWorkspacePath());
                }
            }, ct));

        Assert.Equal(Path.GetFullPath(@"F:\alpha"), observed[0]);
        Assert.Equal(Path.GetFullPath(@"F:\beta"), observed[1]);
        Assert.Equal(Path.GetFullPath(@"F:\default-ws"), accessor.GetWorkspacePath());
    }

    private static WorkspaceServiceAccessor CreateAccessor(string defaultRoot)
    {
        var todo = Substitute.For<ITodoService>();
        var options = MsOptions.Create(new IngestionOptions { RepoRoot = defaultRoot });
        var resolver = new TodoServiceResolver(todo, options, Substitute.For<ITodoServiceFactory>());
        return new WorkspaceServiceAccessor(resolver, Substitute.For<IHttpContextAccessor>(), options);
    }
}
