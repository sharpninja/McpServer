namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent protocol transcripts prove the transport assertions accept and reject relevant behavior.</summary>
/// <remarks>2026-11-18: TASK-FEDPH0-003; FR-MCP-185 ac-RawRemoteCannotBypassPublicationPolicy,
/// ac-ForeignProjectCloneDenied/ac-RevokedCredentialFailsFetchAndPublish; TR-MCP-FEDERATION-012; TEST-MCP-233.
/// Fixtures never call the contract's expectation builder and contain no real credentials or network access.</remarks>
public sealed class Phase0RemoteTransportMockGateTests
{
    /// <summary>All twelve RPCs plus object, redirect, retry, revocation and selector-boundary cases.</summary>
    public static IEnumerable<object[]> Scenarios => Fixtures.Keys.Select(name => new object[] { name });

    /// <summary>Each asserted boundary is individually corrupted, including absent and extra observations.</summary>
    public static IEnumerable<object[]> Faults
    {
        get
        {
            foreach (var pair in Fixtures)
            {
                foreach (var point in new[] { "Before", "After" })
                foreach (var faultField in new[] { "Missing", "RefsMissing", "RefsExtra", "RefsAbsent", "RefBlank",
                    "AtlasObjects", "SecretObjects", "UploadedObjects" })
                    yield return [pair.Key, point + ":" + faultField];
                if (pair.Value.Steps.Length > 0)
                {
                    foreach (var faultField in new[] { "Kind", "Status", "Code", "NodeNull", "AdmissionNull",
                        "BackendNull", "NodeExtra", "AdmissionExtra", "BackendExtra", "NodeAbsent", "AdmissionAbsent" })
                        yield return [pair.Key, "Receipt:" + faultField];
                }
                else
                {
                    foreach (var faultField in new[] { "Kind", "Status", "Code", "NodeNull", "AdmissionNull",
                        "BackendNull", "NodeExtra", "AdmissionExtra", "BackendExtra" })
                        yield return [pair.Key, "Receipt:" + faultField];
                }
                for (var i = 0; i < pair.Value.Steps.Length; i++)
                {
                    foreach (var faultField in RequestFields)
                        yield return [pair.Key, $"Node:{i}:{faultField}"];
                    foreach (var faultField in new[] { "Missing", "Ordinal", "Checks", "Identity", "Project", "Status",
                        "Code", "Body", "BackendBody" })
                        yield return [pair.Key, $"Admission:{i}:{faultField}"];
                }
                var backendCount = pair.Value.Steps.Count(step => step.Forward);
                for (var i = 0; i < backendCount; i++)
                foreach (var faultField in RequestFields)
                    yield return [pair.Key, $"Backend:{i}:{faultField}"];
            }
        }
    }

    /// <summary>Known-correct transcripts must exercise prepare, baseline, exchange and final readback.</summary>
    /// <param name="scenario">Independently specified protocol fixture.</param>
    [Theory]
    [MemberData(nameof(Scenarios))]
    public void CorrectTransportTranscriptPasses(string scenario)
    {
        var probe = new ScriptedProbe(scenario);
        Phase0RemoteTransportContract.AssertScenario(probe, scenario);
        Assert.Equal(new[] { "prepare", "before", "exchange", "after" }, probe.Calls);
    }

    /// <summary>Every corruption must reach its intended seam and fail the same native-binding assertions.</summary>
    /// <param name="scenario">Valid independent fixture.</param>
    /// <param name="fault">One explicitly corrupted observed field.</param>
    [Theory]
    [MemberData(nameof(Faults))]
    public void CorruptTransportTranscriptIsRejected(string scenario, string fault)
    {
        var probe = new ScriptedProbe(scenario, fault);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0RemoteTransportContract.AssertScenario(probe, scenario));
        Assert.True(probe.Injected, "Negative control never reached its intended observation.");
    }

    /// <summary>Native hashes/ports may vary; a continuing stream may reuse its original token validation.</summary>
    /// <param name="scenario">Known valid protocol fixture.</param>
    [Theory]
    [MemberData(nameof(Scenarios))]
    public void ValidIndependentNativeVariationPasses(string scenario)
        => Phase0RemoteTransportContract.AssertScenario(new ScriptedProbe(scenario, variant: true), scenario);

    /// <summary>A probe cannot rewrite a shared initial dictionary and then claim the same changed refs passed.</summary>
    [Fact]
    public void SharedBaselineMutationIsRejected()
    {
        var probe = new ScriptedProbe("RangeGet", "SharedAlias");
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0RemoteTransportContract.AssertScenario(probe, "RangeGet"));
        Assert.True(probe.Injected);
    }

    /// <summary>Independent literal timeline specification; no expected values come from the assertion source.</summary>
    private static readonly IReadOnlyDictionary<string, Fixture> Fixtures = BuildFixtures();

    /// <summary>Individually corrupted request facts, including exact origin and opaque location preservation.</summary>
    private static readonly string[] RequestFields =
    [
        "Missing", "Ordinal", "Method", "Project", "Credential", "Scheme", "Host", "Port", "Path",
        "Query", "UserInfo", "Fragment", "Hash", "Range", "Location", "Advertised"
    ];

    /// <summary>Defines actual protocol observations and final transfer counts as fixture data.</summary>
    /// <returns>All independent timelines, with explicit outcome and denial boundaries.</returns>
    private static IReadOnlyDictionary<string, Fixture> BuildFixtures()
    {
        var rows = new Dictionary<string, Fixture>(StringComparer.Ordinal);
        foreach (var method in new[] { "GetRepoMetadata", "HasChunks", "GetDownloadLocations",
            "StreamDownloadLocations", "StreamChunkLocations", "GetUploadLocations", "Rebase",
            "Root", "Commit", "ListTableFiles", "RefreshTableFileUrl", "AddTableFiles" })
            rows.Add("Rpc:" + method, new([new(method)]));
        rows.Add("RangeGet", new([new("GET", Status: 206, Ranged: true)], Downloads: 1));
        rows.Add("TableFileGet", new([new("GET")], Downloads: 1));
        rows.Add("RefreshedUrl", new([new("GET")], Downloads: 1));
        rows.Add("ObjectPut", new([new("PUT", Put: true)], Uploads: 1));
        foreach (var status in new[] { 307, 308 })
        {
            rows.Add("Redirect" + status, new([new("GET", Status: status, Ranged: true),
                new("GET", Status: 206, Ranged: true)], Downloads: 1));
            rows.Add("PutRedirect" + status, new([new("PUT", Status: status, Put: true),
                new("PUT", Put: true)], Uploads: 1));
        }
        rows.Add("RangeRetry", new([new("GET", Status: 503, Ranged: true),
            new("GET", Status: 206, Ranged: true)], Downloads: 1));
        rows.Add("RevokedNext", new([new("GET", Status: 206, Ranged: true),
            new("GET", Status: 401, Ranged: true, Identity: "inactive", Code: "invalid_token", Forward: false)], Downloads: 1));
        rows.Add("UnavailableNext", new([new("GET", Status: 206, Ranged: true),
            new("GET", Status: 503, Ranged: true, Identity: "unavailable", Code: "identity_unavailable", Forward: false)], Downloads: 1));
        foreach (var selector in new[] { "RepoId", "RepoPath", "RepoToken" })
            rows.Add("Foreign" + selector, new([new("GetDownloadLocations", Status: 403,
                Project: "SecretProject", Code: "unauthorized_project", Forward: false)]));
        foreach (var version in new[] { "V1", "V2" })
        {
            var method = version == "V1" ? "StreamDownloadLocations" : "StreamChunkLocations";
            rows.Add("ForeignStream" + version, new([new(method), new(method, Status: 403,
                Project: "SecretProject", Checks: 0, Code: "unauthorized_project", Forward: false)]));
        }
        rows.Add("ForeignObject", new([new("GET", Status: 403, Ranged: true,
            Project: "SecretProject", Code: "unauthorized_project", Forward: false)]));
        foreach (var target in new[] { "OtherHost", "OtherPort", "Downgrade", "UserInfo" })
            rows.Add("Object" + target, new([], Refused: true));
        foreach (var target in new[] { "OtherHost", "Subdomain", "OtherPort", "Downgrade", "UserInfo" })
            rows.Add("Redirect" + target, new([new("GET", Status: 307, Ranged: true)], Refused: true));
        return rows;
    }

    /// <summary>Literal observation row, representing a request or one independently addressed stream message.</summary>
    /// <remarks>2026-11-18: FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233.</remarks>
    private sealed record Step(string Method, int Status = 200, bool Ranged = false, bool Put = false,
        string Identity = "active", string Project = "Atlas", int Checks = 1, string Code = "", bool Forward = true);

    /// <summary>Explicit outcome timeline and transfer totals.</summary>
    /// <remarks>2026-11-18: FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233.</remarks>
    private sealed record Fixture(Step[] Steps, int Downloads = 0, int Uploads = 0, bool Refused = false);

    /// <summary>Scripted observation provider, deliberately incapable of accessing the network.</summary>
    /// <param name="scenario">Independent fixture name.</param>
    /// <param name="fault">Optional single observation corruption.</param>
    /// <param name="variant">Uses alternative valid hashes, refs, loopback port and stream validation count.</param>
    /// <remarks>2026-11-18: FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233.</remarks>
    private sealed class ScriptedProbe(string scenario, string? fault = null, bool variant = false)
        : IPhase0RemoteTransportProbe
    {
        /// <summary>Observed protocol calls, checked independently of the contract.</summary>
        internal List<string> Calls { get; } = [];
        /// <summary>Records that the deliberate fault reached its requested observation.</summary>
        internal bool Injected { get; private set; }
        /// <summary>Mutable shared dictionary is used only to challenge baseline freezing.</summary>
        private readonly Dictionary<string, string> heads = new()
        {
            ["Atlas/main"] = variant ? "native-C0-variant" : "native-C0",
            ["Atlas/feature"] = "native-F0", ["SecretProject/main"] = "native-S0"
        };
        /// <summary>Tracks whether Exchange ran before the state observation.</summary>
        private bool after;

        /// <inheritdoc/>
        public void Prepare(string requested)
        {
            Assert.Equal(scenario, requested);
            Calls.Add("prepare");
        }

        /// <inheritdoc/>
        public Phase0TransportState? ReadState()
        {
            Calls.Add(after ? "after" : "before");
            var fixture = Fixtures[scenario];
            var state = new Phase0TransportState(heads, after ? fixture.Downloads : 0, 0, after ? fixture.Uploads : 0);
            var prefix = after ? "After:" : "Before:";
            if (fault is null || !fault.StartsWith(prefix, StringComparison.Ordinal)) return state;
            Injected = true;
            var changed = heads.ToDictionary(pair => pair.Key, pair => pair.Value);
            switch (fault[prefix.Length..])
            {
                case "Missing": return null;
                case "RefsMissing": return state with { PublicHeads = null! };
                case "RefsExtra": changed.Add("extra/main", "extra"); break;
                case "RefsAbsent": changed.Remove("Atlas/feature"); break;
                case "RefBlank": changed["Atlas/main"] = ""; break;
                case "AtlasObjects": return state with { AtlasObjects = 99 };
                case "SecretObjects": return state with { SecretObjects = 99 };
                case "UploadedObjects": return state with { UploadedObjects = 99 };
            }
            return state with { PublicHeads = changed };
        }

        /// <inheritdoc/>
        public Phase0TransportExchange? Exchange(string requested)
        {
            Assert.Equal(scenario, requested);
            Calls.Add("exchange");
            after = true;
            if (fault == "SharedAlias") { heads["Atlas/main"] = "unexpected-rewrite"; Injected = true; }
            var fixture = Fixtures[scenario];
            var node = new List<Phase0TransportRequest>();
            var backend = new List<Phase0TransportRequest>();
            var admissions = new List<Phase0TransportAdmission>();
            var hash = new string(variant ? 'E' : 'A', 64);
            var responseHash = new string(variant ? 'F' : 'B', 64);
            var locationHash = new string(variant ? '9' : 'D', 64);
            var uploadHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes("phase0-object-payload")));
            for (var i = 0; i < fixture.Steps.Length; i++)
            {
                var row = fixture.Steps[i];
                var hasUrl = row.Ranged || row.Put || row.Method == "GET";
                var request = new Phase0TransportRequest(i + 1, "https://gateway.fixture.test/", row.Method,
                    row.Project, "Reference", row.Put ? uploadHash : hash, row.Ranged ? "bytes=4-11" : "",
                    hasUrl ? locationHash : "", hasUrl ? locationHash : "");
                node.Add(request);
                if (row.Forward)
                    backend.Add(request with { Origin = variant ? "http://127.0.0.1:41235/" : "http://127.0.0.1:41234/",
                        Credential = "BackendOnly" });
                var body = row.Status is 200 or 206 ? responseHash : "";
                admissions.Add(new(i + 1, variant && row.Checks == 0 ? 1 : row.Checks,
                    row.Identity, row.Project, row.Status, row.Code, body, body));
            }
            var final = fixture.Steps.LastOrDefault();
            var receipt = new Phase0TransportExchange(fixture.Refused ? "TransportFailure" : "Http",
                fixture.Refused ? null : final?.Status, final?.Code ?? "", node, admissions, backend);
            if (fault is null || fault == "SharedAlias" || fault.StartsWith("Before:", StringComparison.Ordinal) ||
                fault.StartsWith("After:", StringComparison.Ordinal)) return receipt;
            Injected = true;
            var pieces = fault.Split(':');
            if (pieces[0] == "Receipt")
            {
                return pieces[1] switch
                {
                    "Kind" => receipt with { Kind = "wrong" },
                    "Status" => receipt with { Status = 999 },
                    "Code" => receipt with { Code = "wrong" },
                    "NodeNull" => receipt with { Node = null! },
                    "AdmissionNull" => receipt with { Admissions = null! },
                    "BackendNull" => receipt with { Backend = null! },
                    "NodeExtra" => receipt with { Node = [..node, ExtraRequest()] },
                    "AdmissionExtra" => receipt with { Admissions = [..admissions, new(99, 1, "active", "Atlas", 200, "", "", "")] },
                    "BackendExtra" => receipt with { Backend = [..backend, ExtraRequest()] },
                    "NodeAbsent" => receipt with { Node = node.Skip(1).ToArray() },
                    "AdmissionAbsent" => receipt with { Admissions = admissions.Skip(1).ToArray() },
                    _ => throw new InvalidOperationException("Unknown receipt control.")
                };
            }
            var index = int.Parse(pieces[1], System.Globalization.CultureInfo.InvariantCulture);
            var field = pieces[2];
            if (pieces[0] == "Admission")
            {
                var row = admissions[index];
                admissions[index] = field switch
                {
                    "Missing" => null!, "Ordinal" => row with { Ordinal = 99 },
                    "Checks" => row with { IntrospectionCalls = 2 },
                    "Identity" => row with { Identity = "wrong" }, "Project" => row with { Project = "wrong" },
                    "Status" => row with { Status = 999 }, "Code" => row with { Code = "wrong" },
                    "Body" => row with { ResponseHash = "wrong" },
                    "BackendBody" => row with { BackendResponseHash = "wrong" },
                    _ => throw new InvalidOperationException("Unknown admission control.")
                };
            }
            else
            {
                var list = pieces[0] == "Node" ? node : backend;
                list[index] = CorruptRequest(list[index], field);
            }
            return receipt;
        }

        /// <summary>Produces an unmistakably extra wire observation without any secret content.</summary>
        /// <returns>Invalid extra request.</returns>
        private static Phase0TransportRequest ExtraRequest() => new(99, "https://wrong.test/", "GET", "wrong",
            "None", "wrong", "", "", "");

        /// <summary>Corrupts exactly one observed protocol field.</summary>
        /// <param name="row">Correct request fact.</param>
        /// <param name="field">Named single-field corruption.</param>
        /// <returns>The deliberately wrong observation.</returns>
        private static Phase0TransportRequest CorruptRequest(Phase0TransportRequest row, string field) => field switch
        {
            "Missing" => null!, "Ordinal" => row with { Ordinal = 99 }, "Method" => row with { Method = "DELETE" },
            "Project" => row with { Project = "wrong" }, "Credential" => row with { Credential = "wrong" },
            "Scheme" => row with { Origin = "ftp://gateway.fixture.test/" },
            "Host" => row with { Origin = "https://other.fixture.test/" },
            "Port" => row with { Origin = "https://gateway.fixture.test:444/" },
            "Path" => row with { Origin = row.Origin + "secret-path" },
            "Query" => row with { Origin = row.Origin + "?credential=fixture" },
            "UserInfo" => row with { Origin = row.Origin.Replace("://", "://user@", StringComparison.Ordinal) },
            "Fragment" => row with { Origin = row.Origin + "#fragment" },
            "Hash" => row with { RequestHash = "wrong" }, "Range" => row with { Range = "bytes=0-1" },
            "Location" => row with { LocationHash = "wrong" }, "Advertised" => row with { AdvertisedLocationHash = "wrong" },
            _ => throw new InvalidOperationException("Unknown request control.")
        };
    }
}
