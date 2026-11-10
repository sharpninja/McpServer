using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Storage;

/// <summary>
/// TEST-MCP-PATHNORM-001 unitgreen-native-host, TR-MCP-PATHNORM-001: real Windows-host binding of the
/// two-argument facade. Integration because this overload chooses the actual process host; fixed rows preserve
/// the Windows and POSIX assertions already mock-gated by WorkspaceNativePathHostContractTests.
/// </summary>
/// <remarks>
/// <para>2026-11-07, TEST-MCP-PATHNORM-001 unitgreen-native-host; TR-MCP-PATHNORM-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole and unchanged from QBrainAi.Support.Mcp.Tests (Storage/WorkspaceNativePathFacadeTests.cs, with its Linux twin) into the integration-owned QBrainAi.Support.Mcp.Environment.IntegrationTests, so the unit-owned project holds only unit cases (slice S5, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); tagged Platform=Windows, it runs in the lane of a Windows host; only the namespace changed.</para>
/// <para>2026-11-07, TEST-MCP-PATHNORM-001 unitgreen-native-host; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: QBrainAi.Support.Mcp.Environment.IntegrationTests, the project the entry above names, is now QBrainAi.Support.Mcp.EnvironmentIntegrationTests, renamed to its root namespace (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name."); the class and its cases are unchanged.</para>
/// </remarks>
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
/// <remarks>
/// <para>2026-11-07, TEST-MCP-PATHNORM-001 unitgreen-native-host; TR-MCP-PATHNORM-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole and unchanged from QBrainAi.Support.Mcp.Tests (Storage/WorkspaceNativePathFacadeTests.cs, with its Windows twin) into the integration-owned QBrainAi.Support.Mcp.Environment.IntegrationTests, so the unit-owned project holds only unit cases (slice S5, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); tagged Platform=Linux, it runs in the lane of a Linux host; only the namespace changed.</para>
/// <para>2026-11-07, TEST-MCP-PATHNORM-001 unitgreen-native-host; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: QBrainAi.Support.Mcp.Environment.IntegrationTests, the project the entry above names, is now QBrainAi.Support.Mcp.EnvironmentIntegrationTests, renamed to its root namespace (operator 2026-11-07: "Project names and root namespaces for the project should match, so fix the project name."); the class and its cases are unchanged.</para>
/// </remarks>
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
