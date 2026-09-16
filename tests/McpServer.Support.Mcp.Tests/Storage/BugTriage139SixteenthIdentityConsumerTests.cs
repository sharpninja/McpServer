using System.Reflection;
using McpServer.Cqrs;
using McpServer.Client;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Database;
using McpServer.Support.Mcp.UseCases;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.UseCases.Queries;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-USECASE-020-AC001: Consumer-facing coverage for cancellable use-case workspace identity.
/// </summary>
[Collection(nameof(WorkspaceIdentityNativeProcessStateCollection))]
public sealed class BugTriage139SixteenthIdentityConsumerTests
{
    /// <summary>
    /// Verifies that the nominally asynchronous use-case workspace resolver reaches the cancellable
    /// physical resolver before database work and propagates caller cancellation end to end.
    /// </summary>
    [Fact]
    public async Task ResolveWorkspaceAsync_CallerCancellationFlowsToPhysicalResolver()
    {
        var resolverField = typeof(WorkspaceIdentityPath).GetField(
            "s_physicalPathResolverOverride",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(resolverField);
        Assert.Null(resolverField!.GetValue(null));

        var entered = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Func<string, WorkspacePathPlatform, CancellationToken, Task<string?>> resolver =
            async (_, _, cancellationToken) =>
            {
                entered.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
                return null;
            };

        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        Task<ResolvedUseCaseWorkspace>? operation = null;
        try
        {
            resolverField.SetValue(null, resolver);
            await using var database = ProviderTestDatabase.CreateSqlite();
            await using var context = database.CreateContext();
            await McpDatabaseMigrationCoordinator.ApplyMigrationsAsync(
                    context,
                    database.ProviderOptions,
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true);

            operation = UseCaseCqrsHelpers.ResolveWorkspaceAsync(
                context,
                new WorkspaceContext { WorkspacePath = Path.GetTempPath() },
                workspacePath: null,
                cancellation.Token);

            await entered.Task.WaitAsync(
                    TimeSpan.FromSeconds(1),
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            cancellation.Cancel();

            var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => operation.WaitAsync(
                        TimeSpan.FromSeconds(2),
                        TestContext.Current.CancellationToken))
                .ConfigureAwait(true);
            Assert.Equal(cancellation.Token, exception.CancellationToken);
        }
        finally
        {
            cancellation.Cancel();
            resolverField.SetValue(null, null);
            if (operation is not null)
            {
                try
                {
                    _ = await operation.WaitAsync(
                            TimeSpan.FromSeconds(2),
                            TestContext.Current.CancellationToken)
                        .ConfigureAwait(true);
                }
                catch (Exception)
                {
                    // Cleanup observes the operation only after the behavioral assertions above.
                }
            }
        }
    }


    /// <summary>
    /// Verifies the runtime type no longer carries the legacy synchronous traversal implementations
    /// that could bypass the bounded resolver contract.
    /// </summary>
    [Fact]
    public void WorkspaceIdentityPath_LegacyUnboundedTraversalImplementations_AreAbsent()
    {
        var privateStaticMethods = typeof(WorkspaceIdentityPath)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Select(method => method.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("TryNormalizeSecurityPath", privateStaticMethods);
        Assert.DoesNotContain("TryResolvePhysicalPathWithNonexistentSuffixStrict", privateStaticMethods);
        Assert.DoesNotContain("TryResolvePhysicalPathWithNonexistentSuffix", privateStaticMethods);
        Assert.DoesNotContain("TryResolveManagedPhysicalPath", privateStaticMethods);
    }

    /// <summary>
    /// Verifies the public synchronous identity surface is a pure lexical operation and never
    /// enters the physical resolver, including comparer and containment entry points.
    /// </summary>
    [Fact]
    public void SynchronousIdentitySurface_DoesNotInvokePhysicalResolver()
    {
        var resolverField = typeof(WorkspaceIdentityPath).GetField(
            "s_physicalPathResolverOverride",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(resolverField);
        Assert.Null(resolverField!.GetValue(null));

        var invocations = 0;
        Func<string, WorkspacePathPlatform, CancellationToken, Task<string?>> resolver =
            (_, _, _) =>
            {
                Interlocked.Increment(ref invocations);
                throw new InvalidOperationException(
                    "The synchronous lexical identity surface entered physical resolution.");
            };

        try
        {
            resolverField.SetValue(null, resolver);
            var root = Path.Combine(Path.GetTempPath(), "bug139-sync-identity");
            var candidate = Path.Combine(root, "child");
            var platform = WorkspaceIdentityPath.DetectPlatform(candidate);
            var expected = WorkspaceIdentityPath.NormalizeLexicalPath(
                Path.GetFullPath(candidate),
                platform);

            Assert.Equal(expected, WorkspaceIdentityPath.NormalizePath(candidate, platform));
            _ = WorkspaceIdentityPath.GetStorageKey(candidate, platform);
            Assert.True(WorkspaceIdentityPath.Comparer.Equals(candidate, candidate));
            _ = WorkspaceIdentityPath.Comparer.GetHashCode(candidate);
            Assert.True(WorkspaceIdentityPath.AreEquivalent(candidate, candidate));
            Assert.True(WorkspaceIdentityPath.IsWithinRoot(root, candidate));
            Assert.Equal(0, Volatile.Read(ref invocations));
        }
        finally
        {
            resolverField.SetValue(null, null);
        }
    }

    /// <summary>
    /// Verifies a real local symbolic-link alias remains lexical on the synchronous surface instead
    /// of triggering uncancellable physical traversal.
    /// </summary>
    [Fact]
    public void SynchronousIdentitySurface_LocalSymbolicLink_RemainsLexical()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"bug139-sync-link-{Guid.NewGuid():N}");
        var target = Path.Combine(root, "target");
        var alias = Path.Combine(root, "alias");
        Directory.CreateDirectory(target);
        try
        {
            Directory.CreateSymbolicLink(alias, target);
            var platform = WorkspaceIdentityPath.DetectPlatform(alias);
            var expectedAlias = WorkspaceIdentityPath.NormalizeLexicalPath(
                Path.GetFullPath(alias),
                platform);
            var expectedTarget = WorkspaceIdentityPath.NormalizeLexicalPath(
                Path.GetFullPath(target),
                platform);

            var actual = WorkspaceIdentityPath.NormalizePath(alias, platform);

            Assert.Equal(expectedAlias, actual);
            Assert.NotEqual(expectedTarget, actual);
        }
        finally
        {
            if (Directory.Exists(alias))
                Directory.Delete(alias);
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// Verifies a real nominally asynchronous use-case query does not enter the synchronous
    /// compatibility resolver and propagates its call-context cancellation to physical identity.
    /// </summary>
    [Fact]
    public async Task ListUseCasesQuery_CallerCancellationFlowsToPhysicalResolver()
    {
        var resolverField = typeof(WorkspaceIdentityPath).GetField(
            "s_physicalPathResolverOverride",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(resolverField);
        Assert.Null(resolverField!.GetValue(null));

        var entered = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Func<string, WorkspacePathPlatform, CancellationToken, Task<string?>> resolver =
            async (_, _, cancellationToken) =>
            {
                entered.TrySetResult();
                var cancellationWait = Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                await Task.WhenAny(cancellationWait, release.Task).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return null;
            };

        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        Task<Result<IReadOnlyList<UseCaseSummaryDto>>>? operation = null;
        try
        {
            await using var database = ProviderTestDatabase.CreateSqlite();
            await using var context = database.CreateContext();
            await McpDatabaseMigrationCoordinator.ApplyMigrationsAsync(
                    context,
                    database.ProviderOptions,
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            resolverField.SetValue(null, resolver);

            var handler = new ListUseCasesQueryHandler(
                context,
                new WorkspaceContext { WorkspacePath = Path.GetTempPath() });
            operation = Task.Run(
                () => handler.HandleAsync(
                    new ListUseCasesQuery(Path.GetTempPath()),
                    new CallContext { CancellationToken = cancellation.Token }),
                TestContext.Current.CancellationToken);
            await entered.Task.WaitAsync(
                    TimeSpan.FromSeconds(1),
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            cancellation.Cancel();

            var winner = await Task.WhenAny(
                    operation,
                    Task.Delay(
                        TimeSpan.FromSeconds(1),
                        TestContext.Current.CancellationToken))
                .ConfigureAwait(true);
            Assert.Same(operation, winner);
            var result = await operation.ConfigureAwait(true);
            Assert.True(result.IsFailure, result.Error);
            var exception = Assert.IsAssignableFrom<OperationCanceledException>(result.Exception);
            Assert.Equal(cancellation.Token, exception.CancellationToken);
        }
        finally
        {
            cancellation.Cancel();
            release.TrySetResult();
            resolverField.SetValue(null, null);
            if (operation is not null)
            {
                try
                {
                    _ = await operation.WaitAsync(
                            TimeSpan.FromSeconds(2),
                            TestContext.Current.CancellationToken)
                        .ConfigureAwait(true);
                }
                catch (Exception)
                {
                    // Cleanup releases the legacy synchronous resolver only after the bounded assertion.
                }
            }
        }
    }

    /// <summary>
    /// Verifies persisted opaque workspace identifiers retain a deterministic lexical key and never
    /// enter the physical filesystem resolver.
    /// </summary>
    [Fact]
    public async Task PersistedOpaqueWorkspaceId_UsesLexicalIdentityWithoutPhysicalResolver()
    {
        const string workspaceId = "ws:test";
        var resolverField = typeof(WorkspaceIdentityPath).GetField(
            "s_physicalPathResolverOverride",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(resolverField);
        Assert.Null(resolverField!.GetValue(null));

        var invocations = 0;
        Func<string, WorkspacePathPlatform, CancellationToken, Task<string?>> resolver =
            (_, _, _) =>
            {
                Interlocked.Increment(ref invocations);
                return Task.FromResult<string?>(null);
            };

        string storageKey;
        try
        {
            resolverField.SetValue(null, resolver);
            storageKey = await WorkspaceIdentity.GetPersistedStorageKeyAsync(
                    workspaceId,
                    workspacePath: null,
                    TimeSpan.FromSeconds(1),
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
        }
        finally
        {
            resolverField.SetValue(null, null);
        }

        var expectedKey = OperatingSystem.IsWindows()
            ? "windows:WS:TEST"
            : "case-sensitive:ws:test";
        Assert.Equal(expectedKey, storageKey);
        Assert.Equal(0, Volatile.Read(ref invocations));
    }

    /// <summary>
    /// Verifies a logical identifier containing Windows separators cannot reach the native-open
    /// boundary while a genuinely rooted path still uses the asynchronous physical resolver.
    /// </summary>
    [Fact]
    public async Task OpaqueWorkspaceIdentity_DoesNotEnterNativeBoundary()
    {
        const string logicalIdentity = @"ws:\test";
        var resolverField = typeof(WorkspaceIdentityPath).GetField(
            "s_physicalPathResolverOverride",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(resolverField);
        Assert.Null(resolverField!.GetValue(null));

        var invocations = 0;
        Func<string, WorkspacePathPlatform, CancellationToken, Task<string?>> resolver =
            (lexicalPath, _, _) =>
            {
                Interlocked.Increment(ref invocations);
                return Task.FromResult<string?>(lexicalPath);
            };

        (string NormalizedPath, string StorageKey) resolution;
        try
        {
            resolverField.SetValue(null, resolver);
            resolution = await WorkspaceIdentity.ResolveAsync(
                    logicalIdentity,
                    TimeSpan.FromSeconds(1),
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
        }
        finally
        {
            resolverField.SetValue(null, null);
        }

        var expectedPath = OperatingSystem.IsWindows()
            ? @"ws:\test"
            : logicalIdentity;
        var expectedKey = OperatingSystem.IsWindows()
            ? @"windows:WS:\TEST"
            : $"case-sensitive:{logicalIdentity}";
        Assert.Equal(expectedPath, resolution.NormalizedPath);
        Assert.Equal(expectedKey, resolution.StorageKey);
        Assert.Equal(0, Volatile.Read(ref invocations));
    }
}
