namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent socket/query scripts prove local-health and reciprocal SQL-isolation assertions discriminate incorrect observations.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-007 B12; FR-MCP-185 ac-SqlPortUnreachableAcrossLink; TR-MCP-FEDERATION-012; TEST-MCP-233. No socket, SQL connection, service, credential or real host is used by these unit controls.</remarks>
public sealed class Phase0SqlNetworkMockGateTests
{
    /// <summary>Local health and both cross-link refusals pass on two run-owned port assignments.</summary>
    /// <param name="alternatePorts">Second independently authored manifest/script.</param>
    [Theory, InlineData(false), InlineData(true)]
    public void CorrectSqlIsolationPasses(bool alternatePorts)
    {
        var probe = new ScriptedProbe(alternatePorts);
        Phase0SqlNetworkContract.AssertIsolation(probe, Targets(alternatePorts));
        Assert.Equal(new[] { "prepare", "LAB-OMARCHY/LAB-OMARCHY/local", "read:0", "PAYTON-LEGION2/PAYTON-LEGION2/local", "read:1", "PAYTON-LEGION2/LAB-OMARCHY/cross", "read:2", "LAB-OMARCHY/PAYTON-LEGION2/cross", "read:3" }, probe.Calls);
    }

    /// <summary>Gets each independent missing, misaddressed or unsafe observed facet at all four actual-attempt boundaries.</summary>
    public static IEnumerable<object[]> Faults
    {
        get
        {
            foreach (var alternatePorts in new[] { false, true })
            foreach (var index in Enumerable.Range(0, 4))
            {
                foreach (var facet in new[] { "Missing", "Source", "Target", "Address", "Port", "Kind", "Connected", "Health", "Receipt", "WhitespaceReceipt" })
                    yield return [alternatePorts, index, facet];
                if (index < 2) yield return [alternatePorts, index, "NoQuery"];
            }
        }
    }

    /// <summary>Every corrupted completed-attempt observation is rejected by the unchanged shared assertions after injection.</summary>
    /// <param name="alternatePorts">Selected independent fixture.</param><param name="index">Corrupted capture.</param><param name="facet">Exactly one wrong observation.</param>
    [Theory, MemberData(nameof(Faults))]
    public void WrongSqlIsolationIsRejected(bool alternatePorts, int index, string facet)
    {
        var probe = new ScriptedProbe(alternatePorts, index, facet);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0SqlNetworkContract.AssertIsolation(probe, Targets(alternatePorts)));
        Assert.True(probe.Injected, "The intended negative observation must be reached.");
    }

    /// <summary>Freezes expected targets independently of observer scripts; returned observations never supply expected values.</summary>
    /// <param name="alternatePorts">Selected run-owned port assignment.</param><returns>Independent target manifest.</returns>
    private static Phase0SqlNetworkEndpoint[] Targets(bool alternatePorts) => alternatePorts
        ? [new("LAB-OMARCHY", "10.42.0.214", 23306), new("PAYTON-LEGION2", "10.42.0.143", 23307)]
        : [new("LAB-OMARCHY", "10.42.0.214", 13306), new("PAYTON-LEGION2", "10.42.0.143", 13307)];

    /// <summary>Literal completed-attempt evidence and independent call ordering; no listener configuration is treated as a connection result.</summary>
    private sealed class ScriptedProbe(bool alternatePorts, int faultIndex = -1, string fault = "") : IPhase0SqlNetworkProbe
    {
        private int captureIndex = -1;
        /// <summary>Gets the ordered requested operations and independent capture reads.</summary>
        internal List<string> Calls { get; } = [];
        /// <summary>Gets whether the intended wrong observation reached the shared assertions.</summary>
        internal bool Injected { get; private set; }
        /// <inheritdoc />
        public void Prepare() { captureIndex = -1; Injected = false; Calls.Clear(); Calls.Add("prepare"); }
        /// <inheritdoc />
        public void Probe(string sourceHost, string targetHost, bool localHealth)
        {
            var expectedCall = Script()[++captureIndex];
            Assert.Equal(expectedCall.SourceHost, sourceHost); Assert.Equal(expectedCall.TargetHost, targetHost); Assert.Equal(expectedCall.LocalHealth, localHealth);
            Calls.Add(sourceHost + "/" + targetHost + "/" + (localHealth ? "local" : "cross"));
        }
        /// <inheritdoc />
        public Phase0SqlNetworkAttempt? ReadAttempt()
        {
            Calls.Add("read:" + captureIndex);
            var observed = Script()[captureIndex];
            if (captureIndex != faultIndex) return observed;
            Injected = true;
            return fault switch
            {
                "Missing" => null, "Source" => observed with { SourceHost = "wrong-source" }, "Target" => observed with { TargetHost = "wrong-target" },
                "Address" => observed with { Address = observed.LocalHealth ? "10.42.0.99" : "127.0.0.1" }, "Port" => observed with { Port = observed.Port + 1 },
                "Kind" => observed with { LocalHealth = !observed.LocalHealth }, "Connected" => observed with { Connected = !observed.Connected },
                "Health" => observed with { HealthValue = observed.LocalHealth ? 0 : 1 }, "NoQuery" => observed with { HealthValue = null },
                "Receipt" => observed with { Receipt = "" }, "WhitespaceReceipt" => observed with { Receipt = " " },
                _ => throw new ArgumentOutOfRangeException(nameof(fault))
            };
        }
        /// <summary>Returns independently authored completed socket/query captures for the selected fixture.</summary>
        /// <returns>Four safe, literal observations.</returns>
        private Phase0SqlNetworkAttempt[] Script() => alternatePorts
            ? [new("LAB-OMARCHY", "LAB-OMARCHY", "127.0.0.1", 23306, true, true, 1, "local-lab-2"),
                new("PAYTON-LEGION2", "PAYTON-LEGION2", "127.0.0.1", 23307, true, true, 1, "local-legion-2"),
                new("PAYTON-LEGION2", "LAB-OMARCHY", "10.42.0.214", 23306, false, false, null, "cross-to-lab-2"),
                new("LAB-OMARCHY", "PAYTON-LEGION2", "10.42.0.143", 23307, false, false, null, "cross-to-legion-2")]
            : [new("LAB-OMARCHY", "LAB-OMARCHY", "127.0.0.1", 13306, true, true, 1, "local-lab-1"),
                new("PAYTON-LEGION2", "PAYTON-LEGION2", "127.0.0.1", 13307, true, true, 1, "local-legion-1"),
                new("PAYTON-LEGION2", "LAB-OMARCHY", "10.42.0.214", 13306, false, false, null, "cross-to-lab-1"),
                new("LAB-OMARCHY", "PAYTON-LEGION2", "10.42.0.143", 13307, false, false, null, "cross-to-legion-1")];
    }
}