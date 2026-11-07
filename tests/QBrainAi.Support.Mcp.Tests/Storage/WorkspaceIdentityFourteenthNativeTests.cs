using System.Diagnostics;
using System.Reflection;
using QBrainAi.Client;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Database;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using static QBrainAi.Support.Mcp.Tests.Storage.WorkspaceIdentityFourteenthNativeTestsSupport;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-USECASE-018: Fourteenth-review consumer and native coverage for bounded physical identity.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-USECASE-018; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 4 Category=Integration cases (AsyncConsumers_AwaitBoundedPhysicalIdentityResolution, SaveChangesAsync_PreCancelled_DoesNotStartNativeResolution, NativeResolver_RepeatedPlatformCallsLeaveNoActiveWorkers and SaveChangesAsync_GlobalEntityWithEmptyWorkspaceId_PreservesLegacyGlobalScope) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the helpers both halves use (ResolveCounter and ReadCounter) moved once into Storage/WorkspaceIdentityFourteenthNativeTestsSupport.cs beside this file, which the integration project links (DRY); the unit cases and the class attributes are unchanged.</remarks>
[Collection(nameof(WorkspaceIdentityNativeProcessStateCollection))]
public sealed class WorkspaceIdentityFourteenthNativeTests
{
    /// <summary>
    /// TEST-MCP-USECASE-018-AC003: The bounded resolver must preserve the historical empty identity
    /// used by global rows without dispatching native physical work.
    /// </summary>
    /// <remarks>Unit: fixed empty input returns before platform detection or native resolution; both empty outputs and the unchanged invocation counter remain asserted (TR-MCP-USECASE-018 AC-006).</remarks>
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

}
