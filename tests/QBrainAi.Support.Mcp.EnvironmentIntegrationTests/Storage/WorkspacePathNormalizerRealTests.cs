using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Storage;

/// <summary>
/// TEST-MCP-FED-PATH-001: the production normalizer bound to the real process host. It depends on the
/// host it runs on, so it is an integration test on every host (TR-MCP-USECASE-018 AC-006).
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-FED-PATH-001; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole with its 1 Category=Integration case from QBrainAi.Support.Mcp.Tests (Storage/WorkspacePathNormalizerRealTests.cs), whose unit classes stay there, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); only the namespace changed.</remarks>
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
