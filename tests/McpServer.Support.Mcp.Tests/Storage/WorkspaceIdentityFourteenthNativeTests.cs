using System.Diagnostics;
using System.Reflection;
using McpServer.Client;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Database;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-USECASE-018: Fourteenth-review consumer and native coverage for bounded physical identity.
/// </summary>
[Collection(nameof(WorkspaceIdentityNativeProcessStateCollection))]
public sealed class WorkspaceIdentityFourteenthNativeTests
{
    /// <summary>
    /// TEST-MCP-USECASE-018-AC003: SaveChangesAsync and federation upsert must await cancellable,
    /// finite physical identity APIs instead of invoking synchronous resolution.
    /// </summary>
    [Fact]
    public void AsyncConsumers_AwaitBoundedPhysicalIdentityResolution()
    {
        var root = McpServer.Support.Mcp.Tests.Infrastructure.RepositoryEvidenceTestSupport
            .ResolveRepositoryRoot();
        var contextSource = File.ReadAllText(
            Path.Combine(root, "src", "McpServer.Storage", "McpDbContext.cs"));
        var federationSource = File.ReadAllText(
            Path.Combine(
                root,
                "src",
                "McpServer.Services",
                "Services",
                "FederationTopologyService.cs"));

        Assert.Contains(
            "await PrepareDbFkChangesAsync(cancellationToken)",
            contextSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "WorkspaceIdentity.ResolveAsync(",
            federationSource,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "WorkspaceIdentity.NormalizePath(request.WorkspacePath)",
            federationSource,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "WorkspaceIdentity.GetWorkspaceHash(canonicalPath)",
            federationSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-USECASE-018-AC003: A token cancelled before SaveChangesAsync starts must prevent
    /// even the native physical resolver from starting.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_PreCancelled_DoesNotStartNativeResolution()
    {
        var invocationField = ResolveCounter("s_nativeResolverInvocationCount");
        var before = ReadCounter(invocationField);
        await using var database = ProviderTestDatabase.CreateSqlite();
        await using var context = database.CreateContext();
        var now = DateTimeOffset.UtcNow;
        context.Workspaces.Add(
            new WorkspaceEntity
            {
                WorkspaceId = Path.Combine(Path.GetTempPath(), "fourteenth-pre-cancel"),
                WorkspacePath = Path.Combine(Path.GetTempPath(), "fourteenth-pre-cancel"),
                Name = "fourteenth pre-cancel",
                TodoPath = "docs/todo.yaml",
                CurrentRequirementLayerKey = "layer-1",
                IsEnabled = true,
                DateTimeCreated = now,
                DateTimeModified = now,
            });

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => context.SaveChangesAsync(cancellation.Token))
            .ConfigureAwait(true);
        Assert.Equal(before, ReadCounter(invocationField));
    }

    /// <summary>
    /// TEST-MCP-USECASE-018-AC003: The real platform resolver must execute for local aliases and
    /// unavailable Windows UNC syntax, bound repeated calls, and leave no native worker behind.
    /// </summary>
    [Fact]
    public async Task NativeResolver_RepeatedPlatformCallsLeaveNoActiveWorkers()
    {
        var stopwatch = Stopwatch.StartNew();
        var activeField = ResolveCounter("s_activeNativeResolverWorkers");
        var invocationField = ResolveCounter("s_nativeResolverInvocationCount");
        var invocationsBefore = ReadCounter(invocationField);

        if (OperatingSystem.IsWindows())
        {
            _ = await WorkspaceIdentityPath.GetStorageKeyAsync(
                    Path.GetTempPath(),
                    WorkspacePathPlatform.Windows,
                    TimeSpan.FromSeconds(2),
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            var unavailable =
                @"\\192.0.2.1\mcp-fourteenth-" + Guid.NewGuid().ToString("N") + @"\child";
            var invocationsBeforeRemoteRejection = ReadCounter(invocationField);
            for (var attempt = 0; attempt < 8; attempt++)
            {
                await Assert.ThrowsAsync<NotSupportedException>(
                        () => WorkspaceIdentityPath.GetStorageKeyAsync(
                            unavailable,
                            WorkspacePathPlatform.Windows,
                            TimeSpan.FromMilliseconds(100),
                            TestContext.Current.CancellationToken))
                    .ConfigureAwait(true);
            }

            Assert.Equal(
                invocationsBeforeRemoteRejection,
                ReadCounter(invocationField));
        }
        else if (OperatingSystem.IsLinux())
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "mcp-fourteenth-native-" + Guid.NewGuid().ToString("N"));
            var target = Path.Combine(root, "target");
            var alias = Path.Combine(root, "alias");
            Directory.CreateDirectory(target);
            Directory.CreateSymbolicLink(alias, target);
            try
            {
                var targetKey = await WorkspaceIdentityPath.GetStorageKeyAsync(
                        target,
                        WorkspacePathPlatform.CaseSensitive,
                        TimeSpan.FromSeconds(2),
                        TestContext.Current.CancellationToken)
                    .ConfigureAwait(true);
                for (var attempt = 0; attempt < 9; attempt++)
                {
                    Assert.Equal(
                        targetKey,
                        await WorkspaceIdentityPath.GetStorageKeyAsync(
                                alias,
                                WorkspacePathPlatform.CaseSensitive,
                                TimeSpan.FromSeconds(2),
                                TestContext.Current.CancellationToken)
                            .ConfigureAwait(true));
                }
            }
            finally
            {
                Directory.Delete(alias);
                Directory.Delete(root, recursive: true);
            }
        }
        else
        {
            for (var attempt = 0; attempt < 9; attempt++)
            {
                _ = await WorkspaceIdentityPath.GetStorageKeyAsync(
                        Path.GetTempPath(),
                        WorkspacePathPlatform.CaseSensitive,
                        TimeSpan.FromSeconds(2),
                        TestContext.Current.CancellationToken)
                    .ConfigureAwait(true);
            }
        }

        Assert.True(
            SpinWait.SpinUntil(
                () => ReadCounter(activeField) == 0,
                TimeSpan.FromSeconds(2)),
            "Every native resolver worker must remain observable until it actually exits.");
        Assert.Equal(0, ReadCounter(activeField));
        var minimumInvocationIncrease = OperatingSystem.IsWindows() ? 1 : 9;
        Assert.True(
            ReadCounter(invocationField) >= invocationsBefore + minimumInvocationIncrease,
            "Every supported local platform call must enter the bounded native resolver.");
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(5),
            $"Repeated native resolution took {stopwatch.Elapsed}.");

        var source = File.ReadAllText(
            Path.Combine(
                McpServer.Support.Mcp.Tests.Infrastructure.RepositoryEvidenceTestSupport
                    .ResolveRepositoryRoot(),
                "src",
                "McpServer.Client",
                "WorkspaceIdentityPath.cs"));
        Assert.DoesNotContain("return Task.Run(", source, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "while (!Directory.Exists(existingAncestor))",
            source,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-USECASE-018-AC003: The bounded resolver must preserve the historical empty identity
    /// used by global rows without dispatching native physical work.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_EmptyWorkspaceId_PreservesGlobalIdentityWithoutNativeResolution()
    {
        var invocationField = ResolveCounter("s_nativeResolverInvocationCount");
        var before = ReadCounter(invocationField);

        var resolution = await WorkspaceIdentity.ResolveAsync(
                string.Empty,
                TimeSpan.FromMilliseconds(100),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal(string.Empty, resolution.NormalizedPath);
        Assert.Equal(string.Empty, resolution.StorageKey);
        Assert.Equal(before, ReadCounter(invocationField));
    }

    /// <summary>
    /// TEST-MCP-USECASE-018-AC003: A real SQLite save for a global entity must retain the empty
    /// workspace scope and must not start physical identity resolution.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_GlobalEntityWithEmptyWorkspaceId_PreservesLegacyGlobalScope()
    {
        var invocationField = ResolveCounter("s_nativeResolverInvocationCount");
        var before = ReadCounter(invocationField);
        await using var database = ProviderTestDatabase.CreateSqlite();
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = database.CreateContext();
        await McpDatabaseMigrationCoordinator.ApplyMigrationsAsync(
                context,
                database.ProviderOptions,
                cancellationToken)
            .ConfigureAwait(true);
        context.ToolBuckets.Add(
            new ToolBucketEntity
            {
                WorkspaceId = string.Empty,
                Name = "fourteenth-global-" + Guid.NewGuid().ToString("N"),
                Owner = "mcpserver",
                Repo = "global-tools",
                DateTimeCreated = DateTimeOffset.UtcNow,
            });

        await context.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        context.ChangeTracker.Clear();

        var persisted = await context.ToolBuckets
            .IgnoreQueryFilters()
            .SingleAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(string.Empty, persisted.WorkspaceId);
        Assert.Equal(before, ReadCounter(invocationField));
    }

    private static FieldInfo ResolveCounter(string name)
    {
        var field = typeof(WorkspaceIdentityPath).GetField(
            name,
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        return field!;
    }

    private static int ReadCounter(FieldInfo field) =>
        Assert.IsType<int>(field.GetValue(null));

}
