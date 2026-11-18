namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent source-derived fixture scripts prove selected automatic-writer assertions discriminate routing and persistence errors.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-004 B2/B3; FR-MCP-184/TR-MCP-FEDERATION-011/TEST-MCP-232 ac-BackgroundWritersDoNotDirtyVersionedBranch/ac-SecondAgentLoggingNeedsNoLease. Mock-only evidence, not native routing or full writer closure.</remarks>
public sealed class Phase0AutomaticWriterMockGateTests
{
    private static readonly string[] Schedule = ["heartbeat", "audit", "automatic-session", "requirements-read-empty", "requirements-read-initialized", "session-backfill",
        "memory-index-main", "memory-index-failure", "co-retrieval-off", "co-retrieval-on", "workspace-bootstrap", "context-ingestion", "agent-seeding", "agent-seeding-repeat", "second-agent-session"];

    /// <summary>Runs the independent valid script with either initial lease and either dictionary enumeration order.</summary>
    /// <param name="owned">Agent1 owns the lease.</param><param name="reverse">Reverse valid unordered cell projections.</param>
    [Theory, InlineData(false,false), InlineData(false,true), InlineData(true,false), InlineData(true,true)]
    public void CorrectAutomaticRoutingPasses(bool owned, bool reverse)
    {
        var probe = new Probe(owned, reverse);
        Phase0AutomaticWriterContract.AssertIsolation(owned, probe);
        var expected = new List<string> { "prepare", "state", "trace" };
        foreach (var operation in Schedule) expected.AddRange(["run:"+operation,"state","trace","reopen","state","trace"]);
        Assert.Equal(expected, probe.Calls);
    }

    /// <summary>Enumerates every named state, cell and execution-receipt corruption at every before/after/reopen checkpoint.</summary>
    public static IEnumerable<object[]> Faults
    {
        get
        {
            foreach (var owned in new[] { false, true })
            for (var checkpoint=0; checkpoint<=30; checkpoint++)
            {
                var stage=(checkpoint+1)/2;
                foreach(var fault in new[] { "StateMissing","Project","Branch","Head","Remote","WorkingSet","Lease","Protected","ProtectedMissing",
                    "AuthoredMissing","OperationalMissing","AuthoredExtra","OperationalExtra","EffectsMissing","EffectsExtra","TraceMissing","CompletedMissing","CompletedExtra","Reopens","ReceiptMissing","ReceiptEmpty" })
                    yield return [owned,checkpoint,fault];
                if(stage>=13) yield return [owned,checkpoint,"EffectsDrop"];
                if(stage>0) yield return [owned,checkpoint,"CompletedDrop"];
                if(stage>1) yield return [owned,checkpoint,"CompletedOrder"];
                var state=Snapshot(stage,owned,false);
                foreach(var pair in new[] { ("Authored",state.Authored!), ("Operational",state.Operational!) })
                foreach(var key in pair.Item2.Keys)
                foreach(var corruptField in new[] { "Drop","Key","Value" })
                    yield return [owned,checkpoint,pair.Item1+":"+corruptField+":"+key];
            }
        }
    }

    /// <summary>A single deliberate wrong readback must be rejected by an assertion, never a runtime exception.</summary>
    /// <param name="owned">Initial lease mode.</param><param name="checkpoint">Only corrupted readback boundary.</param><param name="fault">Single corruption.</param>
    [Theory, MemberData(nameof(Faults))]
    public void WrongAutomaticOutcomeIsRejected(bool owned, int checkpoint, string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0AutomaticWriterContract.AssertIsolation(owned,new Probe(owned,false,checkpoint,fault)));

    /// <summary>Builds independent fixed fixture projections; it never calls the contract's expectation code.</summary>
    /// <param name="stage">Completed operation count.</param><param name="owned">Initial lease.</param><param name="reverse">Valid unordered readback variation.</param><returns>Fixed observed state.</returns>
    private static Phase0AutomaticWriterState Snapshot(int stage, bool owned, bool reverse)
    {
        var content = new Dictionary<string,string?>
        {
            ["MemoryVersion/V1/Content"]="original",["Memory/M1/Content"]="café 雪",["PromotedSession/S1/TodoId"]="TODO-7",
            ["Memory/M1/Version"]="V1",["Layer/default/Name"]="Layer 1",["Requirement/FR-1/Body"]="authored requirement",
            ["PromotedSession/S1/PlanFile"]="docs/plans/original.md",["MemoryEdge/M1/M2/explicit/Weight"]="0.5"
        };
        var operational = new Dictionary<string,string?>
        {
            ["MemoryStatus/feature/M1"]="pending",["HostWorkspace/Atlas/CurrentLayer"]="layer-1",["OriginalAudit/E0/PayloadHash"]="original-audit-hash",
            ["MemoryStatus/main/M1"]=stage>=8?"failed":stage>=7?"indexed":"pending",["MemoryIndex/main/M1/Head"]="C0",
            ["MemoryIndex/main/M1/Hash"]=stage>=7?"authored-M1-hash":"old",["RuntimeAgent/deleted/State"]="deleted",
            ["MemoryEdge/M2/M1/co-retrieved/Weight"]=stage>=10?"0.2":"0.1",["MemoryEdge/M1/M2/co-retrieved/Weight"]=stage>=10?"0.2":"0.1",
            ["Session/auto/TodoId"]=stage>=6?"TODO-8":null,["Session/auto/PlanFile"]=stage>=6?"docs/plans/active.md":null
        };
        if(stage>=1) operational.Add("Node/H/LastSeen","fixture-time-2");
        if(stage>=2) operational.Add("Audit/E1/PayloadHash","new-audit-hash");
        if(stage>=3) operational.Add("Session/auto/Dialog","automatic capture");
        if(stage>=11) operational.Add("HostWorkspace/Beta/Path","fixture-host-path");
        if(stage>=12)
        {
            operational.Add("Vector/K1/Value","fixture-vector"); operational.Add("RawChunk/K1/Text","raw chunk"); operational.Add("RawDocument/D1/Text","raw source");
        }
        if(stage>=13) operational.Add("RuntimeAgent/new/State","active");
        if(stage>=15) operational.Add("Session/agent2/Action","agent2 action");
        return new("Atlas","main","C0","C0","Atlas-main-clean-C0",owned?"agent1":null,"other-project-and-control-baseline",
            reverse?content.Reverse().ToDictionary(x=>x.Key,x=>x.Value):content,
            reverse?operational.Reverse().ToDictionary(x=>x.Key,x=>x.Value):operational,stage>=13?["Created:RuntimeAgent/new"]:[]);
    }

    /// <summary>Independent scripted observations and interaction checker, not a production implementation.</summary>
    /// <param name="owned">Expected initial lease.</param><param name="reverse">Reverse valid dictionary order.</param><param name="checkpoint">Corrupted boundary or minus one.</param><param name="fault">Corruption key.</param>
    private sealed class Probe(bool owned, bool reverse, int checkpoint=-1, string fault="") : IPhase0AutomaticWriterProbe
    {
        private int stage, reopens, reads;
        private bool corruptTrace;
        /// <summary>Records actual assertion interactions for the valid script.</summary>
        internal List<string> Calls { get; }=[];
        /// <inheritdoc />
        public void Prepare(bool actualOwned) { Assert.Equal(owned,actualOwned); Calls.Add("prepare"); }
        /// <inheritdoc />
        public void Run(string kind) { Assert.Equal(Schedule[stage],kind); stage++; Calls.Add("run:"+kind); }
        /// <inheritdoc />
        public void Reopen() { reopens++; Calls.Add("reopen"); }
        /// <inheritdoc />
        public Phase0AutomaticWriterState? ReadState()
        {
            Calls.Add("state"); var state=Snapshot(stage,owned,reverse); corruptTrace=reads++==checkpoint;
            if(!corruptTrace) return state;
            if(fault.StartsWith("Authored:",StringComparison.Ordinal)||fault.StartsWith("Operational:",StringComparison.Ordinal))
            {
                var parts=fault.Split(':',3); var map=new Dictionary<string,string?>(parts[0]=="Authored"?state.Authored!:state.Operational!);
                var value=map[parts[2]];
                if(parts[1] is "Drop" or "Key") map.Remove(parts[2]);
                if(parts[1]=="Key") map.Add("wrong:"+parts[2],value);
                if(parts[1]=="Value") map[parts[2]]=value is null?"":value+":wrong";
                return parts[0]=="Authored"?state with {Authored=map}:state with {Operational=map};
            }
            return fault switch
            {
                "StateMissing"=>null,"Project"=>state with {Project="Other"},"Branch"=>state with {Branch="feature"},
                "Head"=>state with {Head="C1"},"Remote"=>state with {Remote="C1"},"WorkingSet"=>state with {WorkingSet="dirty"},
                "Lease"=>state with {Lease=owned?null:"automatic-owner"},"Protected"=>state with {Protected="changed"},"ProtectedMissing"=>state with {Protected=null},
                "AuthoredMissing"=>state with {Authored=null},"OperationalMissing"=>state with {Operational=null},
                "AuthoredExtra"=>state with {Authored=Extra(state.Authored!)},"OperationalExtra"=>state with {Operational=Extra(state.Operational!)},
                "EffectsMissing"=>state with {Effects=null},"EffectsExtra"=>state with {Effects=[..state.Effects!,"unexpected"]},"EffectsDrop"=>state with {Effects=[]},_=>state
            };
        }
        /// <inheritdoc />
        public Phase0AutomaticWriterTrace? ReadTrace()
        {
            Calls.Add("trace"); var trace=new Phase0AutomaticWriterTrace(Schedule.Take(stage).ToArray(),reopens,"native-completed-receipts");
            if(!corruptTrace) return trace;
            return fault switch
            {
                "TraceMissing"=>null,"CompletedMissing"=>trace with {Completed=null},"CompletedExtra"=>trace with {Completed=[..trace.Completed!,"wrong"]},
                "CompletedDrop"=>trace with {Completed=trace.Completed!.Skip(1).ToArray()},"CompletedOrder"=>trace with {Completed=trace.Completed!.Reverse().ToArray()},
                "Reopens"=>trace with {Reopens=reopens+1},"ReceiptMissing"=>trace with {ReceiptBinding=null},"ReceiptEmpty"=>trace with {ReceiptBinding=""},_=>trace
            };
        }
        /// <summary>Adds an unexpected cell while retaining every original fixture value.</summary><param name="cells">Original projection.</param><returns>Corrupted copy.</returns>
        private static Dictionary<string,string?> Extra(IReadOnlyDictionary<string,string?> cells)
            => new(cells) { ["wrong/cell"]="extra" };
    }
}
