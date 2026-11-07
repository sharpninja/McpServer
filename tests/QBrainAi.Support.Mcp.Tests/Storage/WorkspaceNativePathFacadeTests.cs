using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-PATHNORM-001 unitgreen-native-host, TR-MCP-PATHNORM-001: real Windows-host binding of the
/// two-argument facade. Integration because this overload chooses the actual process host; fixed rows preserve
/// the Windows and POSIX assertions already mock-gated by WorkspaceNativePathHostContractTests.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Windows")]
public sealed class WorkspaceNativePathWindowsFacadeTests
{
    /// <summary>Native Windows dispatch rewrites Windows paths only and preserves POSIX-platform paths.</summary>
    [Theory]
    [InlineData("C:/fixture/repo", WorkspacePathPlatform.Windows, @"C:\fixture\repo")]
    [InlineData("C:/", WorkspacePathPlatform.Windows, @"C:\")]
    [InlineData("//srv/share/x", WorkspacePathPlatform.Windows, @"\\srv\share\x")]
    [InlineData(@"C:\already\native", WorkspacePathPlatform.Windows, @"C:\already\native")]
    [InlineData("/fixture/repo", WorkspacePathPlatform.CaseSensitive, "/fixture/repo")]
    [InlineData("//srv/share/x", WorkspacePathPlatform.CaseSensitive, "//srv/share/x")]
    public void ToNativePath_TwoArguments_UsesWindowsHost(string path, WorkspacePathPlatform platform, string expected) =>
        Assert.Equal(expected, WorkspaceIdentityPath.ToNativePath(path, platform));
}

/// <summary>
/// TEST-MCP-PATHNORM-001 unitgreen-native-host, TR-MCP-PATHNORM-001: actual Linux dispatch preserves every
/// spelling. Integration because the two-argument facade chooses the actual process host, rather than a fake.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Linux")]
public sealed class WorkspaceNativePathLinuxFacadeTests
{
    /// <summary>Linux leaves both foreign Windows and native POSIX-platform inputs unchanged.</summary>
    [Theory]
    [InlineData("C:/fixture/repo", WorkspacePathPlatform.Windows)]
    [InlineData("C:/", WorkspacePathPlatform.Windows)]
    [InlineData("//srv/share/x", WorkspacePathPlatform.Windows)]
    [InlineData(@"C:\already\native", WorkspacePathPlatform.Windows)]
    [InlineData("/fixture/repo", WorkspacePathPlatform.CaseSensitive)]
    [InlineData("//srv/share/x", WorkspacePathPlatform.CaseSensitive)]
    public void ToNativePath_TwoArguments_UsesLinuxHost(string path, WorkspacePathPlatform platform) =>
        Assert.Equal(path, WorkspaceIdentityPath.ToNativePath(path, platform));
}
