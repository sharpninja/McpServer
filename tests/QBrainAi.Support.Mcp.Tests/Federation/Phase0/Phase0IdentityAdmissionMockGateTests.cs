namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Discriminates full-scope identity admission, independently captured claims, time boundaries and current entitlements.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-005 B5; FR-MCP-185 ac-RevokedNodeCannotPublish/ac-LocalAppUserCannotPush;
/// TR-MCP-FEDERATION-012; TEST-MCP-233. Test-only records and private fixture aliases carry no credentials.</remarks>
public sealed class Phase0IdentityAdmissionMockGateTests
{
    private static readonly string[] Actions = ["read", "publish", "control", "audit", "enroll"];
    private static readonly string[] Conditions = ["None", "Issuer", "Audience", "Client", "Owner", "Scope", "Membership", "Branch", "Expired"];
    private static readonly string[] Facets = ["Missing", "Issuer", "Audience", "Client", "Owner", "ScopesMissing", "Scope", "ScopeExtra", "Member", "Branch", "Issued", "Expires", "Now", "Status", "Code"];
    /// <summary>Every canonical action runs with valid identity and all independently invalid claim/permission boundaries.</summary>
    public static IEnumerable<object[]> ValidCases => Actions.SelectMany(a => Conditions.Select(c => new object[] { a, c }));

    /// <summary>Correct allow and deny results retain public state and execute current server authorization observations.</summary>
    /// <param name="action">Canonical action.</param><param name="condition">Fixture claim/permission/time condition.</param>
    [Theory, MemberData(nameof(ValidCases))]
    public void CorrectIdentityAdmissionPasses(string action, string condition)
    {
        var probe = new AdmissionProbe(action, condition);
        Phase0IdentityAdmissionContract.AssertScenario(probe, action, condition);
        Assert.Equal(2, probe.Reads);
        Assert.Equal(1, probe.Attempts);
    }

    /// <summary>Enumerates every safe admission field plus independent baseline/final head, draft and object corruptions.</summary>
    public static IEnumerable<object[]> InvalidCases
    {
        get
        {
            foreach (var item in ValidCases)
            {
                foreach (var facet in Facets) yield return [item[0], item[1], "Admission:" + facet];
                foreach (var checkpoint in new[] { 0, 1 })
                foreach (var facet in new[] { "Missing", "Head", "Draft", "Objects" }) yield return [item[0], item[1], $"State:{checkpoint}:{facet}"];
            }
        }
    }

    /// <summary>Requires xUnit assertion failure from the actual corrupt field, not an exception or earlier fixture failure.</summary>
    /// <param name="action">Canonical action.</param><param name="condition">Fixture condition.</param><param name="fault">Wrong result.</param>
    [Theory, MemberData(nameof(InvalidCases))]
    public void WrongIdentityAdmissionIsRejected(string action, string condition, string fault)
    {
        var probe = new AdmissionProbe(action, condition, fault);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0IdentityAdmissionContract.AssertScenario(probe, action, condition));
        Assert.True(probe.FaultReached, "The targeted wrong response was not consumed: " + fault);
    }

    /// <summary>Literal source-independent claim fixtures and denial envelopes; no policy implementation is embedded in assertions.</summary>
    private sealed class AdmissionProbe(string action, string condition, string fault = "") : IPhase0IdentityAdmissionProbe
    {
        /// <summary>Counts independent state reads.</summary>
        internal int Reads { get; private set; }
        /// <summary>Counts current policy decisions.</summary>
        internal int Attempts { get; private set; }
        /// <summary>Records requested faulty observation consumption.</summary>
        internal bool FaultReached { get; private set; }
        /// <inheritdoc />
        public void Prepare(string requestedAction, string requestedCondition)
        {
            Assert.Equal(action, requestedAction);
            Assert.Equal(condition, requestedCondition);
        }
        /// <inheritdoc />
        public (string Head, string Draft, int Objects)? ReadState()
        {
            var prefix = $"State:{Reads++}:";
            if (!fault.StartsWith(prefix, StringComparison.Ordinal)) return ("C0", "offline draft café 雪", 0);
            FaultReached = true;
            return fault[prefix.Length..] switch
            {
                "Missing" => null,
                "Head" => ("unexpected-head", "offline draft café 雪", 0),
                "Draft" => ("C0", "lost-draft", 0),
                "Objects" => ("C0", "offline draft café 雪", 1),
                _ => throw new InvalidOperationException("Unknown admission state fault.")
            };
        }
        /// <inheritdoc />
        public Phase0IdentityAdmission? Admit(string requestedAction)
        {
            Assert.Equal(action, requestedAction);
            Attempts++;
            var scope = action switch
            {
                "read" => "fed.project.Atlas.read",
                "publish" => "fed.project.Atlas.publish",
                "control" => "fed.project.Atlas.control",
                "audit" => "fed.project.Atlas.audit",
                "enroll" => "fed.project.Atlas.enroll",
                _ => throw new InvalidOperationException("Unknown canonical action.")
            };
            var reply = new Phase0IdentityAdmission("https://issuer.fixture.test", "qbrain-federation", "fed.node.A", "owner-A", [scope], true, true, 1000, 1300, 1050, 200, "");
            reply = condition switch
            {
                "None" => reply,
                "Issuer" => reply with { Issuer = "https://untrusted.fixture.test", Status = 401, Code = "invalid_token" },
                "Audience" => reply with { Audience = "other-audience", Status = 401, Code = "invalid_token" },
                "Client" => reply with { ClientId = "shared-client", Status = 401, Code = "invalid_token" },
                "Owner" => reply with { Owner = "owner-B", Status = 403, Code = "unauthorized_project" },
                "Scope" => reply with { Scopes = [scope.Replace("Atlas", "SecretProject", StringComparison.Ordinal)], Status = 403, Code = "unauthorized_project" },
                "Membership" => reply with { Member = false, Status = 403, Code = "unauthorized_project" },
                "Branch" => reply with { BranchAllowed = false, Status = 403, Code = "unauthorized_project" },
                "Expired" => reply with { Now = 1300, Status = 401, Code = "invalid_token" },

                _ => throw new InvalidOperationException("Unknown admission condition.")
            };
            if (!fault.StartsWith("Admission:", StringComparison.Ordinal)) return reply;
            FaultReached = true;
            return fault[10..] switch
            {
                "Missing" => null,
                "Issuer" => reply with { Issuer = "wrong-issuer" },
                "Audience" => reply with { Audience = "wrong-audience" },
                "Client" => reply with { ClientId = "wrong-client" },
                "Owner" => reply with { Owner = "wrong-owner" },
                "ScopesMissing" => reply with { Scopes = null },
                "Scope" => reply with { Scopes = ["wrong-scope"] },
                "ScopeExtra" => reply with { Scopes = [.. reply.Scopes!, "fed.project.Other.control"] },
                "Member" => reply with { Member = !reply.Member },
                "Branch" => reply with { BranchAllowed = !reply.BranchAllowed },
                "Issued" => reply with { IssuedAt = 999 },
                "Expires" => reply with { ExpiresAt = 999 },
                "Now" => reply with { Now = 999 },
                "Status" => reply with { Status = 202 },
                "Code" => reply with { Code = "wrong-code" },
                _ => throw new InvalidOperationException("Unknown admission fault.")
            };
        }
    }
}