namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent principal/operation scripts discriminate narrow worker rights and all ordinary editing denials.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-005 B5; FR-MCP-185 ac-LocalAppUserCannotPush; TR-MCP-FEDERATION-012; TEST-MCP-233.
/// No real principal, grant, network endpoint, object transfer or live gate is implemented here.</remarks>
public sealed class Phase0WorkerPermissionMockGateTests
{
    private static readonly string[] Operations = ["RemoteFetch", "RemotePush", "RemotePull", "WritePublicRef", "WriteLiveRef", "WriteIntegrationRef"];
    private static readonly string[] Facets = ["Missing", "PublicHead", "LiveHead", "IntegrationHead", "Title", "Downloaded", "Uploaded", "Published", "DirectLive"];
    /// <summary>All six operations run under ordinary and worker principals.</summary>
    public static IEnumerable<object[]> ValidCases => new[] { "OrdinaryEditor", "FederationWorker" }.SelectMany(p => Operations.Select(o => new object[] { p + ":" + o }));

    /// <summary>Checks both positive controls and denied paths with exact attempt and reopen interactions.</summary>
    /// <param name="scenario">One explicit role/operation pair.</param>
    [Theory, MemberData(nameof(ValidCases))]
    public void CorrectPermissionOutcomePasses(string scenario)
    {
        var probe = new WorkerProbe(scenario);
        Phase0WorkerPermissionContract.AssertScenario(probe, scenario);
        Assert.Equal(3, probe.Reads);
        Assert.Equal(1, probe.Attempts);
        Assert.Equal(1, probe.Reopens);
    }

    /// <summary>Each observed state field is corrupted before, after and after reopening; outcome is corrupted separately.</summary>
    public static IEnumerable<object[]> InvalidCases
    {
        get
        {
            foreach (var item in ValidCases)
            {
                var scenario = (string)item[0];
                for (var checkpoint = 0; checkpoint < 3; checkpoint++)
                foreach (var facet in Facets) yield return [scenario, $"State:{checkpoint}:{facet}"];
                yield return [scenario, "Outcome"];
            }
        }
    }

    /// <summary>Requires an assertion rejection and verifies the requested corruption actually reached that assertion path.</summary>
    /// <param name="scenario">Role/operation fixture.</param><param name="fault">Independent wrong outcome or state field.</param>
    [Theory, MemberData(nameof(InvalidCases))]
    public void WrongPermissionOutcomeIsRejected(string scenario, string fault)
    {
        var probe = new WorkerProbe(scenario, fault);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0WorkerPermissionContract.AssertScenario(probe, scenario));
        Assert.True(probe.FaultReached, "The requested corruption was not returned: " + fault);
    }

    /// <summary>Literal scripted expectations exercise the same reusable contract without a database or fabricated SQL error numbers.</summary>
    private sealed class WorkerProbe(string scenario, string fault = "") : IPhase0WorkerPermissionProbe
    {
        private bool attempted;
        /// <summary>Counts independent state observations.</summary>
        internal int Reads { get; private set; }
        /// <summary>Counts actual permission attempts.</summary>
        internal int Attempts { get; private set; }
        /// <summary>Counts reopened SQL sessions.</summary>
        internal int Reopens { get; private set; }
        /// <summary>Records that a single wrong result was consumed.</summary>
        internal bool FaultReached { get; private set; }
        /// <inheritdoc />
        public void Prepare() => attempted = false;
        /// <inheritdoc />
        public string? Attempt(Phase0WorkerPermissionAttempt attempt)
        {
            Attempts++;
            var expected = scenario switch
            {
                "OrdinaryEditor:RemoteFetch" => new Phase0WorkerPermissionAttempt("OrdinaryEditor", "RemoteFetch", "main"),
                "OrdinaryEditor:RemotePush" => new("OrdinaryEditor", "RemotePush", "main"),
                "OrdinaryEditor:RemotePull" => new("OrdinaryEditor", "RemotePull", "integration/private"),
                "OrdinaryEditor:WritePublicRef" => new("OrdinaryEditor", "WritePublicRef", "main"),
                "OrdinaryEditor:WriteLiveRef" => new("OrdinaryEditor", "WriteLiveRef", "main"),
                "OrdinaryEditor:WriteIntegrationRef" => new("OrdinaryEditor", "WriteIntegrationRef", "integration/private"),
                "FederationWorker:RemoteFetch" => new("FederationWorker", "RemoteFetch", "main"),
                "FederationWorker:RemotePush" => new("FederationWorker", "RemotePush", "main"),
                "FederationWorker:RemotePull" => new("FederationWorker", "RemotePull", "integration/private"),
                "FederationWorker:WritePublicRef" => new("FederationWorker", "WritePublicRef", "main"),
                "FederationWorker:WriteLiveRef" => new("FederationWorker", "WriteLiveRef", "main"),
                "FederationWorker:WriteIntegrationRef" => new("FederationWorker", "WriteIntegrationRef", "integration/private"),
                _ => throw new InvalidOperationException("Unknown worker fixture.")
            };
            Assert.Equal(expected, attempt);
            attempted = true;
            var allowed = scenario is "FederationWorker:RemoteFetch" or "FederationWorker:RemotePush" or "FederationWorker:RemotePull" or "FederationWorker:WriteIntegrationRef";
            if (fault != "Outcome") return allowed ? "Allowed" : "SqlPermissionDenied";
            FaultReached = true;
            return allowed ? "SqlPermissionDenied" : "Allowed";
        }
        /// <inheritdoc />
        public Phase0WorkerPermissionState? ReadState()
        {
            var checkpoint = Reads++;
            var state = !attempted ? new Phase0WorkerPermissionState("C0", "B1", "I0", "offline draft café 雪", 0, 0, 0, 0) : scenario switch
            {
                "FederationWorker:RemoteFetch" => new("C0", "B1", "I0", "offline draft café 雪", 1, 0, 0, 0),
                "FederationWorker:RemotePush" => new("C0", "B1", "I0", "offline draft café 雪", 0, 1, 0, 0),
                "FederationWorker:RemotePull" => new("C0", "B1", "I1", "offline draft café 雪", 1, 0, 0, 0),
                "FederationWorker:WriteIntegrationRef" => new("C0", "B1", "I1", "offline draft café 雪", 0, 0, 0, 0),
                _ => new("C0", "B1", "I0", "offline draft café 雪", 0, 0, 0, 0)
            };
            var prefix = $"State:{checkpoint}:";
            if (!fault.StartsWith(prefix, StringComparison.Ordinal)) return state;
            FaultReached = true;
            return fault[prefix.Length..] switch
            {
                "Missing" => null,
                "PublicHead" => state with { PublicHead = "unexpected-publication" },
                "LiveHead" => state with { LiveHead = "bypass-owner-gate" },
                "IntegrationHead" => state with { IntegrationHead = "unexpected-integration" },
                "Title" => state with { LocalTitle = "lost-draft" },
                "Downloaded" => state with { DownloadedObjects = 77 },
                "Uploaded" => state with { UploadedCandidates = 77 },
                "Published" => state with { PublishedObjects = 1 },
                "DirectLive" => state with { DirectLiveWrites = 1 },
                _ => throw new InvalidOperationException("Unknown worker state fault.")
            };
        }
        /// <inheritdoc />
        public void Reopen() { Assert.True(attempted); Reopens++; }
    }
}