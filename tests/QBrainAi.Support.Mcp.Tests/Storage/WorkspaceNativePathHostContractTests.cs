using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-PATHNORM-001 (criterion unitgreen-native-host) / TR-MCP-PATHNORM-001 (plan P1.4c): the
/// native boundary decides by the injected host, so both its Windows-host and its non-Windows-host
/// behavior are proven on every test host against a simulated host, without a host-conditional skip
/// (TR-MCP-USECASE-018 AC-006: no unit test depends on the platform). BDP v4: MockData then Real.
/// </summary>
public abstract class WorkspaceNativePathHostContractTests
{
    /// <summary>The host-aware native-spelling function under test.</summary>
    protected abstract string ToNativePath(string path, WorkspacePathPlatform platform, IWorkspaceHostEnvironment host);

    /// <summary>On a Windows host, Windows-platform paths switch to backslashes.</summary>
    [Theory]
    [InlineData("C:/Users/kingd/repo", @"C:\Users\kingd\repo")]
    [InlineData("C:/", @"C:\")]
    [InlineData("//srv/share/x", @"\\srv\share\x")]
    [InlineData(@"C:\already\native", @"C:\already\native")]
    public void WindowsHost_WindowsPath_UsesBackslashes(string path, string expected)
    {
        AssertNative(expected, ToNativePath(path, WorkspacePathPlatform.Windows, SimulatedWorkspaceHosts.Windows()));
    }

    /// <summary>On a non-Windows host nothing is rewritten, whatever the platform tag.</summary>
    [Theory]
    [InlineData("C:/Users/kingd/repo", WorkspacePathPlatform.Windows)]
    [InlineData("//srv/share/x", WorkspacePathPlatform.Windows)]
    [InlineData("/home/x/repo", WorkspacePathPlatform.CaseSensitive)]
    public void NonWindowsHost_IsUnchanged(string path, WorkspacePathPlatform platform)
    {
        AssertUnchanged(path, ToNativePath(path, platform, SimulatedWorkspaceHosts.Linux()));
    }

    /// <summary>Assertion shared by the non-Windows-host contract and its negative controls.</summary>
    /// <param name="path">Path given to the native-spelling function.</param>
    /// <param name="actual">Value the function returned.</param>
    protected static void AssertUnchanged(string path, string actual) => Assert.Equal(path, actual);

    /// <summary>Assertion shared by the Windows-host contract and its negative controls.</summary>
    /// <param name="expected">Backslash spelling the requirement defines.</param>
    /// <param name="actual">Value the function returned.</param>
    protected static void AssertNative(string expected, string actual) => Assert.Equal(expected, actual);
}

/// <summary>BDP v4 step 2: the host-aware native-boundary contract against a literal data table.</summary>
public sealed class WorkspaceNativePathHostMockDataTests : WorkspaceNativePathHostContractTests
{
    private static readonly Dictionary<(string, WorkspacePathPlatform), string> WindowsHost = new()
    {
        [("C:/Users/kingd/repo", WorkspacePathPlatform.Windows)] = @"C:\Users\kingd\repo",
        [("C:/", WorkspacePathPlatform.Windows)] = @"C:\",
        [("//srv/share/x", WorkspacePathPlatform.Windows)] = @"\\srv\share\x",
        [(@"C:\already\native", WorkspacePathPlatform.Windows)] = @"C:\already\native",
        [("/home/x/repo", WorkspacePathPlatform.CaseSensitive)] = "/home/x/repo",
    };

    /// <inheritdoc />
    protected override string ToNativePath(string path, WorkspacePathPlatform platform, IWorkspaceHostEnvironment host)
    {
        ArgumentNullException.ThrowIfNull(host);
        if (host.Platform != WorkspacePathPlatform.Windows)
            return path;

        return WindowsHost.TryGetValue((path, platform), out var native)
            ? native
            : throw new InvalidOperationException($"No scripted native spelling for '{path}' ({platform}).");
    }

    /// <summary>
    /// BDP v4 step 3 negative controls: a native-spelling function that rewrites on a non-Windows host,
    /// or that decides by a Windows host instead of the injected one, is rejected by the shared
    /// assertion inside this passing harness.
    /// </summary>
    [Fact]
    public void NonWindowsHostNegativeControls_AreRejectedInsidePassingHarness()
    {
        Assert.NotNull(Record.Exception(() => AssertUnchanged("C:/Users/kingd/repo", @"C:\Users\kingd\repo")));
        Assert.NotNull(Record.Exception(() => AssertUnchanged(
            "//srv/share/x",
            ToNativePath("//srv/share/x", WorkspacePathPlatform.Windows, SimulatedWorkspaceHosts.Windows()))));
        Assert.Null(Record.Exception(() => AssertUnchanged(
            "/home/x/repo",
            ToNativePath("/home/x/repo", WorkspacePathPlatform.CaseSensitive, SimulatedWorkspaceHosts.Windows()))));
    }

    /// <summary>
    /// TR-MCP-USECASE-018 (AC-006) negative controls: a native-spelling function that keeps forward slashes
    /// on a Windows host, or that decides by a non-Windows host instead of the injected Windows one, is
    /// rejected by the shared Windows-host assertion inside this passing harness.
    /// </summary>
    [Fact]
    public void WindowsHostNegativeControls_AreRejectedInsidePassingHarness()
    {
        Assert.NotNull(Record.Exception(() => AssertNative(@"C:\Users\kingd\repo", "C:/Users/kingd/repo")));
        Assert.NotNull(Record.Exception(() => AssertNative(
            @"\\srv\share\x",
            ToNativePath("//srv/share/x", WorkspacePathPlatform.Windows, SimulatedWorkspaceHosts.Linux()))));
        Assert.Null(Record.Exception(() => AssertNative(
            @"C:\",
            ToNativePath("C:/", WorkspacePathPlatform.Windows, SimulatedWorkspaceHosts.Windows()))));
    }
}
