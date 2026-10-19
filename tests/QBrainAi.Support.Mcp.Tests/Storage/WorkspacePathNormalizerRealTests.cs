using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-FED-PATH-001, BDP v4 step 3 (plan section 2a): runs the
/// <see cref="WorkspacePathNormalizerContractTests"/> against the production
/// <see cref="WorkspacePathNormalizer"/>, plus checks that only apply to the concrete type.
/// </summary>
public sealed class WorkspacePathNormalizerRealTests : WorkspacePathNormalizerContractTests
{
    /// <inheritdoc />
    protected override IWorkspacePathNormalizer CreateSut(IWorkspaceHostEnvironment host) =>
        new WorkspacePathNormalizer(host);

    /// <summary>TR-MCP-FED-PATH-001: A null host is rejected at construction.</summary>
    [Fact]
    public void Constructor_NullHost_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new WorkspacePathNormalizer(null!));
    }

    /// <summary>
    /// FR-MCP-FED-PATH-001-AC1/AC2: With the real process host, a foreign-platform absolute path
    /// is returned without being re-rooted on the host (Windows runner: POSIX path; Linux runner:
    /// Windows path). Integration of the production normalizer with
    /// <see cref="SystemWorkspaceHostEnvironment"/>; the same behaviour is proven against mocked
    /// data by the contract tests (WindowsHost_PosixAbsolutePath_StaysPosix and
    /// LinuxHost_WindowsDrivePath_IsNotPrefixedWithWorkingDirectory).
    /// </summary>
    [Fact]
    public void ProcessNormalizer_ForeignAbsolutePath_IsNotReRooted()
    {
        var foreign = OperatingSystem.IsWindows() ? "/home/sharpninja/github/McpServer" : @"C:\Users\kingd";

        var result = WorkspacePathNormalizer.Process.Normalize(foreign);

        Assert.Equal(foreign, result);
        Assert.DoesNotContain(Environment.CurrentDirectory, result, StringComparison.OrdinalIgnoreCase);
    }
}
