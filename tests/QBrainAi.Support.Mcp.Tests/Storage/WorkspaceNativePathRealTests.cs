using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>BDP v4 step 3: the native-boundary contract against <see cref="WorkspaceIdentityPath"/>.</summary>
public sealed class WorkspaceNativePathRealTests : WorkspaceNativePathContractTests
{
    /// <inheritdoc />
    protected override string ToNativePath(string path, WorkspacePathPlatform platform, bool isWindowsHost) =>
        isWindowsHost == OperatingSystem.IsWindows()
            ? WorkspaceIdentityPath.ToNativePath(path, platform)
            : WorkspaceIdentityPath.ToNativePath(path, platform, isWindowsHost);
}
