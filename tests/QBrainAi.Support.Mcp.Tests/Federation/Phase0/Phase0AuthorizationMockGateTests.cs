namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Validates authorization assertions using independent scripted fixtures and deliberate faults.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-003/005; FR-MCP-185 ac-RawRemoteCannotBypassPublicationPolicy,
/// ac-ForeignProjectCloneDenied, ac-RevokedNodeCannotPublish, ac-LocalAppUserCannotPush,
/// ac-RevokedCredentialFailsFetchAndPublish, ac-OfflineLocalAuthorizationStillApplies;
/// TR-MCP-FEDERATION-012; TEST-MCP-233. Mock-only evidence, never identity/network/runtime acceptance.
/// </remarks>
public sealed class Phase0AuthorizationMockGateTests
{
    /// <summary>Gets all contract-derived given/when cases, independent of the assertion implementation.</summary>
    public static IEnumerable<object[]> Scenarios => ScenarioNames.Select(name => new object[] { name });

    /// <summary>Gets every independently asserted state, response and identity-check corruption.</summary>
    public static IEnumerable<object[]> Faults
    {
        get
        {
            foreach (var name in ScenarioNames)
            {
                foreach (var checkpoint in new[] { "Before", "After" })
                foreach (var corruptField in new[] { "Missing", "Head", "Title", "Client", "Grants" })
                    yield return [name, checkpoint + ":" + corruptField];
                foreach (var corruptField in new[] { "Transferred", "Published" })
                    yield return [name, "Before:" + corruptField];
                foreach (var fault in new[] { "MissingOutcome", "Kind", "Status", "ChecksExtra" })
                    yield return [name, fault];
                if (name is not ("PrivateBackend" or "SqlAdmin" or "SqlPublicWrite" or "OfflineAuthorized" or "OfflineUnauthorized"))
                {
                    yield return [name, "Error"];
                    yield return [name, "ChecksMissing"];
                    yield return [name, "ChecksWrong"];
                }
                if (name == "RevokedAtCommit")
                {
                    yield return [name, "ChecksReordered"];
                    yield return [name, "CommitRecheckMissing"];
                }
                if (name is "ForeignFetch" or "RevokedFetch" or "RevokedPublish")
                    yield return [name, "After:Transferred"];
                if (name is "SqlAdmin" or "SqlPublicWrite" or "RevokedFetch" or "RevokedPublish")
                    yield return [name, "After:Published"];
            }
        }
    }

    /// <summary>Correct denial/local-save outcomes pass and exercise arrange, attempt and both readbacks.</summary>
    /// <param name="scenario">Canonical given/when case.</param>
    [Theory]
    [MemberData(nameof(Scenarios))]
    public void CorrectAuthorizationOutcomePasses(string scenario)
    {
        var probe = new ScriptedProbe(scenario);
        Phase0AuthorizationContract.AssertScenario(probe, scenario);
        Assert.Equal(new[] { "prepare", "read-before", "attempt", "read-after" }, probe.Calls);
    }

    /// <summary>Every deliberate corruption must be rejected by the same assertions used for real binding.</summary>
    /// <param name="scenario">Canonical given/when case.</param>
    /// <param name="fault">Single corrupted boundary or observation.</param>
    [Theory]
    [MemberData(nameof(Faults))]
    public void WrongAuthorizationOutcomeIsRejected(string scenario, string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            Phase0AuthorizationContract.AssertScenario(new ScriptedProbe(scenario, fault), scenario));

    /// <summary>Commit-time revocation may follow object upload, and unspecified local error details are allowed.</summary>
    [Fact]
    public void DoesNotInventUnspecifiedAcceptanceCriteria()
    {
        Phase0AuthorizationContract.AssertScenario(new ScriptedProbe("RevokedAtCommit", "AllowedUpload"), "RevokedAtCommit");
        Phase0AuthorizationContract.AssertScenario(new ScriptedProbe("OfflineUnauthorized", "AllowedErrorDetail"), "OfflineUnauthorized");
    }

    /// <summary>Stable fixture names; the production assertion source is not used to generate mocks.</summary>
    private static readonly string[] ScenarioNames =
    [
        "PrivateBackend", "ReadOnlyRawPush", "PublisherRawPush", "ForeignFetch", "RevokedAccept",
        "SqlAdmin", "SqlPublicWrite", "RevokedFetch", "RevokedPublish", "RevokedAtCommit",
        "IdentityUnavailableFetch", "IdentityUnavailablePublish", "OfflineAuthorized", "OfflineUnauthorized"
    ];

    /// <summary>Independent scripted responses; no production authorization or transport logic is implemented.</summary>
    /// <param name="scenario">Fixture scenario.</param>
    /// <param name="fault">Optional single observation corruption.</param>
    private sealed class ScriptedProbe(string scenario, string fault = "") : IPhase0AuthorizationProbe
    {
        private bool attempted;

        /// <summary>Actual fixture-boundary calls, checked by the positive controls.</summary>
        internal List<string> Calls { get; } = [];

        /// <inheritdoc />
        public void Prepare()
        {
            Calls.Add("prepare");
            attempted = false;
        }

        /// <inheritdoc />
        public Phase0AuthorizationState? ReadState()
        {
            Calls.Add(attempted ? "read-after" : "read-before");
            var revoked = attempted && scenario is "RevokedAccept" or "RevokedFetch" or "RevokedPublish" or "RevokedAtCommit";
            var state = new Phase0AuthorizationState("C0", 0, 0,
                attempted && scenario == "OfflineAuthorized" ? "edited" : "original", !revoked, revoked ? 0 : 1);
            if (attempted && fault == "AllowedUpload") return state with { TransferredObjects = 2 };
            var prefix = attempted ? "After:" : "Before:";
            if (!fault.StartsWith(prefix, StringComparison.Ordinal)) return state;
            return fault[prefix.Length..] switch
            {
                "Missing" => null,
                "Head" => state with { RemoteHead = "C1" },
                "Title" => state with { LocalTitle = "wrong" },
                "Client" => state with { ClientEnabled = !state.ClientEnabled },
                "Grants" => state with { PersistedReferenceGrants = state.PersistedReferenceGrants + 1 },
                "Transferred" => state with { TransferredObjects = 1 },
                "Published" => state with { PublishedObjects = 1 },
                _ => throw new InvalidOperationException("Unknown state fault: " + fault)
            };
        }

        /// <inheritdoc />
        public Phase0AuthorizationOutcome? Attempt(Phase0AuthorizationAttempt attempt)
        {
            Calls.Add("attempt");
            Assert.Equal(RequestFixture(scenario), attempt);
            attempted = true;
            var outcome = OutcomeFixture(scenario);
            return fault switch
            {
                "MissingOutcome" => null,
                "Kind" => outcome with { Kind = "Allowed" },
                "Status" => outcome with { Status = 200 },
                "Error" => outcome with { Error = "wrong_error" },
                "ChecksExtra" => outcome with { IdentityChecks = [..outcome.IdentityChecks, "unexpected:active"] },
                "ChecksMissing" => outcome with { IdentityChecks = [] },
                "ChecksWrong" => outcome with { IdentityChecks = ["request:wrong"] },
                "ChecksReordered" => outcome with { IdentityChecks = ["commit:inactive", "request:active"] },
                "CommitRecheckMissing" => outcome with { IdentityChecks = ["request:active"] },
                "AllowedErrorDetail" => outcome with { Error = "local_access_denied" },
                _ => outcome
            };
        }

        /// <summary>Independent explicit fixture inputs verify that assertions attempt the correct operation.</summary>
        /// <param name="name">Given/when scenario.</param>
        /// <returns>The expected fixture input.</returns>
        private static Phase0AuthorizationAttempt RequestFixture(string name) => name switch
        {
            "PrivateBackend" => new("private-connect", "Atlas", "Atlas.publish", true, "None"),
            "ReadOnlyRawPush" => new("raw-push", "Atlas", "Atlas.read", true, "None"),
            "PublisherRawPush" => new("raw-push", "Atlas", "Atlas.publish", true, "None"),
            "ForeignFetch" => new("fetch", "SecretProject", "Atlas.read", true, "None"),
            "RevokedAccept" => new("accept", "Atlas", "Atlas.publish", true, "BeforeRequest"),
            "SqlAdmin" => new("sql-admin", "Atlas", "local-editor", true, "None"),
            "SqlPublicWrite" => new("sql-public-write", "Atlas", "local-editor", true, "None"),
            "RevokedFetch" => new("fetch", "Atlas", "Atlas.read", true, "BeforeRequest"),
            "RevokedPublish" => new("publish", "Atlas", "Atlas.publish", true, "BeforeRequest"),
            "RevokedAtCommit" => new("accept", "Atlas", "Atlas.publish", true, "BeforeCommit"),
            "IdentityUnavailableFetch" => new("fetch", "Atlas", "Atlas.read", false, "None"),
            "IdentityUnavailablePublish" => new("publish", "Atlas", "Atlas.publish", false, "None"),
            "OfflineAuthorized" => new("local-edit", "Atlas", "local-editor", false, "None"),
            "OfflineUnauthorized" => new("local-edit", "Atlas", "local-denied", false, "None"),
            _ => throw new ArgumentOutOfRangeException(nameof(name))
        };

        /// <summary>Independent protocol fixtures pinned to FR185 and TR012, never copied from runtime output.</summary>
        /// <param name="name">Given/when scenario.</param>
        /// <returns>The scripted protocol result.</returns>
        private static Phase0AuthorizationOutcome OutcomeFixture(string name) => name switch
        {
            "PrivateBackend" => new("NetworkFailure", null, null, Array.Empty<string>()),
            "ReadOnlyRawPush" => new("Http", 403, "unauthorized_project", new[] { "request:active" }),
            "PublisherRawPush" => new("Http", 409, "publication_required", new[] { "request:active" }),
            "ForeignFetch" => new("Http", 403, "unauthorized_project", new[] { "request:active" }),
            "RevokedAccept" => new("Http", 401, "invalid_token", new[] { "request:inactive" }),
            "SqlAdmin" => new("SqlPermissionDenied", null, null, Array.Empty<string>()),
            "SqlPublicWrite" => new("SqlPermissionDenied", null, null, Array.Empty<string>()),
            "RevokedFetch" => new("Http", 401, "invalid_token", new[] { "request:inactive" }),
            "RevokedPublish" => new("Http", 401, "invalid_token", new[] { "request:inactive" }),
            "RevokedAtCommit" => new("Http", 401, "invalid_token", new[] { "request:active", "commit:inactive" }),
            "IdentityUnavailableFetch" => new("Http", 503, "identity_unavailable", new[] { "request:unavailable" }),
            "IdentityUnavailablePublish" => new("Http", 503, "identity_unavailable", new[] { "request:unavailable" }),
            "OfflineAuthorized" => new("LocalSaved", null, null, Array.Empty<string>()),
            "OfflineUnauthorized" => new("Http", 403, null, Array.Empty<string>()),
            _ => throw new ArgumentOutOfRangeException(nameof(name))
        };
    }
}
