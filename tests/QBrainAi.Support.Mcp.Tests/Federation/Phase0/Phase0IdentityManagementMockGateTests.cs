namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Discriminates owner management, durable node enrollment, secret rotation and protected-store failure assertions.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-005 B5; FR-MCP-185 ac-RevokedNodeCannotPublish/ac-LocalAppUserCannotPush;
/// TR-MCP-FEDERATION-012; TEST-MCP-233; MCP-FEDIDENTITY-001. Scripted metadata is not an issuer or native proof.</remarks>
public sealed class Phase0IdentityManagementMockGateTests
{
    private static readonly string[] Scenarios = ["Enroll", "EnrollDenied", "Unapproved", "WrongOwner", "SecondOwner", "ScopeElevation", "Rotate", "RotateDenied", "StoreUnavailable", "ProvisionOwner", "ProvisionDenied"];
    private static readonly string[] StateFacets = ["Missing", "UsersMissing", "UserMissing", "UserExtra", "OwnerADisabled", "OwnerBDisabled", "NodesMissing", "NodeMissing", "NodeExtra", "StoresMissing", "StoreMissing", "StoreExtra", "StoreDuplicate", "Head", "LocalTitle", "Objects", "SqlCalls", "Fallback"];
    private static readonly string[] NodeFacets = ["Missing", "Owner", "Client", "Enabled", "Generation", "ScopesMissing", "ScopeWrong", "ScopeExtra", "ScopeAbbreviated"];

    /// <summary>Every command path also permits harmless ordering differences in independently observed store metadata.</summary>
    public static IEnumerable<object[]> ValidCases => Scenarios.SelectMany(s => new[] { new object[] { s, false }, new object[] { s, true } });

    /// <summary>Checks correct responses and exact interactions rather than merely accepting a final response.</summary>
    /// <param name="scenario">Independent approved/denied/failure fixture.</param><param name="reverseStores">Harmless metadata ordering variant.</param>
    [Theory, MemberData(nameof(ValidCases))]
    public void CorrectManagementOutcomePasses(string scenario, bool reverseStores)
    {
        var probe = new ManagementProbe(scenario, reverseStores: reverseStores);
        Phase0IdentityManagementContract.AssertScenario(probe, scenario);
        Assert.Equal(scenario == "Rotate" ? 4 : 3, probe.Reads);
        Assert.Equal(1, probe.Commands);
        Assert.Equal(1, probe.Reopens);
        Assert.Equal(scenario == "Rotate" ? 2 : 0, probe.Issues);
    }

    /// <summary>Enumerates each applicable observation, response field and rotation credential slot corruption.</summary>
    public static IEnumerable<object[]> InvalidCases
    {
        get
        {
            foreach (var scenario in Scenarios)
            {
                for (var checkpoint = 0; checkpoint < (scenario == "Rotate" ? 4 : 3); checkpoint++)
                {
                    foreach (var facet in StateFacets) yield return [scenario, $"State:{checkpoint}:{facet}"];
                    foreach (var facet in NodeFacets) yield return [scenario, $"State:{checkpoint}:Node:B:{facet}"];
                    if (scenario is "SecondOwner" or "Rotate" or "RotateDenied" || (scenario == "Enroll" && checkpoint > 0))
                        foreach (var facet in NodeFacets) yield return [scenario, $"State:{checkpoint}:Node:A:{facet}"];
                    if (scenario == "ProvisionOwner" && checkpoint > 0) yield return [scenario, $"State:{checkpoint}:NewOwner"];
                }
                foreach (var facet in new[] { "Missing", "Outcome", "Status", "Code" }) yield return [scenario, "Reply:" + facet];
            }
            foreach (var slot in new[] { "Old", "New" })
            foreach (var facet in new[] { "Missing", "Status", "Error", "Type" }) yield return ["Rotate", $"Issue:{slot}:{facet}"];
        }
    }

    /// <summary>Rejects each deliberately incorrect field and proves the targeted observation was actually reached.</summary>
    /// <param name="scenario">Independent fixture.</param><param name="fault">One corrupt observation.</param>
    [Theory, MemberData(nameof(InvalidCases))]
    public void WrongManagementOutcomeIsRejected(string scenario, string fault)
    {
        var probe = new ManagementProbe(scenario, fault);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0IdentityManagementContract.AssertScenario(probe, scenario));
        Assert.True(probe.FaultReached, "The targeted wrong response was not consumed: " + fault);
    }

    /// <summary>Independent scripts expect literal commands, durable snapshots and native OAuth envelopes without carrying secret values.</summary>
    private sealed class ManagementProbe(string scenario, string fault = "", bool reverseStores = false) : IPhase0IdentityManagementProbe
    {
        private bool applied;
        /// <summary>Counts independent durable reads.</summary>
        internal int Reads { get; private set; }
        /// <summary>Counts management requests.</summary>
        internal int Commands { get; private set; }
        /// <summary>Counts reopen operations.</summary>
        internal int Reopens { get; private set; }
        /// <summary>Counts old/new private-slot issuance attempts.</summary>
        internal int Issues { get; private set; }
        /// <summary>Records that the requested corruption was returned.</summary>
        internal bool FaultReached { get; private set; }

        /// <inheritdoc />
        public void Prepare(bool existingNode)
        {
            Assert.Equal(scenario is "SecondOwner" or "Rotate" or "RotateDenied", existingNode);
            applied = false;
        }

        /// <inheritdoc />
        public Phase0IdentityCommandReply? Execute(Phase0IdentityCommand command)
        {
            Commands++;
            Assert.Equal(scenario is "ProvisionOwner" or "ProvisionDenied" ? "ProvisionOwner" : scenario is "Rotate" or "RotateDenied" ? "RotateClientSecret" : "EnrollNode", command.Operation);
            Assert.Equal("A", command.Node);
            Assert.Equal(scenario is "ProvisionOwner" or "ProvisionDenied" ? "new-owner" : scenario is "WrongOwner" or "SecondOwner" ? "owner-B" : "owner-A", command.Owner);
            Assert.Equal(scenario is not ("EnrollDenied" or "RotateDenied" or "ProvisionDenied"), command.OperatorAuthorized);
            Assert.Equal(scenario != "Unapproved", command.BindingApproved);
            Assert.Equal(scenario == "ScopeElevation" ? new[] { "fed.project.SecretProject.publish" } : new[] { "fed.project.Atlas.publish" }, command.Scopes);
            Assert.Equal(scenario != "StoreUnavailable", command.StoreAvailable);
            applied = scenario is "Enroll" or "Rotate" or "ProvisionOwner";
            var reply = scenario == "StoreUnavailable" ? new Phase0IdentityCommandReply("Unavailable", 503, "identity_unavailable") :
                applied ? new Phase0IdentityCommandReply("Applied", null, null) : new Phase0IdentityCommandReply("Denied", 403, "unauthorized_project");
            if (!fault.StartsWith("Reply:", StringComparison.Ordinal)) return reply;
            FaultReached = true;
            return fault[6..] switch
            {
                "Missing" => null,
                "Outcome" => reply with { Outcome = "Wrong" },
                "Status" => reply with { Status = 202 },
                "Code" => reply with { Code = "Wrong" },
                _ => throw new InvalidOperationException("Unknown fixture fault.")
            };
        }

        /// <inheritdoc />
        public Phase0IdentityManagementState? ReadState()
        {
            var checkpoint = Reads++;
            var users = new Dictionary<string, bool>(StringComparer.Ordinal) { ["owner-A"] = true, ["owner-B"] = true };
            if (applied && scenario == "ProvisionOwner") users["new-owner"] = true;
            var nodes = new Dictionary<string, Phase0ManagedNode>(StringComparer.Ordinal)
            {
                ["B"] = new("owner-B", "fed.node.B", true, 1, ["fed.project.Atlas.publish"])
            };
            if (scenario is "SecondOwner" or "Rotate" or "RotateDenied" || (applied && scenario == "Enroll"))
                nodes["A"] = new("owner-A", "fed.node.A", true, applied && scenario == "Rotate" ? 2 : 1, ["fed.project.Atlas.publish"]);
            string[] stores = reverseStores ? ["DuendeOperational", "HumanIdentity", "DuendeConfiguration"] : ["HumanIdentity", "DuendeConfiguration", "DuendeOperational"];
            var state = new Phase0IdentityManagementState(users, nodes, stores, "C0", "offline draft café 雪", 0, 0, 0);
            var prefix = $"State:{checkpoint}:";
            if (!fault.StartsWith(prefix, StringComparison.Ordinal)) return state;
            FaultReached = true;
            var facet = fault[prefix.Length..];
            if (facet.StartsWith("Node:", StringComparison.Ordinal))
            {
                var parts = facet.Split(':');
                var alias = parts[1];
                var node = nodes[alias];
                nodes[alias] = parts[2] switch
                {
                    "Missing" => null!,
                    "Owner" => node with { Owner = "other-owner" },
                    "Client" => node with { ClientId = "shared-client" },
                    "Enabled" => node with { Enabled = false },
                    "Generation" => node with { SecretGeneration = 77 },
                    "ScopesMissing" => node with { Scopes = null! },
                    "ScopeWrong" => node with { Scopes = ["fed.project.Atlas.read"] },
                    "ScopeExtra" => node with { Scopes = ["fed.project.Atlas.publish", "fed.project.SecretProject.publish"] },
                    "ScopeAbbreviated" => node with { Scopes = ["Atlas.publish"] },
                    _ => throw new InvalidOperationException("Unknown fixture node fault.")
                };
                return state;
            }
            switch (facet)
            {
                case "Missing": return null;
                case "UsersMissing": return state with { Users = null };
                case "UserMissing": users.Remove("owner-A"); break;
                case "UserExtra": users["unauthorized-owner"] = true; break;
                case "OwnerADisabled": users["owner-A"] = false; break;
                case "OwnerBDisabled": users["owner-B"] = false; break;
                case "NewOwner": users["new-owner"] = false; break;
                case "NodesMissing": return state with { Nodes = null };
                case "NodeMissing": nodes.Remove("B"); break;
                case "NodeExtra": nodes["unapproved"] = nodes["B"]; break;
                case "StoresMissing": return state with { ProtectedStores = null };
                case "StoreMissing": return state with { ProtectedStores = ["HumanIdentity", "DuendeConfiguration"] };
                case "StoreExtra": return state with { ProtectedStores = [.. stores, "VersionedProject"] };
                case "StoreDuplicate": return state with { ProtectedStores = [.. stores, "HumanIdentity"] };
                case "Head": return state with { RemoteHead = "unexpected-head" };
                case "LocalTitle": return state with { LocalTitle = "lost-local-work" };
                case "Objects": return state with { PublishedObjects = 1 };
                case "SqlCalls": return state with { RemoteSqlCalls = 1 };
                case "Fallback": return state with { InMemoryFallbacks = 1 };
                default: throw new InvalidOperationException("Unknown fixture state fault.");
            }
            return state;
        }

        /// <inheritdoc />
        public void Reopen() { Reopens++; Assert.Equal(1, Commands); }

        /// <inheritdoc />
        public Phase0NodeTokenIssue? Issue(string slot)
        {
            Assert.Equal(Issues++ == 0 ? "Old" : "New", slot);
            Assert.Equal(1, Reopens);
            var issue = slot == "Old" ? new Phase0NodeTokenIssue(401, "invalid_client", null) : new Phase0NodeTokenIssue(200, null, "Reference");
            var prefix = "Issue:" + slot + ":";
            if (!fault.StartsWith(prefix, StringComparison.Ordinal)) return issue;
            FaultReached = true;
            return fault[prefix.Length..] switch
            {
                "Missing" => null,
                "Status" => issue with { Status = 202 },
                "Error" => issue with { Error = slot == "Old" ? null : "unexpected" },
                "Type" => issue with { AccessTokenType = "JWT" },
                _ => throw new InvalidOperationException("Unknown fixture issuance fault.")
            };
        }
    }
}