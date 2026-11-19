namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>One exact dependency/image coordinate; synthetic mocks never establish adoption.</summary>
/// <param name="Role">Unique artifact role within its host.</param><param name="Version">Exact selected version.</param>
/// <param name="Sha256">Package, executable or immutable image digest bytes, in hexadecimal.</param>
/// <param name="License">Reviewed license identity.</param><param name="Source">Pinned artifact/source coordinate.</param>
/// <remarks>2026-11-18: Task001 B1; FR-MCP-186 ac-FourProvidersResolve; TR-MCP-FEDERATION-013; TEST-MCP-234.</remarks>
internal sealed record Phase0B1Artifact(string Role, string Version, string Sha256, string License, string Source);

/// <summary>Independent per-host B1 inventory observations.</summary>
/// <param name="Host">Lab host identity.</param><param name="Platform">Native OS identity.</param>
/// <param name="Artifacts">Complete dependency, executable and image inventory.</param>
/// <param name="FixtureVersions">All four provider fixture versions.</param>
/// <param name="NativeFacts">Native SCM/ACL or container/mount facts, gathered independently.</param>
/// <remarks>2026-11-18: Task001 B1/TR-MCP-FEDERATION-013; lifecycle uses the separate lifecycle seam.</remarks>
internal sealed record Phase0B1HostEvidence(string Host, string Platform,
    IReadOnlyList<Phase0B1Artifact>? Artifacts, IReadOnlyDictionary<string, string>? FixtureVersions,
    IReadOnlyDictionary<string, string>? NativeFacts);

/// <summary>Read-only observation boundary, later bound to disposable native inventory.</summary>
/// <remarks>2026-11-18: Task001 B1; observations must not be copied from requested configuration.</remarks>
internal interface IPhase0B1EvidenceProbe
{
    /// <summary>Reads distinct Windows and Linux host observations.</summary>
    /// <returns>Complete observed rows, or null for a missing-evidence control.</returns>
    IReadOnlyList<Phase0B1HostEvidence>? Read();
}

/// <summary>Exact B1 artifact/fixture/native-inventory assertions reused by mocks and later native binding.</summary>
/// <remarks>
/// 2026-11-18: FR-MCP-186 ac-FourProvidersResolve (supporting dependency evidence),
/// TR-MCP-FEDERATION-013 Ph0 B1 native-host/all-OS evidence; TEST-MCP-234; plan section5 B1.
/// These assertions establish evidence discrimination, not real dependency compatibility, ACL enforcement,
/// lifecycle, full criterion satisfaction or operator B1 acceptance. The expected policy is frozen before observation.
/// </remarks>
internal static class Phase0B1EvidenceContract
{
    /// <summary>Requires exact independent host, artifact, fixture and native-fact observations.</summary>
    /// <param name="expected">Independently reviewed fixture policy, never derived from the adapter's output.</param>
    /// <param name="probe">Independent scripted or later native observer.</param>
    internal static void AssertEvidence(IReadOnlyList<Phase0B1HostEvidence> expected, IPhase0B1EvidenceProbe probe)
    {
        // Freeze every mutable collection before the observer can mutate a shared cache.
        var frozen = expected.Select(host => host with
        {
            Artifacts = host.Artifacts!.ToArray(),
            FixtureVersions = host.FixtureVersions!.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            NativeFacts = host.NativeFacts!.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
        }).ToArray();
        Assert.Equal(new[] { "LAB-OMARCHY", "PAYTON-LEGION2" }, frozen.Select(host => host.Host).Order(StringComparer.Ordinal));
        var observed = probe.Read();
        Assert.NotNull(observed);
        Assert.Equal(frozen.Length, observed.Count);
        Assert.Equal(frozen.Select(host => host.Host).Order(StringComparer.Ordinal), observed.Select(host => host.Host).Order(StringComparer.Ordinal));
        foreach (var policy in frozen)
        {
            var row = Assert.Single(observed, host => host.Host == policy.Host);
            Assert.Equal(policy.Platform, row.Platform);
            Assert.NotEmpty(policy.Artifacts!);
            Assert.NotNull(row.Artifacts);
            Assert.Equal(policy.Artifacts!.Count, row.Artifacts.Count);
            Assert.Equal(policy.Artifacts.Select(artifact => artifact.Role).Order(StringComparer.Ordinal),
                row.Artifacts.Select(artifact => artifact.Role).Order(StringComparer.Ordinal));
            foreach (var pin in policy.Artifacts)
            {
                Assert.All(new[] { pin.Role, pin.Version, pin.License, pin.Source }, value => Assert.False(string.IsNullOrWhiteSpace(value)));
                Assert.Matches("^[0-9A-Fa-f]{64}$", pin.Sha256);
                var actual = Assert.Single(row.Artifacts, artifact => artifact.Role == pin.Role);
                Assert.Equal(pin.Version, actual.Version);
                Assert.Equal(pin.Sha256, actual.Sha256, ignoreCase: true);
                Assert.Equal(pin.License, actual.License);
                Assert.Equal(pin.Source, actual.Source);
            }
            Assert.Equal(new[] { "dolt", "postgresql", "sqlite", "sqlserver" }, policy.FixtureVersions!.Keys.Order(StringComparer.Ordinal));
            AssertFacts(policy.FixtureVersions, row.FixtureVersions);
            Assert.NotEmpty(policy.NativeFacts!);
            AssertFacts(policy.NativeFacts!, row.NativeFacts);
        }
    }

    /// <summary>Requires exact complete facts while accepting harmless enumeration-order variation.</summary>
    private static void AssertFacts(IReadOnlyDictionary<string, string> expected, IReadOnlyDictionary<string, string>? observed)
    {
        Assert.NotNull(observed);
        Assert.All(expected, pair => Assert.False(string.IsNullOrWhiteSpace(pair.Value)));
        Assert.Equal(expected.OrderBy(pair => pair.Key, StringComparer.Ordinal), observed.OrderBy(pair => pair.Key, StringComparer.Ordinal));
    }
}
