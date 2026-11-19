using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit.Sdk;

namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Test-owned additive boundary inputs; numeric standalone IDs remain numeric.</summary>
/// <param name="Operation">Exact local adapter command from the frozen identity, projection, guidance and composition roster.</param>
/// <param name="ContractVersion">Requested contract version.</param><param name="Project">Immutable scope.</param>
/// <param name="Origin">Original creator.</param><param name="Kind">Entity family.</param>
/// <param name="LegacyType">Exact original type discriminator.</param><param name="LegacyValue">Exact original representation.</param>
/// <param name="NumericId">Standalone Int64 identity.</param><param name="CanonicalId">Versioned string identity.</param>
/// <remarks>2026-11-19: TASK-FEDPH0-004 B4; FR-MCP-182 ac-LegacyIdsRemainValid; TR-MCP-FEDERATION-009; TEST-MCP-230.
/// This is an adapter design fixture, not a newly shipped public DTO or route.</remarks>
internal sealed record Phase0DtoCommand(string Operation, int ContractVersion = 1, string Project = "Atlas",
    string Origin = "LA", string Kind = "UseCase", string? LegacyType = null, string? LegacyValue = null,
    long? NumericId = null, string? CanonicalId = null);

/// <summary>Independent response observation retaining identity shape and stable existing error mapping.</summary>
/// <param name="Status">Observed status.</param><param name="Code">Existing stable error code, if any.</param>
/// <param name="Body">Observed DTO properties; null means no body.</param>
/// <remarks>2026-11-19: FR-MCP-182 ac-LegacyIdsRemainValid; approved main plan contract-version/error sections.</remarks>
internal sealed record Phase0DtoReply(int Status, string? Code, JsonObject? Body);

/// <summary>Independent stored state and effects, read separately from a DTO response.</summary>
/// <param name="NumericId">Unmodified standalone identity.</param><param name="CanonicalId">Unmodified canonical identity.</param>
/// <param name="History">Exact original reference text.</param><param name="Writes">Mutations during reads/refusals.</param>
/// <param name="SqlFallbacks">Forbidden direct-SQL fallback count.</param><param name="ShellFallbacks">Forbidden shell fallback count.</param>
/// <remarks>2026-11-19: TR-MCP-FEDERATION-009/011; TEST-MCP-230/232.</remarks>
internal sealed record Phase0DtoState(long NumericId, string CanonicalId, string History, int Writes, int SqlFallbacks, int ShellFallbacks);

/// <summary>Additive standalone/versioned client seam for later disposable native bindings.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-004 B4; FR-MCP-182 ac-LegacyIdsRemainValid; TR-MCP-FEDERATION-009; TEST-MCP-230.</remarks>
internal interface IPhase0IdentityDtoProbe
{
    /// <summary>Seeds numeric Int64.MaxValue, tagged canonical identity, typed legacy aliases and original history.</summary>
    void Prepare();
    /// <summary>Invokes the exact documented read or capability request.</summary><param name="command">Complete typed input.</param>
    /// <returns>Actual DTO/error response.</returns>
    Phase0DtoReply? Execute(Phase0DtoCommand command);
    /// <summary>Reads all fixture rows and forbidden-effect counters independently.</summary><returns>Complete persisted observation.</returns>
    Phase0DtoState? ReadState();
    /// <summary>Reopens readers without resetting rows, aliases or effect counters.</summary>
    void Reopen();
}

/// <summary>Freezes additive identity shape, typed legacy distinctions and capability refusal assertions.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-004 B4; FR-MCP-182 ac-LegacyIdsRemainValid; TR-MCP-FEDERATION-009; TEST-MCP-230.
/// Concrete public endpoint naming remains with the existing typed transport owner. No new HTTP error is introduced.
/// 2026-11-19: GitHubController451-477 consumes a project TODO legacy alias ISSUE-{number}; the separate external issue number
/// retains its numeric meaning. This exact mixed-boundary fixture supplements the grammar rejection of ISSUE-87 as a new generated ID.
/// 2026-11-19: Source-disposition closure adds exact traceability graph/Int64, layer-key versus creator metadata, mixed operational
/// soft references, versioned guidance and composition/replay observations. These are local adapter observations, not new public DTOs.</remarks>
internal static class Phase0IdentityDtoBoundaryContract
{
    internal const string Canonical = "UC-LA-9223372036854775807";
    internal const string Historical = "commit/PR/session/memory/document UC-7\r\ncafé 雪\n";

    /// <summary>Checks byte/type fidelity, distinct typed alias lookups, refusal before effects, and reopen durability.</summary>
    /// <param name="probe">Scripted or later real disposable adapter.</param>
    internal static void AssertBoundary(IPhase0IdentityDtoProbe probe)
    {
        var commands = Commands();
        var expected = Replies();
        probe.Prepare();
        AssertState(probe.ReadState());
        for (var index = 0; index < commands.Length; index++)
        {
            var actual = probe.Execute(commands[index]);
            Assert.NotNull(actual);
            Assert.Equal(expected[index].Status, actual.Status);
            Assert.Equal(expected[index].Code, actual.Code);
            Assert.True(JsonNode.DeepEquals(expected[index].Body, actual.Body), "DTO values/types/membership differ.");
            if (index == 0)
            {
                var wire = JsonSerializer.SerializeToNode(actual.Body)!.AsObject();
                Assert.Equal(long.MaxValue, wire["id"]!.GetValue<long>());
                Assert.False(wire.ContainsKey("canonicalId"));
            }
            if (index == 1)
            {
                var wire = JsonSerializer.SerializeToNode(actual.Body)!.AsObject();
                Assert.Equal(Canonical, wire["canonicalId"]!.GetValue<string>());
                Assert.False(wire.ContainsKey("id"));
            }
            AssertState(probe.ReadState());
        }
        probe.Reopen();
        AssertState(probe.ReadState());
    }

    /// <summary>Defines fifteen exact identity, traceability, layer, mixed-reference, guidance, composition and refusal requests.</summary>
    /// <returns>Inputs bound unchanged by the later adapter.</returns>
    internal static Phase0DtoCommand[] Commands() =>
    [
        new("ReadStandalone", NumericId: long.MaxValue),
        new("ReadVersioned", CanonicalId: Canonical),
        new("ResolveLegacy", LegacyType: "string", LegacyValue: "007"),
        new("ResolveLegacy", LegacyType: "string", LegacyValue: "7"),
        new("ResolveLegacy", LegacyType: "int64", LegacyValue: "7"),
        new("ResolveLegacy", Kind: "Todo", LegacyType: "string", LegacyValue: "ISSUE-87"),
        new("ReadExternalIssue", Kind: "GitHubIssue", NumericId: 87),
        new("ReadStandaloneTraceability", NumericId: long.MaxValue),
        new("ReadVersionedTraceability", CanonicalId: Canonical),
        new("ReadLayerProjection", Kind: "RequirementLayer", LegacyType: "string", LegacyValue: "local-project"),
        new("ReadMixedOperationalReferences", Kind: "Handoff", LegacyValue: "handoff-7"),
        new("ReadVersionedGuidance", Kind: "PluginGuidance"),
        new("ReadComposition", Kind: "ProjectContext"),
        new("Negotiate", ContractVersion: 2),
        new("UnsupportedCommand")
    ];

    /// <summary>Defines independent expected response shapes rather than adapting current numeric APIs by casting.</summary>
    /// <returns>Exact finite scripted observations.</returns>
    internal static Phase0DtoReply[] Replies() =>
    [
        new(200, null, new JsonObject { ["id"] = long.MaxValue, ["title"] = "existing numeric standalone" }),
        new(200, null, Reference(Canonical, null, null)),
        new(200, null, Reference("UC-LA-101", "string", "007")),
        new(200, null, Reference("UC-LA-102", "string", "7")),
        new(200, null, Reference("UC-LA-103", "int64", "7")),
        new(200, null, Reference("TODO-LA-104", "string", "ISSUE-87", "Todo")),
        new(200, null, new JsonObject { ["issueNumber"] = 87L, ["repository"] = "sharpninja/McpServer" }),
        new(200, null, new JsonObject { ["useCaseId"] = long.MaxValue, ["frId"] = "FR-MCP-007" }),
        new(200, null, new JsonObject
        {
            ["projectId"] = "Atlas", ["useCaseId"] = Canonical, ["frId"] = "FR-MCP-LG-1000",
            ["flowId"] = "UC-FLOW-LA-001", ["stepId"] = "UC-STEP-LA-001",
            ["sequenceNumber"] = 1000, ["stepNumber"] = 7, ["versionNumber"] = 2, ["linkOrder"] = 1,
            ["diagramNodeId"] = "node-7"
        }),
        new(200, null, new JsonObject
        {
            ["projectId"] = "Atlas", ["originNodeId"] = "LA", ["layerKey"] = "local-project", ["order"] = 7,
            ["requirementId"] = "FR-MCP-LG-1000", ["area"] = "MCP", ["subarea"] = null, ["creatorTag"] = "LG"
        }),
        new(200, null, new JsonObject
        {
            ["runId"] = "handoff-7", ["requestId"] = "req-20261119T171310Z-prompt-5c2d",
            ["gitSha"] = "64f098b2bdd9cdd5c04a4e6eb6bf727eaf85292d", ["auditVersion"] = 2,
            ["todoRef"] = Reference("TODO-LA-104", "string", "ISSUE-87", "Todo"), ["sourceText"] = Historical
        }),
        new(200, null, new JsonObject
        {
            ["todo999"] = "EXEC-TODO-LA-999", ["todo1000"] = "EXEC-TODO-LA-1000",
            ["memory999"] = "MEMORY-FACT-LA-999", ["memory1000"] = "MEMORY-FACT-LA-1000",
            ["sort"] = "parsed-family/tag/numeric-sequence", ["legacyGuidanceRetained"] = true,
            ["newNodeImplementation"] = false
        }),
        new(200, null, new JsonObject
        {
            ["projectId"] = "Atlas", ["versionedProjectProvider"] = "Dolt", ["standaloneProvider"] = "SQLite",
            ["canonicalBoundaryBound"] = true, ["legacyReplayAllowedForVersionedProject"] = false,
            ["localReads"] = true, ["workspaceAuthorizationRetained"] = true,
            ["operationalStoresRetained"] = true, ["crossStoreJoinCount"] = 0
        }),
        new(409, "unsupported_contract", null),
        new(501, "unsupported_capability", null)
    ];

    /// <summary>Preserves original type/value separately from display and canonical identity.</summary>
    private static JsonObject Reference(string canonical, string? type, string? value, string kind = "UseCase") => new()
    {
        ["contractVersion"] = 1, ["projectId"] = "Atlas", ["originNodeId"] = "LA", ["entityKind"] = kind,
        ["canonicalId"] = canonical, ["legacyType"] = type, ["legacyValue"] = value,
        ["displayLabel"] = "User label UC-7", ["provenance"] = "reviewed-import-LA"
    };

    /// <summary>Checks every durable field and each forbidden fallback after every command and reopen.</summary>
    private static void AssertState(Phase0DtoState? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(new Phase0DtoState(long.MaxValue, Canonical, Historical, 0, 0, 0), actual);
    }
}

/// <summary>Discriminating correct/wrong observations for the additive identity boundary.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-004; FR-MCP-182 ac-LegacyIdsRemainValid; TR-MCP-FEDERATION-009; TEST-MCP-230.
/// Mock proof validates assertions and input shape, not current product compatibility or native transport.</remarks>
public sealed class Phase0IdentityDtoBoundaryMockGateTests
{
    /// <summary>Runs every exact request and complete readback through a finite independent script.</summary>
    [Fact]
    public void CorrectAdditiveDtoObservationsPass() => Phase0IdentityDtoBoundaryContract.AssertBoundary(new ScriptedProbe());

    /// <summary>JSON object ordering is presentation only and is not a new compatibility constraint.</summary>
    [Fact]
    public void EquivalentObjectMemberOrderingPasses() =>
        Phase0IdentityDtoBoundaryContract.AssertBoundary(new ScriptedProbe(reverseBodyMembers: true));

    /// <summary>Enumerates every response field, omissions and every persisted field/checkpoint.</summary>
    /// <returns>One deliberately wrong observation per test row.</returns>
    public static IEnumerable<object[]> WrongObservations()
    {
        var replies = Phase0IdentityDtoBoundaryContract.Replies();
        for (var index = 0; index < replies.Length; index++)
        {
            foreach (var field in new[] { "missing", "status", "code", "body" }) yield return ["reply", index, field];
            if (replies[index].Body is { } body)
                foreach (var key in body.Select(pair => pair.Key)) yield return ["reply", index, "body:" + key];
        }
        for (var index = 0; index < replies.Length + 2; index++)
            foreach (var field in new[] { "missing", "numeric", "canonical", "history", "writes", "sql", "shell" })
                yield return ["state", index, field];
    }

    /// <summary>Requires the intended mutation to execute and the shared contract to reject it with an xUnit assertion.</summary>
    /// <param name="target">Response or independent state.</param><param name="index">Checkpoint.</param><param name="field">Single fault.</param>
    [Theory]
    [MemberData(nameof(WrongObservations))]
    public void WrongObservationIsRejected(string target, int index, string field)
    {
        var probe = new ScriptedProbe(target, index, field);
        var failure = Record.Exception(() => Phase0IdentityDtoBoundaryContract.AssertBoundary(probe));
        Assert.True(probe.Injected);
        Assert.IsAssignableFrom<XunitException>(failure);
    }

    /// <summary>Finite response/readback script with independent wrong observations and exact command checks.</summary>
    private sealed class ScriptedProbe(string? target = null, int faultIndex = -1, string field = "", bool reverseBodyMembers = false) : IPhase0IdentityDtoProbe
    {
        private int commandIndex;
        private int readIndex;
        private bool reopened;
        /// <summary>Whether the requested negative control reached its intended seam.</summary>
        internal bool Injected { get; private set; }
        /// <inheritdoc/>
        public void Prepare() { commandIndex = 0; readIndex = 0; reopened = false; }
        /// <inheritdoc/>
        public Phase0DtoReply? Execute(Phase0DtoCommand command)
        {
            Assert.Equal(Phase0IdentityDtoBoundaryContract.Commands()[commandIndex], command);
            var index = commandIndex++;
            var reply = Phase0IdentityDtoBoundaryContract.Replies()[index];
            if (reverseBodyMembers && reply.Body is { } body)
                reply = reply with { Body = new JsonObject(body.Reverse().Select(pair =>
                    new KeyValuePair<string, JsonNode?>(pair.Key, pair.Value?.DeepClone()))) };
            if (target != "reply" || index != faultIndex) return reply;
            Injected = true;
            return field switch
            {
                "missing" => null,
                "status" => reply with { Status = -1 },
                "code" => reply with { Code = "wrong" },
                "body" => reply with { Body = reply.Body is null ? new() : null },
                _ => MutateBody(reply, field[5..])
            };
        }
        /// <summary>Changes exactly one response DTO field without changing its expected counterpart.</summary>
        private static Phase0DtoReply MutateBody(Phase0DtoReply reply, string key)
        {
            var body = reply.Body!.DeepClone().AsObject();
            body[key] = "wrong";
            return reply with { Body = body };
        }
        /// <inheritdoc/>
        public Phase0DtoState? ReadState()
        {
            if (readIndex == Phase0IdentityDtoBoundaryContract.Commands().Length + 1) Assert.True(reopened);
            var index = readIndex++;
            var state = new Phase0DtoState(long.MaxValue, Phase0IdentityDtoBoundaryContract.Canonical,
                Phase0IdentityDtoBoundaryContract.Historical, 0, 0, 0);
            if (target != "state" || index != faultIndex) return state;
            Injected = true;
            return field switch
            {
                "missing" => null, "numeric" => state with { NumericId = 9007199254740992 },
                "canonical" => state with { CanonicalId = "1" }, "history" => state with { History = "rewritten" },
                "writes" => state with { Writes = 1 }, "sql" => state with { SqlFallbacks = 1 },
                "shell" => state with { ShellFallbacks = 1 }, _ => throw new InvalidOperationException(field)
            };
        }
        /// <inheritdoc/>
        public void Reopen() { Assert.Equal(Phase0IdentityDtoBoundaryContract.Commands().Length, commandIndex); reopened = true; }
    }
}