namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 (AC-8): Binds the <see cref="ProvisioningDownloadContract"/> cases and assertions, unchanged from the
/// BDPv4 mock gate, to the production <see cref="ProvisioningDownload"/>. Every case runs in memory through a scripted
/// message handler and a temporary folder; no network is used.
/// </summary>
public sealed class ProvisioningDownloadTests
{
    /// <summary>TR-MCP-NUKE-019 AC-8: the production download meets every contract case.</summary>
    [Fact]
    public async Task Download_MeetsContract()
        => await ProvisioningDownloadContract.AssertDownloadAsync(new ProvisioningDownload()).ConfigureAwait(true);

    /// <summary>TR-MCP-NUKE-019 AC-8: the provisioning targets use a two-minute inactivity limit.</summary>
    [Fact]
    public void DefaultInactivityLimit_IsTwoMinutes()
        => Assert.Equal(TimeSpan.FromMinutes(2), ProvisioningDownload.DefaultInactivityLimit);
}
