namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent host readback scripts prove lifecycle assertions reject corrupted evidence.</summary>
/// <remarks>
/// 2026-11-18: TASK-FEDPH0-001 B1; FR-MCP-186; TR-MCP-FEDERATION-013 native host lifecycle; TEST-MCP-234.
/// These candidate-policy unit mocks neither install/adopt WinSW nor prove OS, SQL, ACL or locking behavior.
/// 2026-11-18: B1 lifecycle negative controls also mutate shared baseline dictionary instances to detect expectation aliasing.
/// </remarks>
public sealed class Phase0HostLifecycleMockGateTests
{
    /// <summary>Valid independent inventories pass and every lifecycle operation is requested in order.</summary>
    /// <param name="variation">Normal, reordered with new diagnostics, or connected helper with reusable numeric PIDs.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void CorrectLifecyclePasses(int variation)
    {
        var probe = new ScriptedProbe(variation: variation);
        Phase0HostLifecycleContract.AssertLifecycle(probe, Policy());
        Assert.Equal(new[] { "prepare", "config", "read:0", "start", "read:1", "stop", "read:2",
            "start", "read:3", "fail:0", "read:4", "fail:1", "read:5", "fail:2", "read:6",
            "stop", "read:7", "unregister", "read:8" }, probe.Calls);
    }

    /// <summary>Gets distinct configuration, checkpoint, tree, retained-state and recovery corruptions.</summary>
    public static IEnumerable<object[]> Faults
    {
        get
        {
            foreach (var fault in new[] { "Missing", "Name", "Account", "Mode", "Hash", "DelaysMissing", "DelaysCount", "DelaysOrder", "Delay0", "Delay1", "Delay2", "Reboot" })
                yield return [-1, "Config:" + fault];
            for (var checkpoint = 0; checkpoint < 9; checkpoint++)
            {
                foreach (var fault in new[] { "Missing", "Registered", "State", "ProcessesMissing", "DataNull", "DataChanged", "HeadNull", "HeadChanged",
                    "ArtifactsMissing", "StoreMissing", "LogMissing", "StoreChanged", "LogChanged", "UnrelatedMissing", "UnrelatedDropped", "UnrelatedExtra", "UnrelatedChanged" })
                    yield return [checkpoint, fault];
                if (checkpoint > 0)
                    foreach (var sharedFault in new[] { "SharedStoreChanged", "SharedUnrelatedChanged" }) yield return [checkpoint, sharedFault];
                if (checkpoint is 0 or 2 or 7 or 8) yield return [checkpoint, "Orphan"];
                if (checkpoint is 2 or 7)
                    foreach (var fault in new[] { "Forced", "StopMissing", "StopNegative", "StopLate" }) yield return [checkpoint, fault];
                if (checkpoint is 1 or 3 or 4 or 5 or 6)
                {
                    foreach (var fault in new[] { "EmptyTree", "RootMissing", "ChildMissing", "DuplicateIdentity", "ExtraRoot", "ExtraDolt", "DanglingHelper", "HelperCycle" })
                        yield return [checkpoint, fault];
                    for (var process = 0; process < 2; process++)
                    foreach (var corruptField in new[] { "Identity", "Parent", "Role", "Owner", "Account" })
                        yield return [checkpoint, "Process:" + process + ":" + corruptField];
                }
                if (checkpoint is >= 4 and <= 6)
                    foreach (var fault in new[] { "FailedMissing", "FailedWrong", "FailedStillAlive", "RecoveryMissing", "RecoveryNegative", "RecoveryEarly" })
                        yield return [checkpoint, fault];
            }
        }
    }

    /// <summary>Each deliberately wrong independent response must fail the same reusable assertions.</summary>
    /// <param name="checkpoint">Single corrupted state index, or minus one for configuration.</param>
    /// <param name="fault">One corrupt field or missing observation.</param>
    [Theory, MemberData(nameof(Faults))]
    public void WrongLifecycleEvidenceIsRejected(int checkpoint, string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() =>
            Phase0HostLifecycleContract.AssertLifecycle(new ScriptedProbe(checkpoint, fault), Policy()));

    /// <summary>Defines the pending candidate fixture policy, independently of the observation script.</summary>
    private static Phase0HostPolicy Policy() => new("fixture-run-18", "QBrainDoltPh0Run18A", "NT AUTHORITY/LocalService",
        "Manual", "PINNED-CANDIDATE-HOST-HASH", [10_000, 20_000, 60_000], 60_000, ["seeded-store", "original-log"]);

    /// <summary>Fixed response script; no service or lifecycle implementation is simulated by production code.</summary>
    private sealed class ScriptedProbe(int checkpoint = -2, string fault = "", int variation = 0) : IPhase0HostLifecycleProbe
    {
        private int _reads;
        private readonly Dictionary<string, string> _sharedArtifacts = new() { ["seeded-store"] = "STORE-SEED-HASH", ["original-log"] = "LOG-ORIGINAL-HASH" };
        private readonly Dictionary<string, string> _sharedUnrelated = new() { ["live-service"] = "RUNNING-PID-CREATION-A", ["other-project"] = "PROVIDER-AND-DATA-B" };
        /// <summary>Retains independent observed interaction order.</summary>
        internal List<string> Calls { get; } = [];
        /// <inheritdoc />
        public void Prepare(Phase0HostPolicy policy) { _ = policy; Calls.Add("prepare"); }
        /// <inheritdoc />
        public void Start() => Calls.Add("start");
        /// <inheritdoc />
        public void StopGracefully() => Calls.Add("stop");
        /// <inheritdoc />
        public void FailChildAndWait(int ordinal) => Calls.Add("fail:" + ordinal);
        /// <inheritdoc />
        public void Unregister() => Calls.Add("unregister");
        /// <inheritdoc />
        public Phase0HostConfiguration? ReadConfiguration()
        {
            Calls.Add("config");
            var value = new Phase0HostConfiguration("QBrainDoltPh0Run18A", "NT AUTHORITY/LocalService", "Manual",
                "PINNED-CANDIDATE-HOST-HASH", [10_000, 20_000, 60_000], false);
            if (checkpoint != -1) return value;
            return fault switch
            {
                "Config:Missing" => null,
                "Config:Name" => value with { ServiceName = "UnrelatedService" },
                "Config:Account" => value with { Account = "LocalSystem" },
                "Config:Mode" => value with { StartMode = "Automatic" },
                "Config:Hash" => value with { BinaryHash = "wrong-host" },
                "Config:DelaysMissing" => value with { RecoveryDelaysMs = null },
                "Config:DelaysCount" => value with { RecoveryDelaysMs = [10_000, 20_000] },
                "Config:DelaysOrder" => value with { RecoveryDelaysMs = [60_000, 20_000, 10_000] },
                "Config:Delay0" => value with { RecoveryDelaysMs = [1, 20_000, 60_000] },
                "Config:Delay1" => value with { RecoveryDelaysMs = [10_000, 1, 60_000] },
                "Config:Delay2" => value with { RecoveryDelaysMs = [10_000, 20_000, 1] },
                "Config:Reboot" => value with { RebootsMachine = true },
                _ => throw new InvalidOperationException("Unknown configuration fault")
            };
        }

        /// <inheritdoc />
        public Phase0HostState? ReadState()
        {
            var index = _reads++; Calls.Add("read:" + index);
            var running = index is 1 or 3 or 4 or 5 or 6;
            var child = Child(index);
            var processes = new List<Phase0HostProcess>();
            if (running)
            {
                processes.Add(Process("500@host", null, "host"));
                processes.Add(Process(child, "500@host", "dolt"));
                if (variation == 2) processes.Add(Process("502@helper", child, "helper"));
            }
            var artifacts = new Dictionary<string, string> { ["seeded-store"] = "STORE-SEED-HASH", ["original-log"] = "LOG-ORIGINAL-HASH" };
            var unrelated = new Dictionary<string, string> { ["live-service"] = "RUNNING-PID-CREATION-A", ["other-project"] = "PROVIDER-AND-DATA-B" };
            if (variation == 1)
            {
                processes.Reverse(); artifacts = artifacts.Reverse().ToDictionary(); unrelated = unrelated.Reverse().ToDictionary();
                if (index > 0) artifacts["new-diagnostic-" + index] = "NEW-HASH";
            }
            var state = new Phase0HostState(index != 8, index == 8 ? "Absent" : running ? "Running" : "Stopped",
                processes, "COMPLETE-SEEDED-DATA", "UNPUBLISHED-A1", artifacts, unrelated,
                index is 2 or 7 ? 125 : null, false,
                index switch { 4 => 10_025, 5 => 20_100, 6 => 60_050, _ => null },
                index is >= 4 and <= 6 ? Child(index - 1) : null);
            if (fault.StartsWith("Shared", StringComparison.Ordinal))
            {
                if (index == checkpoint)
                {
                    if (fault == "SharedStoreChanged") _sharedArtifacts["seeded-store"] = "mutated-shared-store";
                    else _sharedUnrelated["other-project"] = "mutated-shared-unrelated";
                }
                return state with { Artifacts = _sharedArtifacts, Unrelated = _sharedUnrelated };
            }
            if (index != checkpoint) return state;
            if (fault.StartsWith("Process:", StringComparison.Ordinal))
            {
                var parts = fault.Split(':'); var target = int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                var process = processes[target];
                processes[target] = parts[2] switch
                {
                    "Identity" => process with { Identity = " " },
                    "Parent" => process with { Parent = process.Parent is null ? "outside" : null },
                    "Role" => process with { Role = "unexpected" },
                    "Owner" => process with { Owner = "unrelated-run" },
                    "Account" => process with { Account = "LocalSystem" },
                    _ => throw new InvalidOperationException("Unknown process field")
                };
                return state;
            }
            switch (fault)
            {
                case "StoreMissing": artifacts.Remove("seeded-store"); return state;
                case "LogMissing": artifacts.Remove("original-log"); return state;
                case "StoreChanged": artifacts["seeded-store"] = "changed"; return state;
                case "LogChanged": artifacts["original-log"] = "changed"; return state;
                case "UnrelatedDropped": unrelated.Remove("live-service"); return state;
                case "UnrelatedExtra": unrelated["intruder"] = "unexpected"; return state;
                case "UnrelatedChanged": unrelated["other-project"] = "changed"; return state;
                case "Orphan": processes.Add(Process("orphan@creation", null, "dolt")); return state;
                case "RootMissing": processes.RemoveAt(0); return state;
                case "ChildMissing": processes.RemoveAt(1); return state;
                case "DuplicateIdentity": processes[1] = processes[1] with { Identity = processes[0].Identity }; return state;
                case "ExtraRoot": processes.Add(Process("extra@host", null, "host")); return state;
                case "ExtraDolt": processes.Add(Process("extra@child", "500@host", "dolt")); return state;
                case "DanglingHelper": processes.Add(Process("helper@creation", "absent-parent", "helper")); return state;
                case "HelperCycle":
                    processes.Add(Process("helper@first", "helper@second", "helper"));
                    processes.Add(Process("helper@second", "helper@first", "helper")); return state;
                case "FailedStillAlive": processes.Add(Process(state.FailedChild!, "500@host", "helper")); return state;
            }
            return fault switch
            {
                "Missing" => null,
                "Registered" => state with { Registered = !state.Registered },
                "State" => state with { State = "Wrong" },
                "ProcessesMissing" => state with { Processes = null },
                "EmptyTree" => state with { Processes = [] },
                "DataNull" => state with { DataFingerprint = null },
                "DataChanged" => state with { DataFingerprint = "wrong-data" },
                "HeadNull" => state with { UnpublishedHead = null },
                "HeadChanged" => state with { UnpublishedHead = "wrong-head" },
                "ArtifactsMissing" => state with { Artifacts = null },
                "UnrelatedMissing" => state with { Unrelated = null },
                "Forced" => state with { ForcedTermination = true },
                "StopMissing" => state with { StopElapsedMs = null },
                "StopNegative" => state with { StopElapsedMs = -1 },
                "StopLate" => state with { StopElapsedMs = 60_001 },
                "FailedMissing" => state with { FailedChild = null },
                "FailedWrong" => state with { FailedChild = "not-the-failed-child" },
                "RecoveryMissing" => state with { RecoveryElapsedMs = null },
                "RecoveryNegative" => state with { RecoveryElapsedMs = -1 },
                "RecoveryEarly" => state with { RecoveryElapsedMs = index switch { 4 => 9_999, 5 => 19_999, _ => 59_999 } },
                _ => throw new InvalidOperationException("Unknown state fault: " + fault)
            };
        }

        /// <summary>Fixed observed creation identities allow the numeric child PID to be reused safely.</summary>
        private static string Child(int index) => index switch
        {
            1 => "501@first", 3 => "501@second", 4 => "501@recovered-one",
            5 => "501@recovered-two", 6 => "501@recovered-three", _ => "no-child"
        };
        /// <summary>Constructs a fixture process without reading the assertion policy.</summary>
        private static Phase0HostProcess Process(string identity, string? parent, string role)
            => new(identity, parent, role, "fixture-run-18", "NT AUTHORITY/LocalService");
    }
}