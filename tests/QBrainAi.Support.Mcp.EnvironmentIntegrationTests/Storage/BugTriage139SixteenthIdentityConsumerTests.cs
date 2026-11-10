using System.Reflection;
using QBrainAi.Cqrs;
using QBrainAi.Client;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Database;
using QBrainAi.Support.Mcp.UseCases;
using QBrainAi.Support.Mcp.UseCases.Models;
using QBrainAi.Support.Mcp.UseCases.Queries;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Storage;

/// <summary>
/// TEST-MCP-USECASE-020-AC001: Consumer-facing coverage for cancellable use-case workspace identity.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-USECASE-020-AC001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Storage/BugTriage139SixteenthIdentityConsumerTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case and the helpers only it uses (AssertPersistedOpaqueWorkspaceIdAsync and AssertOpaqueIdentityOutsideNativeBoundaryAsync) moved unchanged with the class attributes; only the namespace changed.</remarks>
[Collection(nameof(WorkspaceIdentityNativeProcessStateCollection))]
public sealed class BugTriage139SixteenthIdentityConsumerTests
{
    /// <summary>
    /// Verifies a real local symbolic-link alias remains lexical on the synchronous surface instead
    /// of triggering uncancellable physical traversal.
    /// </summary>
    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
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
    /// Verifies persisted opaque workspace identifiers retain a deterministic lexical key and never
    /// enter the physical filesystem resolver. The key carries the running host's platform, so the cases
    /// live in the Windows and Linux integration classes below (operator 2026-10-19: "Integration +
    /// Platform (Recommended)"; TR-MCP-USECASE-019 AC-008).
    /// </summary>
    /// <param name="expectedKey">Storage key the requirement defines for the running host.</param>
    internal static async Task AssertPersistedOpaqueWorkspaceIdAsync(string expectedKey)
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

        Assert.Equal(expectedKey, storageKey);
        Assert.Equal(0, Volatile.Read(ref invocations));
    }

    /// <summary>
    /// Verifies a logical identifier containing Windows separators cannot reach the native-open
    /// boundary while a genuinely rooted path still uses the asynchronous physical resolver. The result
    /// carries the running host's platform, so the cases live in the Windows and Linux integration
    /// classes below (TR-MCP-USECASE-019 AC-008).
    /// </summary>
    /// <param name="expectedPath">Normalized path the requirement defines for the running host.</param>
    /// <param name="expectedKey">Storage key the requirement defines for the running host.</param>
    internal static async Task AssertOpaqueIdentityOutsideNativeBoundaryAsync(string expectedPath, string expectedKey)
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

        Assert.Equal(expectedPath, resolution.NormalizedPath);
        Assert.Equal(expectedKey, resolution.StorageKey);
        Assert.Equal(0, Volatile.Read(ref invocations));
    }
}

/// <summary>
/// TEST-MCP-USECASE-020-AC001: opaque workspace identity on a Windows host keeps the Windows-platform
/// key (TR-MCP-USECASE-019 AC-008, TR-MCP-NUKE-019).
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-USECASE-020-AC001; TR-MCP-USECASE-019 AC-008; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole with its 2 Windows-only Category=Integration cases from QBrainAi.Support.Mcp.Tests (Storage/BugTriage139SixteenthIdentityConsumerTests.cs), whose unit classes stay there, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); only the namespace changed.</remarks>
[Collection(nameof(WorkspaceIdentityNativeProcessStateCollection))]
[Trait("Category", "Integration")]
[Trait("Platform", "Windows")]
public sealed class BugTriage139SixteenthIdentityConsumerWindowsTests
{
    /// <summary>A persisted opaque identifier keeps its lexical Windows-platform key without the physical resolver.</summary>
    [Fact]
    public Task PersistedOpaqueWorkspaceId_UsesLexicalIdentityWithoutPhysicalResolver() =>
        BugTriage139SixteenthIdentityConsumerTests.AssertPersistedOpaqueWorkspaceIdAsync("windows:WS:TEST");

    /// <summary>A logical identifier with Windows separators stays outside the native-open boundary.</summary>
    [Fact]
    public Task OpaqueWorkspaceIdentity_DoesNotEnterNativeBoundary() =>
        BugTriage139SixteenthIdentityConsumerTests.AssertOpaqueIdentityOutsideNativeBoundaryAsync(@"ws:\test", @"windows:WS:\TEST");
}

/// <summary>
/// TEST-MCP-USECASE-020-AC001: opaque workspace identity on a Linux host keeps the case-sensitive key
/// (TR-MCP-USECASE-019 AC-008, TR-MCP-NUKE-019).
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-USECASE-020-AC001; TR-MCP-USECASE-019 AC-008; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole with its 2 Linux-only Category=Integration cases from QBrainAi.Support.Mcp.Tests (Storage/BugTriage139SixteenthIdentityConsumerTests.cs), whose unit classes stay there, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); only the namespace changed.</remarks>
[Collection(nameof(WorkspaceIdentityNativeProcessStateCollection))]
[Trait("Category", "Integration")]
[Trait("Platform", "Linux")]
public sealed class BugTriage139SixteenthIdentityConsumerLinuxTests
{
    /// <summary>A persisted opaque identifier keeps its lexical case-sensitive key without the physical resolver.</summary>
    [Fact]
    public Task PersistedOpaqueWorkspaceId_UsesLexicalIdentityWithoutPhysicalResolver() =>
        BugTriage139SixteenthIdentityConsumerTests.AssertPersistedOpaqueWorkspaceIdAsync("case-sensitive:ws:test");

    /// <summary>A logical identifier with Windows separators stays outside the native-open boundary.</summary>
    [Fact]
    public Task OpaqueWorkspaceIdentity_DoesNotEnterNativeBoundary() =>
        BugTriage139SixteenthIdentityConsumerTests.AssertOpaqueIdentityOutsideNativeBoundaryAsync(@"ws:\test", @"case-sensitive:ws:\test");
}
