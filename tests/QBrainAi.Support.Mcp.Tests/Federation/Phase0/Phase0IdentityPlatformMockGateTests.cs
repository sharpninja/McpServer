namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent scripts discriminate exact-platform, issuance, restart and revocation assertion failures.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-005; B5; FR-MCP-185 ac-RevokedNodeCannotPublish;
/// TR-MCP-FEDERATION-012; TEST-MCP-233. No real issuer, credentials, database or production adapter exists here.
/// 2026-11-19: Current canonical scope plus abbreviated-scope negative controls at every active introspection.
/// </remarks>
public sealed class Phase0IdentityPlatformMockGateTests
{
    /// <summary>Valid durable lifecycle preserves B and local work; either standard client-auth failure status is accepted.</summary>
    /// <param name="disabledStatus">Native OAuth client-authentication refusal status.</param>
    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    public void CorrectIdentityPlatformPasses(int disabledStatus)
    {
        var probe = new PlatformProbe(disabledStatus: disabledStatus);
        Phase0IdentityPlatformContract.AssertRevocationLifecycle(probe);
        Assert.Equal(5, probe.StateReads);
        Assert.Equal(3, probe.Issues);
        Assert.Equal(8, probe.Introspections);
        Assert.Equal(2, probe.Restarts);
        Assert.Equal(1, probe.Revocations);
        Assert.Equal(1, probe.Publications);
    }

    /// <summary>Gets faults at every independent state, issuance and introspection checkpoint.</summary>
    public static IEnumerable<object[]> PlatformFaults
    {
        get
        {
            for (var checkpoint = 0; checkpoint < 5; checkpoint++)
            {
                foreach (var facet in new[] { "Missing", "PackagesMissing", "StoresMissing", "NodeAEnabled", "NodeAGrants", "NodeBEnabled", "NodeBGrants", "PublicHead", "LocalTitle" })
                    yield return ["State:" + checkpoint + ":" + facet];
                foreach (var package in PackageIds)
                foreach (var facet in new[] { "Missing", "Version" })
                    yield return ["State:" + checkpoint + ":Package:" + package + ":" + facet];
                foreach (var store in new[] { "Client", "Grant" })
                foreach (var facet in new[] { "Missing", "Type" })
                    yield return ["State:" + checkpoint + ":Store:" + store + ":" + facet];
            }
            for (var checkpoint = 0; checkpoint < 2; checkpoint++)
            foreach (var facet in new[] { "Missing", "Status", "Error", "Type" })
                yield return ["Issue:" + checkpoint + ":" + facet];
            foreach (var facet in new[] { "Missing", "Status", "Error" })
                yield return ["Issue:2:" + facet];
            for (var checkpoint = 0; checkpoint < 8; checkpoint++)
            {
                foreach (var facet in new[] { "Missing", "Status", "Active" })
                    yield return ["Introspect:" + checkpoint + ":" + facet];
                if (checkpoint is 4 or 6) continue;
                foreach (var facet in new[] { "ClientId", "Audience", "Project", "Owner", "ScopesMissing", "ScopeWrong", "ScopeExtra", "ScopeAbbreviated" })
                    yield return ["Introspect:" + checkpoint + ":" + facet];
            }
            foreach (var facet in new[] { "Missing", "Status", "Code" })
                yield return ["Publish:" + facet];
        }
    }

    /// <summary>Every corrupt observation must produce an xUnit assertion failure, never count an adapter exception as rejection.</summary>
    /// <param name="fault">Single independent corruption with its checkpoint.</param>
    [Theory, MemberData(nameof(PlatformFaults))]
    public void WrongPlatformOutcomeIsRejected(string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            Phase0IdentityPlatformContract.AssertRevocationLifecycle(new PlatformProbe(fault)));

    private static readonly string[] PackageIds = ["Duende.IdentityServer", "Duende.IdentityServer.Storage", "Duende.IdentityServer.AspNetIdentity", "Duende.IdentityServer.EntityFramework", "Duende.IdentityServer.EntityFramework.Storage"];

    /// <summary>Independent source-pinned observations; no real secrets are generated or exposed by these scripted responses.</summary>
    private sealed class PlatformProbe(string fault = "", int disabledStatus = 401) : IPhase0IdentityPlatformProbe
    {
        private bool issued;
        private bool revoked;
        /// <summary>Counts independent platform-state reads.</summary>
        internal int StateReads { get; private set; }
        /// <summary>Counts issuance requests.</summary>
        internal int Issues { get; private set; }
        /// <summary>Counts actual calls for the same private fixture token aliases.</summary>
        internal int Introspections { get; private set; }
        /// <summary>Counts issuer restart requests.</summary>
        internal int Restarts { get; private set; }
        /// <summary>Counts explicit administrative revocations.</summary>
        internal int Revocations { get; private set; }
        /// <summary>Counts rejected publication attempts.</summary>
        internal int Publications { get; private set; }
        /// <inheritdoc />
        public void Prepare() { issued = false; revoked = false; }
        /// <inheritdoc />
        public Phase0IdentityPlatformState? ReadState()
        {
            var prefix = "State:" + StateReads++ + ":";
            var packages = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Duende.IdentityServer"] = "7.4.7", ["Duende.IdentityServer.Storage"] = "7.4.7",
                ["Duende.IdentityServer.AspNetIdentity"] = "7.4.6", ["Duende.IdentityServer.EntityFramework"] = "7.4.6",
                ["Duende.IdentityServer.EntityFramework.Storage"] = "7.4.6"
            };
            var stores = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Client"] = "Duende.IdentityServer.EntityFramework.Stores.ClientStore",
                ["Grant"] = "Duende.IdentityServer.EntityFramework.Stores.PersistedGrantStore"
            };
            var state = new Phase0IdentityPlatformState(packages, stores, !revoked, issued && !revoked ? 1 : 0,
                true, issued ? 1 : 0, "C0", "offline draft café 雪");
            if (!fault.StartsWith(prefix, StringComparison.Ordinal)) return state;
            var facet = fault[prefix.Length..];
            if (facet.StartsWith("Package:", StringComparison.Ordinal))
            {
                var parts = facet.Split(':');
                if (parts[2] == "Missing") packages.Remove(parts[1]); else packages[parts[1]] = "8.0.0";
                return state;
            }
            if (facet.StartsWith("Store:", StringComparison.Ordinal))
            {
                var parts = facet.Split(':');
                if (parts[2] == "Missing") stores.Remove(parts[1]); else stores[parts[1]] = "InMemoryStore";
                return state;
            }
            return facet switch
            {
                "Missing" => null, "PackagesMissing" => state with { Packages = null },
                "StoresMissing" => state with { Stores = null },
                "NodeAEnabled" => state with { NodeAEnabled = !state.NodeAEnabled },
                "NodeAGrants" => state with { NodeAGrants = state.NodeAGrants + 1 },
                "NodeBEnabled" => state with { NodeBEnabled = false },
                "NodeBGrants" => state with { NodeBGrants = state.NodeBGrants + 1 },
                "PublicHead" => state with { PublicHead = "A1" },
                "LocalTitle" => state with { LocalTitle = "lost draft" }, _ => throw new InvalidOperationException(facet)
            };
        }
        /// <inheritdoc />
        public Phase0NodeTokenIssue? Issue(string node)
        {
            var checkpoint = Issues++;
            Assert.Equal(checkpoint == 1 ? "B" : "A", node);
            if (checkpoint == 1) issued = true;
            var reply = checkpoint == 2 ? new Phase0NodeTokenIssue(disabledStatus, "invalid_client", null) : new(200, null, "Reference");
            var prefix = "Issue:" + checkpoint + ":";
            if (!fault.StartsWith(prefix, StringComparison.Ordinal)) return reply;
            return fault[prefix.Length..] switch
            {
                "Missing" => null, "Status" => reply with { Status = checkpoint == 2 ? 200 : 500 },
                "Error" => reply with { Error = checkpoint == 2 ? null : "invalid_client" },
                "Type" => reply with { AccessTokenType = "Jwt" }, _ => throw new InvalidOperationException(fault)
            };
        }
        /// <inheritdoc />
        public Phase0NodeIntrospection? Introspect(string node)
        {
            var checkpoint = Introspections++;
            Assert.Equal(checkpoint % 2 == 0 ? "A" : "B", node);
            var reply = new Phase0NodeIntrospection(200, node != "A" || !revoked, "fed.node." + node,
                "qbrain-federation", "Atlas", "owner-" + node, ["fed.project.Atlas.publish"]);
            var prefix = "Introspect:" + checkpoint + ":";
            if (!fault.StartsWith(prefix, StringComparison.Ordinal)) return reply;
            return fault[prefix.Length..] switch
            {
                "Missing" => null, "Status" => reply with { Status = 503 },
                "Active" => reply with { Active = !reply.Active }, "ClientId" => reply with { ClientId = "foreign" },
                "Audience" => reply with { Audience = "foreign" }, "Project" => reply with { Project = "SecretProject" },
                "Owner" => reply with { Owner = "foreign" }, "ScopesMissing" => reply with { Scopes = null },
                "ScopeWrong" => reply with { Scopes = ["fed.project.Atlas.read"] },
                "ScopeExtra" => reply with { Scopes = ["fed.project.Atlas.publish", "fed.project.SecretProject.publish"] },
                "ScopeAbbreviated" => reply with { Scopes = ["Atlas.publish"] },
                _ => throw new InvalidOperationException(fault)
            };
        }
        /// <inheritdoc />
        public void RestartIssuer() => Restarts++;
        /// <inheritdoc />
        public void RevokeNodeA() { Revocations++; revoked = true; }
        /// <inheritdoc />
        public Phase0IdentityPublishReply? PublishNodeA()
        {
            Publications++;
            return fault switch
            {
                "Publish:Missing" => null, "Publish:Status" => new(200, "invalid_token"),
                "Publish:Code" => new(401, "wrong"), _ => new(401, "invalid_token")
            };
        }
    }
}
