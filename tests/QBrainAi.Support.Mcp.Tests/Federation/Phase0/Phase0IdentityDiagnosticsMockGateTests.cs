namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent diagnostic scripts prove identity-review assertions reject incomplete or mutating results.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-004; B4; FR-MCP-182 ac-PluginIdentityDiagnostics;
/// TR-MCP-FEDERATION-009; TEST-MCP-230. These are generic selected-surface fixtures, not installed plugins.
/// Ph7 must bind the same assertions to every actual first-party plugin. No public DTO is adopted here.
/// </remarks>
public sealed class Phase0IdentityDiagnosticsMockGateTests
{
    /// <summary>Both display orders preserve complete diagnostics, stable reopen and protected state.</summary>
    /// <param name="reverse">Whether the two candidate sides appear in reverse display order.</param>
    /// <param name="extraEvidence">Additional similarity facts are valid; the contract requires specified facts without forbidding extras.</param>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void CorrectIdentityDiagnosticsPass(bool reverse, bool extraEvidence)
    {
        var probe = new DiagnosticProbe(reverse: reverse, extraEvidence: extraEvidence);
        Phase0IdentityDiagnosticsContract.AssertReview(probe);
        Assert.Equal(new[] { "prepare", "rows:0", "aliases:0", "mutations:0", "open", "rows:1", "aliases:1", "mutations:1", "reopen", "rows:2", "aliases:2", "mutations:2" }, probe.Calls);
    }

    /// <summary>Gets each independently corrupted display field at initial open and reopen.</summary>
    public static IEnumerable<object[]> DisplayFaults
    {
        get
        {
            for (var read = 0; read < 2; read++)
                foreach (var fault in new[] { "Missing", "ReviewId", "Project", "MissingSides", "MissingSide", "ExtraSide", "OriginLab", "OriginalLab", "CanonicalLab", "OriginLegion", "OriginalLegion", "ProvisionalLegion", "NaN", "Infinity", "MissingEvidence", "MissingTitleEvidence", "MissingCanonicalLab", "MissingProvisionalLegion", "TitleEvidence", "DescriptionEvidence", "DependencyEvidence", "ReservationStatus", "ReservationCode", "HumanId", "ReopenScore" })
                {
                    if (fault == "ReopenScore" && read == 0) continue;
                    yield return ["display:" + read + ":" + fault];
                }
        }
    }

    /// <summary>Gets every independent state read defect, including a transient mutation restored before readback.</summary>
    public static IEnumerable<object[]> StateFaults
    {
        get
        {
            for (var read = 0; read < 3; read++)
            {
                foreach (var fault in new[] { "Missing", "Empty", "Extra", "LabTitle", "LegionId", "LegionDescription", "Dependencies" })
                    yield return ["rows:" + read + ":" + fault];
                foreach (var fault in new[] { "Missing", "Added" })
                    yield return ["aliases:" + read + ":" + fault];
                yield return ["mutations:" + read];
            }
        }
    }

    /// <summary>Every omitted or changed diagnostic fact must be rejected by the shared assertions.</summary>
    /// <param name="fault">One corrupted display observation.</param>
    [Theory, MemberData(nameof(DisplayFaults))]
    public void WrongDisplayIsRejected(string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0IdentityDiagnosticsContract.AssertReview(new DiagnosticProbe(fault)));

    /// <summary>Unchanged display output must not hide missing rows, unauthorized aliases or transient writes.</summary>
    /// <param name="fault">One corrupt independent state observation.</param>
    [Theory, MemberData(nameof(StateFaults))]
    public void WrongProtectedStateIsRejected(string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0IdentityDiagnosticsContract.AssertReview(new DiagnosticProbe(fault)));

    /// <summary>Literal scripted responses with separate response and durable-observation mutation points.</summary>
    private sealed class DiagnosticProbe(string fault = "", bool reverse = false, bool extraEvidence = false) : IPhase0IdentityDiagnosticsProbe
    {
        private int displayRead;
        private int rowRead;
        private int aliasRead;
        private int mutationRead;
        /// <summary>Gets ordered boundary calls; no expected value comes from production output.</summary>
        internal List<string> Calls { get; } = [];
        /// <inheritdoc />
        public void Prepare() { displayRead = rowRead = aliasRead = mutationRead = 0; Calls.Clear(); Calls.Add("prepare"); }
        /// <inheritdoc />
        public Phase0IdentityReviewDiagnostic? OpenReview(bool reopen)
        {
            Assert.Equal(displayRead != 0, reopen);
            Calls.Add(reopen ? "reopen" : "open");
            var current = displayRead++;
            var active = fault.StartsWith("display:" + current + ":", StringComparison.Ordinal) ? fault[(fault.LastIndexOf(':') + 1)..] : "";
            var lab = new Phase0IdentityDiagnosticSide("LAB", "TODO-7", "fixture-canonical-lab-7", null);
            var legion = new Phase0IdentityDiagnosticSide("LEGION", "TODO-91", null, "fixture-provisional-legion-91");
            lab = active switch { "OriginLab" => lab with { Origin = "OTHER" }, "OriginalLab" => lab with { OriginalId = "TODO-8" }, "CanonicalLab" => lab with { CanonicalId = "wrong" }, "MissingCanonicalLab" => lab with { CanonicalId = null }, _ => lab };
            legion = active switch { "OriginLegion" => legion with { Origin = "OTHER" }, "OriginalLegion" => legion with { OriginalId = "TODO-92" }, "ProvisionalLegion" => legion with { ProvisionalId = "wrong" }, "MissingProvisionalLegion" => legion with { ProvisionalId = null }, _ => legion };
            var sides = reverse ? new[] { legion, lab } : new[] { lab, legion };
            var evidence = new Dictionary<string, string> { ["normalizedTitle"] = "fix login", ["normalizedDescription"] = "repair café login", ["normalizedDependencies"] = "TODO-2|TODO-3" };
            if (active == "TitleEvidence") evidence["normalizedTitle"] = "wrong";
            if (active == "DescriptionEvidence") evidence["normalizedDescription"] = "wrong";
            if (active == "DependencyEvidence") evidence["normalizedDependencies"] = "TODO-4";
            if (extraEvidence) evidence["additionalFeature"] = "valid additional similarity fact";
            if (active == "MissingTitleEvidence") evidence.Remove("normalizedTitle");
            var result = new Phase0IdentityReviewDiagnostic("7c6474a7-0234-4c51-a72f-f1d30bd7e691", "Atlas", sides, 0.98, evidence, 409, "human_id_unreserved", "IMPL-UNITGREEN-001");
            return active switch
            {
                "Missing" => null, "ReviewId" => result with { ReviewId = "other" }, "Project" => result with { Project = "SecretProject" },
                "MissingSides" => result with { Sides = null }, "MissingSide" => result with { Sides = [lab] }, "ExtraSide" => result with { Sides = [lab, legion, lab] },
                "NaN" => result with { Similarity = double.NaN }, "Infinity" => result with { Similarity = double.PositiveInfinity },
                "MissingEvidence" => result with { Evidence = null }, "ReservationStatus" => result with { ReservationStatus = 200 },
                "ReservationCode" => result with { ReservationCode = null }, "HumanId" => result with { RequestedHumanId = "OTHER-001" },
                "ReopenScore" => result with { Similarity = 0.5 }, _ => result
            };
        }
        /// <inheritdoc />
        public IReadOnlyList<Phase0IdentityDiagnosticRow>? ReadRows()
        {
            var current = rowRead++; Calls.Add("rows:" + current);
            var active = fault.StartsWith("rows:" + current + ":", StringComparison.Ordinal) ? fault[(fault.LastIndexOf(':') + 1)..] : "";
            var lab = new Phase0IdentityDiagnosticRow("LAB", "TODO-7", "fixture-canonical-lab-7", "Fix login", "repair café login", "TODO-2|TODO-3");
            var legion = new Phase0IdentityDiagnosticRow("LEGION", "TODO-91", "fixture-provisional-legion-91", "Fix login", "repair café login", "TODO-2|TODO-3");
            return active switch
            {
                "Missing" => null, "Empty" => [], "Extra" => [lab, legion, lab], "LabTitle" => [lab with { Title = "wrong" }, legion],
                "LegionId" => [lab, legion with { Id = "renamed" }], "LegionDescription" => [lab, legion with { Description = "lost" }],
                "Dependencies" => [lab with { Dependencies = "TODO-4" }, legion], _ => [lab, legion]
            };
        }
        /// <inheritdoc />
        public IReadOnlyList<string>? ReadAliases()
        {
            var current = aliasRead++; Calls.Add("aliases:" + current);
            if (fault == "aliases:" + current + ":Missing") return null;
            return fault == "aliases:" + current + ":Added" ? ["unauthorized-alias"] : [];
        }
        /// <inheritdoc />
        public int ReadMutationCount()
        {
            var current = mutationRead++; Calls.Add("mutations:" + current);
            return fault == "mutations:" + current ? 1 : 0;
        }
    }
}
