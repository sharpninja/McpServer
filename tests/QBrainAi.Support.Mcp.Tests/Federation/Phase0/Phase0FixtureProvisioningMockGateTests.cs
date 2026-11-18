namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent mock observations prove the Ph0 three-peer prerequisite assertions discriminate unsafe fixtures.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-008; FR-MCP-186 ac-RealHostAcceptanceMatrix; TR-MCP-FEDERATION-013; TEST-MCP-234.
/// Only unit mock observations are used; no process, directory, port, database or enrollment is created.
/// The full Ph8 acceptance matrix and actual Ph0 Nuke provisioning remain unproven.
/// </remarks>
public sealed class Phase0FixtureProvisioningMockGateTests
{
    /// <summary>Gets each required-field corruption for each of the three peers plus missing lifecycle observations.</summary>
    public static IEnumerable<object[]> FixtureFaults
    {
        get
        {
            foreach (var fault in new[] { "MissingProvision", "MissingPeer", "ExtraPeer", "MissingStop", "MissingStoppedPeer", "ExtraStoppedPeer", "MissingBaseline" })
                yield return [-1, fault];
            for (var index = 0; index < 3; index++)
            foreach (var fault in new[] { "Name", "EmptyDatabase", "SharedDatabase", "SharedRoot", "NestedRoot", "EscapedRoot", "RelativeRoot", "InvalidAddress", "PublicAddress", "ZeroPort", "HighPort", "SharedPort", "ZeroPid", "SharedPid", "NotRunning", "ProductionEnrolled", "StillRunning", "StoppedIdentity" })
                yield return [index, fault];
            foreach (var read in new[] { 1, 2 })
            foreach (var fault in new[] { "MissingRead", "ProviderChanged", "DataChanged", "ProjectDropped", "ProjectAdded" })
                yield return [read, fault];
        }
    }

    /// <summary>Correct IPv4 or IPv6 loopback peers and reordered observations preserve all lifecycle assertions.</summary>
    /// <param name="ipv6">Use IPv6 loopback.</param><param name="reverse">Reverse returned order.</param>
    [Theory, InlineData(false, false), InlineData(true, false), InlineData(false, true), InlineData(true, true)]
    public void CorrectFixtureLifecyclePasses(bool ipv6, bool reverse)
    {
        var probe = new FixtureProbe(ipv6: ipv6, reverse: reverse);
        Phase0FixtureProvisioningContract.AssertIsolatedThreePeerLifecycle(probe, Path.GetFullPath("TestResults/phase0-fixture-mock"));
        Assert.Equal(new[] { "read-unrelated", "provision", "read-unrelated", "stop-read", "read-unrelated" }, probe.Calls);
    }

    /// <summary>Every missing, unsafe, shared, enrolled or unrelated-project mutation fails the same reusable contract.</summary>
    /// <param name="index">Peer index or unrelated-project checkpoint.</param><param name="fault">Independent wrong observation.</param>
    [Theory, MemberData(nameof(FixtureFaults))]
    public void WrongFixtureOutcomeIsRejected(int index, string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0FixtureProvisioningContract.AssertIsolatedThreePeerLifecycle(new FixtureProbe(index, fault), Path.GetFullPath("TestResults/phase0-fixture-mock")));

    /// <summary>Independent scripted provisioning and shutdown observations, never a real fixture implementation.</summary>
    private sealed class FixtureProbe(int index = -1, string fault = "", bool ipv6 = false, bool reverse = false) : IPhase0FixtureProvisioningProbe
    {
        private int reads;
        private Phase0FixturePeer[] original = [];
        internal List<string> Calls { get; } = [];
        /// <inheritdoc />
        public IReadOnlyDictionary<string, string>? ReadUnrelatedProjects()
        {
            Calls.Add("read-unrelated");
            var read = reads++;
            if (fault == "MissingBaseline" && read == 0) return null;
            var projects = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["unrelated-local"] = "sqlite/data-fingerprint-1",
                ["unrelated-server"] = "postgresql/data-fingerprint-2"
            };
            if (read != index) return projects;
            if (fault == "MissingRead") return null;
            if (fault == "ProviderChanged") projects["unrelated-local"] = "dolt/data-fingerprint-1";
            if (fault == "DataChanged") projects["unrelated-local"] = "sqlite/wrong-data";
            if (fault == "ProjectDropped") projects.Remove("unrelated-local");
            if (fault == "ProjectAdded") projects.Add("unexpected-project", "dolt/new-data");
            return projects;
        }
        /// <inheritdoc />
        public IReadOnlyList<Phase0FixturePeer>? Provision(string runRoot)
        {
            Calls.Add("provision");
            original = new[] { "FED-ATLAS-H", "FED-ATLAS-A", "FED-ATLAS-B" }.Select((name, position) =>
                new Phase0FixturePeer(name, "isolated-db-" + position, Path.Combine(runRoot, name), ipv6 ? "::1" : "127.0.0.1", 43001 + position, 1001 + position, true, false)).ToArray();
            var observed = original.ToList();
            if (fault == "MissingProvision") return null;
            if (fault == "MissingPeer") observed.RemoveAt(2);
            if (fault == "ExtraPeer") observed.Add(original[0] with { Name = "unexpected-peer" });
            if (index >= 0 && index < 3 && observed.Count == 3)
            {
                var peer = observed[index]; var other = original[(index + 1) % 3];
                observed[index] = fault switch
                {
                    "Name" => peer with { Name = "wrong-peer" }, "EmptyDatabase" => peer with { DatabaseIdentity = " " },
                    "SharedDatabase" => peer with { DatabaseIdentity = other.DatabaseIdentity }, "SharedRoot" => peer with { Root = other.Root },
                    "NestedRoot" => peer with { Root = Path.Combine(other.Root, "nested") }, "EscapedRoot" => peer with { Root = Path.Combine(runRoot, "..", "outside") },
                    "RelativeRoot" => peer with { Root = "relative-peer" }, "InvalidAddress" => peer with { Address = "not-an-address" },
                    "PublicAddress" => peer with { Address = "192.0.2.1" }, "ZeroPort" => peer with { Port = 0 }, "HighPort" => peer with { Port = 65536 },
                    "SharedPort" => peer with { Port = other.Port }, "ZeroPid" => peer with { ProcessId = 0 }, "SharedPid" => peer with { ProcessId = other.ProcessId },
                    "NotRunning" => peer with { IsRunning = false }, "ProductionEnrolled" => peer with { ProductionEnrolled = true }, _ => peer
                };
            }
            if (reverse) observed.Reverse();
            return observed;
        }
        /// <inheritdoc />
        public IReadOnlyList<Phase0FixturePeer>? StopAndRead()
        {
            Calls.Add("stop-read");
            if (fault == "MissingStop") return null;
            var stopped = original.Select(peer => peer with { IsRunning = false }).ToList();
            if (fault == "MissingStoppedPeer") stopped.RemoveAt(2);
            if (fault == "ExtraStoppedPeer") stopped.Add(original[0] with { IsRunning = false, Name = "unexpected-peer" });
            if (index >= 0 && index < 3)
            {
                if (fault == "StillRunning") stopped[index] = stopped[index] with { IsRunning = true };
                if (fault == "StoppedIdentity") stopped[index] = stopped[index] with { DatabaseIdentity = "wrong-db" };
            }
            if (reverse) stopped.Reverse();
            return stopped;
        }
    }
}
