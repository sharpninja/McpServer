using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// BDP v4 step 5: the host-aware native-boundary contract (TEST-MCP-PATHNORM-001 criterion
/// unitgreen-native-host) against the production <see cref="WorkspaceIdentityPath"/> overload.
/// </summary>
public sealed class WorkspaceNativePathHostRealTests : WorkspaceNativePathHostContractTests
{
    /// <inheritdoc />
    protected override string ToNativePath(string path, WorkspacePathPlatform platform, IWorkspaceHostEnvironment host) =>
        WorkspaceIdentityPath.ToNativePath(path, platform, host);
}
