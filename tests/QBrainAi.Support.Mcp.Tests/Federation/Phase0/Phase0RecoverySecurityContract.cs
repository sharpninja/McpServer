namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent hub restore, report and ancestry observations.</summary>
/// <param name="HubEpoch">Epoch published by the reopened hub.</param><param name="Generation">Restored generation.</param>
/// <param name="NodeHead">Retained node head.</param><param name="HubHead">Observed hub head.</param>
/// <param name="ForceResets">Observed force-reset attempts.</param><param name="OldReportStatus">Old-epoch report status.</param>
/// <param name="OldReportError">Old-epoch report error.</param>
/// <remarks>2026-11-17: FR-MCP-179 ac-HubRestoreFromOlderBackup; TR-MCP-FEDERATION-006; TEST-MCP-227.</remarks>
internal sealed record Phase0RestoreObservation(Guid HubEpoch, long Generation, string NodeHead, string HubHead,
    int ForceResets, int OldReportStatus, string OldReportError);

/// <summary>Observed ancestry recovery without prescribing the hub's final merge head or generation.</summary>
/// <param name="NodeHead">Retained node head.</param><param name="AncestryVerified">Ancestry verification result.</param>
/// <param name="PublishedAncestors">Readback of published history.</param><param name="ForceResets">Force-reset attempts.</param>
/// <remarks>2026-11-17: FR-MCP-179 ac-HubRestoreFromOlderBackup; TEST-MCP-227.</remarks>
internal sealed record Phase0AncestryRecovery(string NodeHead, bool AncestryVerified,
    IReadOnlyCollection<string> PublishedAncestors, int ForceResets);

/// <summary>Disposable restore boundary; a later real adapter must observe persisted refs and report responses.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-007; FR-MCP-179; TR-MCP-FEDERATION-006; TEST-MCP-227.</remarks>
internal interface IPhase0RestoreProbe
{
    /// <summary>Remembers epoch E1 generation90 and node A1, with a hub backup at C0 generation40.</summary>
    /// <param name="rememberedEpoch">Independent nonempty original epoch.</param>
    void Prepare(Guid rememberedEpoch);
    /// <summary>Restores and reopens the hub, attempts an E1 report, and independently reads both heads.</summary>
    /// <returns>Actual observations before ancestry recovery.</returns>
    Phase0RestoreObservation? RestoreAndReport();
    /// <summary>Verifies ancestry, republishes missing history and reads back retained and published history.</summary>
    /// <returns>Actual recovery observations.</returns>
    Phase0AncestryRecovery? RecoverVerifiedAncestry();
}

/// <summary>TLS refusal observed before any bearer transmission.</summary>
/// <param name="Error">TLS failure outcome.</param><param name="BearerSends">Bearer transmissions observed.</param>
/// <remarks>2026-11-17: FR-MCP-185 ac-UnknownCertificatePinRejected; TR-MCP-FEDERATION-012; TEST-MCP-233.</remarks>
internal sealed record Phase0TlsRefusal(string Error, int BearerSends);

/// <summary>Disposable TLS seam that observes control and remote request attempts.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-007; FR-MCP-185; TR-MCP-FEDERATION-012; TEST-MCP-233.</remarks>
internal interface IPhase0TlsProbe
{
    /// <summary>Attempts a request with either an unapproved SPKI or a wrong SAN.</summary>
    /// <param name="endpoint">control or remote.</param><param name="certificateFault">spki or san.</param>
    /// <returns>Observed TLS outcome and bearer send count.</returns>
    Phase0TlsRefusal? Attempt(string endpoint, string certificateFault);
}

/// <summary>Disposable credential-output capture boundary, with no real credential or secret-store write in mocks.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-007; FR-MCP-184; TR-MCP-FEDERATION-011; TEST-MCP-232.</remarks>
internal interface IPhase0CredentialCaptureProbe
{
    /// <summary>Accepts a process-memory fixture through a protected reference seam.</summary>
    /// <param name="canary">Unique ephemeral fixture, never a test name or external command argument.</param>
    /// <returns>Opaque reference that must not contain the value.</returns>
    string? PrepareProtectedReference(string canary);
    /// <summary>Captures every required output channel for a host and operation.</summary>
    /// <param name="host">LAB-OMARCHY or PAYTON-LEGION2 fixture label.</param><param name="operation">command, retry or failure.</param>
    /// <param name="reference">Opaque prepared reference.</param>
    /// <returns>Channel text, with null or missing captures treated as incomplete evidence.</returns>
    IReadOnlyDictionary<string, string?>? Capture(string host, string operation, string reference);
}

/// <summary>Reusable partial Task007 recovery and security assertions, independent of production output.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-007; FR-MCP-179 ac-HubRestoreFromOlderBackup;
/// FR-MCP-184 ac-ReceiptsAndLogsContainNoCredentials; FR-MCP-185 ac-UnknownCertificatePinRejected;
/// TR-MCP-FEDERATION-006/011/012; TEST-MCP-227/232/233.
/// Mock evidence does not establish real TLS, protected stores, history scans, restore behavior or B10-B12 acceptance.
/// </remarks>
internal static class Phase0RecoverySecurityContract
{
    /// <summary>Every explicitly required secret-output channel, including the scanner's own output.</summary>
    internal static IReadOnlyList<string> CredentialChannels { get; } = Array.AsReadOnly(new[]
    {
        "argv", "receipts", "logs", "chat_payloads", "audit_payloads", "memory_stores", "generated_files",
        "git_staged_blobs", "git_new_commits", "dolt_working_sets", "dolt_new_history", "scanner_diagnostics", "rejected_commit_previews"
    });

    /// <summary>Checks epoch rejection, retained heads and verified recovery without requiring a force reset.</summary>
    /// <param name="probe">Scripted or later real disposable restore adapter.</param>
    internal static void AssertOlderBackupRecovery(IPhase0RestoreProbe probe)
    {
        var originalEpoch = Guid.Parse("11000000-0000-0000-0000-000000000001");
        probe.Prepare(originalEpoch);
        var restored = probe.RestoreAndReport(); Assert.NotNull(restored);
        Assert.NotEqual(Guid.Empty, restored.HubEpoch); Assert.NotEqual(originalEpoch, restored.HubEpoch);
        Assert.Equal(40, restored.Generation); Assert.Equal("A1", restored.NodeHead); Assert.Equal("C0", restored.HubHead);
        Assert.Equal(0, restored.ForceResets); Assert.Equal(409, restored.OldReportStatus);
        Assert.Equal("hub_epoch_changed", restored.OldReportError);
        var recovered = probe.RecoverVerifiedAncestry(); Assert.NotNull(recovered);
        Assert.True(recovered.AncestryVerified); Assert.Equal("A1", recovered.NodeHead);
        Assert.Contains("A1", recovered.PublishedAncestors); Assert.Equal(0, recovered.ForceResets);
    }

    /// <summary>Checks TLS refusal precedes bearer transmission for the selected endpoint and certificate defect.</summary>
    /// <param name="probe">Scripted or later real TLS adapter.</param><param name="endpoint">control or remote.</param>
    /// <param name="certificateFault">spki or san.</param>
    internal static void AssertUntrustedCertificate(IPhase0TlsProbe probe, string endpoint, string certificateFault)
    {
        var result = probe.Attempt(endpoint, certificateFault); Assert.NotNull(result);
        Assert.Equal("tls_peer_untrusted", result.Error); Assert.Equal(0, result.BearerSends);
    }

    /// <summary>Checks complete captures and all returned channels without ever printing the secret fixture.</summary>
    /// <param name="probe">Scripted or later real isolated protected-reference and capture adapter.</param>
    /// <param name="canary">Unique ephemeral fixture, generated inside a test invocation.</param>
    internal static void AssertCredentialContainment(IPhase0CredentialCaptureProbe probe, string canary)
    {
        var reference = probe.PrepareProtectedReference(canary);
        Assert.True(!string.IsNullOrEmpty(reference), "Protected reference is missing.");
        Assert.False(reference.Contains(canary, StringComparison.Ordinal), "Protected reference exposed its fixture value.");
        foreach (var host in new[] { "LAB-OMARCHY", "PAYTON-LEGION2" })
        foreach (var operation in new[] { "command", "retry", "failure" })
        {
            var captures = probe.Capture(host, operation, reference);
            Assert.True(captures is not null, $"Capture missing: {host}/{operation}.");
            foreach (var channel in CredentialChannels)
                Assert.True(captures.ContainsKey(channel), $"Required channel missing: {host}/{operation}/{channel}.");
            foreach (var capture in captures)
            {
                // Scan keys before using them in diagnostics, then values, without displaying either on a leak.
                Assert.False(capture.Key.Contains(canary, StringComparison.Ordinal), $"Credential fixture detected in channel label: {host}/{operation}.");
                Assert.True(capture.Value is not null, $"Channel capture missing: {host}/{operation}/{capture.Key}.");
                Assert.False(capture.Value.Contains(canary, StringComparison.Ordinal), $"Credential fixture detected: {host}/{operation}/{capture.Key}.");
            }
        }
    }
}
