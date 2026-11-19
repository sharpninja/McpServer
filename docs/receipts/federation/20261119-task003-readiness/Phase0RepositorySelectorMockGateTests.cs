namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Exact protobuf repository selectors; null Org/Name means absent RepoId.</summary>
/// <param name="Org">RepoId.org.</param><param name="Name">RepoId.repo_name.</param>
/// <param name="Path">repo_path.</param><param name="Token">Opaque repo_token, never bearer authorization.</param>
/// <remarks>2026-11-19: FR-MCP-185 ac-RawRemoteCannotBypassPublicationPolicy/ac-ForeignProjectCloneDenied;
/// TR-MCP-FEDERATION-012; TEST-MCP-233. Commit fixtures never supply Token because its protobuf has no such field.</remarks>
internal sealed record Phase0RepositorySelectors(string? Org, string? Name, string? Path, string? Token);

/// <summary>Exact allowlist entry for a physical fixture repository.</summary>
/// <param name="Selectors">Equivalent wire identities.</param><param name="Project">Immutable project identity.</param>
/// <param name="Repository">Physical repository identity.</param><param name="Role">Public or Upload.</param>
/// <param name="Owner">Upload owner, or empty for public.</param><param name="Publication">Upload request, or empty.</param>
/// <remarks>2026-11-19: FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233. No prefix, filesystem or URL-decoding interpretation.</remarks>
internal sealed record Phase0RepositoryBinding(Phase0RepositorySelectors Selectors, string Project, string Repository,
    string Role, string Owner, string Publication);

/// <summary>Complete authorization input for one real RPC, including later streamed messages.</summary>
/// <param name="Method">One pinned remotesapi method.</param><param name="Rights">read or publish.</param>
/// <param name="Node">Authenticated node.</param><param name="Publication">Bound publication request.</param>
/// <param name="Frames">Actual per-message repository selectors.</param><param name="Allowlist">Exact run-specific mappings.</param>
/// <remarks>2026-11-19: TASK-FEDPH0-003; FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233.
/// Identity is active in these cases; revocation/unavailability is asserted by the existing authorization/transport seams.</remarks>
internal sealed record Phase0RepositoryCall(string Method, string Rights, string Node, string Publication,
    IReadOnlyList<Phase0RepositorySelectors> Frames, IReadOnlyList<Phase0RepositoryBinding> Allowlist);

/// <summary>Observed actual forwarding and physical repository selections, separate from public diagnostics.</summary>
/// <param name="Status">Final gateway status.</param><param name="Code">Safe result code.</param>
/// <param name="Introspections">Uncached new-RPC identity checks.</param><param name="SelectorChecks">Per-message selector checks.</param>
/// <param name="BackendCalls">Actual forwarded messages before rejection.</param><param name="Bindings">Actually selected physical targets.</param>
/// <param name="ForeignObjects">Foreign-project transferred objects.</param><param name="PublicMutations">Public-ref changes.</param>
/// <param name="Before">Independent public-state fingerprint before exchange.</param><param name="After">Read-back fingerprint.</param>
/// <param name="PublicBody">Sanitized public error body; empty on success.</param>
/// <remarks>2026-11-19: FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233. Internal safe binding labels do not expose foreign existence publicly.</remarks>
internal sealed record Phase0RepositoryObservation(int Status, string Code, int Introspections, int SelectorChecks,
    int BackendCalls, IReadOnlyList<string> Bindings, int ForeignObjects, int PublicMutations, string Before, string After, string PublicBody);

/// <summary>Test-owned gateway observation boundary, later bound to actual protobuf serialization and backend capture.</summary>
/// <remarks>2026-11-19: FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233. No authorization implementation in the mock provider.</remarks>
internal interface IPhase0RepositoryProbe
{
    /// <summary>Executes the exact RPC input in a fresh disposable fixture.</summary>
    /// <param name="input">Complete immutable fixture input.</param><returns>Observed forwarding, state and safe result.</returns>
    Phase0RepositoryObservation? Evaluate(Phase0RepositoryCall input);
}

/// <summary>Independent physical-repository and publication-role assertions.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-003; FR-MCP-185 ac-RawRemoteCannotBypassPublicationPolicy,
/// ac-ForeignProjectCloneDenied; TR-MCP-FEDERATION-012; TEST-MCP-233. Expectations are frozen before observation.</remarks>
internal static class Phase0RepositoryContract
{
    /// <summary>Rejects project-only authorization, selector precedence bypass, stream switching and unsafe errors.</summary>
    /// <param name="probe">Scripted or later real gateway observer.</param><param name="input">Actual protobuf-shaped fixture input.</param>
    /// <param name="expected">Independent literal required outcome.</param>
    internal static void AssertBinding(IPhase0RepositoryProbe probe, Phase0RepositoryCall input, Phase0RepositoryObservation expected)
    {
        var bindings = expected.Bindings.ToArray();
        var actual = probe.Evaluate(input); Assert.NotNull(actual);
        Assert.Equal(expected.Status, actual.Status); Assert.Equal(expected.Code, actual.Code);
        Assert.Equal(1, actual.Introspections); Assert.Equal(input.Frames.Count, actual.SelectorChecks);
        Assert.Equal(expected.BackendCalls, actual.BackendCalls); Assert.Equal(bindings, actual.Bindings);
        Assert.Equal(0, actual.ForeignObjects); Assert.Equal(0, actual.PublicMutations);
        Assert.False(string.IsNullOrWhiteSpace(actual.Before)); Assert.Equal(actual.Before, actual.After);
        Assert.Equal(expected.Code, actual.PublicBody);
    }
}

/// <summary>Concrete pinned-proto input fixtures and independent forwarding observations.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-003; FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233.
/// All12RPCs are covered. Commit has no repo_token. Scripts do not resolve selectors, invoke a gateway or move any ref.</remarks>
public sealed class Phase0RepositorySelectorMockGateTests
{
    /// <summary>Every compatible selector/binding fixture for the pinned12RPCs.</summary>
    public static IEnumerable<object[]> Cases
    {
        get
        {
            foreach (var method in Methods)
            {
                foreach (var name in new[] { "IdOnly", "PathOnly", "Combined", "AllMissing", "PartialId", "UnknownPath",
                    "PrefixPath", "EncodedPath", "MixedSameProject", "TwoUploads", "ForeignPath" }) yield return [method, name];
                if (method != "Commit") foreach (var name in new[] { "TokenOnly", "UnknownToken", "MixedToken" }) yield return [method, name];
                if (Writes.Contains(method)) foreach (var name in new[] { "ReadOnlyMutation", "PublicMutation", "WrongOwner", "WrongPublication" }) yield return [method, name];
                else foreach (var name in new[] { "UploadRead", "UploadReadWrongOwner", "UploadReadWrongPublication", "ReadOnlyUpload" }) yield return [method, name];
                if (method is "StreamDownloadLocations" or "StreamChunkLocations") yield return [method, "StreamSwitch"];
            }
        }
    }
    /// <summary>Every observed field is independently challenged for each explicit input.</summary>
    public static IEnumerable<object[]> Faults => Cases.SelectMany(row => Fields.Select(faultName => new object[] { row[0], row[1], faultName }));
    /// <summary>Correct selector and physical-role observations pass the same assertions intended for native binding.</summary>
    /// <param name="method">Pinned method.</param><param name="name">Concrete selector input.</param>
    [Theory, MemberData(nameof(Cases))]
    public void CorrectRepositoryBindingPasses(string method, string name)
    {
        var fixture = Build(method, name); var probe = new ScriptedProbe(fixture.Expected);
        Phase0RepositoryContract.AssertBinding(probe, fixture.Input, fixture.Expected);
        Assert.Same(fixture.Input, probe.LastInput); Assert.Equal(1, probe.Calls);
    }
    /// <summary>Wrong authorization, forwarding, state or public disclosure is rejected with injection reachability.</summary>
    /// <param name="method">Pinned method.</param><param name="name">Concrete selector input.</param><param name="fault">Single corruption.</param>
    [Theory, MemberData(nameof(Faults))]
    public void WrongRepositoryBindingIsRejected(string method, string name, string fault)
    {
        var fixture = Build(method, name); var probe = new ScriptedProbe(fixture.Expected, fault);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0RepositoryContract.AssertBinding(probe, fixture.Input, fixture.Expected));
        Assert.True(probe.Injected); Assert.Same(fixture.Input, probe.LastInput); Assert.Equal(1, probe.Calls);
    }
    /// <summary>Pinned service method inventory, not inferred from production enumeration.</summary>
    private static readonly string[] Methods = ["GetRepoMetadata", "HasChunks", "GetDownloadLocations", "StreamDownloadLocations",
        "StreamChunkLocations", "GetUploadLocations", "Rebase", "Root", "Commit", "ListTableFiles", "RefreshTableFileUrl", "AddTableFiles"];
    /// <summary>Only these raw RPCs mutate storage; they can address private upload repositories only.</summary>
    private static readonly string[] Writes = ["GetUploadLocations", "Commit", "AddTableFiles"];
    /// <summary>All output invariants, including zero foreign objects and unchanged public state.</summary>
    private static readonly string[] Fields = ["Missing", "Status", "Code", "Introspection", "Checks", "Backend", "Bindings",
        "ExtraBinding", "ForeignObjects", "PublicMutation", "BeforeBlank", "After", "Disclosure"];
    /// <summary>Literal exact allowlist; two upload repositories deliberately share project and role but not identity.</summary>
    private static readonly Phase0RepositoryBinding[] Registry =
    [
        new(new("atlas", "public", "atlas/public", "rt-atlas-public"), "Atlas", "atlas-public", "Public", "", ""),
        new(new("atlas", "upload-a42", "atlas/upload-a42", "rt-atlas-upload-a42"), "Atlas", "atlas-upload-a42", "Upload", "A", "pub42"),
        new(new("atlas", "upload-b99", "atlas/upload-b99", "rt-atlas-upload-b99"), "Atlas", "atlas-upload-b99", "Upload", "B", "pub99"),
        new(new("secret", "public", "secret/public", "rt-secret-public"), "SecretProject", "secret-public", "Public", "", "")
    ];
    /// <summary>Creates complete actual input and independent required outcome without implementing a selector resolver.</summary>
    /// <param name="method">Pinned RPC.</param><param name="name">Concrete fixture case.</param><returns>Input and literal outcome.</returns>
    private static (Phase0RepositoryCall Input, Phase0RepositoryObservation Expected) Build(string method, string name)
    {
        var write = Writes.Contains(method); var uploadRead = name is "UploadRead" or "UploadReadWrongOwner" or "UploadReadWrongPublication" or "ReadOnlyUpload"; var target = Registry[write || uploadRead ? 1 : 0];
        var s = target.Selectors; var rights = write || uploadRead ? "publish" : "read";
        var frame = s with { Token = method == "Commit" ? null : s.Token };
        var owner = "A"; var publication = "pub42";
        var status = 403; var code = "unauthorized_project"; var forwarded = 0; string[] bindings = [];
        switch (name)
        {
            case "IdOnly": frame = new(s.Org, s.Name, null, null); goto case "Allowed";
            case "PathOnly": frame = new(null, null, s.Path, null); goto case "Allowed";
            case "TokenOnly": frame = new(null, null, null, s.Token); goto case "Allowed";
            case "Combined": case "UploadRead": goto case "Allowed";
            case "Allowed": status = 200; code = ""; forwarded = 1; bindings = [target.Project + "/" + target.Repository + "/" + target.Role]; break;
            case "AllMissing": frame = new(null, null, null, null); break;
            case "PartialId": frame = new("atlas", null, null, null); break;
            case "UnknownPath": frame = new(null, null, "atlas/unknown", null); break;
            case "PrefixPath": frame = new(null, null, s.Path + "/../public", null); break;
            case "EncodedPath": frame = new(null, null, "atlas%2Fpublic", null); break;
            case "MixedSameProject": frame = new(s.Org, s.Name, Registry[write ? 0 : 1].Selectors.Path, null); break;
            case "TwoUploads": frame = new("atlas", "upload-a42", "atlas/upload-b99", null); break;
            case "ForeignPath": frame = new(null, null, "secret/public", null); break;
            case "UnknownToken": frame = new(null, null, null, "unknown-repository-token"); break;
            case "MixedToken": frame = new(s.Org, s.Name, s.Path, Registry[write ? 0 : 1].Selectors.Token); break;
            case "ReadOnlyMutation": rights = "read"; break;
            case "PublicMutation": frame = Registry[0].Selectors with { Token = method == "Commit" ? null : Registry[0].Selectors.Token };
                status = 409; code = "publication_required"; break;
            case "WrongOwner": case "UploadReadWrongOwner": owner = "B"; break;
            case "WrongPublication": case "UploadReadWrongPublication": publication = "pub99"; break;
            case "ReadOnlyUpload": rights = "read"; break;
            case "StreamSwitch": frame = new("atlas", "public", "atlas/upload-a42", null); forwarded = 1; bindings = ["Atlas/atlas-public/Public"]; break;
            default: throw new ArgumentOutOfRangeException(nameof(name));
        }
        var frames = name == "StreamSwitch" ? new[] { Registry[0].Selectors, frame } : [frame];
        // Commit's actual request schema cannot carry a repo_token; assert the input inventory obeys that fact.
        if (method == "Commit") Assert.All(frames, item => Assert.Null(item.Token));
        return (new(method, rights, owner, publication, frames, Registry.ToArray()),
            new(status, code, 1, frames.Length, forwarded, bindings, 0, 0, "fixture-public-heads", "fixture-public-heads", code));
    }
    /// <summary>Pure independent response scripts; no policy resolution or native implementation.</summary>
    /// <param name="expected">Literal fixture output.</param><param name="fault">Optional single wrong observation.</param>
    private sealed class ScriptedProbe(Phase0RepositoryObservation expected, string? fault = null) : IPhase0RepositoryProbe
    {
        /// <summary>Exact input instance observed.</summary>
        internal Phase0RepositoryCall? LastInput { get; private set; }
        /// <summary>Call count.</summary>
        internal int Calls { get; private set; }
        /// <summary>Fault reachability.</summary>
        internal bool Injected { get; private set; }
        /// <inheritdoc/>
        public Phase0RepositoryObservation? Evaluate(Phase0RepositoryCall input)
        {
            LastInput = input; Calls++; if (fault is null) return expected; Injected = true;
            return fault switch
            {
                "Missing" => null, "Status" => expected with { Status = expected.Status + 1 },
                "Code" => expected with { Code = "wrong" }, "Introspection" => expected with { Introspections = 0 },
                "Checks" => expected with { SelectorChecks = input.Frames.Count - 1 },
                "Backend" => expected with { BackendCalls = expected.BackendCalls + 1 },
                "Bindings" => expected with { Bindings = ["Atlas/other-repository/Public"] },
                "ExtraBinding" => expected with { Bindings = expected.Bindings.Append("Atlas/other/Upload").ToArray() },
                "ForeignObjects" => expected with { ForeignObjects = 1 }, "PublicMutation" => expected with { PublicMutations = 1 },
                "BeforeBlank" => expected with { Before = "" }, "After" => expected with { After = "changed" },
                "Disclosure" => expected with { PublicBody = expected.Code + ": secret repository exists" },
                _ => throw new InvalidOperationException("Unknown repository fault.")
            };
        }
    }
}
