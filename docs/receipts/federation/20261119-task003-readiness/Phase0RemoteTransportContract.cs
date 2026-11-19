namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Safe request evidence; credential labels require private byte comparison in a real adapter.</summary>
/// <param name="Ordinal">Per-exchange request ordinal.</param>
/// <param name="Origin">Observed scheme/host/port, with no path, user info or secret query.</param>
/// <param name="Method">Wire method or fully qualified remotesapi method name.</param>
/// <param name="Project">Resolved immutable project alias, including opaque repository selectors.</param>
/// <param name="Credential">Reference or BackendOnly, derived from exact invocation-local credential bytes.</param>
/// <param name="RequestHash">Observed request body hash after protocol framing.</param>
/// <param name="Range">Exact Range header for a byte-range request; empty otherwise.</param>
/// <param name="LocationHash">Hash of actual requested sealed URL before sanitization.</param>
/// <param name="AdvertisedLocationHash">Independent hash from the advertised object location.</param>
/// <remarks>2026-11-18: TASK-FEDPH0-003; FR-MCP-185 ac-RawRemoteCannotBypassPublicationPolicy,
/// ac-ForeignProjectCloneDenied, ac-RevokedCredentialFailsFetchAndPublish; TR-MCP-FEDERATION-012; TEST-MCP-233.</remarks>
internal sealed record Phase0TransportRequest(int Ordinal, string Origin, string Method, string Project,
    string Credential, string RequestHash, string Range, string LocationHash, string AdvertisedLocationHash);

/// <summary>One gateway admission result, independently correlated to an actual outbound request.</summary>
/// <param name="Ordinal">Request ordinal, never a connection identifier.</param>
/// <param name="IntrospectionCalls">Actual uncached introspections for this request.</param>
/// <param name="Identity">active, inactive or unavailable.</param>
/// <param name="Project">Project resolved before transfer or existence disclosure.</param>
/// <param name="Status">Observed gateway HTTP status.</param>
/// <param name="Code">Stable contract error code; empty on success.</param>
/// <param name="ResponseHash">Exact response digest at the node, empty on denied/redirected requests.</param>
/// <param name="BackendResponseHash">Independent response digest before gateway forwarding.</param>
/// <remarks>2026-11-18: FR-MCP-185 ac-ForeignProjectCloneDenied/ac-RevokedCredentialFailsFetchAndPublish;
/// TR-MCP-FEDERATION-012; TEST-MCP-233. No positive introspection cache is permitted.</remarks>
internal sealed record Phase0TransportAdmission(int Ordinal, int IntrospectionCalls, string Identity,
    string Project, int Status, string Code, string ResponseHash, string BackendResponseHash);

/// <summary>Independent retained refs and byte-transfer counters around the complete exchange.</summary>
/// <param name="PublicHeads">All public fixture refs, including unrelated project/branch refs.</param>
/// <param name="AtlasObjects">Completed Atlas object transfers.</param>
/// <param name="SecretObjects">Completed foreign-project object transfers.</param>
/// <param name="UploadedObjects">Candidate-namespace uploads; never public-ref advancement.</param>
/// <remarks>2026-11-18: FR-MCP-185 ac-RawRemoteCannotBypassPublicationPolicy/ac-ForeignProjectCloneDenied;
/// TR-MCP-FEDERATION-012; TEST-MCP-233.</remarks>
internal sealed record Phase0TransportState(IReadOnlyDictionary<string, string> PublicHeads,
    int AtlasObjects, int SecretObjects, int UploadedObjects);

/// <summary>Safe exchange receipt from independent node, gateway and private-backend observation points.</summary>
/// <param name="Kind">Http or TransportFailure.</param>
/// <param name="Status">Final observed gateway status, absent when destination was refused locally.</param>
/// <param name="Code">Stable HTTP contract error code, empty otherwise.</param>
/// <param name="Node">Actual node-to-gateway requests.</param>
/// <param name="Admissions">Per-request gateway identity/authorization decisions.</param>
/// <param name="Backend">Actual gateway-to-loopback-backend requests.</param>
/// <remarks>2026-11-18: TASK-FEDPH0-003; FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233.
/// These observations are not production response DTOs and contain no credential values.</remarks>
internal sealed record Phase0TransportExchange(string Kind, int? Status, string Code,
    IReadOnlyList<Phase0TransportRequest> Node, IReadOnlyList<Phase0TransportAdmission> Admissions,
    IReadOnlyList<Phase0TransportRequest> Backend);

/// <summary>Isolated transport fixture seam shared by discriminating mocks and later native integration.</summary>
/// <remarks>2026-11-18: TASK-FEDPH0-003; FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233.
/// Prepare seeds Atlas/main=C0, Atlas/feature=F0 and SecretProject/main=S0; all counters start at zero.
/// Actual hash strings may differ. A real adapter captures wire methods, bodies, ranges and credentials
/// independently, compares canaries privately, and returns only safe labels. It must not echo its inputs.</remarks>
internal interface IPhase0RemoteTransportProbe
{
    /// <summary>Arranges a new disposable fixture for the named protocol case.</summary>
    /// <param name="scenario">Contract case, not a public API fault option.</param>
    void Prepare(string scenario);

    /// <summary>Reads every fixture public ref and independent object-transfer counters.</summary>
    /// <returns>Actual state, or null on failed observation.</returns>
    Phase0TransportState? ReadState();

    /// <summary>Executes the native protocol sequence with fixture-only faults when applicable.</summary>
    /// <param name="scenario">Exact named fixture sequence.</param>
    /// <returns>Independent safe observations from all three transport boundaries.</returns>
    Phase0TransportExchange? Exchange(string scenario);
}

/// <summary>Approved-criterion assertions for all RPC channels, object requests and origin containment.</summary>
/// <remarks>2026-11-18: TASK-FEDPH0-003; FR-MCP-185 ac-RawRemoteCannotBypassPublicationPolicy,
/// ac-ForeignProjectCloneDenied/ac-RevokedCredentialFailsFetchAndPublish; TR-MCP-FEDERATION-012; TEST-MCP-233.
/// Real binding remains pending. 2026-11-19: Correct the native-header interpretation for the same FR/TR/ACs:
/// upstream supports Proxy-Authorization bearer; the approved records do not mandate a relay header name.
/// Header support alone establishes neither exact-origin containment nor native feasibility.
/// 2026-11-19: Same FR/TR/ACs: add advertised subdomain/fragment refusal, redirect-fragment refusal,
/// relative GET/PUT redirects and canonical host/default443 acceptance without weakening sealed-location checks.</remarks>
internal static class Phase0RemoteTransportContract
{
    /// <summary>Checks the scenario against independently frozen refs and explicit wire expectations.</summary>
    /// <param name="probe">Scripted or later native integration boundary.</param>
    /// <param name="scenario">One known contract case.</param>
    internal static void AssertScenario(IPhase0RemoteTransportProbe probe, string scenario)
    {
        var spec = Describe(scenario);
        probe.Prepare(scenario);
        var before = probe.ReadState();
        Assert.NotNull(before);
        Assert.NotNull(before.PublicHeads);
        var heads = before.PublicHeads.ToDictionary(pair => pair.Key, pair => pair.Value);
        Assert.Equal(new[] { "Atlas/feature", "Atlas/main", "SecretProject/main" }, heads.Keys.Order());
        Assert.All(heads.Values, value => Assert.False(string.IsNullOrWhiteSpace(value)));
        Assert.Equal(0, before.AtlasObjects);
        Assert.Equal(0, before.SecretObjects);
        Assert.Equal(0, before.UploadedObjects);
        var receipt = probe.Exchange(scenario);
        Assert.NotNull(receipt);
        Assert.Equal(spec.Refused ? "TransportFailure" : "Http", receipt.Kind);
        Assert.Equal(spec.Refused ? null : (int?)spec.FinalStatus, receipt.Status);
        Assert.Equal(spec.FinalCode, receipt.Code);
        Assert.NotNull(receipt.Node);
        Assert.NotNull(receipt.Admissions);
        Assert.NotNull(receipt.Backend);
        Assert.Equal(spec.Requests, receipt.Node.Count);
        Assert.Equal(spec.Requests, receipt.Admissions.Count);
        var backendOrdinal = 0;
        for (var index = 0; index < spec.Requests; index++)
        {
            var ordinal = index + 1;
            var denied = spec.DenyAt == ordinal;
            var status = denied ? spec.FinalStatus :
                index == 0 && spec.RedirectStatus != 0 ? spec.RedirectStatus :
                index == 0 && spec.Retry ? 503 : spec.ObjectRead ? 206 : 200;
            var request = receipt.Node[index];
            AssertRequest(request, ordinal, spec.Method, spec.ObjectRead, spec.Upload, backend: false);
            Assert.Equal(denied && spec.Foreign ? "SecretProject" : "Atlas", request.Project);
            var admission = receipt.Admissions[index];
            Assert.NotNull(admission);
            Assert.Equal(ordinal, admission.Ordinal);
            if (index > 0 && scenario is "ForeignStreamV1" or "ForeignStreamV2")
                Assert.InRange(admission.IntrospectionCalls, 0, 1); // Same RPC; project is still checked per message.
            else
                Assert.Equal(1, admission.IntrospectionCalls);
            Assert.Equal(denied ? spec.Identity : "active", admission.Identity);
            Assert.Equal(request.Project, admission.Project);
            Assert.Equal(status, admission.Status);
            Assert.Equal(denied ? spec.FinalCode : "", admission.Code);
            var transfersBody = !denied && status is 200 or 206;
            if (transfersBody)
            {
                Assert.Matches("^[0-9A-F]{64}$", admission.BackendResponseHash);
                Assert.Equal(admission.BackendResponseHash, admission.ResponseHash);
            }
            else
            {
                Assert.Equal("", admission.ResponseHash);
                Assert.Equal("", admission.BackendResponseHash);
            }
            if (!denied)
            {
                Assert.True(backendOrdinal < receipt.Backend.Count, "Missing private-backend observation.");
                var backend = receipt.Backend[backendOrdinal++];
                AssertRequest(backend, ordinal, spec.Method, spec.ObjectRead, spec.Upload, backend: true);
                Assert.Equal("Atlas", backend.Project);
                Assert.Equal(request.RequestHash, backend.RequestHash);
                Assert.Equal(request.Range, backend.Range);
                Assert.Equal(request.LocationHash, backend.LocationHash);
            }
        }
        Assert.Equal(backendOrdinal, receipt.Backend.Count);
        var after = probe.ReadState();
        Assert.NotNull(after);
        Assert.NotNull(after.PublicHeads);
        Assert.Equal(heads.OrderBy(pair => pair.Key), after.PublicHeads.OrderBy(pair => pair.Key));
        Assert.Equal(spec.Downloads, after.AtlasObjects);
        Assert.Equal(0, after.SecretObjects);
        Assert.Equal(spec.Upload && spec.DenyAt == 0 && !spec.Refused ? 1 : 0, after.UploadedObjects);
    }

    /// <summary>Checks credentials, origin and exact payload/range without printing secret values.</summary>
    /// <param name="request">Safe observation derived from actual captured bytes.</param>
    /// <param name="ordinal">Expected request identity.</param>
    /// <param name="method">Expected gRPC name or HTTP verb.</param>
    /// <param name="ranged">Whether Range bytes=4-11 must survive.</param>
    /// <param name="upload">Whether a PUT body is required.</param>
    /// <param name="backend">Checks backend credentials and loopback independently.</param>
    private static void AssertRequest(Phase0TransportRequest? request, int ordinal, string method,
        bool ranged, bool upload, bool backend)
    {
        Assert.NotNull(request);
        Assert.Equal(ordinal, request.Ordinal);
        Assert.Equal(method, request.Method);
        Assert.Equal(backend ? "BackendOnly" : "Reference", request.Credential);
        Assert.True(Uri.TryCreate(request.Origin, UriKind.Absolute, out var origin));
        Assert.NotNull(origin);
        Assert.Equal(backend ? "http" : "https", origin.Scheme);
        Assert.Equal(backend ? "127.0.0.1" : "gateway.fixture.test", origin.Host);
        Assert.True(backend ? origin.Port is > 0 and < 65536 : origin.Port == 443);
        Assert.Equal("/", origin.AbsolutePath);
        Assert.Equal("", origin.UserInfo);
        Assert.Equal("", origin.Query);
        Assert.Equal("", origin.Fragment);
        Assert.Matches("^[0-9A-F]{64}$", request.RequestHash);
        if (upload) Assert.Equal(UploadHash, request.RequestHash);
        Assert.Equal(ranged ? "bytes=4-11" : "", request.Range);
        if (ranged || upload || method == "GET")
        {
            Assert.Matches("^[0-9A-F]{64}$", request.AdvertisedLocationHash);
            Assert.Equal(request.AdvertisedLocationHash, request.LocationHash);
        }
        else
        {
            Assert.Equal("", request.LocationHash);
            Assert.Equal("", request.AdvertisedLocationHash);
        }
    }

    /// <summary>Builds expectations from explicit protocol fixtures, never from the mock or provider.</summary>
    /// <param name="scenario">Known fixture name.</param>
    /// <returns>Observable exchange contract, including exact denial boundary and allowed earlier transfers.</returns>
    private static Specification Describe(string scenario)
    {
        if (scenario.StartsWith("Rpc:", StringComparison.Ordinal))
        {
            var method = scenario[4..];
            Assert.Contains(method, new[] { "GetRepoMetadata", "HasChunks", "GetDownloadLocations",
                "StreamDownloadLocations", "StreamChunkLocations", "GetUploadLocations", "Rebase",
                "Root", "Commit", "ListTableFiles", "RefreshTableFileUrl", "AddTableFiles" });
            return new(method);
        }
        return scenario switch
        {
            "RangeGet" => new("GET", ObjectRead: true, Downloads: 1, FinalStatus: 206),
            "TableFileGet" or "RefreshedUrl" or "CanonicalObjectOrigin" => new("GET", Downloads: 1),
            "ObjectPut" => new("PUT", Upload: true),
            "Redirect307" or "RelativeRedirect307" => new("GET", Requests: 2, ObjectRead: true, Downloads: 1, RedirectStatus: 307, FinalStatus: 206),
            "Redirect308" or "RelativeRedirect308" => new("GET", Requests: 2, ObjectRead: true, Downloads: 1, RedirectStatus: 308, FinalStatus: 206),
            "PutRedirect307" or "RelativePutRedirect307" => new("PUT", Requests: 2, Upload: true, RedirectStatus: 307),
            "PutRedirect308" or "RelativePutRedirect308" => new("PUT", Requests: 2, Upload: true, RedirectStatus: 308),
            "RangeRetry" => new("GET", Requests: 2, ObjectRead: true, Downloads: 1, Retry: true, FinalStatus: 206),
            "RevokedNext" => new("GET", Requests: 2, ObjectRead: true, Downloads: 1,
                DenyAt: 2, Identity: "inactive", FinalStatus: 401, FinalCode: "invalid_token"),
            "UnavailableNext" => new("GET", Requests: 2, ObjectRead: true, Downloads: 1,
                DenyAt: 2, Identity: "unavailable", FinalStatus: 503, FinalCode: "identity_unavailable"),
            "ForeignRepoId" or "ForeignRepoPath" or "ForeignRepoToken" =>
                new("GetDownloadLocations", DenyAt: 1, Foreign: true, FinalStatus: 403, FinalCode: "unauthorized_project"),
            "ForeignStreamV1" => new("StreamDownloadLocations", Requests: 2, DenyAt: 2,
                Foreign: true, FinalStatus: 403, FinalCode: "unauthorized_project"),
            "ForeignStreamV2" => new("StreamChunkLocations", Requests: 2, DenyAt: 2,
                Foreign: true, FinalStatus: 403, FinalCode: "unauthorized_project"),
            "ForeignObject" => new("GET", ObjectRead: true, DenyAt: 1, Foreign: true,
                FinalStatus: 403, FinalCode: "unauthorized_project"),
            "ObjectOtherHost" or "ObjectSubdomain" or "ObjectOtherPort" or "ObjectDowngrade" or "ObjectUserInfo" or "ObjectFragment" =>
                new("GET", Requests: 0, Refused: true),
            "RedirectOtherHost" or "RedirectSubdomain" or "RedirectOtherPort" or
                "RedirectDowngrade" or "RedirectUserInfo" or "RedirectFragment" =>
                new("GET", ObjectRead: true, RedirectStatus: 307, Refused: true),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown transport case.")
        };
    }

    /// <summary>SHA256 of the independently specified uploaded UTF-8 bytes, not a fabricated native digest.</summary>
    private static readonly string UploadHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes("phase0-object-payload")));

    /// <summary>Test-only contract projection, not a production protocol or policy implementation.</summary>
    /// <remarks>2026-11-18: FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233.</remarks>
    private sealed record Specification(string Method, int Requests = 1, bool ObjectRead = false,
        bool Upload = false, int Downloads = 0, int DenyAt = 0, string Identity = "active",
        bool Foreign = false, int RedirectStatus = 0, bool Retry = false, bool Refused = false,
        int FinalStatus = 200, string FinalCode = "");
}
