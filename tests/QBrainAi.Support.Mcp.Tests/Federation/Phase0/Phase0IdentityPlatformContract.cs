namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Safe identity-platform metadata and independent durable-state observations; no token or secret values.</summary>
/// <param name="Packages">Resolved package identities and exact versions.</param><param name="Stores">Selected client and grant store types.</param>
/// <param name="NodeAEnabled">Durable client A state.</param><param name="NodeAGrants">Persisted reference grants for A.</param>
/// <param name="NodeBEnabled">Durable unrelated client B state.</param><param name="NodeBGrants">Persisted reference grants for B.</param>
/// <param name="PublicHead">Actual remote project ref.</param><param name="LocalTitle">Retained local authored content.</param>
/// <remarks>2026-11-17: TASK-FEDPH0-005; B5; FR-MCP-185 ac-RevokedNodeCannotPublish; TR-MCP-FEDERATION-012; TEST-MCP-233.</remarks>
internal sealed record Phase0IdentityPlatformState(IReadOnlyDictionary<string, string>? Packages,
    IReadOnlyDictionary<string, string>? Stores, bool NodeAEnabled, int NodeAGrants,
    bool NodeBEnabled, int NodeBGrants, string? PublicHead, string? LocalTitle);

/// <summary>Safe issuance metadata; the adapter retains the actual reference token privately.</summary>
/// <param name="Status">Observed protocol status.</param><param name="Error">Native OAuth error when denied.</param>
/// <param name="AccessTokenType">Observed storage/issuance type, not inferred from bearer token_type.</param>
/// <remarks>2026-11-17: B5; FR-MCP-185 ac-RevokedNodeCannotPublish; TEST-MCP-233.</remarks>
internal sealed record Phase0NodeTokenIssue(int Status, string? Error, string? AccessTokenType);

/// <summary>Safe introspection and effective authorization observations with no credential material.</summary>
/// <param name="Status">Introspection HTTP outcome.</param><param name="Active">Actual token activity.</param>
/// <param name="ClientId">Authenticated node client.</param><param name="Audience">Effective federation audience.</param>
/// <param name="Project">Authorized project.</param><param name="Owner">Enrolled owner resolved independently of machine authentication.</param>
/// <param name="Scopes">Actual authorized project/action scopes.</param>
/// <remarks>2026-11-17: B5; FR-MCP-185 ac-RevokedNodeCannotPublish; TR-MCP-FEDERATION-012; TEST-MCP-233.</remarks>
internal sealed record Phase0NodeIntrospection(int Status, bool Active, string? ClientId, string? Audience,
    string? Project, string? Owner, IReadOnlyList<string>? Scopes);

/// <summary>Disposable exact-platform boundary for later real issuance, restart and revocation evidence.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-005; FR-MCP-185 ac-RevokedNodeCannotPublish; TR-MCP-FEDERATION-012; TEST-MCP-233.
/// Tokens and client/API secrets stay inside the adapter; A/B arguments are safe fixture aliases, never credentials.
/// </remarks>
internal interface IPhase0IdentityPlatformProbe
{
    /// <summary>Prepares enabled clients fed.node.A/B, owner bindings, Atlas scope, empty grant sets and public C0.</summary>
    void Prepare();
    /// <summary>Reads resolved packages, store types, durable client/grant state, public ref and local row.</summary>
    /// <returns>Independent observations, or null on read failure.</returns>
    Phase0IdentityPlatformState? ReadState();
    /// <summary>Issues a token using the node's protected client credentials and captures it privately.</summary>
    /// <param name="node">Safe fixture node alias A or B.</param><returns>Safe issuance metadata only.</returns>
    Phase0NodeTokenIssue? Issue(string node);
    /// <summary>Calls authenticated introspection for the exact previously captured node token.</summary>
    /// <param name="node">Safe fixture node alias.</param><returns>Activity and effective authorization, never a token.</returns>
    Phase0NodeIntrospection? Introspect(string node);
    /// <summary>Restarts the isolated issuer process while retaining its protected databases and key material.</summary>
    void RestartIssuer();
    /// <summary>Disables A's client and removes its reference grants through supported IdentityServer stores.</summary>
    void RevokeNodeA();
    /// <summary>Attempts guarded publication with A's original token after revocation.</summary>
    /// <returns>Canonical gateway refusal.</returns>
    Phase0IdentityPublishReply? PublishNodeA();
}

/// <summary>Requirements-derived platform assertions, independent of any production or mock implementation.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-005; B5; FR-MCP-185 ac-RevokedNodeCannotPublish; TR-MCP-FEDERATION-012; TEST-MCP-233.</remarks>
internal static class Phase0IdentityPlatformContract
{
    /// <summary>Checks exact dependencies, durable issuance/restart, client-plus-grant revocation and uncached rejection.</summary>
    /// <param name="probe">Scripted or later real disposable platform boundary.</param>
    internal static void AssertRevocationLifecycle(IPhase0IdentityPlatformProbe probe)
    {
        probe.Prepare();
        AssertState(probe.ReadState(), revoked: false, issued: false);
        AssertIssue(probe.Issue("A"));
        AssertIssue(probe.Issue("B"));
        AssertActive(probe.Introspect("A"), "A");
        AssertActive(probe.Introspect("B"), "B");
        AssertState(probe.ReadState(), revoked: false, issued: true);
        probe.RestartIssuer();
        AssertActive(probe.Introspect("A"), "A");
        AssertActive(probe.Introspect("B"), "B");
        AssertState(probe.ReadState(), revoked: false, issued: true);
        probe.RevokeNodeA();
        AssertState(probe.ReadState(), revoked: true, issued: true);
        AssertInactive(probe.Introspect("A"));
        AssertActive(probe.Introspect("B"), "B");
        var publication = probe.PublishNodeA();
        Assert.NotNull(publication);
        Assert.Equal(401, publication.Status);
        Assert.Equal("invalid_token", publication.Code);
        var issuance = probe.Issue("A");
        Assert.NotNull(issuance);
        Assert.Contains(issuance.Status, new[] { 400, 401 });
        Assert.Equal("invalid_client", issuance.Error);
        probe.RestartIssuer();
        AssertInactive(probe.Introspect("A"));
        AssertActive(probe.Introspect("B"), "B");
        AssertState(probe.ReadState(), revoked: true, issued: true);
    }

    /// <summary>Verifies reference storage rather than incorrectly equating OAuth bearer type with a JWT/reference distinction.</summary>
    private static void AssertIssue(Phase0NodeTokenIssue? issue)
    {
        Assert.NotNull(issue);
        Assert.Equal(200, issue.Status);
        Assert.Null(issue.Error);
        Assert.Equal("Reference", issue.AccessTokenType);
    }

    /// <summary>Checks active node identity, audience, owner binding, project and granted scope.</summary>
    private static void AssertActive(Phase0NodeIntrospection? observation, string node)
    {
        Assert.NotNull(observation);
        Assert.Equal(200, observation.Status);
        Assert.True(observation.Active);
        Assert.Equal("fed.node." + node, observation.ClientId);
        Assert.Equal("qbrain-federation", observation.Audience);
        Assert.Equal("Atlas", observation.Project);
        Assert.Equal("owner-" + node, observation.Owner);
        Assert.Equal(new[] { "Atlas.publish" }, Assert.IsAssignableFrom<IReadOnlyList<string>>(observation.Scopes));
    }

    /// <summary>Checks the same previously valid token is now inactive; no positive result can be reused.</summary>
    private static void AssertInactive(Phase0NodeIntrospection? observation)
    {
        Assert.NotNull(observation);
        Assert.Equal(200, observation.Status);
        Assert.False(observation.Active);
    }

    /// <summary>Checks packages, durable stores, isolated node state and independently unchanged project content.</summary>
    private static void AssertState(Phase0IdentityPlatformState? state, bool revoked, bool issued)
    {
        Assert.NotNull(state);
        var packages = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(state.Packages);
        foreach (var id in new[] { "Duende.IdentityServer", "Duende.IdentityServer.Storage" })
        {
            Assert.True(packages.TryGetValue(id, out var version));
            Assert.Equal("7.4.7", version);
        }
        foreach (var id in new[] { "Duende.IdentityServer.AspNetIdentity", "Duende.IdentityServer.EntityFramework", "Duende.IdentityServer.EntityFramework.Storage" })
        {
            Assert.True(packages.TryGetValue(id, out var version));
            Assert.Equal("7.4.6", version);
        }
        var stores = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(state.Stores);
        Assert.True(stores.TryGetValue("Client", out var clientStore));
        Assert.Equal("Duende.IdentityServer.EntityFramework.Stores.ClientStore", clientStore);
        Assert.True(stores.TryGetValue("Grant", out var grantStore));
        Assert.Equal("Duende.IdentityServer.EntityFramework.Stores.PersistedGrantStore", grantStore);
        Assert.Equal(!revoked, state.NodeAEnabled);
        Assert.Equal(issued && !revoked ? 1 : 0, state.NodeAGrants);
        Assert.True(state.NodeBEnabled);
        Assert.Equal(issued ? 1 : 0, state.NodeBGrants);
        Assert.Equal("C0", state.PublicHead);
        Assert.Equal("offline draft café 雪", state.LocalTitle);
    }
}
