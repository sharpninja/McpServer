namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent scripted observations prove missing Task007 B10/B12 assertions reject wrong behavior.</summary>
/// <remarks>2026-11-19: TASK-FEDPH0-007; FR-MCP-179 ac-HubRestoreFromOlderBackup;
/// FR-MCP-184 ac-ReceiptsAndLogsContainNoCredentials/ac-CommitPreviewBlocksSecret;
/// FR-MCP-185 ac-UnknownCertificatePinRejected; TR-MCP-FEDERATION-006/008/011/012; TEST-MCP-227/232/233.
/// B10 cases prove reporting design only. No actual host, measurement, credential, history or service is touched.</remarks>
public sealed class Phase0OperationsReadinessMockGateTests
{
    /// <summary>Gets both host labels.</summary>
    public static IEnumerable<object[]> Hosts => new[] { "LAB-OMARCHY", "PAYTON-LEGION2" }.Select(host => new object[] { host });

    /// <summary>Gets independently correct reports, including a retained budget finding, shrink and failed attempt.</summary>
    public static IEnumerable<object[]> ScaleCases => from host in Hosts from kind in new[] { "normal", "over-budget", "shrink", "failed-attempt" } select new object[] { host[0], kind };

    /// <summary>Gets one corruption in every raw/reported scalar, sample or aggregate boundary.</summary>
    public static IEnumerable<object[]> ScaleFaults
    {
        get
        {
            foreach (var host in Hosts)
            {
                foreach (var side in new[] { "Raw", "Report" })
                foreach (var facet in new[] { "Host", "Rows", "Commits", "Subscribers", "Payload", "Hash", "MissingSample", "DuplicateSample", "SampleTime", "SampleOutcome", "DataBefore", "DataAfter", "HistoryBefore", "HistoryAfter", "Memory", "Cpu", "MemoryBudget", "StorageBudget" })
                    yield return [host[0], side + facet];
                foreach (var facet in new[] { "MissingReport", "MissingRaw", "DataGrowth", "HistoryGrowth", "P50", "P95", "Maximum", "Failures", "Budget", "RowLimit", "CommitLimit", "SubscriberLimit", "Sla", "BothNegativeData", "BothNegativeHistory", "BothNegativeMemory", "BothNanCpu", "BothNegativeSample", "BothNanSample", "BothInfinitySample", "BothWrongHash", "BothWrongBudget" })
                    yield return [host[0], facet];
            }
        }
    }

    /// <summary>Gets every restore checkpoint with one independently wrong observed facet.</summary>
    public static IEnumerable<object[]> RestoreFaults => from host in Hosts from index in Enumerable.Range(0, 9)
        from facet in new[] { "Stage", "Ingress", "Epoch", "Generation", "HubHead", "NodeHead", "Audit", "Components", "Ancestry", "Reset", "History", "Status", "Code", "Original", "Unrelated", "Receipt" }
        select new object[] { host[0], index, facet };

    /// <summary>Gets wrong public-policy/listener observations.</summary>
    public static IEnumerable<object[]> TrustFaults => from host in Hosts
        from facet in new[] { "Missing", "Host", "Owner", "Issuer", "Root", "Pins", "MissingPin", "Sans", "Expiry", "Expired", "Rotation", "Bootstrap", "Tofu", "Bypass", "SqlExternal", "MissingListener", "ExtraListener" }
        select new object[] { host[0], facet };

    /// <summary>Gets unsafe preview/disclosure effects and leaks in every string facet.</summary>
    public static IEnumerable<object[]> SecretFaults => from host in Hosts
        from facet in new[] { "Missing", "Status", "Code", "Commit", "Head", "Draft", "Detector", "Rotation", "Removal", "Rewrite", "Reclone", "ForcePush", "LeakCode", "LeakHead", "LeakDraft", "LeakDetector", "LeakDiagnostics" }
        select new object[] { host[0], facet };

    /// <summary>Correct reports preserve findings and failed attempts rather than inventing successful operating limits.</summary>
    /// <param name="host">Host.</param><param name="kind">Correct observation variant.</param>
    [Theory, MemberData(nameof(ScaleCases))]
    public void CorrectMeasuredReportPasses(string host, string kind)
    {
        var probe = new Probe(kind: kind); Phase0OperationsReadinessContract.AssertScale(probe, host, Inputs(host));
        Assert.Equal(new[] { "prepare/" + host, "scale", "raw" }, probe.Calls);
    }

    /// <summary>Wrong independent captures or aggregates are rejected.</summary>
    /// <param name="host">Host.</param><param name="fault">Single observation corruption.</param>
    [Theory, MemberData(nameof(ScaleFaults))]
    public void WrongMeasuredReportIsRejected(string host, string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0OperationsReadinessContract.AssertScale(new Probe(scaleFault: fault), host, Inputs(host)));

    /// <summary>Correct restore ordering permits generation gaps and retains recovered state on reopen.</summary>
    /// <param name="host">Host.</param>
    [Theory, MemberData(nameof(Hosts))]
    public void CorrectRestoreOrderingPasses(string host)
    {
        foreach (var generation in new long[] { 41, 47 })
        {
            var probe = new Probe(recoveredGeneration: generation);
            Phase0OperationsReadinessContract.AssertRestoreOrdering(probe, host, Inputs(host));
            Assert.Equal(new[] { "prepare/" + host, "restore", "trace" }, probe.Calls);
        }
    }

    /// <summary>Every checkpoint facet matters independently, including before listeners open and after reopen.</summary>
    /// <param name="host">Host.</param><param name="index">Checkpoint.</param><param name="facet">Single corruption.</param>
    [Theory, MemberData(nameof(RestoreFaults))]
    public void WrongRestoreCheckpointIsRejected(string host, int index, string facet)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0OperationsReadinessContract.AssertRestoreOrdering(new Probe(restoreIndex: index, restoreField: facet), host, Inputs(host)));

    /// <summary>Missing or incomplete restore traces are rejected with assertion diagnostics.</summary>
    /// <param name="host">Host.</param>
    [Theory, MemberData(nameof(Hosts))]
    public void MissingRestoreEvidenceIsRejected(string host)
    {
        foreach (var facet in new[] { "Missing", "Omitted", "Extra" })
            Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0OperationsReadinessContract.AssertRestoreOrdering(new Probe(restoreField: facet), host, Inputs(host)));
    }

    /// <summary>Approved public policy and owned addresses pass on both host labels.</summary>
    /// <param name="host">Host.</param>
    [Theory, MemberData(nameof(Hosts))]
    public void CorrectPublicTrustPasses(string host)
    {
        var probe = new Probe(); Phase0OperationsReadinessContract.AssertPublicTrust(probe, host, Inputs(host));
        Assert.Equal(new[] { "prepare/" + host, "trust" }, probe.Calls);
    }

    /// <summary>Wrong public policy or exposed backend listeners are rejected.</summary>
    /// <param name="host">Host.</param><param name="facet">Single corruption.</param>
    [Theory, MemberData(nameof(TrustFaults))]
    public void WrongPublicTrustIsRejected(string host, string facet)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0OperationsReadinessContract.AssertPublicTrust(new Probe(trustFault: facet), host, Inputs(host)));

    /// <summary>Preview refusal and required safe disclosure actions pass without destructive authorization.</summary>
    /// <param name="host">Host.</param>
    [Theory, MemberData(nameof(Hosts))]
    public void CorrectSecretResponsePasses(string host)
    {
        var probe = new Probe(); Phase0OperationsReadinessContract.AssertSecretResponse(probe, host, Inputs(host), Guid.NewGuid().ToString("N"));
        Assert.Equal(new[] { "prepare/" + host, "preview-disclosure" }, probe.Calls);
    }

    /// <summary>Unsafe effects or leaks fail, and assertion diagnostics never expose the invocation-private value.</summary>
    /// <param name="host">Host.</param><param name="facet">Single corruption.</param>
    [Theory, MemberData(nameof(SecretFaults))]
    public void WrongSecretResponseIsRejectedWithoutLeakingCanary(string host, string facet)
    {
        var canary = Guid.NewGuid().ToString("N");
        var error = Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0OperationsReadinessContract.AssertSecretResponse(new Probe(secretFault: facet), host, Inputs(host), canary));
        Assert.False(error.ToString().Contains(canary, StringComparison.Ordinal), "Assertion diagnostic exposed private fixture.");
    }

    // Expected public inputs are authored independently of the scripted observer factories below.
    private static Phase0OperationsInputs Inputs(string host) => new(new string('A', 64), 1000, 1000, "original-baseline", "unrelated-baseline",
        new(host, "fixture-operator", "QBrain Lab Federation Root CA", new string('B', 64), [new string('C', 64), new string('D', 64)], ["LAB-OMARCHY", "10.42.0.214"],
            DateTimeOffset.Parse("2027-01-01T00:00:00Z"), DateTimeOffset.Parse("2026-11-19T00:00:00Z"), "rotate-current-to-next", "existing-ssh-trust", false, false,
            host == "LAB-OMARCHY" ? new Dictionary<string, string> { ["sql"] = "127.0.0.1:13306", ["backend"] = "127.0.0.1:17448", ["gateway"] = "10.42.0.214:17447" }
                : new Dictionary<string, string> { ["sql"] = "127.0.0.1:13306" }), "detector-source-and-config-pin");

    private sealed class Probe(string kind = "normal", string? scaleFault = null, int restoreIndex = 0, string? restoreField = null,
        long recoveredGeneration = 41, string? trustFault = null, string? secretFault = null) : IPhase0OperationsReadinessProbe
    {
        private string host = "";
        internal List<string> Calls { get; } = [];
        public void Prepare(string selectedHost) { host = selectedHost; Calls.Add("prepare/" + host); }
        public Phase0ScaleReport? RunScale()
        {
            Calls.Add("scale"); if (scaleFault == "MissingReport") return null;
            var capture = Capture();
            if (scaleFault?.StartsWith("Report", StringComparison.Ordinal) == true) capture = CorruptCapture(capture, scaleFault[6..]);
            if (scaleFault?.StartsWith("Both", StringComparison.Ordinal) == true) capture = CorruptBoth(capture, scaleFault[4..]);
            var report = new Phase0ScaleReport(capture, kind == "shrink" ? -20 : 100, 300, 8, 15, 15, kind == "failed-attempt" ? 1 : 0, kind == "over-budget", 100, 10, 3, false);
            return scaleFault switch
            {
                "DataGrowth" => report with { DataGrowth = 101 }, "HistoryGrowth" => report with { HistoryGrowth = 301 },
                "P50" => report with { P50 = 7 }, "P95" => report with { P95 = 14 }, "Maximum" => report with { Maximum = 16 },
                "Failures" => report with { Failures = 1 }, "Budget" => report with { BudgetExceeded = true },
                "RowLimit" => report with { RowLimit = 101 }, "CommitLimit" => report with { CommitLimit = 11 }, "SubscriberLimit" => report with { SubscriberLimit = 4 },
                "Sla" => report with { ClaimsSla = true }, _ => report
            };
        }
        public Phase0ScaleCapture? ReadScaleCapture()
        {
            Calls.Add("raw"); if (scaleFault == "MissingRaw") return null;
            var raw = Capture();
            if (scaleFault?.StartsWith("Raw", StringComparison.Ordinal) == true) raw = CorruptCapture(raw, scaleFault[3..]);
            return scaleFault?.StartsWith("Both", StringComparison.Ordinal) == true ? CorruptBoth(raw, scaleFault[4..]) : raw;
        }
        private Phase0ScaleCapture Capture()
        {
            var samples = Enumerable.Range(1, 15).Select(i => new Phase0ScaleSample(new[] { "save", "query", "commit", "poll", "fetch" }[(i - 1) / 3], (i - 1) % 3 + 1, i, kind != "failed-attempt" || i != 1)).ToArray();
            return new(host, 100, 10, 3, 128, new string('A', 64), samples, kind == "shrink" ? 220 : 100, 200, 50, 350, kind == "over-budget" ? 2000 : 500, 25, 1000, 1000);
        }
        private static Phase0ScaleCapture CorruptCapture(Phase0ScaleCapture raw, string facet) => facet switch
        {
            "Host" => raw with { Host = "wrong" }, "Rows" => raw with { Rows = 99 }, "Commits" => raw with { Commits = 9 }, "Subscribers" => raw with { Subscribers = 2 },
            "Payload" => raw with { PayloadBytes = 127 }, "Hash" => raw with { FixtureHash = new string('F', 64) },
            "MissingSample" => raw with { Samples = raw.Samples.Skip(1).ToArray() },
            "DuplicateSample" => raw with { Samples = [raw.Samples[1], .. raw.Samples.Skip(1)] },
            "SampleTime" => raw with { Samples = [raw.Samples[0] with { Milliseconds = 0 }, .. raw.Samples.Skip(1)] },
            "SampleOutcome" => raw with { Samples = [raw.Samples[0] with { Succeeded = false }, .. raw.Samples.Skip(1)] },
            "DataBefore" => raw with { DataBefore = 101 }, "DataAfter" => raw with { DataAfter = 201 }, "HistoryBefore" => raw with { HistoryBefore = 51 }, "HistoryAfter" => raw with { HistoryAfter = 351 },
            "Memory" => raw with { PeakMemory = 501 }, "Cpu" => raw with { CpuMilliseconds = 26 }, "MemoryBudget" => raw with { MemoryBudget = 1001 }, "StorageBudget" => raw with { StorageBudget = 1001 },
            _ => throw new ArgumentOutOfRangeException(nameof(facet))
        };
        private static Phase0ScaleCapture CorruptBoth(Phase0ScaleCapture raw, string facet) => facet switch
        {
            "NegativeData" => raw with { DataBefore = -1 }, "NegativeHistory" => raw with { HistoryAfter = -1 }, "NegativeMemory" => raw with { PeakMemory = -1 },
            "NanCpu" => raw with { CpuMilliseconds = double.NaN }, "WrongHash" => raw with { FixtureHash = new string('E', 64) }, "WrongBudget" => raw with { MemoryBudget = 2000 },
            "NegativeSample" => raw with { Samples = [raw.Samples[0] with { Milliseconds = -1 }, .. raw.Samples.Skip(1)] },
            "NanSample" => raw with { Samples = [raw.Samples[0] with { Milliseconds = double.NaN }, .. raw.Samples.Skip(1)] },
            "InfinitySample" => raw with { Samples = [raw.Samples[0] with { Milliseconds = double.PositiveInfinity }, .. raw.Samples.Skip(1)] },
            _ => throw new ArgumentOutOfRangeException(nameof(facet))
        };
        public void RestoreOlderBackup() => Calls.Add("restore");
        public IReadOnlyList<Phase0RestoreCheckpoint>? ReadRestoreTrace()
        {
            Calls.Add("trace"); if (restoreField == "Missing") return null;
            var e1 = Guid.Parse("11000000-0000-0000-0000-000000000001"); var e2 = Guid.Parse("22000000-0000-0000-0000-000000000002");
            string[] components = ["objects-refs", "control", "audit", "outbox", "identity-config", "identity-operational", "permissions"];
            Phase0RestoreCheckpoint Row(string stage, bool open, Guid epoch, long generation, string head, string[] audit, bool ancestry, int status = 200, string? code = null)
                => new(stage, open, epoch, generation, head, "A1", audit, [.. components], ancestry, 0, 0, status, code, "original-baseline", "unrelated-baseline", "retained-operation/" + stage);
            Phase0RestoreCheckpoint[] rows =
            [
                Row("ingress-disabled", false, e1, 90, "C0", ["E0"], false),
                Row("checkpoints-restored", false, e1, 40, "C0", ["E0"], false),
                Row("epoch-persisted", false, e2, 40, "C0", ["E0"], false),
                Row("heads-verified", false, e2, 40, "C0", ["E0"], false),
                Row("listeners-open", true, e2, 40, "C0", ["E0"], false),
                Row("old-report-rejected", true, e2, 40, "C0", ["E0"], false, 409, "hub_epoch_changed"),
                Row("ancestry-recovered", true, e2, recoveredGeneration, "A1", ["E0"], true),
                Row("audit-reconciled", true, e2, recoveredGeneration, "A1", ["E0", "E1"], true),
                Row("reopened", true, e2, recoveredGeneration, "A1", ["E0", "E1"], true)
            ];
            if (restoreField == "Omitted") return rows.Skip(1).ToArray(); if (restoreField == "Extra") return [.. rows, rows[8]];
            var row = rows[restoreIndex];
            rows[restoreIndex] = restoreField switch
            {
                null => row, "Stage" => row with { Stage = "wrong" }, "Ingress" => row with { IngressOpen = !row.IngressOpen },
                "Epoch" => row with { Epoch = Guid.Empty }, "Generation" => row with { Generation = 0 }, "HubHead" => row with { HubHead = "lost" }, "NodeHead" => row with { NodeHead = "lost" },
                "Audit" => row with { AuditIds = [] }, "Components" => row with { Components = components.Skip(1).ToArray() }, "Ancestry" => row with { AncestryVerified = !row.AncestryVerified },
                "Reset" => row with { ForceResets = 1 }, "History" => row with { NewHistoryCommits = 1 }, "Status" => row with { Status = 500 }, "Code" => row with { Code = "wrong" },
                "Original" => row with { OriginalFingerprint = "changed" }, "Unrelated" => row with { UnrelatedFingerprint = "changed" }, "Receipt" => row with { NativeReceipt = "" },
                _ => throw new ArgumentOutOfRangeException(nameof(restoreField))
            };
            return rows;
        }
        public Phase0PublicTrust? ReadPublicTrust()
        {
            Calls.Add("trust"); if (trustFault == "Missing") return null;
            var addresses = new Dictionary<string, string> { ["sql"] = "127.0.0.1:13306" };
            if (host == "LAB-OMARCHY") { addresses["backend"] = "127.0.0.1:17448"; addresses["gateway"] = "10.42.0.214:17447"; }
            var observed = new Phase0PublicTrust(host, "fixture-operator", "QBrain Lab Federation Root CA", new string('B', 64), [new string('C', 64), new string('D', 64)], ["10.42.0.214", "LAB-OMARCHY"],
                new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 11, 19, 0, 0, 0, TimeSpan.Zero), "rotate-current-to-next", "existing-ssh-trust", false, false, addresses);
            if (trustFault == "SqlExternal") addresses["sql"] = "0.0.0.0:13306";
            if (trustFault == "MissingListener") addresses.Remove("sql");
            if (trustFault == "ExtraListener") addresses["unapproved"] = "0.0.0.0:12345";
            return trustFault switch
            {
                "Host" => observed with { Host = "wrong" }, "Owner" => observed with { Owner = "unapproved" }, "Issuer" => observed with { Issuer = "wrong" },
                "Root" => observed with { RootHash = new string('F', 64) }, "Pins" => observed with { Pins = [new string('F', 64), new string('D', 64)] }, "MissingPin" => observed with { Pins = [new string('C', 64)] },
                "Sans" => observed with { Sans = ["wrong"] }, "Expiry" => observed with { Expires = observed.Expires.AddDays(-1) }, "Expired" => observed with { Observed = observed.Expires },
                "Rotation" => observed with { Rotation = "" }, "Bootstrap" => observed with { Bootstrap = "tofu" }, "Tofu" => observed with { Tofu = true }, "Bypass" => observed with { Bypass = true }, _ => observed
            };
        }
        public Phase0SecretResponse? PreviewAndHandleDisclosure(string canary)
        {
            Calls.Add("preview-disclosure"); if (secretFault == "Missing") return null;
            var response = new Phase0SecretResponse(422, "secret_detected", 0, "C0", "retained-draft", "detector-source-and-config-pin", true, true, 0, 0, 0, "protected fixture refused; rotate and remove live content");
            return secretFault switch
            {
                "Status" => response with { Status = 200 }, "Code" => response with { Code = "wrong" }, "Commit" => response with { Commits = 1 }, "Head" => response with { Head = "changed" }, "Draft" => response with { Draft = "lost" },
                "Detector" => response with { Detector = "unselected" }, "Rotation" => response with { RotationRequested = false }, "Removal" => response with { LiveRemovalRequested = false },
                "Rewrite" => response with { HistoryRewrites = 1 }, "Reclone" => response with { Reclones = 1 }, "ForcePush" => response with { ForcePushes = 1 },
                "LeakCode" => response with { Code = canary }, "LeakHead" => response with { Head = canary }, "LeakDraft" => response with { Draft = canary },
                "LeakDetector" => response with { Detector = canary }, "LeakDiagnostics" => response with { Diagnostics = canary }, _ => response
            };
        }
    }
}
