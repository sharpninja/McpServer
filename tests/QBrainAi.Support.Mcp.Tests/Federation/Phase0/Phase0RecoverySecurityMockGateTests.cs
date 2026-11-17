namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent scripted responses prove partial Task007 assertions discriminate unsafe results.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-007; FR-MCP-179 ac-HubRestoreFromOlderBackup;
/// FR-MCP-184 ac-ReceiptsAndLogsContainNoCredentials; FR-MCP-185 ac-UnknownCertificatePinRejected;
/// TR-MCP-FEDERATION-006/011/012; TEST-MCP-227/232/233.
/// No actual credential, secret store, TLS server, Git/Dolt history or host restore is used by these unit controls.
/// </remarks>
public sealed class Phase0RecoverySecurityMockGateTests
{
    /// <summary>The restored epoch rejects old reports and verified recovery retains and republishes node A1.</summary>
    [Fact]
    public void CorrectRestoreRecoveryPasses()
    {
        var probe = new RestoreProbe(); Phase0RecoverySecurityContract.AssertOlderBackupRecovery(probe);
        Assert.Equal(new[] { "prepare", "restore-report", "recover-ancestry" }, probe.Calls);
    }

    /// <summary>Gets each independent epoch, report, head and ancestry corruption.</summary>
    public static IEnumerable<object[]> RestoreFaults => new[]
    {
        "MissingRestore", "EmptyEpoch", "ReusedEpoch", "Generation", "NodeHead", "HubHead", "RestoreReset", "Status", "Error",
        "MissingRecovery", "Unverified", "LostNode", "MissingAncestor", "RecoveryReset"
    }.Select(fault => new object[] { fault });

    /// <summary>Gets every required TLS endpoint and independent certificate defect.</summary>
    public static IEnumerable<object[]> TlsCases =>
        from endpoint in new[] { "control", "remote" }
        from certificateFault in new[] { "spki", "san" }
        select new object[] { endpoint, certificateFault };

    /// <summary>Gets missing TLS evidence, a wrong refusal, and an early bearer send for each TLS case.</summary>
    public static IEnumerable<object[]> TlsFaults =>
        from item in TlsCases from fault in new[] { "Missing", "Error", "Bearer" }
        select new object[] { item[0], item[1], fault };

    /// <summary>Gets every named host, operation, required channel and incomplete/leaked capture mutation.</summary>
    public static IEnumerable<object[]> CredentialFaults
    {
        get
        {
            foreach (var host in new[] { "LAB-OMARCHY", "PAYTON-LEGION2" })
            foreach (var operation in new[] { "command", "retry", "failure" })
            {
                foreach (var channel in Phase0RecoverySecurityContract.CredentialChannels)
                foreach (var fault in new[] { "Leak", "MissingChannel", "NullChannel" })
                    yield return [host, operation, channel, fault];
                yield return [host, operation, "", "MissingCapture"];
                yield return [host, operation, "extra", "ExtraLeak"];
                yield return [host, operation, "", "KeyLeak"];
            }
            yield return ["", "", "", "LeakedReference"];
            yield return ["", "", "", "MissingReference"];
        }
    }

    /// <summary>Wrong restored state or premature/unverified recovery fails the reusable assertions.</summary>
    /// <param name="fault">Single independent observation corruption.</param>
    [Theory, MemberData(nameof(RestoreFaults))]
    public void WrongRestoreOutcomeIsRejected(string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0RecoverySecurityContract.AssertOlderBackupRecovery(new RestoreProbe(fault)));

    /// <summary>Both TLS defects on both endpoints refuse before bearer transmission.</summary>
    /// <param name="endpoint">Selected endpoint.</param><param name="certificateFault">Independent TLS defect.</param>
    [Theory, MemberData(nameof(TlsCases))]
    public void CorrectTlsRefusalPasses(string endpoint, string certificateFault)
        => Phase0RecoverySecurityContract.AssertUntrustedCertificate(new TlsProbe(), endpoint, certificateFault);

    /// <summary>Missing/wrong refusals or bearer sends fail each endpoint and defect scenario.</summary>
    /// <param name="endpoint">Selected endpoint.</param><param name="certificateFault">Independent TLS defect.</param>
    /// <param name="fault">Single wrong outcome.</param>
    [Theory, MemberData(nameof(TlsFaults))]
    public void WrongTlsOutcomeIsRejected(string endpoint, string certificateFault, string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0RecoverySecurityContract.AssertUntrustedCertificate(new TlsProbe(fault), endpoint, certificateFault));

    /// <summary>Every required capture is scanned for command, retry and failure on both host labels.</summary>
    /// <param name="includeExtraSafeChannel">Whether a harmless extra capture is returned.</param>
    [Theory, InlineData(false), InlineData(true)]
    public void CorrectCredentialContainmentPasses(bool includeExtraSafeChannel)
    {
        var canary = Guid.NewGuid().ToString("N"); var probe = new CredentialProbe(extraSafe: includeExtraSafeChannel);
        Phase0RecoverySecurityContract.AssertCredentialContainment(probe, canary);
        Assert.Equal(new[] { "LAB-OMARCHY/command", "LAB-OMARCHY/retry", "LAB-OMARCHY/failure", "PAYTON-LEGION2/command", "PAYTON-LEGION2/retry", "PAYTON-LEGION2/failure" }, probe.Calls);
    }

    /// <summary>Missing captures and every leak are rejected without putting the value into failure diagnostics.</summary>
    /// <param name="host">Faulted host label.</param><param name="operation">Faulted operation.</param>
    /// <param name="channel">Faulted channel label.</param><param name="fault">Independent missing or leaked evidence.</param>
    [Theory, MemberData(nameof(CredentialFaults))]
    public void WrongCredentialOutcomeIsRejected(string host, string operation, string channel, string fault)
    {
        var canary = Guid.NewGuid().ToString("N");
        var error = Record.Exception(() => Phase0RecoverySecurityContract.AssertCredentialContainment(new CredentialProbe(host, operation, channel, fault), canary));
        Assert.True(error is Xunit.Sdk.XunitException, "Unsafe or incomplete evidence must produce an assertion failure.");
        Assert.False(error.ToString().Contains(canary, StringComparison.Ordinal), "Assertion diagnostic exposed the fixture value.");
    }

    /// <summary>Independent restore fixture, not a restore or ancestry implementation.</summary>
    private sealed class RestoreProbe(string fault = "") : IPhase0RestoreProbe
    {
        private Guid rememberedEpoch;
        internal List<string> Calls { get; } = [];
        /// <inheritdoc />
        public void Prepare(Guid originalEpoch) { Assert.NotEqual(Guid.Empty, originalEpoch); rememberedEpoch = originalEpoch; Calls.Add("prepare"); }
        /// <inheritdoc />
        public Phase0RestoreObservation? RestoreAndReport()
        {
            Calls.Add("restore-report");
            var observed = new Phase0RestoreObservation(Guid.Parse("22000000-0000-0000-0000-000000000002"), 40, "A1", "C0", 0, 409, "hub_epoch_changed");
            return fault switch
            {
                "MissingRestore" => null, "EmptyEpoch" => observed with { HubEpoch = Guid.Empty }, "ReusedEpoch" => observed with { HubEpoch = rememberedEpoch },
                "Generation" => observed with { Generation = 90 }, "NodeHead" => observed with { NodeHead = "C0" }, "HubHead" => observed with { HubHead = "wrong" },
                "RestoreReset" => observed with { ForceResets = 1 }, "Status" => observed with { OldReportStatus = 200 }, "Error" => observed with { OldReportError = "wrong" }, _ => observed
            };
        }
        /// <inheritdoc />
        public Phase0AncestryRecovery? RecoverVerifiedAncestry()
        {
            Calls.Add("recover-ancestry");
            var observed = new Phase0AncestryRecovery("A1", true, new[] { "C0", "A1" }, 0);
            return fault switch
            {
                "MissingRecovery" => null, "Unverified" => observed with { AncestryVerified = false }, "LostNode" => observed with { NodeHead = "C0" },
                "MissingAncestor" => observed with { PublishedAncestors = new[] { "C0" } }, "RecoveryReset" => observed with { ForceResets = 1 }, _ => observed
            };
        }
    }

    /// <summary>Independent TLS response and transmission-count fixture.</summary>
    private sealed class TlsProbe(string fault = "") : IPhase0TlsProbe
    {
        /// <inheritdoc />
        public Phase0TlsRefusal? Attempt(string endpoint, string certificateFault)
        {
            Assert.Contains(endpoint, new[] { "control", "remote" }); Assert.Contains(certificateFault, new[] { "spki", "san" });
            return fault switch { "Missing" => null, "Error" => new("wrong", 0), "Bearer" => new("tls_peer_untrusted", 1), _ => new("tls_peer_untrusted", 0) };
        }
    }

    /// <summary>Independent captures keep canaries in memory only and never include them in test names.</summary>
    private sealed class CredentialProbe(string host = "", string operation = "", string channel = "", string fault = "", bool extraSafe = false) : IPhase0CredentialCaptureProbe
    {
        // Keep the independent fixture list separate from the contract's required-channel list.
        private static readonly string[] Channels = ["argv", "receipts", "logs", "chat_payloads", "audit_payloads", "memory_stores", "generated_files", "git_staged_blobs", "git_new_commits", "dolt_working_sets", "dolt_new_history", "scanner_diagnostics", "rejected_commit_previews"];
        private string fixtureValue = "";
        internal List<string> Calls { get; } = [];
        /// <inheritdoc />
        public string? PrepareProtectedReference(string canary)
        {
            fixtureValue = canary;
            return fault switch { "LeakedReference" => canary, "MissingReference" => null, _ => "fixture-protected-reference" };
        }
        /// <inheritdoc />
        public IReadOnlyDictionary<string, string?>? Capture(string selectedHost, string selectedOperation, string reference)
        {
            Assert.Equal("fixture-protected-reference", reference); Calls.Add($"{selectedHost}/{selectedOperation}");
            var captures = Channels.ToDictionary(item => item, _ => (string?)"safe captured text", StringComparer.Ordinal);
            if (extraSafe) captures.Add("extra-safe", "safe extra capture");
            if (selectedHost != host || selectedOperation != operation) return captures;
            if (fault == "MissingCapture") return null;
            if (fault == "Leak") captures[channel] = fixtureValue;
            if (fault == "MissingChannel") captures.Remove(channel);
            if (fault == "NullChannel") captures[channel] = null;
            if (fault == "ExtraLeak") captures.Add("extra", fixtureValue);
            if (fault == "KeyLeak") captures.Add(fixtureValue, "safe captured text");
            return captures;
        }
    }
}
