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
}

/// <summary>
/// TEST-MCP-FED-PATH-001: the production normalizer bound to the real process host. It depends on the
/// host it runs on, so it is an integration test on every host (TR-MCP-USECASE-018 AC-006).
/// </summary>
[Trait("Category", "Integration")]
public sealed class WorkspacePathNormalizerProcessTests
{
    /// <summary>
    /// FR-MCP-FED-PATH-001-AC1/AC2: With the real process host, a foreign-platform absolute path
    /// is returned without being re-rooted on the host (Windows runner: POSIX path; Linux runner:
    /// Windows path). Integration of the production normalizer with
    /// <see cref="SystemWorkspaceHostEnvironment"/>; the same behaviour is proven against mocked
    /// data by the contract tests (WindowsHost_PosixAbsolutePath_StaysPosix and
    /// LinuxHost_WindowsDrivePath_IsNotPrefixedWithWorkingDirectory). FR-MCP-PATHNORM-001: the
    /// Windows path comes back in its canonical '/' spelling (<see cref="ExpectedWorkspaceKey"/>).
    /// </summary>
    [Fact]
    public void ProcessNormalizer_ForeignAbsolutePath_IsNotReRooted()
    {
        var foreign = OperatingSystem.IsWindows() ? "/fixture/workspaces/McpServer" : @"C:\Users\fixture-user";

        var result = WorkspacePathNormalizer.Process.Normalize(foreign);

        Assert.Equal(ExpectedWorkspaceKey.Of(foreign), result);
        Assert.DoesNotContain(Environment.CurrentDirectory, result, StringComparison.OrdinalIgnoreCase);
    }
}
