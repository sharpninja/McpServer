using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-PATHNORM-001 / TR-MCP-PATHNORM-001 (plan P1.4c): the native boundary. Canonical Windows
/// paths use '/', and <c>ToNativePath</c> gives the backslash spelling only where a Windows host API
/// does not accept '/' (Win32 device or long-path forms, <c>cmd.exe</c> built-ins). It changes nothing
/// for POSIX-platform paths. The Windows-host and non-Windows-host cases run on every host against a
/// simulated host in <see cref="WorkspaceNativePathHostContractTests"/> (TR-MCP-USECASE-018 AC-006: no
/// unit test depends on the platform). BDP v4 (plan section 2a): MockData then Real.
/// </summary>
public abstract class WorkspaceNativePathContractTests
{
    /// <summary>The native-spelling function under test.</summary>
    protected abstract string ToNativePath(string path, WorkspacePathPlatform platform);

    /// <summary>POSIX-platform paths are never rewritten, on any host.</summary>
    [Theory]
    [InlineData("/home/x/repo")]
    [InlineData("//srv/share/x")]
    public void PosixPath_IsUnchanged(string path)
    {
        Assert.Equal(path, ToNativePath(path, WorkspacePathPlatform.CaseSensitive));
    }
}

/// <summary>BDP v4 step 2: the native-boundary contract against a literal data table.</summary>
public sealed class WorkspaceNativePathMockDataTests : WorkspaceNativePathContractTests
{
    private static readonly Dictionary<(string, WorkspacePathPlatform), string> AnyHost = new()
    {
        [("/home/x/repo", WorkspacePathPlatform.CaseSensitive)] = "/home/x/repo",
        [("//srv/share/x", WorkspacePathPlatform.CaseSensitive)] = "//srv/share/x",
    };

    /// <inheritdoc />
    protected override string ToNativePath(string path, WorkspacePathPlatform platform) =>
        AnyHost.TryGetValue((path, platform), out var native)
            ? native
            : throw new InvalidOperationException($"No scripted native spelling for '{path}' ({platform}).");
}
