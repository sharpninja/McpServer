namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-018, BDPv4 mock gate: proves the <see cref="ProviderTestProvisioningContract"/>
/// assertions accept a scripted provisioner that produces the contract-derived effects, and reject deliberately wrong
/// provisioners (negative controls), before any production provisioning code is written.
/// </summary>
public sealed class ProviderTestProvisioningMockGateTests
{
    /// <summary>TR-MCP-NUKE-018: every scenario passes against the scripted provisioner.</summary>
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
    public void ScriptedProvisioner_PassesScenario(string scenario)
        => ProviderTestProvisioningContract.AssertScenario(scenario, new ScriptedProvisioner(Fault.None));

    /// <summary>TR-MCP-NUKE-018: the theory above covers every contract scenario.</summary>
    [Fact]
    public void ScriptedProvisionerTheory_CoversEveryScenario()
        => Assert.Equal(8, ProviderTestProvisioningContract.ScenarioNames.Length);

    /// <summary>
    /// TR-MCP-NUKE-018 negative controls: each wrong provisioner is rejected by the scenario it violates
    /// (a value leak (AC-4), a foreign container, a file read although set, a missing file ignored,
    /// a probe skipped).
    /// </summary>
    /// <param name="scenario">Scenario the fault violates.</param>
    /// <param name="fault">Injected fault.</param>
    [Theory]
    [InlineData("load-from-file", Fault.LeaksValue)]
    [InlineData("load-from-file", Fault.TouchesOtherContainer)]
    [InlineData("adopt-set", Fault.ReadsFileWhenSet)]
    [InlineData("file-missing", Fault.IgnoresMissingFile)]
    [InlineData("unreachable", Fault.SkipsProbe)]
    public void WrongProvisioner_IsRejected(string scenario, Fault fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => ProviderTestProvisioningContract.AssertScenario(scenario, new ScriptedProvisioner(fault)));

    /// <summary>Faults injected into the scripted provisioner for negative controls.</summary>
    public enum Fault
    {
        /// <summary>No fault: the contract-derived behavior.</summary>
        None,

        /// <summary>Writes a connection-string value into an action.</summary>
        LeaksValue,

        /// <summary>Inspects a container other than the two qbrain-test containers.</summary>
        TouchesOtherContainer,

        /// <summary>Reads the env file although both variables are set.</summary>
        ReadsFileWhenSet,

        /// <summary>Reports no error when the env file is missing.</summary>
        IgnoresMissingFile,

        /// <summary>Reports success without probing the endpoints.</summary>
        SkipsProbe,
    }

    /// <summary>Scripted mock: produces the expected effects of the fake host's scenario, with one optional fault.</summary>
    private sealed class ScriptedProvisioner(Fault fault) : IProviderTestProvisioner
    {
        public ProviderTestProvisioningResult Provision(string envFilePath, IProviderTestHost host)
        {
            var fake = (ProviderTestProvisioningContract.FakeProviderTestHost)host;
            var actions = new List<string>();
            var errors = new List<string>();
            const string pg = ProviderTestNames.PostgresVariable;
            const string sql = ProviderTestNames.SqlServerVariable;
            switch (fake.Scenario)
            {
                case "load-from-file":
                case "unreachable":
                case "stopped-container":
                    host.ReadFile(envFilePath);
                    host.SetVariable(pg, ProviderTestContractValues.Postgres);
                    host.SetVariable(sql, ProviderTestContractValues.SqlServer);
                    actions.Add("load " + pg + " from " + envFilePath);
                    actions.Add("load " + sql + " from " + envFilePath);
                    if (fake.Scenario == "stopped-container" && host.GetContainerState(ProviderTestNames.PostgresContainer) == ProviderTestContainerState.Stopped)
                    {
                        host.StartContainer(ProviderTestNames.PostgresContainer);
                        actions.Add("start " + ProviderTestNames.PostgresContainer);
                    }

                    if (fault == Fault.TouchesOtherContainer)
                    {
                        host.GetContainerState("sql-server-developer");
                    }

                    if (fault == Fault.LeaksValue)
                    {
                        actions.Add("loaded value " + ProviderTestContractValues.Postgres);
                    }

                    if (fault != Fault.SkipsProbe)
                    {
                        var pgUp = host.CanConnect("localhost", 55432, TimeSpan.FromSeconds(1));
                        var sqlUp = host.CanConnect("localhost", 51433, TimeSpan.FromSeconds(1));
                        if (!pgUp)
                        {
                            errors.Add("endpoint localhost:55432 for " + pg + " is not accepting connections");
                        }

                        if (!sqlUp)
                        {
                            errors.Add("endpoint localhost:51433 for " + sql + " is not accepting connections");
                        }
                    }

                    break;
                case "adopt-set":
                    if (fault == Fault.ReadsFileWhenSet)
                    {
                        host.ReadFile(envFilePath);
                    }

                    actions.Add("adopt " + pg + " (already set)");
                    actions.Add("adopt " + sql + " (already set)");
                    host.CanConnect("db.example", 6543, TimeSpan.FromSeconds(1));
                    host.CanConnect("sql.example", 1444, TimeSpan.FromSeconds(1));
                    break;
                case "file-missing":
                    host.ReadFile(envFilePath);
                    if (fault != Fault.IgnoresMissingFile)
                    {
                        errors.Add("test-database env file not found: " + envFilePath);
                    }

                    break;
                case "variable-missing-in-file":
                    host.ReadFile(envFilePath);
                    host.SetVariable(pg, ProviderTestContractValues.Postgres);
                    errors.Add(sql + " is not defined in " + envFilePath);
                    break;
                case "missing-container":
                    host.ReadFile(envFilePath);
                    host.SetVariable(pg, ProviderTestContractValues.Postgres);
                    host.SetVariable(sql, ProviderTestContractValues.SqlServer);
                    host.GetContainerState(ProviderTestNames.SqlServerContainer);
                    errors.Add("container " + ProviderTestNames.SqlServerContainer + " not found");
                    break;
                case "localdb-on-linux":
                    host.ReadFile(envFilePath);
                    host.SetVariable(pg, ProviderTestContractValues.Postgres);
                    errors.Add(sql + " names a LocalDB server, which has no TCP endpoint on Linux");
                    break;
                default:
                    throw new InvalidOperationException("Unknown scenario " + fake.Scenario);
            }

            return new ProviderTestProvisioningResult(actions, errors);
        }
    }

    /// <summary>Contract values the scripted mock sets.</summary>
    private static class ProviderTestContractValues
    {
        public const string Postgres = ProviderTestProvisioningContract.PostgresValue;
        public const string SqlServer = ProviderTestProvisioningContract.SqlServerValue;
    }
}
