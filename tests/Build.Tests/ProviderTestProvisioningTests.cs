namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-018: Binds the <see cref="ProviderTestProvisioningContract"/> scenarios and assertions,
/// unchanged from the BDPv4 mock gate, to the production <see cref="LinuxProviderTestProvisioner"/>. The recording fake
/// host stands in for the process environment, the env file, Docker and TCP; no container or socket is touched.
/// </summary>
public sealed class ProviderTestProvisioningTests
{
    private readonly LinuxProviderTestProvisioner provisioner = new();

    /// <summary>TR-MCP-NUKE-018: the production provisioner meets every contract scenario.</summary>
    /// <param name="scenario">Scenario name from <see cref="ProviderTestProvisioningContract.ScenarioNames"/>.</param>
    [Theory]
    [InlineData("load-from-file")]
    [InlineData("adopt-set")]
    [InlineData("file-missing")]
    [InlineData("variable-missing-in-file")]
    [InlineData("stopped-container")]
    [InlineData("missing-container")]
    [InlineData("unreachable")]
    [InlineData("localdb-on-linux")]
    public void Provision_MeetsContract(string scenario)
        => ProviderTestProvisioningContract.AssertScenario(scenario, provisioner);

    /// <summary>TR-MCP-NUKE-018: the theory above covers every contract scenario.</summary>
    [Fact]
    public void ProvisionTheory_CoversEveryScenario()
        => InlineTheoryCoverage.AssertRows<ProviderTestProvisioningTests>(nameof(Provision_MeetsContract), ProviderTestProvisioningContract.ScenarioNames);
}
