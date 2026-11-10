namespace NukeBuild.Tests;

/// <summary>
/// TR-MCP-NUKE-018: Contract scenarios and assertions for <see cref="IProviderTestProvisioner"/>.
/// A recording fake host stands in for the process environment, the test-database env file, Docker and TCP. The
/// fixtures pin the retained provisioning design (plan sections 8 and 22); the active AC-4 confidentiality rule is still asserted. Fake secret markers must never appear in
/// actions or errors. The same assertions run against the scripted mock (BDPv4 mock gate), wrong mocks (negative
/// controls), and the production provisioner.
/// </summary>
internal static class ProviderTestProvisioningContract
{
    /// <summary>Every scenario name, in the order the contract checks them.</summary>
    internal static readonly string[] ScenarioNames =
        ["load-from-file", "adopt-set", "file-missing", "variable-missing-in-file", "stopped-container", "missing-container", "unreachable", "localdb-on-linux"];

    /// <summary>Env file path used by every scenario.</summary>
    internal const string EnvFile = "/home/fixture/.config/qbrain-test/test-db.env";

    /// <summary>Fake PostgreSQL secret marker.</summary>
    internal const string PostgresSecret = "PG_SECRET_VALUE_7";

    /// <summary>Fake SQL Server secret marker.</summary>
    internal const string SqlServerSecret = "SQL_SECRET_VALUE_9";

    /// <summary>PostgreSQL connection string in the env file.</summary>
    internal const string PostgresValue = "Host=localhost;Port=55432;Username=tester;Password=" + PostgresSecret + ";Database=postgres";

    /// <summary>SQL Server connection string in the env file.</summary>
    internal const string SqlServerValue = "Server=localhost,51433;User ID=sa;Password=" + SqlServerSecret + ";TrustServerCertificate=True";

    /// <summary>Env file defining both variables, with a comment, an export prefix and a quoted value.</summary>
    internal const string FullEnvFile =
        "# QBrain.AI provider test servers\n"
        + "export " + ProviderTestNames.PostgresVariable + "=\"" + PostgresValue + "\"\n"
        + "\n"
        + ProviderTestNames.SqlServerVariable + "=" + SqlServerValue + "\n";

    /// <summary>Builds the fake host of a named scenario.</summary>
    internal static FakeProviderTestHost CreateHost(string scenario)
    {
        var host = new FakeProviderTestHost(scenario);
        host.Containers[ProviderTestNames.PostgresContainer] = ProviderTestContainerState.Running;
        host.Containers[ProviderTestNames.SqlServerContainer] = ProviderTestContainerState.Running;
        host.Containers["sql-server-developer"] = ProviderTestContainerState.Running;
        host.Files[EnvFile] = FullEnvFile;
        switch (scenario)
        {
            case "load-from-file":
            case "unreachable":
                if (scenario == "unreachable")
                {
                    host.Unreachable.Add("localhost:51433");
                }

                break;
            case "adopt-set":
                host.Variables[ProviderTestNames.PostgresVariable] = "Host=db.example;Port=6543;Password=" + PostgresSecret;
                host.Variables[ProviderTestNames.SqlServerVariable] = "Server=tcp:sql.example,1444;Password=" + SqlServerSecret;
                host.Files.Remove(EnvFile);
                break;
            case "file-missing":
                host.Files.Remove(EnvFile);
                break;
            case "variable-missing-in-file":
                host.Files[EnvFile] = ProviderTestNames.PostgresVariable + "=" + PostgresValue + "\n";
                break;
            case "stopped-container":
                host.Containers[ProviderTestNames.PostgresContainer] = ProviderTestContainerState.Stopped;
                break;
            case "missing-container":
                host.Containers.Remove(ProviderTestNames.SqlServerContainer);
                break;
            case "localdb-on-linux":
                host.Variables[ProviderTestNames.SqlServerVariable] = "Server=(localdb)\\MSSQLLocalDB;Integrated Security=True";
                break;
            default:
                throw new InvalidOperationException("Unknown scenario " + scenario);
        }

        return host;
    }

    /// <summary>Runs one scenario against the provisioner and asserts its contract.</summary>
    internal static void AssertScenario(string scenario, IProviderTestProvisioner provisioner)
    {
        var host = CreateHost(scenario);
        var result = provisioner.Provision(EnvFile, host);
        var text = string.Join("\n", result.Actions.Concat(result.Errors));

        Assert.DoesNotContain(PostgresSecret, text, StringComparison.Ordinal);
        Assert.DoesNotContain(SqlServerSecret, text, StringComparison.Ordinal);
        Assert.All(host.ContainerCalls, name => Assert.StartsWith("qbrain-test-", name, StringComparison.Ordinal));

        switch (scenario)
        {
            case "load-from-file":
                Assert.True(result.Succeeded, string.Join("; ", result.Errors));
                Assert.Equal(PostgresValue, host.Variables[ProviderTestNames.PostgresVariable]);
                Assert.Equal(SqlServerValue, host.Variables[ProviderTestNames.SqlServerVariable]);
                Assert.Empty(host.Started);
                Assert.Contains("localhost:55432", host.Probes);
                Assert.Contains("localhost:51433", host.Probes);
                Assert.Contains("load " + ProviderTestNames.PostgresVariable + " from " + EnvFile, result.Actions);
                Assert.Contains("load " + ProviderTestNames.SqlServerVariable + " from " + EnvFile, result.Actions);
                break;
            case "adopt-set":
                Assert.True(result.Succeeded, string.Join("; ", result.Errors));
                Assert.Equal(0, host.ReadFileCalls);
                Assert.Empty(host.ContainerCalls);
                Assert.Empty(host.SetCalls);
                Assert.Contains("db.example:6543", host.Probes);
                Assert.Contains("sql.example:1444", host.Probes);
                Assert.Contains("adopt " + ProviderTestNames.PostgresVariable + " (already set)", result.Actions);
                break;
            case "file-missing":
                Assert.False(result.Succeeded);
                Assert.Contains(result.Errors, error => error.Contains(EnvFile, StringComparison.Ordinal));
                Assert.Empty(host.SetCalls);
                Assert.Empty(host.Probes);
                break;
            case "variable-missing-in-file":
                Assert.False(result.Succeeded);
                Assert.Contains(result.Errors, error => error.Contains(ProviderTestNames.SqlServerVariable, StringComparison.Ordinal) && error.Contains(EnvFile, StringComparison.Ordinal));
                Assert.Equal(PostgresValue, host.Variables[ProviderTestNames.PostgresVariable]);
                break;
            case "stopped-container":
                Assert.True(result.Succeeded, string.Join("; ", result.Errors));
                Assert.Equal(new[] { ProviderTestNames.PostgresContainer }, host.Started);
                Assert.Contains("start " + ProviderTestNames.PostgresContainer, result.Actions);
                break;
            case "missing-container":
                Assert.False(result.Succeeded);
                Assert.Contains(result.Errors, error => error.Contains(ProviderTestNames.SqlServerContainer, StringComparison.Ordinal));
                Assert.DoesNotContain(ProviderTestNames.SqlServerContainer, host.Started);
                break;
            case "unreachable":
                Assert.False(result.Succeeded);
                Assert.Contains(result.Errors, error => error.Contains("localhost:51433", StringComparison.Ordinal));
                break;
            case "localdb-on-linux":
                Assert.False(result.Succeeded);
                Assert.Contains(result.Errors, error => error.Contains(ProviderTestNames.SqlServerVariable, StringComparison.Ordinal) && error.Contains("LocalDB", StringComparison.Ordinal));
                break;
            default:
                throw new InvalidOperationException("Unknown scenario " + scenario);
        }
    }

    /// <summary>Recording fake of <see cref="IProviderTestHost"/>.</summary>
    internal sealed class FakeProviderTestHost(string scenario) : IProviderTestHost
    {
        /// <summary>Scenario name (read by the scripted mock only).</summary>
        public string Scenario { get; } = scenario;

        /// <summary>Process environment.</summary>
        public Dictionary<string, string> Variables { get; } = new(StringComparer.Ordinal);

        /// <summary>Files by path.</summary>
        public Dictionary<string, string> Files { get; } = new(StringComparer.Ordinal);

        /// <summary>Container states by name.</summary>
        public Dictionary<string, ProviderTestContainerState> Containers { get; } = new(StringComparer.Ordinal);

        /// <summary>host:port endpoints that refuse connections.</summary>
        public HashSet<string> Unreachable { get; } = new(StringComparer.Ordinal);

        /// <summary>Variables set through the boundary.</summary>
        public List<string> SetCalls { get; } = [];

        /// <summary>Container names passed to GetContainerState or StartContainer.</summary>
        public List<string> ContainerCalls { get; } = [];

        /// <summary>Containers started.</summary>
        public List<string> Started { get; } = [];

        /// <summary>host:port endpoints probed.</summary>
        public List<string> Probes { get; } = [];

        /// <summary>Number of file reads.</summary>
        public int ReadFileCalls { get; private set; }

        /// <inheritdoc />
        public string? GetVariable(string name) => Variables.TryGetValue(name, out var value) ? value : null;

        /// <inheritdoc />
        public void SetVariable(string name, string value)
        {
            SetCalls.Add(name);
            Variables[name] = value;
        }

        /// <inheritdoc />
        public string? ReadFile(string path)
        {
            ReadFileCalls++;
            return Files.TryGetValue(path, out var text) ? text : null;
        }

        /// <inheritdoc />
        public ProviderTestContainerState GetContainerState(string containerName)
        {
            ContainerCalls.Add(containerName);
            return Containers.TryGetValue(containerName, out var state) ? state : ProviderTestContainerState.Missing;
        }

        /// <inheritdoc />
        public bool StartContainer(string containerName)
        {
            ContainerCalls.Add(containerName);
            if (!Containers.TryGetValue(containerName, out var state) || state == ProviderTestContainerState.Missing)
            {
                return false;
            }

            Started.Add(containerName);
            Containers[containerName] = ProviderTestContainerState.Running;
            return true;
        }

        /// <inheritdoc />
        public bool CanConnect(string host, int port, TimeSpan timeout)
        {
            var endpoint = host + ":" + port.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Probes.Add(endpoint);
            return !Unreachable.Contains(endpoint);
        }
    }
}
