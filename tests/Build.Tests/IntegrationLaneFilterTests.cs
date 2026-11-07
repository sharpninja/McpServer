namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 (AC-1): Binds the <see cref="IntegrationLaneFilterContract"/> cases and assertions, unchanged from
/// the BDPv4 mock gate, to the production <see cref="IntegrationLaneFilter"/>. Pure string composition; no host,
/// process or test runner is touched.
/// </summary>
public sealed class IntegrationLaneFilterTests
{
    private readonly IntegrationLaneFilter filter = new();

    /// <summary>TR-MCP-NUKE-019: the production filter meets the contract on every host.</summary>
    /// <param name="host">Host name from <see cref="IntegrationLaneFilterContract.HostNames"/>.</param>
    [Theory]
    [InlineData("windows")]
    [InlineData("linux")]
    public void Compose_MeetsContract(string host)
        => IntegrationLaneFilterContract.AssertHost(host, filter);

    /// <summary>TR-MCP-NUKE-019: the theory above covers every contract host.</summary>
    [Fact]
    public void ComposeTheory_CoversEveryHost()
        => Assert.Equal(2, IntegrationLaneFilterContract.HostNames.Length);
}
