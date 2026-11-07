namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-019 (AC-1), BDPv4 mock gate: proves the <see cref="IntegrationLaneFilterContract"/> assertions accept a
/// scripted filter that produces the contract-derived filters, and reject deliberately wrong filters (negative
/// controls), before any production filter code is written.
/// </summary>
public sealed class IntegrationLaneFilterMockGateTests
{
    /// <summary>TR-MCP-NUKE-019: every host passes against the scripted filter.</summary>
    /// <param name="host">Host name from <see cref="IntegrationLaneFilterContract.HostNames"/>.</param>
    [Theory]
    [InlineData("windows")]
    [InlineData("linux")]
    public void ScriptedFilter_PassesHost(string host)
        => IntegrationLaneFilterContract.AssertHost(host, new ScriptedFilter(Fault.None));

    /// <summary>TR-MCP-NUKE-019: the theory above covers every contract host.</summary>
    [Fact]
    public void ScriptedFilterTheory_CoversEveryHost()
        => InlineTheoryCoverage.AssertRows<IntegrationLaneFilterMockGateTests>(nameof(ScriptedFilter_PassesHost), IntegrationLaneFilterContract.HostNames);

    /// <summary>
    /// TR-MCP-NUKE-019 negative controls: each wrong filter is rejected (the other host's cases admitted, untagged
    /// cases dropped, hosts swapped, the Category=Integration term dropped).
    /// </summary>
    /// <param name="host">Host the fault is checked on.</param>
    /// <param name="fault">Injected fault.</param>
    [Theory]
    [InlineData("windows", Fault.AdmitsOtherHost)]
    [InlineData("linux", Fault.AdmitsOtherHost)]
    [InlineData("windows", Fault.DropsUntagged)]
    [InlineData("linux", Fault.DropsUntagged)]
    [InlineData("windows", Fault.SwapsHosts)]
    [InlineData("linux", Fault.SwapsHosts)]
    [InlineData("windows", Fault.DropsCategory)]
    [InlineData("linux", Fault.DropsCategory)]
    public void WrongFilter_IsRejected(string host, Fault fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => IntegrationLaneFilterContract.AssertHost(host, new ScriptedFilter(fault)));

    /// <summary>
    /// TR-MCP-NUKE-019 negative controls: the selection assertions alone, without the exact-text check, reject each
    /// wrong filter. Each fault breaks a different assertion (other host admitted, untagged dropped, own host
    /// excluded, non-integration admitted), so every selection assertion is shown to discriminate.
    /// </summary>
    /// <param name="host">Host the fault is checked on.</param>
    /// <param name="fault">Injected fault.</param>
    [Theory]
    [InlineData("windows", Fault.AdmitsOtherHost)]
    [InlineData("linux", Fault.AdmitsOtherHost)]
    [InlineData("windows", Fault.DropsUntagged)]
    [InlineData("linux", Fault.DropsUntagged)]
    [InlineData("windows", Fault.SwapsHosts)]
    [InlineData("linux", Fault.SwapsHosts)]
    [InlineData("windows", Fault.DropsCategory)]
    [InlineData("linux", Fault.DropsCategory)]
    public void WrongFilter_FailsSelection(string host, Fault fault)
    {
        var parsed = IntegrationLaneFilterContract.Parse(host);
        var filter = new ScriptedFilter(fault).Compose(parsed);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => IntegrationLaneFilterContract.AssertSelection(parsed, filter));
    }

    /// <summary>TR-MCP-NUKE-019: the contract evaluator refuses a term it cannot read instead of guessing.</summary>
    /// <param name="filter">Filter with an unsupported term.</param>
    [Theory]
    [InlineData("Category~Integration")]
    [InlineData("Category=Integration&Owner!=Linux")]
    public void Evaluator_RejectsUnsupportedTerm(string filter)
        => Assert.Throws<FormatException>(() => IntegrationLaneFilterContract.Selects(filter, "Integration", null));

    /// <summary>Faults injected into the scripted filter for negative controls.</summary>
    public enum Fault
    {
        /// <summary>No fault: the contract-derived filter.</summary>
        None,

        /// <summary>Selects every integration case, including the other host's.</summary>
        AdmitsOtherHost,

        /// <summary>Selects only cases tagged for the host, dropping untagged integration cases.</summary>
        DropsUntagged,

        /// <summary>Excludes the host's own cases instead of the other host's.</summary>
        SwapsHosts,

        /// <summary>Drops the Category=Integration term.</summary>
        DropsCategory,
    }

    /// <summary>Scripted filter: the contract-derived filters, with an optional injected fault.</summary>
    private sealed class ScriptedFilter(Fault fault) : IIntegrationLaneFilter
    {
        /// <inheritdoc />
        public string Compose(IntegrationLaneHost host)
        {
            var own = host == IntegrationLaneHost.Windows ? "Windows" : "Linux";
            var other = host == IntegrationLaneHost.Windows ? "Linux" : "Windows";
            return fault switch
            {
                Fault.None => $"Category=Integration&Platform!={other}",
                Fault.AdmitsOtherHost => "Category=Integration",
                Fault.DropsUntagged => $"Category=Integration&Platform={own}",
                Fault.SwapsHosts => $"Category=Integration&Platform!={own}",
                Fault.DropsCategory => $"Platform!={other}",
                _ => throw new ArgumentOutOfRangeException(nameof(fault), fault, "Unknown fault."),
            };
        }
    }
}
