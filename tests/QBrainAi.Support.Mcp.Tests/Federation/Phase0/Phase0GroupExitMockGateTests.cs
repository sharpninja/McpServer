using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit.Sdk;

namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Proves the group-exit assertions discriminate missing or inconsistent review evidence.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-009; PLAN-FEDDOLT-001 sections5/15.2/15.4;
/// FR-MCP-175/177/179/180/182/184/185/186/187/189, exact18-AC union assigned to task009;
/// TR-MCP-FEDERATION-002/004/006/007/009/011/012/013/014/016;
/// TEST-MCP-223/225/227/228/230/232/233/234/235/237.
/// All bytes and observations are in-memory fakes. No reviewer, file, service, DB or task-state mutation occurs.
/// A passing mock is not actual HV, operator acceptance or evidence of product behavior.
/// </remarks>
public sealed class Phase0GroupExitMockGateTests
{
    private const string Head = "1111111111111111111111111111111111111111";

    /// <summary>Accepts complete coherent mocks at every boundary score permitted by the gate.</summary>
    /// <param name="score">Independent fixture score at98,99 or100.</param>
    [Theory]
    [InlineData(98)]
    [InlineData(99)]
    [InlineData(100)]
    public void CorrectMockArtifactsAreAccepted(double score)
    {
        var probe = CreateProbe();
        probe.Observation = probe.Observation! with
        {
            Accuracy = score, Completeness = score,
            Tasks = probe.Observation.Tasks.Select(row => row with { Accuracy = score, Completeness = score }).ToArray()
        };
        Phase0GroupExitContract.AssertExitArtifacts(probe, Head);
    }

    /// <summary>Operational-only issues stay recorded without overruling a substantively complete AGREE.</summary>
    /// <param name="status">An operational FAIL or UNKNOWN with otherwise complete required evidence.</param>
    [Theory]
    [InlineData("FAIL")]
    [InlineData("UNKNOWN")]
    public void CorrectMockOperationalIssuesDoNotInvalidateSubstantiveAgree(string status)
    {
        var probe = CreateProbe();
        probe.Observation = probe.Observation! with
        { Findings = [new("operational", status, "Retained infrastructure issue; required evidence remains available.")] };
        Phase0GroupExitContract.AssertExitArtifacts(probe, Head);
    }

    /// <summary>Each malformed mock must fail a real xUnit assertion rather than throw an unrelated exception.</summary>
    /// <param name="fault">Exactly one independent evidence corruption.</param>
    [Theory]
    [MemberData(nameof(NegativeControls))]
    public void NegativeControlRejectsInvalidArtifacts(string fault)
    {
        var probe = CreateProbe();
        ApplyFault(probe, fault);
        var error = Record.Exception(() => Phase0GroupExitContract.AssertExitArtifacts(probe, Head));
        Assert.IsAssignableFrom<XunitException>(error);
    }

    /// <summary>Enumerates corruptions of every task, host gate and blocking-output row, plus manifest boundaries.</summary>
    /// <returns>Stable, credential-free negative-control identifiers.</returns>
    public static IEnumerable<object[]> NegativeControls()
    {
        foreach (var field in new[] { "missing", "head", "host", "model", "effort", "session", "request", "verdict", "accuracy", "completeness", "nan", "infinity", "above100", "negative" })
            yield return [$"header:{field}"];
        for (var task = 0; task < 9; task++)
        foreach (var field in new[] { "omit", "duplicate", "id", "verdict", "accuracy", "completeness", "nan" })
            yield return [$"task:{task}:{field}"];
        for (var gate = 0; gate < 10; gate++)
        foreach (var field in new[] { "omit", "duplicate", "host", "target", "head", "exit", "total", "failed", "skipped", "artifact" })
            yield return [$"gate:{gate}:{field}"];
        for (var output = 0; output < 24; output++)
        foreach (var field in new[] { "omit", "duplicate", "host", "id", "status", "role", "actor", "artifact" })
            yield return [$"output:{output}:{field}"];
        for (var artifact = 0; artifact < 4; artifact++)
        foreach (var field in new[] { "omit", "duplicate", "kind", "bytes", "missing", "empty", "hash" })
            yield return [$"artifact:{artifact}:{field}"];
        foreach (var field in new[] { "substantiveFail", "substantiveUnknown", "kind", "status", "description", "requestJson", "responseJson", "readback" })
            yield return [$"other:{field}"];
    }

    /// <summary>Creates independently specified valid evidence, with exact hashes of fake bytes.</summary>
    /// <returns>A scripted probe, never an adapter to actual files or live stores.</returns>
    private static Probe CreateProbe()
    {
        var bytes = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var artifacts = new List<Phase0ExitArtifact>();
        void Add(string kind, string key, byte[] payload)
        {
            bytes.Add(key, payload);
            artifacts.Add(new(kind, key, Convert.ToHexString(SHA256.HashData(payload))));
        }
        Add("request_jsonl", "request", JsonSerializer.SerializeToUtf8Bytes(new { type = "request", head = Head }));
        Add("response_jsonl", "response", JsonSerializer.SerializeToUtf8Bytes(new { type = "final", verdict = "AGREE" }));
        Add("verdict", "verdict", Encoding.UTF8.GetBytes("Complete fixture verdict body with all findings."));
        Add("persisted_verdict", "readback", Encoding.UTF8.GetBytes("Complete fixture verdict body with all findings."));
        var tasks = Enumerable.Range(1, 9).Select(index => new Phase0TaskVerdict($"TASK-FEDPH0-{index:000}", "AGREE", 99, 99)).ToArray();
        var gates = new List<Phase0FinalGate>();
        var outputs = new List<Phase0BlockingAcceptance>();
        foreach (var host in new[] { "PAYTON-LEGION2", "LAB-OMARCHY" })
        {
            foreach (var target in new[] { "Compile", "Test", "MigrationIntegrationTests", "Build.Tests", "ValidateTraceability" })
            {
                var key = host + "/" + target;
                Add("evidence", key, Encoding.UTF8.GetBytes("Independently specified fixture evidence: " + key));
                var total = target is "Test" or "MigrationIntegrationTests" or "Build.Tests" ? 20 : 0;
                gates.Add(new(host, target, Head, 0, total, 0, 0, key));
            }
            for (var output = 1; output <= 12; output++)
            {
                var key = host + "/B" + output;
                Add("evidence", key, Encoding.UTF8.GetBytes("Fixture human acceptance decision: " + key));
                outputs.Add(new($"B{output}", host, "accepted", "human", "fixture-operator", key));
            }
        }
        return new(new(Head, "LAB-OMARCHY", "gpt-5.6-sol", "max", "fixture-review-session", "fixture-review-turn",
            "AGREE", 99, 99, tasks, gates, outputs, [], artifacts), bytes);
    }

    /// <summary>Changes one contract observable without calculating expectations from the assertions.</summary>
    /// <param name="probe">Valid fixture to corrupt.</param>
    /// <param name="fault">Stable corruption selector.</param>
    private static void ApplyFault(Probe probe, string fault)
    {
        var parts = fault.Split(':');
        var current = probe.Observation!;
        var index = parts.Length == 3 ? int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture) : 0;
        var field = parts[^1];
        switch (parts[0])
        {
            case "header":
                probe.Observation = field switch
                {
                    "missing" => null, "head" => current with { Head = new('2', 40) },
                    "host" => current with { ReviewerHost = "PAYTON-LEGION2" },
                    "model" => current with { Model = "different-model" }, "effort" => current with { Effort = "high" },
                    "session" => current with { SessionId = " " }, "request" => current with { RequestId = "" },
                    "verdict" => current with { Verdict = "DISAGREE" }, "accuracy" => current with { Accuracy = 97.99 },
                    "completeness" => current with { Completeness = 97.99 }, "nan" => current with { Accuracy = double.NaN },
                    "infinity" => current with { Completeness = double.PositiveInfinity }, "above100" => current with { Accuracy = 101 },
                    "negative" => current with { Completeness = -1 }, _ => throw new InvalidOperationException(fault)
                };
                break;
            case "task":
                var tasks = current.Tasks.ToList();
                var task = tasks[index];
                if (field == "omit") tasks.RemoveAt(index);
                else if (field == "duplicate") tasks.Add(task);
                else tasks[index] = field switch
                {
                    "id" => task with { TaskId = "TASK-FEDPH0-999" }, "verdict" => task with { Verdict = "DISAGREE" },
                    "accuracy" => task with { Accuracy = 97.99 }, "completeness" => task with { Completeness = 97.99 },
                    "nan" => task with { Completeness = double.NaN }, _ => throw new InvalidOperationException(fault)
                };
                probe.Observation = current with { Tasks = tasks };
                break;
            case "gate":
                var gates = current.Gates.ToList();
                var gate = gates[index];
                if (field == "omit") gates.RemoveAt(index);
                else if (field == "duplicate") gates.Add(gate);
                else gates[index] = field switch
                {
                    "host" => gate with { Host = "unknown-host" }, "target" => gate with { Target = "FocusedTest" },
                    "head" => gate with { Head = new('2', 40) }, "exit" => gate with { ExitCode = 1 },
                    "total" => gate with { Total = gate.Total == 0 ? 1 : 0 }, "failed" => gate with { Failed = 1 },
                    "skipped" => gate with { Skipped = 1 }, "artifact" => gate with { ArtifactKey = "unretained" },
                    _ => throw new InvalidOperationException(fault)
                };
                probe.Observation = current with { Gates = gates };
                break;
            case "output":
                var outputs = current.Outputs.ToList();
                var output = outputs[index];
                if (field == "omit") outputs.RemoveAt(index);
                else if (field == "duplicate") outputs.Add(output);
                else outputs[index] = field switch
                {
                    "host" => output with { Host = "unknown-host" }, "id" => output with { OutputId = "B99" },
                    "status" => output with { Status = "unproven" }, "role" => output with { ActorRole = "agent" },
                    "actor" => output with { ActorId = " " }, "artifact" => output with { DecisionKey = "unretained" },
                    _ => throw new InvalidOperationException(fault)
                };
                probe.Observation = current with { Outputs = outputs };
                break;
            case "artifact":
                var artifacts = current.Artifacts.ToList();
                var artifact = artifacts[index];
                if (field == "omit") artifacts.RemoveAt(index);
                else if (field == "duplicate") artifacts.Add(artifact);
                else if (field == "kind") artifacts[index] = artifact with { Kind = "evidence" };
                else if (field == "bytes") probe.Bytes[artifact.Key] = Encoding.UTF8.GetBytes("corrupted");
                else if (field == "missing") probe.Bytes.Remove(artifact.Key);
                else if (field == "empty") probe.Bytes[artifact.Key] = [];
                else if (field == "hash") artifacts[index] = artifact with { Sha256 = "" };
                else throw new InvalidOperationException(fault);
                probe.Observation = current with { Artifacts = artifacts };
                break;
            case "other":
                if (field is "requestJson" or "responseJson")
                {
                    var key = field == "requestJson" ? "request" : "response";
                    probe.Bytes[key] = Encoding.UTF8.GetBytes("[]");
                    probe.Observation = current with { Artifacts = current.Artifacts.Select(row => row.Key == key
                        ? row with { Sha256 = Convert.ToHexString(SHA256.HashData(probe.Bytes[key])) } : row).ToArray() };
                }
                else if (field == "readback")
                {
                    probe.Bytes["readback"] = Encoding.UTF8.GetBytes("truncated verdict");
                    probe.Observation = current with { Artifacts = current.Artifacts.Select(row => row.Key == "readback"
                        ? row with { Sha256 = Convert.ToHexString(SHA256.HashData(probe.Bytes["readback"])) } : row).ToArray() };
                }
                else probe.Observation = current with { Findings = [field switch
                {
                    "substantiveFail" => new("substantive", "FAIL", "Required behavior failed."),
                    "substantiveUnknown" => new("substantive", "UNKNOWN", "Required evidence inaccessible."),
                    "kind" => new("ignored", "FAIL", "Unclassified issue."),
                    "status" => new("operational", "IGNORED", "Invalid status."),
                    "description" => new("operational", "FAIL", " "), _ => throw new InvalidOperationException(fault)
                }] };
                break;
            default: throw new InvalidOperationException(fault);
        }
    }

    /// <summary>Scripted evidence boundary that cannot consult actual files or live acceptance state.</summary>
    /// <param name="observation">Independent initial fixture.</param>
    /// <param name="bytes">Independent retained-byte fixture.</param>
    private sealed class Probe(Phase0GroupExitObservation observation, Dictionary<string, byte[]> bytes) : IPhase0GroupExitProbe
    {
        /// <summary>Fixture observation, mutable only to inject a named negative control.</summary>
        internal Phase0GroupExitObservation? Observation { get; set; } = observation;
        /// <summary>Fixture bytes, never actual review or credential data.</summary>
        internal Dictionary<string, byte[]> Bytes { get; } = bytes;
        /// <inheritdoc />
        public Phase0GroupExitObservation? ReadReview() => Observation;
        /// <inheritdoc />
        public byte[]? ReadArtifact(string key) => Bytes.GetValueOrDefault(key);
    }
}
