namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent B1 inventory fixtures prove exact pin, host and native-fact assertions discriminate.</summary>
/// <remarks>
/// 2026-11-18: Task001 B1; FR-MCP-186 ac-FourProvidersResolve; TR-MCP-FEDERATION-013 Ph0 B1; TEST-MCP-234.
/// Every version/hash/license/coordinate below is synthetic test data. No dependency is adopted and no OS is queried.
/// Policy and observer literals are deliberately independent; negative controls are grouped by artifact/provider/host,
/// retaining the rejected fault's identity without one launcher argument per scalar mutation.
/// </remarks>
public sealed class Phase0B1EvidenceMockGateTests
{
    /// <summary>Accepts exact independent observations and harmless host/collection order and hash-case differences.</summary>
    /// <param name="reordered">Whether to permute all unordered inventories.</param>
    [Theory, InlineData(false), InlineData(true)]
    public void CorrectCompleteB1EvidencePasses(bool reordered)
    {
        var rows = Observations();
        if (reordered)
            rows = rows.Reverse().Select(row => row with
            {
                Artifacts = row.Artifacts!.Reverse().Select(pin => pin with { Sha256 = pin.Sha256.ToLowerInvariant() }).ToArray(),
                FixtureVersions = row.FixtureVersions!.Reverse().ToDictionary(pair => pair.Key, pair => pair.Value),
                NativeFacts = row.NativeFacts!.Reverse().ToDictionary(pair => pair.Key, pair => pair.Value),
            }).ToArray();
        Phase0B1EvidenceContract.AssertEvidence(Policy(), new Probe(() => rows));
    }

    /// <summary>Each host/artifact is independently attacked for role, version, digest, license, source and omission.</summary>
    public static IEnumerable<object[]> ArtifactCases => Policy().SelectMany(host => host.Artifacts!.Select(pin => new object[] { host.Host, pin.Role }));

    /// <summary>Every four-provider fixture is independently attacked on each host.</summary>
    public static IEnumerable<object[]> FixtureCases => Policy().SelectMany(host => host.FixtureVersions!.Keys.Select(provider => new object[] { host.Host, provider }));

    /// <summary>Rejects one wrong field at a time through the same full B1 assertion path.</summary>
    /// <param name="host">Required native host.</param><param name="role">Required artifact role.</param>
    [Theory, MemberData(nameof(ArtifactCases))]
    public void WrongArtifactEvidenceIsRejected(string host, string role)
    {
        foreach (var fault in new[] { "missing", "role", "version", "hash", "hash-empty", "license", "source" })
            Reject(host + "/" + role + "/" + fault, () => ChangeHost(host, row => row with
            {
                Artifacts = row.Artifacts!.Where(pin => fault != "missing" || pin.Role != role).Select(pin => pin.Role != role ? pin : fault switch
                {
                    "role" => pin with { Role = "foreign" }, "version" => pin with { Version = "wrong" },
                    "hash" => pin with { Sha256 = new string('0', 64) }, "hash-empty" => pin with { Sha256 = "" },
                    "license" => pin with { License = "unreviewed" }, "source" => pin with { Source = "foreign" }, _ => pin,
                }).ToArray(),
            }));
    }

    /// <summary>Rejects missing, wrong and empty provider fixture versions on both required hosts.</summary>
    /// <param name="host">Required native host.</param><param name="provider">One canonical fixture provider.</param>
    [Theory, MemberData(nameof(FixtureCases))]
    public void WrongFixtureEvidenceIsRejected(string host, string provider)
    {
        foreach (var fault in new[] { "missing", "wrong", "empty" })
            Reject(host + "/fixture/" + provider + "/" + fault, () => ChangeHost(host, row =>
            {
                var versions = row.FixtureVersions!.ToDictionary(pair => pair.Key, pair => pair.Value);
                if (fault == "missing") versions.Remove(provider); else versions[provider] = fault == "empty" ? "" : "wrong";
                return row with { FixtureVersions = versions };
            }));
    }

    /// <summary>Rejects missing/duplicate hosts, incomplete inventories and every native SCM/ACL/mount fact.</summary>
    /// <param name="host">Required host whose facts are independently corrupted.</param>
    [Theory, InlineData("PAYTON-LEGION2"), InlineData("LAB-OMARCHY")]
    public void WrongHostEvidenceIsRejected(string host)
    {
        Reject(host + "/missing-report", () => null);
        Reject(host + "/missing-host", () => Observations().Where(row => row.Host != host).ToArray());
        Reject(host + "/duplicate-host", () => Observations().Select(row => row.Host == host ? row with { Host = host == "PAYTON-LEGION2" ? "LAB-OMARCHY" : "PAYTON-LEGION2" } : row).ToArray());
        Reject(host + "/extra-host", () => Observations().Concat([Observations()[0] with { Host = "unrelated" }]).ToArray());
        foreach (var fault in new[] { "platform", "artifacts-null", "artifact-duplicate", "artifact-extra", "fixtures-null", "fixture-extra", "facts-null", "fact-extra" })
            Reject(host + "/" + fault, () => ChangeHost(host, row => fault switch
            {
                "platform" => row with { Platform = "foreign-os" }, "artifacts-null" => row with { Artifacts = null },
                "artifact-duplicate" => row with { Artifacts = row.Artifacts!.Select((pin, index) => index == 1 ? row.Artifacts![0] : pin).ToArray() },
                "artifact-extra" => row with { Artifacts = row.Artifacts!.Concat([row.Artifacts![0] with { Role = "extra" }]).ToArray() },
                "fixtures-null" => row with { FixtureVersions = null }, "fixture-extra" => row with { FixtureVersions = Extra(row.FixtureVersions!) },
                "facts-null" => row with { NativeFacts = null }, "fact-extra" => row with { NativeFacts = Extra(row.NativeFacts!) },
                _ => throw new InvalidOperationException("Unknown fixture fault"),
            }));
        foreach (var key in Policy().Single(row => row.Host == host).NativeFacts!.Keys)
        foreach (var fault in new[] { "missing", "wrong", "empty" })
            Reject(host + "/native/" + key + "/" + fault, () => ChangeHost(host, row =>
            {
                var facts = row.NativeFacts!.ToDictionary(pair => pair.Key, pair => pair.Value);
                if (fault == "missing") facts.Remove(key); else facts[key] = fault == "empty" ? "" : "wrong";
                return row with { NativeFacts = facts };
            }));
    }

    /// <summary>A mutable observer cache cannot rewrite expected policy to make a wrong observation pass.</summary>
    /// <param name="collection">Artifact, fixture or native-fact collection.</param>
    [Theory, InlineData(0), InlineData(1), InlineData(2)]
    public void ObserverCannotRewriteFrozenExpectation(int collection)
    {
        var expected = Policy();
        Reject("shared-policy/" + collection, () =>
        {
            var shared = expected[0];
            var changed = collection switch
            {
                0 => shared with { Artifacts = ReplaceSharedArtifact(shared.Artifacts!) },
                1 => shared with { FixtureVersions = ReplaceSharedFact(shared.FixtureVersions!) },
                _ => shared with { NativeFacts = ReplaceSharedFact(shared.NativeFacts!) },
            };
            return Observations().Select(row => row.Host == shared.Host ? changed : row).ToArray();
        }, expected);
    }

    /// <summary>Creates an extra unexpected fact.</summary>
    private static Dictionary<string, string> Extra(IReadOnlyDictionary<string, string> source)
    { var copy = source.ToDictionary(pair => pair.Key, pair => pair.Value); copy.Add("extra", "unexpected"); return copy; }
    /// <summary>Mutates an intentionally shared fixture to test the pre-read snapshot.</summary>
    private static IReadOnlyDictionary<string, string> ReplaceSharedFact(IReadOnlyDictionary<string, string> source)
    { ((Dictionary<string, string>)source)[source.Keys.First()] = "wrong"; return source; }
    /// <summary>Mutates an intentionally shared artifact array.</summary>
    private static IReadOnlyList<Phase0B1Artifact> ReplaceSharedArtifact(IReadOnlyList<Phase0B1Artifact> source)
    { ((Phase0B1Artifact[])source)[0] = source[0] with { Version = "wrong" }; return source; }
    /// <summary>Changes one independent host response.</summary>
    private static Phase0B1HostEvidence[] ChangeHost(string host, Func<Phase0B1HostEvidence, Phase0B1HostEvidence> change)
        => Observations().Select(row => row.Host == host ? change(row) : row).ToArray();
    /// <summary>Requires an assertion failure and preserves the fault identity in a failed control.</summary>
    private static void Reject(string fault, Func<IReadOnlyList<Phase0B1HostEvidence>?> read, Phase0B1HostEvidence[]? expected = null)
    {
        var error = Record.Exception(() => Phase0B1EvidenceContract.AssertEvidence(expected ?? Policy(), new Probe(read)));
        Assert.True(error is Xunit.Sdk.XunitException, "B1 negative control was not rejected by an assertion: " + fault);
    }
    /// <summary>Independent expected policy; intentionally synthetic, not a dependency lock file.</summary>
    private static Phase0B1HostEvidence[] Policy() =>
    [
        new("PAYTON-LEGION2", "Windows", ExpectedArtifacts(true), ExpectedVersions(), new Dictionary<string, string>
        {
            ["serviceName"] = "QBrainDoltPh0FixtureA", ["account"] = "NT AUTHORITY/LocalService", ["startMode"] = "Manual",
            ["executablePath"] = "F:/fixture/tools/WinSW.exe", ["doltPath"] = "F:/fixture/tools/dolt.exe", ["dataRoot"] = "F:/fixture/A",
            ["serviceSidEnabled"] = "true", ["aclProtected"] = "true", ["acl:serviceSid"] = "Modify", ["acl:SYSTEM"] = "FullControl",
            ["acl:Administrators"] = "FullControl", ["listener"] = "127.0.0.1", ["recoveryMs"] = "10000,20000,60000", ["stopTimeoutMs"] = "60000",
        }),
        new("LAB-OMARCHY", "Linux", ExpectedArtifacts(false), ExpectedVersions(), new Dictionary<string, string>
        {
            ["containerName"] = "QBrainDoltPh0FixtureA", ["mountSource"] = "/fixture/A", ["mountTarget"] = "/var/lib/dolt",
            ["mountReadWrite"] = "true", ["listener"] = "127.0.0.1",
        }),
    ];
    /// <summary>Expected synthetic artifact inventory includes common packages and OS-specific executables/images.</summary>
    private static Phase0B1Artifact[] ExpectedArtifacts(bool windows)
    {
        var roles = windows ? new[] { "efcore", "provider", "driver", "dolt-native", "winsw", "postgres-image", "sqlserver-image", "sqlite-native" }
            : new[] { "efcore", "provider", "driver", "dolt-image", "postgres-image", "sqlserver-image", "sqlite-native" };
        return roles.Select(role => new Phase0B1Artifact(role, "fixture-v1", new string('A', 64), "fixture-license", "fixture-source/" + role)).ToArray();
    }
    /// <summary>Expected independent versions for all four canonical fixture providers.</summary>
    private static Dictionary<string, string> ExpectedVersions() => new()
    { ["sqlite"] = "fixture-sqlite-1", ["postgresql"] = "fixture-postgres-1", ["sqlserver"] = "fixture-sqlserver-1", ["dolt"] = "fixture-dolt-1" };

    /// <summary>Independent observer literals, never derived from Policy or its expected dictionaries.</summary>
    private static Phase0B1HostEvidence[] Observations() =>
    [
        new("PAYTON-LEGION2", "Windows",
        [ObservedArtifact("efcore"), ObservedArtifact("provider"), ObservedArtifact("driver"), ObservedArtifact("dolt-native"), ObservedArtifact("winsw"), ObservedArtifact("postgres-image"), ObservedArtifact("sqlserver-image"), ObservedArtifact("sqlite-native")],
        ObservedVersions(), new Dictionary<string, string>
        {
            ["account"] = "NT AUTHORITY/LocalService", ["serviceName"] = "QBrainDoltPh0FixtureA", ["startMode"] = "Manual",
            ["dataRoot"] = "F:/fixture/A", ["doltPath"] = "F:/fixture/tools/dolt.exe", ["executablePath"] = "F:/fixture/tools/WinSW.exe",
            ["acl:SYSTEM"] = "FullControl", ["acl:Administrators"] = "FullControl", ["acl:serviceSid"] = "Modify", ["aclProtected"] = "true",
            ["serviceSidEnabled"] = "true", ["stopTimeoutMs"] = "60000", ["recoveryMs"] = "10000,20000,60000", ["listener"] = "127.0.0.1",
        }),
        new("LAB-OMARCHY", "Linux",
        [ObservedArtifact("efcore"), ObservedArtifact("provider"), ObservedArtifact("driver"), ObservedArtifact("dolt-image"), ObservedArtifact("postgres-image"), ObservedArtifact("sqlserver-image"), ObservedArtifact("sqlite-native")],
        ObservedVersions(), new Dictionary<string, string>
        {
            ["mountTarget"] = "/var/lib/dolt", ["mountReadWrite"] = "true", ["mountSource"] = "/fixture/A",
            ["containerName"] = "QBrainDoltPh0FixtureA", ["listener"] = "127.0.0.1",
        }),
    ];
    /// <summary>Independent observed pin literals.</summary>
    private static Phase0B1Artifact ObservedArtifact(string role)
        => new(role, "fixture-v1", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "fixture-license", "fixture-source/" + role);
    /// <summary>Independent observer provider-version literals.</summary>
    private static Dictionary<string, string> ObservedVersions() => new()
    { ["dolt"] = "fixture-dolt-1", ["sqlite"] = "fixture-sqlite-1", ["sqlserver"] = "fixture-sqlserver-1", ["postgresql"] = "fixture-postgres-1" };
    /// <summary>Only supplies scripted evidence; it implements no dependency or OS behavior.</summary>
    private sealed class Probe(Func<IReadOnlyList<Phase0B1HostEvidence>?> read) : IPhase0B1EvidenceProbe
    { /// <inheritdoc />
      public IReadOnlyList<Phase0B1HostEvidence>? Read() => read(); }
}
