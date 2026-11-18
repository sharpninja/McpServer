using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>One independently adjudicated task and its own numeric scores.</summary>
/// <param name="TaskId">Canonical task identity.</param>
/// <param name="Verdict">AGREE or DISAGREE.</param>
/// <param name="Accuracy">Task accuracy, not copied from the overall score.</param>
/// <param name="Completeness">Task completeness.</param>
/// <remarks>2026-11-17: TASK-FEDPH0-009; PLAN-FEDDOLT-001 section15.2; FR-MCP-186; TEST-MCP-234.</remarks>
internal sealed record Phase0TaskVerdict(string TaskId, string Verdict, double Accuracy, double Completeness);

/// <summary>One final-head Nuke gate observation.</summary>
/// <param name="Host">Actual execution host.</param>
/// <param name="Target">Required Nuke target or complete Build.Tests lane.</param>
/// <param name="Head">Actual source commit.</param>
/// <param name="ExitCode">Native exit status.</param>
/// <param name="Total">Executed test cases, zero for non-test targets.</param>
/// <param name="Failed">Failed cases.</param>
/// <param name="Skipped">Skipped cases.</param>
/// <param name="ArtifactKey">TRX for test lanes, log for other targets.</param>
/// <remarks>2026-11-17: TASK-FEDPH0-008/009; FR-MCP-186 ac-RealHostAcceptanceMatrix; TEST-MCP-234.</remarks>
internal sealed record Phase0FinalGate(string Host, string Target, string Head, int ExitCode,
    int Total, int Failed, int Skipped, string ArtifactKey);

/// <summary>Operator decision and evidence for one host's blocking output.</summary>
/// <param name="OutputId">B1 through B12.</param>
/// <param name="Host">Explicit host evidence row.</param>
/// <param name="Status">Observed acceptance state.</param>
/// <param name="ActorRole">Human acceptance is distinct from agent investigation permission.</param>
/// <param name="ActorId">Recorded human identity.</param>
/// <param name="DecisionKey">Retained decision artifact key.</param>
/// <remarks>2026-11-17: plan section5 B1-B12 and TASK-FEDPH0-008/009.</remarks>
internal sealed record Phase0BlockingAcceptance(string OutputId, string Host, string Status,
    string ActorRole, string ActorId, string DecisionKey);

/// <summary>A finding whose operational classification never conceals missing substantive evidence.</summary>
/// <param name="Kind">operational or substantive, as actually adjudicated.</param>
/// <param name="Status">PASS, FAIL or UNKNOWN.</param>
/// <param name="Description">Complete finding description.</param>
/// <remarks>2026-11-17: TASK-FEDPH0-009; current MEMORY-HV-100 rule6.</remarks>
internal sealed record Phase0ExitFinding(string Kind, string Status, string Description);

/// <summary>Manifest for bytes retained separately from the summary.</summary>
/// <param name="Kind">request_jsonl, response_jsonl, verdict, persisted_verdict or evidence.</param>
/// <param name="Key">Unique evidence lookup key.</param>
/// <param name="Sha256">Expected byte hash recorded with the review.</param>
/// <remarks>2026-11-17: TASK-FEDPH0-009; full paired-stream and verdict-readback contract.</remarks>
internal sealed record Phase0ExitArtifact(string Kind, string Key, string Sha256);

/// <summary>Structural observations supplied by the later read-only review-evidence adapter.</summary>
/// <param name="Head">Reviewed commit.</param>
/// <param name="ReviewerHost">Other lab machine.</param>
/// <param name="Model">Actual reviewer runtime model.</param>
/// <param name="Effort">Actual reviewer runtime effort.</param>
/// <param name="SessionId">Reviewer audit session.</param>
/// <param name="RequestId">Reviewer audit turn.</param>
/// <param name="Verdict">Overall adjudication.</param>
/// <param name="Accuracy">Overall accuracy.</param>
/// <param name="Completeness">Overall completeness.</param>
/// <param name="Tasks">Every task's independent adjudication.</param>
/// <param name="Gates">Both hosts' complete final-head gate records.</param>
/// <param name="Outputs">Host-separated B1-B12 operator acceptances.</param>
/// <param name="Findings">All findings, with operational issues retained.</param>
/// <param name="Artifacts">Byte-hashed retained artifacts.</param>
/// <remarks>2026-11-17: TASK-FEDPH0-009 and its exact18-AC union in plan section15.4. This schema cannot establish semantic truth.</remarks>
internal sealed record Phase0GroupExitObservation(string Head, string ReviewerHost, string Model,
    string Effort, string SessionId, string RequestId, string Verdict, double Accuracy, double Completeness,
    IReadOnlyList<Phase0TaskVerdict> Tasks, IReadOnlyList<Phase0FinalGate> Gates,
    IReadOnlyList<Phase0BlockingAcceptance> Outputs, IReadOnlyList<Phase0ExitFinding> Findings,
    IReadOnlyList<Phase0ExitArtifact> Artifacts);

/// <summary>Reads review records and bytes without running builds, tests or a reviewer.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-009; PLAN-FEDDOLT-001 section15.2.</remarks>
internal interface IPhase0GroupExitProbe
{
    /// <summary>Reads the complete review observation.</summary>
    /// <returns>Actual readback, or null when missing.</returns>
    Phase0GroupExitObservation? ReadReview();

    /// <summary>Reads artifact bytes by a manifest key.</summary>
    /// <param name="key">Exact retained artifact identity.</param>
    /// <returns>Bytes, or null when unavailable.</returns>
    byte[]? ReadArtifact(string key);
}

/// <summary>Structural gate assertions shared by mocks and a later real evidence reader.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-009; exact18-AC union and TR/TEST constraints in plan section15.4.
/// No assertion here judges the semantic adequacy or truth of reports, performs hostile validation,
/// authenticates an actor, or authorizes task completion. The independent reviewer must do so.
/// </remarks>
internal static class Phase0GroupExitContract
{
    /// <summary>Rejects missing, stale, incomplete or internally inconsistent acceptance artifacts.</summary>
    /// <param name="probe">Scripted or later actual read-only evidence reader.</param>
    /// <param name="expectedHead">Externally pinned final source head.</param>
    internal static void AssertExitArtifacts(IPhase0GroupExitProbe probe, string expectedHead)
    {
        var actual = probe.ReadReview();
        Assert.NotNull(actual);
        Assert.Equal(expectedHead, actual.Head);
        Assert.Equal("LAB-OMARCHY", actual.ReviewerHost);
        Assert.Equal("gpt-5.6-sol", actual.Model);
        Assert.Equal("max", actual.Effort);
        Assert.False(string.IsNullOrWhiteSpace(actual.SessionId));
        Assert.False(string.IsNullOrWhiteSpace(actual.RequestId));
        AssertScores(actual.Verdict, actual.Accuracy, actual.Completeness);
        Assert.Equal(9, actual.Tasks.Count);
        for (var task = 1; task <= 9; task++)
        {
            var row = Assert.Single(actual.Tasks, value => value.TaskId == $"TASK-FEDPH0-{task:000}");
            AssertScores(row.Verdict, row.Accuracy, row.Completeness);
        }

        string[] hosts = ["PAYTON-LEGION2", "LAB-OMARCHY"];
        string[] targets = ["Compile", "Test", "MigrationIntegrationTests", "Build.Tests", "ValidateTraceability"];
        Assert.Equal(hosts.Length * targets.Length, actual.Gates.Count);
        foreach (var host in hosts)
        foreach (var target in targets)
        {
            var gate = Assert.Single(actual.Gates, row => row.Host == host && row.Target == target);
            Assert.Equal(expectedHead, gate.Head);
            Assert.Equal(0, gate.ExitCode);
            Assert.Equal(0, gate.Failed);
            Assert.Equal(0, gate.Skipped);
            var isTest = target is "Test" or "MigrationIntegrationTests" or "Build.Tests";
            Assert.True(isTest ? gate.Total > 0 : gate.Total == 0);
            AssertArtifactKey(actual, gate.ArtifactKey);
        }

        Assert.Equal(24, actual.Outputs.Count);
        foreach (var host in hosts)
        for (var output = 1; output <= 12; output++)
        {
            var row = Assert.Single(actual.Outputs, value => value.Host == host && value.OutputId == $"B{output}");
            Assert.Equal("accepted", row.Status);
            Assert.Equal("human", row.ActorRole);
            Assert.False(string.IsNullOrWhiteSpace(row.ActorId));
            AssertArtifactKey(actual, row.DecisionKey);
        }

        foreach (var finding in actual.Findings)
        {
            Assert.Contains(finding.Kind, new[] { "operational", "substantive" });
            Assert.Contains(finding.Status, new[] { "PASS", "FAIL", "UNKNOWN" });
            Assert.False(string.IsNullOrWhiteSpace(finding.Description));
            if (finding.Kind == "substantive") Assert.Equal("PASS", finding.Status);
        }

        Assert.Equal(actual.Artifacts.Count, actual.Artifacts.Select(row => row.Key).Distinct(StringComparer.Ordinal).Count());
        foreach (var kind in new[] { "request_jsonl", "response_jsonl", "verdict", "persisted_verdict" })
            Assert.Single(actual.Artifacts, row => row.Kind == kind);
        var verdict = Assert.Single(actual.Artifacts, row => row.Kind == "verdict");
        var readback = Assert.Single(actual.Artifacts, row => row.Kind == "persisted_verdict");
        Assert.Equal(verdict.Sha256, readback.Sha256);
        foreach (var artifact in actual.Artifacts)
        {
            Assert.False(string.IsNullOrWhiteSpace(artifact.Key));
            var bytes = probe.ReadArtifact(artifact.Key);
            Assert.NotNull(bytes);
            Assert.NotEmpty(bytes);
            Assert.Equal(artifact.Sha256, Convert.ToHexString(SHA256.HashData(bytes)));
            if (artifact.Kind is not ("request_jsonl" or "response_jsonl")) continue;
            var lines = Encoding.UTF8.GetString(bytes).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.NotEmpty(lines);
            foreach (var line in lines)
            {
                JsonDocument? parsed = null;
                var exception = Record.Exception(() => parsed = JsonDocument.Parse(line));
                Assert.Null(exception);
                using (parsed) Assert.Equal(JsonValueKind.Object, parsed!.RootElement.ValueKind);
            }
        }
    }

    /// <summary>Checks one verdict without accepting nonfinite or out-of-range numeric scores.</summary>
    /// <param name="verdict">Adjudicated verdict.</param>
    /// <param name="accuracy">Accuracy score.</param>
    /// <param name="completeness">Completeness score.</param>
    private static void AssertScores(string verdict, double accuracy, double completeness)
    {
        Assert.Equal("AGREE", verdict);
        Assert.True(double.IsFinite(accuracy));
        Assert.True(double.IsFinite(completeness));
        Assert.InRange(accuracy, 98, 100);
        Assert.InRange(completeness, 98, 100);
    }

    /// <summary>Requires referenced evidence to have a unique hashed manifest entry.</summary>
    /// <param name="actual">Complete observation.</param>
    /// <param name="key">Referenced artifact identity.</param>
    private static void AssertArtifactKey(Phase0GroupExitObservation actual, string key)
    {
        Assert.False(string.IsNullOrWhiteSpace(key));
        Assert.Single(actual.Artifacts, artifact => artifact.Key == key);
    }
}
