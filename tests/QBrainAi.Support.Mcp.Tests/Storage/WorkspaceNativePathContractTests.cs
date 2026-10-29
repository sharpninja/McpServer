using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-PATHNORM-001 / TR-MCP-PATHNORM-001 (plan P1.4c): the native boundary. Canonical Windows
/// paths use '/', and <c>ToNativePath</c> gives the backslash spelling only where a Windows host API
/// does not accept '/' (Win32 device or long-path forms, <c>cmd.exe</c> built-ins). It changes nothing
/// for POSIX-platform paths or on a non-Windows host. BDP v4 (plan section 2a): MockData then Real.
/// </summary>
public abstract class WorkspaceNativePathContractTests
{
    /// <summary>The native-spelling function under test.</summary>
    protected abstract string ToNativePath(string path, WorkspacePathPlatform platform);

    /// <summary>On a Windows host, Windows-platform paths switch to backslashes.</summary>
    [Theory]
    [InlineData("C:/Users/kingd/repo", @"C:\Users\kingd\repo")]
    [InlineData("C:/", @"C:\")]
    [InlineData("//srv/share/x", @"\\srv\share\x")]
    [InlineData(@"C:\already\native", @"C:\already\native")]
    public void WindowsPath_OnWindowsHost_UsesBackslashes(string path, string expected)
    {
        // Run every literal on either host: the non-Windows result must preserve the supplied spelling.
        Assert.Equal(OperatingSystem.IsWindows() ? expected : path, ToNativePath(path, WorkspacePathPlatform.Windows));
    }

    /// <summary>POSIX-platform paths are never rewritten.</summary>
    [Theory]
    [InlineData("/home/x/repo")]
    [InlineData("//srv/share/x")]
    public void PosixPath_IsUnchanged(string path)
    {
        Assert.Equal(path, ToNativePath(path, WorkspacePathPlatform.CaseSensitive));
    }

    /// <summary>TEST-MCP-PATHNORM-001: the active native host selects its documented spelling; no platform case is skipped.</summary>
    [Fact]
    public void NativeHost_SelectsDocumentedSpelling()
    {
        var expected = OperatingSystem.IsWindows() ? @"C:\Users\kingd\repo" : "C:/Users/kingd/repo";
        Assert.Equal(expected, ToNativePath("C:/Users/kingd/repo", WorkspacePathPlatform.Windows));
    }
}

/// <summary>BDP v4 step 2: the native-boundary contract against a literal data table.</summary>
public sealed class WorkspaceNativePathMockDataTests : WorkspaceNativePathContractTests
{
    private static readonly Dictionary<(string, WorkspacePathPlatform), string> WindowsHost = new()
    {
        [("C:/Users/kingd/repo", WorkspacePathPlatform.Windows)] = @"C:\Users\kingd\repo",
        [("C:/", WorkspacePathPlatform.Windows)] = @"C:\",
        [("//srv/share/x", WorkspacePathPlatform.Windows)] = @"\\srv\share\x",
        [(@"C:\already\native", WorkspacePathPlatform.Windows)] = @"C:\already\native",
        [("/home/x/repo", WorkspacePathPlatform.CaseSensitive)] = "/home/x/repo",
        [("//srv/share/x", WorkspacePathPlatform.CaseSensitive)] = "//srv/share/x",
    };

    /// <inheritdoc />
    protected override string ToNativePath(string path, WorkspacePathPlatform platform)
    {
        if (!OperatingSystem.IsWindows())
            return path;

        return WindowsHost.TryGetValue((path, platform), out var native)
            ? native
            : throw new InvalidOperationException($"No scripted native spelling for '{path}' ({platform}).");
    }
}
