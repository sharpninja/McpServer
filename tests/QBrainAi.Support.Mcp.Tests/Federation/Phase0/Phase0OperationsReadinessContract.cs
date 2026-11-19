namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>One independently captured elapsed sample, including failed operation attempts.</summary>
/// <param name="Operation">Observed operation.</param><param name="Ordinal">One-based repetition.</param><param name="Milliseconds">Elapsed time.</param><param name="Succeeded">Observed outcome.</param>
/// <remarks>2026-11-19: TASK-FEDPH0-007, plan section5 B10; measurement evidence, no performance SLA.</remarks>
internal sealed record Phase0ScaleSample(string Operation, int Ordinal, double Milliseconds, bool Succeeded);

/// <summary>Raw measurement capture read independently of the aggregate report.</summary>
/// <param name="Host">Actual host.</param><param name="Rows">Exercised rows.</param><param name="Commits">History commits.</param><param name="Subscribers">Subscribers.</param>
/// <param name="PayloadBytes">Payload width.</param><param name="FixtureHash">Prepared fixture SHA256.</param><param name="Samples">All observations.</param>
/// <param name="DataBefore">Database bytes before.</param><param name="DataAfter">Database bytes after.</param><param name="HistoryBefore">History bytes before.</param><param name="HistoryAfter">History bytes after.</param>
/// <param name="PeakMemory">Peak working set.</param><param name="CpuMilliseconds">CPU usage.</param><param name="MemoryBudget">Experiment memory budget.</param><param name="StorageBudget">Experiment storage budget.</param>
/// <remarks>2026-11-19: TASK-FEDPH0-007 B10; scripted observations do not establish native measurements.</remarks>
internal sealed record Phase0ScaleCapture(string Host, int Rows, int Commits, int Subscribers, int PayloadBytes, string FixtureHash,
    Phase0ScaleSample[] Samples, long DataBefore, long DataAfter, long HistoryBefore, long HistoryAfter,
    long PeakMemory, double CpuMilliseconds, long MemoryBudget, long StorageBudget);

/// <summary>Reported observations and exercised limits, never an unsupported SLA.</summary>
/// <param name="Capture">Reported raw values.</param><param name="DataGrowth">Signed database growth.</param><param name="HistoryGrowth">Signed history growth.</param>
/// <param name="P50">Nearest-rank median.</param><param name="P95">Nearest-rank95th percentile.</param><param name="Maximum">Maximum.</param><param name="Failures">Failed attempts.</param>
/// <param name="BudgetExceeded">Resource-limit finding.</param><param name="RowLimit">Exercised rows.</param><param name="CommitLimit">Exercised commits.</param><param name="SubscriberLimit">Exercised subscribers.</param><param name="ClaimsSla">Unsupported SLA claim.</param>
/// <remarks>2026-11-19: TASK-FEDPH0-007 B10; reporting a finding does not accept an operating budget.</remarks>
internal sealed record Phase0ScaleReport(Phase0ScaleCapture Capture, long DataGrowth, long HistoryGrowth, double P50, double P95, double Maximum,
    int Failures, bool BudgetExceeded, int RowLimit, int CommitLimit, int SubscriberLimit, bool ClaimsSla);

/// <summary>Ordered restore boundary read from independent process/storage receipts.</summary>
/// <param name="Stage">Completed boundary.</param><param name="IngressOpen">Listener state.</param><param name="Epoch">Durable epoch.</param><param name="Generation">Epoch-scoped generation.</param>
/// <param name="HubHead">Public head.</param><param name="NodeHead">Retained node head.</param><param name="AuditIds">Durable audit identities.</param><param name="Components">Verified backup manifest.</param>
/// <param name="AncestryVerified">Ancestry validation.</param><param name="ForceResets">Destructive resets.</param><param name="NewHistoryCommits">Invented history.</param><param name="Status">Old-report status.</param><param name="Code">Old-report code.</param>
/// <param name="OriginalFingerprint">Retained originals.</param><param name="UnrelatedFingerprint">Unrelated resources.</param><param name="NativeReceipt">Retained completed-operation receipt key.</param>
/// <remarks>2026-11-19: FR-MCP-179 ac-HubRestoreFromOlderBackup; TR-MCP-FEDERATION-006/008; TEST-MCP-227; B12.</remarks>
internal sealed record Phase0RestoreCheckpoint(string Stage, bool IngressOpen, Guid Epoch, long Generation, string HubHead, string NodeHead,
    string[] AuditIds, string[] Components, bool AncestryVerified, int ForceResets, int NewHistoryCommits, int Status, string? Code,
    string OriginalFingerprint, string UnrelatedFingerprint, string NativeReceipt);

/// <summary>Public-only trust policy or independently observed configuration.</summary>
/// <param name="Host">Host.</param><param name="Owner">Approved trust owner.</param><param name="Issuer">Issuer.</param><param name="RootHash">Root SHA256.</param>
/// <param name="Pins">Current/next SPKI hashes.</param><param name="Sans">Leaf SANs.</param><param name="Expires">Leaf expiry.</param><param name="Observed">Observation time.</param>
/// <param name="Rotation">Rotation procedure.</param><param name="Bootstrap">Established trust-distribution route.</param><param name="Tofu">Trust-on-first-use.</param><param name="Bypass">Validation bypass.</param><param name="Listeners">Owned addresses.</param>
/// <remarks>2026-11-19: FR-MCP-185 ac-UnknownCertificatePinRejected; TR-MCP-FEDERATION-012; TEST-MCP-233; B12.</remarks>
internal sealed record Phase0PublicTrust(string Host, string Owner, string Issuer, string RootHash, string[] Pins, string[] Sans,
    DateTimeOffset Expires, DateTimeOffset Observed, string Rotation, string Bootstrap, bool Tofu, bool Bypass, IReadOnlyDictionary<string, string> Listeners);

/// <summary>Frozen inputs selected before probe preparation; never populated from returned observations.</summary>
/// <param name="FixtureHash">Prepared workload SHA256.</param><param name="MemoryBudget">Experiment memory budget.</param><param name="StorageBudget">Experiment storage budget.</param>
/// <param name="OriginalFingerprint">Original-resource baseline.</param><param name="UnrelatedFingerprint">Unrelated-resource baseline.</param><param name="Trust">Approved public policy.</param><param name="Detector">Pinned detector/configuration identity.</param>
/// <remarks>2026-11-19: TASK-FEDPH0-007 B10/B12; native binding supplies actual approved public identities without changing assertions.</remarks>
internal sealed record Phase0OperationsInputs(string FixtureHash, long MemoryBudget, long StorageBudget, string OriginalFingerprint,
    string UnrelatedFingerprint, Phase0PublicTrust Trust, string Detector);

/// <summary>Secret-preview and disclosure effects containing only safe diagnostics.</summary>
/// <param name="Status">Preview status.</param><param name="Code">Preview code.</param><param name="Commits">History commits.</param><param name="Head">Retained head.</param><param name="Draft">Retained draft fingerprint.</param>
/// <param name="Detector">Pinned detector identity.</param><param name="RotationRequested">Credential rotation required.</param><param name="LiveRemovalRequested">Live removal/revert required.</param>
/// <param name="HistoryRewrites">Unapproved history rewrites.</param><param name="Reclones">Unapproved re-clones.</param><param name="ForcePushes">Force pushes.</param><param name="Diagnostics">Safe response/log text.</param>
/// <remarks>2026-11-19: FR-MCP-184 ac-ReceiptsAndLogsContainNoCredentials/ac-CommitPreviewBlocksSecret; TR-MCP-FEDERATION-011; TEST-MCP-232; B12.</remarks>
internal sealed record Phase0SecretResponse(int Status, string Code, int Commits, string Head, string Draft, string Detector,
    bool RotationRequested, bool LiveRemovalRequested, int HistoryRewrites, int Reclones, int ForcePushes, string Diagnostics);

/// <summary>Test-only missing B10/B12 boundaries. Native execution waits for the all-nine barrier.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-007; FR-MCP-179/184/185; TR-MCP-FEDERATION-006/008/011/012; TEST-MCP-227/232/233.</remarks>
internal interface IPhase0OperationsReadinessProbe
{
    /// <summary>Prepares isolated fixture inputs.</summary><param name="host">Host label.</param>
    void Prepare(string host);
    /// <summary>Executes the operation matrix.</summary><returns>Aggregate result.</returns>
    Phase0ScaleReport? RunScale();
    /// <summary>Independently reads retained raw captures.</summary><returns>Raw observation.</returns>
    Phase0ScaleCapture? ReadScaleCapture();
    /// <summary>Performs older-backup restore and retained-history/audit recovery.</summary>
    void RestoreOlderBackup();
    /// <summary>Reads actual ordered operation/storage evidence.</summary><returns>Restore boundaries.</returns>
    IReadOnlyList<Phase0RestoreCheckpoint>? ReadRestoreTrace();
    /// <summary>Reads public trust/listener observations.</summary><returns>Configuration.</returns>
    Phase0PublicTrust? ReadPublicTrust();
    /// <summary>Previews a protected fixture and handles disclosure without rewrite authorization.</summary><param name="canary">Invocation-private value.</param><returns>Safe effects.</returns>
    Phase0SecretResponse? PreviewAndHandleDisclosure(string canary);
}

/// <summary>Reusable B10/B12 assertions, combined with existing B11, retention, TLS and secret-channel assertions.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-007; canonical identities unchanged. No native feasibility or B-output acceptance claimed.</remarks>
internal static class Phase0OperationsReadinessContract
{
    /// <summary>Checks raw/report equality and truthful measured bounds without imposing a performance SLA.</summary>
    /// <param name="probe">Disposable boundary.</param><param name="host">Host.</param><param name="inputs">Preselected inputs.</param>
    internal static void AssertScale(IPhase0OperationsReadinessProbe probe, string host, Phase0OperationsInputs inputs)
    {
        probe.Prepare(host);
        var report = probe.RunScale(); Assert.NotNull(report);
        var raw = probe.ReadScaleCapture(); Assert.NotNull(raw);
        Assert.Equal(host, raw.Host); Assert.Equal(100, raw.Rows); Assert.Equal(10, raw.Commits); Assert.Equal(3, raw.Subscribers); Assert.Equal(128, raw.PayloadBytes);
        Assert.Matches("\\A[0-9A-Fa-f]{64}\\z", inputs.FixtureHash); Assert.Equal(inputs.FixtureHash, raw.FixtureHash);
        Assert.Equal(inputs.MemoryBudget, raw.MemoryBudget); Assert.Equal(inputs.StorageBudget, raw.StorageBudget);
        Assert.Equal(15, raw.Samples.Length);
        var expectedKeys = from op in new[] { "save", "query", "commit", "poll", "fetch" } from ordinal in new[] { 1, 2, 3 } select op + "/" + ordinal;
        Assert.Equal(expectedKeys.Order(), raw.Samples.Select(s => s.Operation + "/" + s.Ordinal).Order());
        Assert.All(raw.Samples, s => Assert.True(double.IsFinite(s.Milliseconds) && s.Milliseconds >= 0));
        Assert.True(raw.DataBefore >= 0 && raw.DataAfter >= 0 && raw.HistoryBefore >= 0 && raw.HistoryAfter >= 0 && raw.PeakMemory >= 0);
        Assert.True(double.IsFinite(raw.CpuMilliseconds) && raw.CpuMilliseconds >= 0); Assert.True(raw.MemoryBudget > 0 && raw.StorageBudget > 0);
        var noSamples = Array.Empty<Phase0ScaleSample>();
        Assert.Equal(raw with { Samples = noSamples }, report.Capture with { Samples = noSamples });
        Assert.Equal(raw.Samples.OrderBy(SampleKey), report.Capture.Samples.OrderBy(SampleKey));
        Assert.Equal(raw.DataAfter - raw.DataBefore, report.DataGrowth); Assert.Equal(raw.HistoryAfter - raw.HistoryBefore, report.HistoryGrowth);
        var elapsed = raw.Samples.Select(s => s.Milliseconds).Order().ToArray();
        Assert.Equal(elapsed[7], report.P50); Assert.Equal(elapsed[14], report.P95); Assert.Equal(elapsed[14], report.Maximum);
        Assert.Equal(raw.Samples.Count(s => !s.Succeeded), report.Failures);
        Assert.Equal(raw.PeakMemory > raw.MemoryBudget || (decimal)raw.DataAfter + raw.HistoryAfter > raw.StorageBudget, report.BudgetExceeded);
        Assert.InRange(report.RowLimit, 1, raw.Rows); Assert.InRange(report.CommitLimit, 1, raw.Commits); Assert.InRange(report.SubscriberLimit, 1, raw.Subscribers);
        Assert.False(report.ClaimsSla);
    }

    /// <summary>Checks ordering, epoch fencing, original retention, ancestry recovery and audit resend.</summary>
    /// <param name="probe">Disposable boundary.</param><param name="host">Host.</param><param name="inputs">Preselected inputs.</param>
    internal static void AssertRestoreOrdering(IPhase0OperationsReadinessProbe probe, string host, Phase0OperationsInputs inputs)
    {
        probe.Prepare(host); probe.RestoreOlderBackup();
        var trace = probe.ReadRestoreTrace(); Assert.NotNull(trace);
        var stages = new[] { "ingress-disabled", "checkpoints-restored", "epoch-persisted", "heads-verified", "listeners-open", "old-report-rejected", "ancestry-recovered", "audit-reconciled", "reopened" };
        Assert.Equal(stages, trace.Select(s => s.Stage));
        var oldEpoch = Guid.Parse("11000000-0000-0000-0000-000000000001");
        var newEpoch = trace[2].Epoch; Assert.NotEqual(Guid.Empty, newEpoch); Assert.NotEqual(oldEpoch, newEpoch);
        var recoveredGeneration = trace[6].Generation; Assert.True(recoveredGeneration > 40);
        for (var index = 0; index < trace.Count; index++)
        {
            var row = trace[index]; Assert.Equal(index >= 4, row.IngressOpen); Assert.Equal(index < 2 ? oldEpoch : newEpoch, row.Epoch);
            Assert.Equal(index == 0 ? 90 : index < 6 ? 40 : recoveredGeneration, row.Generation);
            Assert.Equal(index >= 6 ? "A1" : "C0", row.HubHead); Assert.Equal("A1", row.NodeHead);
            Assert.Equal(index >= 7 ? new[] { "E0", "E1" } : new[] { "E0" }, row.AuditIds.Order());
            Assert.Equal(new[] { "audit", "control", "identity-config", "identity-operational", "objects-refs", "outbox", "permissions" }, row.Components.Order());
            Assert.Equal(index >= 6, row.AncestryVerified); Assert.Equal(0, row.ForceResets); Assert.Equal(0, row.NewHistoryCommits);
            Assert.Equal(index == 5 ? 409 : 200, row.Status); Assert.Equal(index == 5 ? "hub_epoch_changed" : null, row.Code);
            Assert.Equal(inputs.OriginalFingerprint, row.OriginalFingerprint); Assert.Equal(inputs.UnrelatedFingerprint, row.UnrelatedFingerprint);
            Assert.False(string.IsNullOrWhiteSpace(row.NativeReceipt));
        }
    }

    /// <summary>Checks actual configuration against public policy frozen before endpoint setup.</summary>
    /// <param name="probe">Disposable boundary.</param><param name="host">Host.</param><param name="inputs">Preselected inputs.</param>
    internal static void AssertPublicTrust(IPhase0OperationsReadinessProbe probe, string host, Phase0OperationsInputs inputs)
    {
        var expected = inputs.Trust;
        probe.Prepare(host); var trust = probe.ReadPublicTrust(); Assert.NotNull(trust);
        Assert.Equal(host, trust.Host); Assert.Equal(expected.Owner, trust.Owner); Assert.Equal(expected.Issuer, trust.Issuer);
        Assert.Equal(expected.RootHash, trust.RootHash); Assert.Equal(expected.Pins.Order(), trust.Pins.Order()); Assert.Equal(expected.Sans.Order(), trust.Sans.Order());
        Assert.Equal(expected.Expires, trust.Expires); Assert.True(trust.Expires > trust.Observed);
        Assert.Equal(expected.Rotation, trust.Rotation); Assert.Equal(expected.Bootstrap, trust.Bootstrap); Assert.False(trust.Tofu); Assert.False(trust.Bypass);
        Assert.False(string.IsNullOrWhiteSpace(trust.Owner)); Assert.False(string.IsNullOrWhiteSpace(trust.Rotation));
        Assert.Matches("\\A[0-9A-Fa-f]{64}\\z", trust.RootHash); Assert.Equal(2, trust.Pins.Distinct().Count());
        Assert.All(trust.Pins, pin => Assert.Matches("\\A[0-9A-Fa-f]{64}\\z", pin));
        Assert.Equal(expected.Listeners.OrderBy(p => p.Key), trust.Listeners.OrderBy(p => p.Key));
    }

    /// <summary>Checks safe preview refusal and disclosure response without unapproved history actions.</summary>
    /// <param name="probe">Disposable boundary.</param><param name="host">Host.</param><param name="inputs">Preselected inputs.</param><param name="canary">Private value.</param>
    internal static void AssertSecretResponse(IPhase0OperationsReadinessProbe probe, string host, Phase0OperationsInputs inputs, string canary)
    {
        probe.Prepare(host); var result = probe.PreviewAndHandleDisclosure(canary); Assert.NotNull(result);
        foreach (var text in new[] { result.Code, result.Head, result.Draft, result.Detector, result.Diagnostics })
            Assert.True(!text.Contains(canary, StringComparison.Ordinal), "Secret fixture leaked into captured response.");
        Assert.Equal(422, result.Status); Assert.Equal("secret_detected", result.Code); Assert.Equal(0, result.Commits); Assert.Equal("C0", result.Head); Assert.Equal("retained-draft", result.Draft);
        Assert.Equal(inputs.Detector, result.Detector); Assert.True(result.RotationRequested); Assert.True(result.LiveRemovalRequested);
        Assert.Equal(0, result.HistoryRewrites); Assert.Equal(0, result.Reclones); Assert.Equal(0, result.ForcePushes);
    }

    private static string SampleKey(Phase0ScaleSample sample) => sample.Operation + "/" + sample.Ordinal;
}

