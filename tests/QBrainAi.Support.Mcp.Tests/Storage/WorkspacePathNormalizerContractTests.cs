using NSubstitute;
using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-FED-PATH-001: contract tests for <see cref="IWorkspacePathNormalizer"/>. Request-supplied
/// workspace paths are interpreted by their own syntax, never by the host OS. The host is an
/// NSubstitute double so a Windows test run can simulate a Linux hub (working directory
/// /opt/mcpserver/app) and vice versa.
/// BDP v4 (plan section 2a): <see cref="WorkspacePathNormalizerMockDataTests"/> proves these tests
/// green against scripted data; <see cref="WorkspacePathNormalizerRealTests"/> runs the same tests
/// against the production implementation.
/// </summary>
public abstract class WorkspacePathNormalizerContractTests
{
    /// <summary>Working directory of the simulated Linux hub.</summary>
    protected const string LinuxWorkingDirectory = SimulatedWorkspaceHosts.LinuxWorkingDirectory;

    /// <summary>Creates the normalizer under test for the given host.</summary>
    protected abstract IWorkspacePathNormalizer CreateSut(IWorkspaceHostEnvironment host);

    /// <summary>
    /// FR-MCP-FED-PATH-001-AC1: A Windows drive path received by a Linux host is never prefixed
    /// with the host working directory and is never handed to host-native resolution.
    /// </summary>
    [Fact]
    public void LinuxHost_WindowsDrivePath_IsNotPrefixedWithWorkingDirectory()
    {
        var host = LinuxHost();
        var normalizer = CreateSut(host);

        var result = normalizer.Normalize(@"C:\Users\kingd");

        Assert.DoesNotContain(LinuxWorkingDirectory, result, StringComparison.Ordinal);
        Assert.Equal(@"C:\Users\kingd", result);
        Assert.Equal(WorkspacePathPlatform.Windows, normalizer.DetectPlatform(@"C:\Users\kingd"));
        host.DidNotReceive().NormalizeNativePath(Arg.Any<string>());
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001-AC1: Backslash and forward-slash spellings of one Windows drive path
    /// resolve to the same identity on a Linux host.
    /// </summary>
    [Theory]
    [InlineData(@"C:\Users\kingd")]
    [InlineData("C:/Users/kingd")]
    [InlineData(@"C:\Users/kingd\")]
    public void LinuxHost_WindowsDrivePathSpellings_ResolveToOneIdentity(string input)
    {
        Assert.Equal(@"C:\Users\kingd", CreateSut(LinuxHost()).Normalize(input));
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001-AC1: Both UNC spellings are Windows paths on a Linux host and resolve
    /// to the same identity without host-native resolution.
    /// </summary>
    [Fact]
    public void LinuxHost_UncSpellings_AreWindowsAndEquivalent()
    {
        var host = LinuxHost();
        var normalizer = CreateSut(host);

        var backslash = normalizer.Normalize(@"\\srv\share\x");
        var forward = normalizer.Normalize("//srv/share/x");

        Assert.Equal(@"\\srv\share\x", backslash);
        Assert.Equal(backslash, forward);
        Assert.Equal(WorkspacePathPlatform.Windows, normalizer.DetectPlatform("//srv/share/x"));
        host.DidNotReceive().NormalizeNativePath(Arg.Any<string>());
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001-AC1: Mixed separators and dot segments in a Windows path collapse
    /// lexically on a Linux host.
    /// </summary>
    [Fact]
    public void LinuxHost_MixedSeparatorsAndDotSegments_CollapseLexically()
    {
        Assert.Equal(
            @"C:\Users\kingd\repo\x",
            CreateSut(LinuxHost()).Normalize(@"C:\Users/kingd\\repo/.\y\..\x"));
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001: A POSIX absolute path on a Linux host is host-native and goes through
    /// host resolution exactly once.
    /// </summary>
    [Fact]
    public void LinuxHost_PosixAbsolutePath_UsesHostResolution()
    {
        var host = LinuxHost();

        var result = CreateSut(host).Normalize("/home/sharpninja/github/McpServer");

        Assert.Equal("/home/sharpninja/github/McpServer", result);
        host.Received(1).NormalizeNativePath("/home/sharpninja/github/McpServer");
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001: A relative path belongs to the host, so a Linux host anchors it at its
    /// working directory and reports POSIX semantics even when the test runs on Windows.
    /// </summary>
    [Fact]
    public void LinuxHost_RelativePath_ResolvesAgainstHostWorkingDirectory()
    {
        var host = LinuxHost();
        var normalizer = CreateSut(host);

        var result = normalizer.Normalize("relative/x");

        Assert.Equal("/opt/mcpserver/app/relative/x", result);
        Assert.Equal(WorkspacePathPlatform.CaseSensitive, normalizer.DetectPlatform("relative/x"));
        host.Received(1).NormalizeNativePath("relative/x");
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001-AC2: A POSIX path received by a Windows host stays a POSIX path and is
    /// never handed to host-native resolution (which would re-root it on a drive).
    /// </summary>
    [Fact]
    public void WindowsHost_PosixAbsolutePath_StaysPosix()
    {
        var host = WindowsHost();
        var normalizer = CreateSut(host);

        var result = normalizer.Normalize("/home/sharpninja/github/McpServer");

        Assert.Equal("/home/sharpninja/github/McpServer", result);
        Assert.Equal(
            WorkspacePathPlatform.CaseSensitive,
            normalizer.DetectPlatform("/home/sharpninja/github/McpServer"));
        host.DidNotReceive().NormalizeNativePath(Arg.Any<string>());
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001: Windows drive paths and relative paths on a Windows host are
    /// host-native and use host resolution.
    /// </summary>
    [Theory]
    [InlineData(@"C:\Users\kingd", @"C:\Users\kingd")]
    [InlineData("relative", @"C:\svc\relative")]
    public void WindowsHost_NativePaths_UseHostResolution(string input, string expected)
    {
        var host = WindowsHost();
        var normalizer = CreateSut(host);

        Assert.Equal(expected, normalizer.Normalize(input));
        host.Received(1).NormalizeNativePath(input);
        Assert.Equal(WorkspacePathPlatform.Windows, normalizer.DetectPlatform(input));
    }

    /// <summary>TR-MCP-FED-PATH-001: Leading and trailing whitespace is ignored.</summary>
    [Fact]
    public void Normalize_TrimsWhitespace()
    {
        Assert.Equal(@"C:\Users\kingd", CreateSut(LinuxHost()).Normalize(@"  C:\Users\kingd  "));
    }

    /// <summary>TR-MCP-FED-PATH-001: Blank input is rejected.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_Blank_Throws(string input)
    {
        var normalizer = CreateSut(LinuxHost());

        Assert.ThrowsAny<ArgumentException>(() => normalizer.Normalize(input));
        Assert.ThrowsAny<ArgumentException>(() => normalizer.DetectPlatform(input));
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001-AC1: The leaf name of a Windows path is split on Windows separators
    /// even on a Linux host, and the leaf of a POSIX path is split on '/' even on a Windows host.
    /// </summary>
    [Theory]
    [InlineData(@"C:\Users\kingd", "kingd")]
    [InlineData("C:/Users/kingd/", "kingd")]
    [InlineData(@"\\srv\share\repo", "repo")]
    [InlineData("/home/sharpninja/github/McpServer", "McpServer")]
    public void GetLeafName_UsesPathPlatformSeparators_OnEitherHost(string input, string expected)
    {
        Assert.Equal(expected, CreateSut(LinuxHost()).GetLeafName(input));
        Assert.Equal(expected, CreateSut(WindowsHost()).GetLeafName(input));
    }

    /// <summary>TR-MCP-FED-PATH-001: The leaf name of a bare root is the root itself.</summary>
    [Theory]
    [InlineData(@"C:\", @"C:\")]
    [InlineData("/", "/")]
    public void GetLeafName_Root_ReturnsNormalizedRoot(string input, string expected)
    {
        Assert.Equal(expected, CreateSut(LinuxHost()).GetLeafName(input));
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001-AC4: Host-native is decided by syntax against the host platform, so a
    /// Linux hub treats Windows proxy paths as foreign and a Windows host treats POSIX paths as foreign.
    /// </summary>
    [Theory]
    [InlineData(@"C:\Users\kingd", false, true)]
    [InlineData("//srv/share/x", false, true)]
    [InlineData("/home/x", true, false)]
    [InlineData("relative", true, true)]
    public void IsHostNative_ComparesSyntaxPlatformWithHost(string input, bool nativeOnLinux, bool nativeOnWindows)
    {
        Assert.Equal(nativeOnLinux, CreateSut(LinuxHost()).IsHostNative(input));
        Assert.Equal(nativeOnWindows, CreateSut(WindowsHost()).IsHostNative(input));
    }

    /// <summary>
    /// TR-MCP-FED-PATH-001: Combining onto a foreign root uses the root's separators on either
    /// host, so a Linux hub derives C:\x\docs\sessions (not C:\x/docs/sessions) and a Windows host
    /// derives /home/x/docs/sessions (not /home/x\docs\sessions).
    /// </summary>
    [Theory]
    [InlineData(@"C:\Users\kingd", @"C:\Users\kingd\docs\sessions")]
    [InlineData("/home/sharpninja/github/McpServer", "/home/sharpninja/github/McpServer/docs/sessions")]
    public void Combine_UsesRootPlatformSyntax_OnEitherHost(string root, string expected)
    {
        Assert.Equal(expected, CreateSut(LinuxHost()).Combine(root, "docs", "sessions"));
        Assert.Equal(expected, CreateSut(WindowsHost()).Combine(root, "docs", "sessions"));
    }

    /// <summary>TR-MCP-FED-PATH-001: Dot segments collapse within the root's platform.</summary>
    [Fact]
    public void Combine_CollapsesDotSegments()
    {
        Assert.Equal(@"C:\Users\kingd\b", CreateSut(LinuxHost()).Combine(@"C:\Users\kingd", "a", "..", "b"));
    }

    /// <summary>TR-MCP-FED-PATH-001: A rooted segment replaces the root, as Path.Combine does.</summary>
    [Fact]
    public void Combine_RootedSegment_ReplacesRoot()
    {
        Assert.Equal("/srv/other", CreateSut(LinuxHost()).Combine(@"C:\Users\kingd", "/srv/other"));
    }

    /// <summary>Simulated Linux hub: POSIX host, working directory /opt/mcpserver/app.</summary>
    protected static IWorkspaceHostEnvironment LinuxHost() => SimulatedWorkspaceHosts.Linux();

    /// <summary>Simulated Windows host: working directory C:\svc.</summary>
    protected static IWorkspaceHostEnvironment WindowsHost() => SimulatedWorkspaceHosts.Windows();
}
