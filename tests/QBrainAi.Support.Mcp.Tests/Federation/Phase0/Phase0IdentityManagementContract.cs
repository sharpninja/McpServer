namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Safe operator command for the existing identity service; credentials never enter this record.</summary>
/// <param name="Operation">ProvisionOwner, EnrollNode or RotateClientSecret.</param>
/// <param name="Node">Safe fixture node identity.</param><param name="Owner">Human subject to bind, distinct from the node client.</param>
/// <param name="OperatorAuthorized">Fixture authorization context, established outside a product request body.</param>
/// <param name="BindingApproved">Fixture evidence of the authenticated operator's owner-binding approval.</param>
/// <param name="Scopes">Requested canonical scope names.</param><param name="StoreAvailable">Fixture-only fault boundary.</param>
/// <remarks>2026-11-19: TASK-FEDPH0-005 B5; FR-MCP-185 ac-RevokedNodeCannotPublish/ac-LocalAppUserCannotPush;
/// TR-MCP-FEDERATION-012; TEST-MCP-233; retained MCP-FEDIDENTITY-001. These are test commands, not new public APIs.</remarks>
internal sealed record Phase0IdentityCommand(string Operation, string Node, string Owner,
    bool OperatorAuthorized, bool BindingApproved, IReadOnlyList<string> Scopes, bool StoreAvailable = true);

/// <summary>Protected identity metadata only; no secrets, secret hashes, reference handles or serialized grant data.</summary>
/// <param name="Owner">One authenticated human owner.</param><param name="ClientId">Per-node Duende client identity.</param>
/// <param name="Enabled">Durable enabled state.</param><param name="SecretGeneration">Safe observation of which protected credential slot is active.</param>
/// <param name="Scopes">Complete allowed scope inventory.</param>
/// <remarks>2026-11-19: B5; FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233.</remarks>
internal sealed record Phase0ManagedNode(string Owner, string ClientId, bool Enabled, int SecretGeneration, IReadOnlyList<string> Scopes);

/// <summary>Independent durable metadata, plus absence of unintended project, SQL or fallback effects.</summary>
/// <param name="Users">Host user subjects and enabled states.</param><param name="Nodes">Node to single-owner client bindings.</param>
/// <param name="ProtectedStores">Selected protected contexts, never a versioned project context.</param>
/// <param name="RemoteHead">Public project ref.</param><param name="LocalTitle">Retained local work.</param>
/// <param name="PublishedObjects">Published object count.</param><param name="RemoteSqlCalls">Calls using editing credentials to remote administrative procedures.</param>
/// <param name="InMemoryFallbacks">Fallbacks after protected store failure.</param>
/// <remarks>2026-11-19: B5; FR-MCP-185 ac-LocalAppUserCannotPush; TR-MCP-FEDERATION-012; TEST-MCP-233.</remarks>
internal sealed record Phase0IdentityManagementState(IReadOnlyDictionary<string, bool>? Users,
    IReadOnlyDictionary<string, Phase0ManagedNode>? Nodes, IReadOnlyList<string>? ProtectedStores,
    string? RemoteHead, string? LocalTitle, int PublishedObjects, int RemoteSqlCalls, int InMemoryFallbacks);

/// <summary>Non-secret command outcome; protocol values are asserted only where the existing contract specifies them.</summary>
/// <param name="Outcome">Applied, Denied or Unavailable.</param><param name="Status">Existing authorization/outage HTTP status; absent for internal administration success.</param>
/// <param name="Code">Existing stable authorization/outage code.</param>
/// <remarks>2026-11-19: B5; FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233.</remarks>
internal sealed record Phase0IdentityCommandReply(string Outcome, int? Status, string? Code);

/// <summary>Test-owned existing-platform management seam, later implemented only by an isolated integration adapter.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-005 B5; FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233. Safe slot aliases never carry credentials.</remarks>
internal interface IPhase0IdentityManagementProbe
{
    /// <summary>Prepares enabled host owners A/B, optional existing node A and an unrelated node B.</summary>
    /// <param name="existingNode">Whether the scenario begins with node A already bound.</param>
    void Prepare(bool existingNode);
    /// <summary>Reads protected durable metadata independently from the command response.</summary>
    /// <returns>Current observed metadata, or null on read failure.</returns>
    Phase0IdentityManagementState? ReadState();
    /// <summary>Invokes the current platform's authorized host/node management boundary.</summary>
    /// <param name="command">Safe operation and independently supplied fixture authorization context.</param><returns>Safe outcome.</returns>
    Phase0IdentityCommandReply? Execute(Phase0IdentityCommand command);
    /// <summary>Reopens issuer/store resources; the same durable owner/client state must survive.</summary>
    void Reopen();
    /// <summary>Attempts token issuance with one of the adapter-private old/new credential slots.</summary>
    /// <param name="slot">Safe fixture alias Old or New, never a secret reference accepted by a public API.</param><returns>Safe native OAuth result.</returns>
    Phase0NodeTokenIssue? Issue(string slot);
}

/// <summary>Full-definition management assertions supplementing existing issuance/revocation and SQL permission contracts.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-005 B5; FR-MCP-185 ac-RevokedNodeCannotPublish/ac-LocalAppUserCannotPush;
/// TR-MCP-FEDERATION-012; TEST-MCP-233. No native implementation or requirement satisfaction is established by mocks.</remarks>
internal static class Phase0IdentityManagementContract
{
    /// <summary>Exercises explicit owner enrollment, host management, rotation and fail-closed protected store boundaries.</summary>
    /// <param name="probe">Independent scripted or future real isolated observer.</param><param name="scenario">Named existing-contract scenario.</param>
    internal static void AssertScenario(IPhase0IdentityManagementProbe probe, string scenario)
    {
        var existing = scenario is "SecondOwner" or "Rotate" or "RotateDenied";
        var approved = scenario is "Enroll" or "Rotate" or "ProvisionOwner";
        var unavailable = scenario == "StoreUnavailable";
        var denied = !approved && !unavailable;
        probe.Prepare(existing);
        AssertState(probe.ReadState(), existing, false, false);
        var command = new Phase0IdentityCommand(scenario == "ProvisionOwner" || scenario == "ProvisionDenied" ? "ProvisionOwner" :
            scenario.StartsWith("Rotate", StringComparison.Ordinal) ? "RotateClientSecret" : "EnrollNode", "A",
            scenario is "ProvisionOwner" or "ProvisionDenied" ? "new-owner" : scenario is "WrongOwner" or "SecondOwner" ? "owner-B" : "owner-A",
            scenario is not ("EnrollDenied" or "RotateDenied" or "ProvisionDenied"), scenario != "Unapproved",
            scenario == "ScopeElevation" ? ["fed.project.SecretProject.publish"] : ["fed.project.Atlas.publish"], !unavailable);
        var reply = probe.Execute(command);
        Assert.NotNull(reply);
        Assert.Equal(unavailable ? "Unavailable" : denied ? "Denied" : "Applied", reply.Outcome);
        Assert.Equal(unavailable ? 503 : denied ? 403 : (int?)null, reply.Status);
        Assert.Equal(unavailable ? "identity_unavailable" : denied ? "unauthorized_project" : null, reply.Code);
        var enrolled = existing || scenario == "Enroll";
        AssertState(probe.ReadState(), enrolled, scenario == "Rotate", scenario == "ProvisionOwner");
        probe.Reopen();
        AssertState(probe.ReadState(), enrolled, scenario == "Rotate", scenario == "ProvisionOwner");
        if (scenario != "Rotate") return;
        var oldIssue = probe.Issue("Old");
        Assert.NotNull(oldIssue);
        Assert.Contains(oldIssue.Status, new[] { 400, 401 });
        Assert.Equal("invalid_client", oldIssue.Error);
        Assert.Null(oldIssue.AccessTokenType);
        var newIssue = probe.Issue("New");
        Assert.NotNull(newIssue);
        Assert.Equal(200, newIssue.Status);
        Assert.Null(newIssue.Error);
        Assert.Equal("Reference", newIssue.AccessTokenType);
        AssertState(probe.ReadState(), true, true, false);
    }

    /// <summary>Checks every explicit metadata field, the unrelated node and absence of publication/credential fallback effects.</summary>
    private static void AssertState(Phase0IdentityManagementState? state, bool enrolled, bool rotated, bool provisioned)
    {
        Assert.NotNull(state);
        var users = Assert.IsAssignableFrom<IReadOnlyDictionary<string, bool>>(state.Users);
        Assert.Equal(provisioned ? 3 : 2, users.Count);
        Assert.True(users.TryGetValue("owner-A", out var a) && a);
        Assert.True(users.TryGetValue("owner-B", out var b) && b);
        if (provisioned) Assert.True(users.TryGetValue("new-owner", out var user) && user);
        var nodes = Assert.IsAssignableFrom<IReadOnlyDictionary<string, Phase0ManagedNode>>(state.Nodes);
        Assert.Equal(enrolled ? 2 : 1, nodes.Count);
        Assert.True(nodes.TryGetValue("B", out var nodeB));
        AssertNode(nodeB, "B", 1);
        if (enrolled)
        {
            Assert.True(nodes.TryGetValue("A", out var nodeA));
            AssertNode(nodeA, "A", rotated ? 2 : 1);
        }
        Assert.Equal(new[] { "DuendeConfiguration", "DuendeOperational", "HumanIdentity" },
            Assert.IsAssignableFrom<IReadOnlyList<string>>(state.ProtectedStores).OrderBy(x => x, StringComparer.Ordinal));
        Assert.Equal("C0", state.RemoteHead);
        Assert.Equal("offline draft café 雪", state.LocalTitle);
        Assert.Equal(0, state.PublishedObjects);
        Assert.Equal(0, state.RemoteSqlCalls);
        Assert.Equal(0, state.InMemoryFallbacks);
    }

    /// <summary>Checks exact single-owner binding and canonical project/action scope for a node.</summary>
    private static void AssertNode(Phase0ManagedNode? node, string alias, int generation)
    {
        Assert.NotNull(node);
        Assert.Equal("owner-" + alias, node.Owner);
        Assert.Equal("fed.node." + alias, node.ClientId);
        Assert.True(node.Enabled);
        Assert.Equal(generation, node.SecretGeneration);
        Assert.Equal(new[] { "fed.project.Atlas.publish" }, node.Scopes);
    }
}
