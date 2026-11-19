namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Explicit inputs to one disposable authorization attempt.</summary>
/// <param name="Operation">Connection, fetch, raw push, publication acceptance, SQL or local edit operation.</param>
/// <param name="Project">Requested project, including a foreign-project fixture.</param>
/// <param name="Credential">Fixture credential identifier, never an actual secret.</param>
/// <param name="IdentityAvailable">Whether the fixture identity service is reachable.</param>
/// <param name="RevocationPoint">None, BeforeRequest or BeforeCommit; the adapter injects this fixture event.</param>
/// <remarks>2026-11-17: FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233. Test-only design seam.</remarks>
internal sealed record Phase0AuthorizationAttempt(string Operation, string Project, string Credential,
    bool IdentityAvailable = true, string RevocationPoint = "None");

/// <summary>Independent readbacks from the fixture after an operation.</summary>
/// <param name="RemoteHead">Observed remote/main ref.</param>
/// <param name="TransferredObjects">Objects transferred during the fixture sequence.</param>
/// <param name="PublishedObjects">Objects published during the fixture sequence.</param>
/// <param name="LocalTitle">Actual local TODO row value.</param>
/// <param name="ClientEnabled">IdentityServer client enabled state.</param>
/// <param name="PersistedReferenceGrants">Remaining reference-token grants for the fixture node.</param>
/// <remarks>2026-11-17: FR-MCP-185 ac-RawRemoteCannotBypassPublicationPolicy, ac-ForeignProjectCloneDenied,
/// ac-RevokedNodeCannotPublish, ac-LocalAppUserCannotPush, ac-RevokedCredentialFailsFetchAndPublish,
/// ac-OfflineLocalAuthorizationStillApplies; TEST-MCP-233.</remarks>
internal sealed record Phase0AuthorizationState(string RemoteHead, int TransferredObjects, int PublishedObjects,
    string LocalTitle, bool ClientEnabled, int PersistedReferenceGrants);

/// <summary>Returned protocol outcome and ordered identity observations for the attempted request.</summary>
/// <param name="Kind">NetworkFailure, Http, SqlPermissionDenied or LocalSaved.</param>
/// <param name="Status">HTTP status when the boundary is HTTP; otherwise absent.</param>
/// <param name="Error">Contract error code when specified; otherwise absent.</param>
/// <param name="IdentityChecks">Observed request/commit introspection outcomes, not configured intentions.</param>
/// <remarks>2026-11-17: FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233.</remarks>
internal sealed record Phase0AuthorizationOutcome(string Kind, int? Status, string? Error,
    IReadOnlyList<string> IdentityChecks);

/// <summary>Arrange, attempt and independently inspect a disposable authorization fixture.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-003/005; FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233.
/// A later real adapter must provision node A, owner-A, client fed.node.A, reference grants,
/// audience qbrain-federation, Atlas project scopes and explicit project membership/branch rights.
/// Atlas.read lacks publish; Atlas.publish has publish permission. SecretProject has no grant.
/// local-editor has ordinary editing SQL rights and authorized local TODO access; local-denied does not.
/// Prepare establishes remote/main C0 and local TODO title original. Revoke disables the client and
/// removes persisted reference grants at the requested fixture boundary. No production adapter exists here.
/// </remarks>
internal interface IPhase0AuthorizationProbe
{
    /// <summary>Resets the isolated fixture to the documented baseline with identity grants active.</summary>
    void Prepare();

    /// <summary>Reads actual refs, transfer counters, local row and IdentityServer grant state.</summary>
    /// <returns>Independent observed state, or null when the fixture read failed.</returns>
    Phase0AuthorizationState? ReadState();

    /// <summary>Performs the explicit request, including fault injection at the named revocation boundary.</summary>
    /// <param name="attempt">Complete fixture input; production APIs do not receive fault-injection options.</param>
    /// <returns>Actual protocol result and ordered identity-check observations.</returns>
    Phase0AuthorizationOutcome? Attempt(Phase0AuthorizationAttempt attempt);
}

/// <summary>Requirement-derived assertion scenarios shared by mocks and future disposable real bindings.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-003/005; FR-MCP-185 ac-RawRemoteCannotBypassPublicationPolicy,
/// ac-ForeignProjectCloneDenied, ac-RevokedNodeCannotPublish, ac-LocalAppUserCannotPush,
/// ac-RevokedCredentialFailsFetchAndPublish, ac-OfflineLocalAuthorizationStillApplies;
/// TR-MCP-FEDERATION-012; TEST-MCP-233. Partial contracts only: token relay, exact Duende binary
/// issuance/introspection/grant APIs, repeated-request caching, SQL-port isolation, TLS and plugin
/// capability display require separate evidence. These mocks cannot establish B5 acceptance.
/// </remarks>
internal static class Phase0AuthorizationContract
{
    /// <summary>Runs one explicit attempt and validates its result plus independent before/after state.</summary>
    /// <param name="probe">Scripted or later real disposable fixture boundary.</param>
    /// <param name="scenario">One named requirement-derived scenario.</param>
    internal static void AssertScenario(IPhase0AuthorizationProbe probe, string scenario)
    {
        var attempt = Input(scenario);
        probe.Prepare();
        AssertState(probe.ReadState(), "original", revoked: false);
        var outcome = probe.Attempt(attempt);
        Assert.NotNull(outcome);
        var expected = Expected(scenario);
        Assert.Equal(expected.Kind, outcome.Kind);
        Assert.Equal(expected.Status, outcome.Status);
        if (expected.Error is not null) Assert.Equal(expected.Error, outcome.Error);
        Assert.Equal(expected.IdentityChecks, outcome.IdentityChecks);
        var finalState = probe.ReadState();
        AssertState(finalState, scenario == "OfflineAuthorized" ? "edited" : "original",
            attempt.RevocationPoint != "None", baseline: false);
        // Only these ACs require zero transfer. A commit-time revocation may follow an upload.
        if (scenario is "ForeignFetch" or "RevokedFetch" or "RevokedPublish")
            Assert.Equal(0, finalState!.TransferredObjects);
        if (scenario is "SqlAdmin" or "SqlPublicWrite" or "RevokedFetch" or "RevokedPublish")
            Assert.Equal(0, finalState!.PublishedObjects);
    }

    /// <summary>Gets explicit request inputs from the approved given/when conditions.</summary>
    /// <param name="scenario">Named fixture case.</param>
    /// <returns>Operation, project, credential, identity availability and revocation boundary.</returns>
    private static Phase0AuthorizationAttempt Input(string scenario) => scenario switch
    {
        "PrivateBackend" => new("private-connect", "Atlas", "Atlas.publish"),
        "ReadOnlyRawPush" => new("raw-push", "Atlas", "Atlas.read"),
        "PublisherRawPush" => new("raw-push", "Atlas", "Atlas.publish"),
        "ForeignFetch" => new("fetch", "SecretProject", "Atlas.read"),
        "RevokedAccept" => new("accept", "Atlas", "Atlas.publish", RevocationPoint: "BeforeRequest"),
        "SqlAdmin" => new("sql-admin", "Atlas", "local-editor"),
        "SqlPublicWrite" => new("sql-public-write", "Atlas", "local-editor"),
        "RevokedFetch" => new("fetch", "Atlas", "Atlas.read", RevocationPoint: "BeforeRequest"),
        "RevokedPublish" => new("publish", "Atlas", "Atlas.publish", RevocationPoint: "BeforeRequest"),
        "RevokedAtCommit" => new("accept", "Atlas", "Atlas.publish", RevocationPoint: "BeforeCommit"),
        "IdentityUnavailableFetch" => new("fetch", "Atlas", "Atlas.read", IdentityAvailable: false),
        "IdentityUnavailablePublish" => new("publish", "Atlas", "Atlas.publish", IdentityAvailable: false),
        "OfflineAuthorized" => new("local-edit", "Atlas", "local-editor", IdentityAvailable: false),
        "OfflineUnauthorized" => new("local-edit", "Atlas", "local-denied", IdentityAvailable: false),
        _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown authorization scenario.")
    };

    /// <summary>Gets outcomes specified by FR185 and TR012, without inventing numeric SQL errors.</summary>
    /// <param name="scenario">Named fixture case.</param>
    /// <returns>Expected externally visible protocol outcome and introspection sequence.</returns>
    private static Phase0AuthorizationOutcome Expected(string scenario) => scenario switch
    {
        "PrivateBackend" => new("NetworkFailure", null, null, []),
        "ReadOnlyRawPush" or "ForeignFetch" => new("Http", 403, "unauthorized_project", ["request:active"]),
        "PublisherRawPush" => new("Http", 409, "publication_required", ["request:active"]),
        "SqlAdmin" or "SqlPublicWrite" => new("SqlPermissionDenied", null, null, []),
        "RevokedAccept" or "RevokedFetch" or "RevokedPublish" => new("Http", 401, "invalid_token", ["request:inactive"]),
        "RevokedAtCommit" => new("Http", 401, "invalid_token", ["request:active", "commit:inactive"]),
        "IdentityUnavailableFetch" or "IdentityUnavailablePublish" => new("Http", 503, "identity_unavailable", ["request:unavailable"]),
        "OfflineAuthorized" => new("LocalSaved", null, null, []),
        "OfflineUnauthorized" => new("Http", 403, null, []),
        _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown authorization scenario.")
    };

    /// <summary>Checks baseline and final state, including denied operations that accidentally transfer data.</summary>
    /// <param name="state">Independently reread fixture state.</param>
    /// <param name="title">Expected persisted local value.</param>
    /// <param name="revoked">Whether client disablement and persisted grant removal must both be observed.</param>
    /// <param name="baseline">Checks zero counters only for the initial clean fixture.</param>
    private static void AssertState(Phase0AuthorizationState? state, string title, bool revoked, bool baseline = true)
    {
        Assert.NotNull(state);
        Assert.Equal("C0", state.RemoteHead);
        if (baseline)
        {
            Assert.Equal(0, state.TransferredObjects);
            Assert.Equal(0, state.PublishedObjects);
        }
        Assert.Equal(title, state.LocalTitle);
        Assert.Equal(!revoked, state.ClientEnabled);
        Assert.Equal(revoked ? 0 : 1, state.PersistedReferenceGrants);
    }
}
