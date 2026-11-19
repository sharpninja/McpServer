namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Safe claim and current authorization observations captured separately from an admission decision.</summary>
/// <param name="Issuer">Actual configured issuer identity.</param><param name="Audience">Introspected audience.</param>
/// <param name="ClientId">Authenticated node client.</param><param name="Owner">Protected enrolled human owner.</param>
/// <param name="Scopes">Complete granted canonical scopes.</param><param name="Member">Current server project membership.</param>
/// <param name="BranchAllowed">Current server branch permission.</param><param name="IssuedAt">Fixture clock issuance seconds.</param>
/// <param name="ExpiresAt">Observed expiry seconds.</param><param name="Now">Independent fixture clock.</param>
/// <param name="Status">Observed admission status.</param><param name="Code">Stable denial code, empty on success.</param>
/// <remarks>2026-11-19: TASK-FEDPH0-005 B5; FR-MCP-185 ac-RevokedNodeCannotPublish/ac-LocalAppUserCannotPush;
/// TR-MCP-FEDERATION-012; TEST-MCP-233. No raw token, secret, JWT conversion or new product API is introduced.</remarks>
internal sealed record Phase0IdentityAdmission(string? Issuer, string? Audience, string? ClientId, string? Owner,
    IReadOnlyList<string>? Scopes, bool Member, bool BranchAllowed, long IssuedAt, long ExpiresAt, long Now, int Status, string? Code);

/// <summary>Fixture for complete canonical action scopes plus issuer, lifetime and current server-side entitlement checks.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-005 B5; FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233.
/// The 300-second lifetime is a disposable fixture choice, not a newly imposed production timeout.</remarks>
internal interface IPhase0IdentityAdmissionProbe
{
    /// <summary>Prepares node A/owner-A, Atlas/main and an explicit isolated claim or membership fault.</summary>
    /// <param name="action">read, publish, control, audit or enroll.</param><param name="condition">One fixture condition.</param>
    void Prepare(string action, string condition);
    /// <summary>Independently reads public head, retained local draft and zero publication counters.</summary>
    /// <returns>Observed public head, draft and object count, or null on failed observation.</returns>
    (string Head, string Draft, int Objects)? ReadState();
    /// <summary>Performs gateway admission using the privately retained reference token and current server authorization.</summary>
    /// <param name="action">Requested canonical action.</param><returns>Safe independent claims, entitlements, timing and decision observations.</returns>
    Phase0IdentityAdmission? Admit(string action);
}

/// <summary>Same assertions bind correct/incorrect mock observations now and the actual protected gateway later.</summary>
/// <remarks>2026-11-19: B5; FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233. Admission alone never publishes data.</remarks>
internal static class Phase0IdentityAdmissionContract
{
    /// <summary>Checks all identity inputs independently of the resulting allow/deny envelope and frozen project state.</summary>
    /// <param name="probe">Scripted or later isolated native adapter.</param><param name="action">Canonical requested action.</param>
    /// <param name="condition">Exact known claim, scope, permission or time fixture.</param>
    internal static void AssertScenario(IPhase0IdentityAdmissionProbe probe, string action, string condition)
    {
        probe.Prepare(action, condition);
        Assert.Equal(("C0", "offline draft café 雪", 0), probe.ReadState());
        var result = probe.Admit(action);
        Assert.NotNull(result);
        Assert.Equal(condition == "Issuer" ? "https://untrusted.fixture.test" : "https://issuer.fixture.test", result.Issuer);
        Assert.Equal(condition == "Audience" ? "other-audience" : "qbrain-federation", result.Audience);
        Assert.Equal(condition == "Client" ? "shared-client" : "fed.node.A", result.ClientId);
        Assert.Equal(condition == "Owner" ? "owner-B" : "owner-A", result.Owner);
        Assert.Equal(new[] { condition == "Scope" ? "fed.project.SecretProject." + action : "fed.project.Atlas." + action }, result.Scopes);
        Assert.Equal(condition != "Membership", result.Member);
        Assert.Equal(condition != "Branch", result.BranchAllowed);
        Assert.Equal(1000, result.IssuedAt);
        Assert.Equal(1300, result.ExpiresAt);
        Assert.Equal(condition == "Expired" ? 1300 : 1050, result.Now);
        var invalidToken = condition is "Issuer" or "Audience" or "Client" or "Expired";
        Assert.Equal(condition == "None" ? 200 : invalidToken ? 401 : 403, result.Status);
        Assert.Equal(condition == "None" ? "" : invalidToken ? "invalid_token" : "unauthorized_project", result.Code);
        Assert.Equal(("C0", "offline draft café 雪", 0), probe.ReadState());
    }
}