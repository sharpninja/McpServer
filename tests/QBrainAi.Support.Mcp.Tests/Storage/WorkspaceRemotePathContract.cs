using QBrainAi.Client;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-PATHNORM-001 / TR-MCP-PATHNORM-001 (plan P1.4b): separator-agnostic Windows path
/// predicates. A remote UNC path is rejected before any native open whichever separator spells it
/// (the canonical form is <c>//server/share</c>), and the local administrative share alias resolves to
/// the drive form from either spelling. BDP v4 (plan section 2a): MockData fixture then Real fixture.
/// The scripted MockData fixture is a unit test on every host; the Real fixture resolves through the
/// Windows filesystem, so it is an integration test that runs on Windows hosts only (TR-MCP-USECASE-018
/// AC-006, TR-MCP-NUKE-019), without a skip.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-PATHNORM-001; TR-MCP-PATHNORM-001; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the contract moved unchanged from Storage/WorkspaceRemotePathContractTests.cs into this file, which QBrainAi.Support.Mcp.Tests compiles and QBrainAi.Support.Mcp.EnvironmentIntegrationTests links, because its scripted fixture stays a unit fixture in QBrainAi.Support.Mcp.Tests while its Windows filesystem fixture moved to the integration-owned project (slice S7, hostile validation finding R14-F4); one definition for both projects (DRY).</remarks>
public abstract class WorkspaceRemotePathContractTests
{
    /// <summary>Resolves a Windows-platform workspace path to its canonical normalized form.</summary>
    protected abstract Task<string> ResolveWindowsAsync(string path);

    /// <summary>Remote UNC paths are not supported for workspace identity, in either spelling.</summary>
    [Theory]
    [InlineData(@"\\192.0.2.1\pathnorm-share\child")]
    [InlineData("//192.0.2.1/pathnorm-share/child")]
    public async Task RemoteUnc_EitherSpelling_IsRejected(string path)
    {
        await Assert.ThrowsAsync<NotSupportedException>(() => ResolveWindowsAsync(path));
    }

    /// <summary>The local administrative share alias resolves to the drive form from either spelling.</summary>
    [Theory]
    [InlineData(@"\\localhost\C$\Windows")]
    [InlineData("//localhost/C$/Windows")]
    public async Task LocalAdministrativeShare_EitherSpelling_ResolvesToDrive(string path)
    {
        Assert.Equal("C:/Windows", await ResolveWindowsAsync(path), ignoreCase: true);
    }
}
