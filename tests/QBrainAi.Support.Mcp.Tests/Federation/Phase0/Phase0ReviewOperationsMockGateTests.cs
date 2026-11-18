namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>Independent deterministic review-clock/publication mocks discriminate each new assertion.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-006/007; FR-MCP-180 ac-ReviewAgeVisible/ac-UnrelatedPublicationContinues; TR-MCP-FEDERATION-007; TEST-MCP-228. No scheduler, gateway or plugin implementation is present.</remarks>
public sealed class Phase0ReviewOperationsMockGateTests
{
    /// <summary>Exact boundary displays and well-spaced durable repeat history pass.</summary>
    [Fact]
    public void CorrectAgePasses()
    {
        var probe = new Probe(); Phase0ReviewOperationsContract.AssertReviewAge(probe);
        Assert.Equal(new[] { "prepare", "clock:900", "visibility", "clock:86400", "visibility", "clock:86460", "visibility", "restart", "visibility", "clock:90000", "visibility" }, probe.Calls);
    }
    /// <summary>At-most-hourly does not require any minimum repeat count.</summary>
    [Fact]
    public void NoRepeatsRemainValid() => Phase0ReviewOperationsContract.AssertReviewAge(new Probe(noRepeats: true));
    /// <summary>Selected display enumeration order is not a product constraint.</summary>
    [Fact]
    public void ReversedViewsRemainValid() => Phase0ReviewOperationsContract.AssertReviewAge(new Probe(reverse: true));
    /// <summary>Feature and other-project publication succeed without advancing blocked main or clearing review.</summary>
    [Fact]
    public void CorrectUnrelatedPublicationPasses()
    {
        var probe = new Probe(); Phase0ReviewOperationsContract.AssertUnrelatedPublication(probe);
        Assert.Equal(new[] { "prepare", "refs", "publish:Atlas/feature:F2", "refs", "publish:Other/main:X1", "refs", "restart", "refs" }, probe.Calls);
    }
    /// <summary>Gets every surface-field and notification-history corruption at each of five clock/reopen reads.</summary>
    public static IEnumerable<object[]> AgeFaults
    {
        get
        {
            for (var checkpoint = 0; checkpoint < 5; checkpoint++)
            {
                foreach (var fault in new[] { "Missing", "SurfaceMissing", "SurfaceExtra", "Email", "Slack", "RepeatNegative", "RepeatFuture", "RepeatTooSoon", "RepeatDuplicate", "RepeatUnsorted" }) yield return [checkpoint, fault];
                foreach (var surface in new[] { "status", "plugin", "inbox" })
                foreach (var member in new[] { "Id", "Project", "Branch", "State", "Age", "Severity" }) yield return [checkpoint, surface + ":" + member];
                if (checkpoint >= 2)
                    foreach (var fault in new[] { "HistoryLost", "HistoryChanged" }) yield return [checkpoint, fault];
            }
        }
    }
    /// <summary>Lost status/inbox details, premature repeats, invisible escalation or lost restart history must fail assertions.</summary>
    /// <param name="checkpoint">Corrupted clock/read checkpoint.</param><param name="fault">One independent observation fault.</param>
    [Theory, MemberData(nameof(AgeFaults))]
    public void WrongAgeIsRejected(int checkpoint, string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0ReviewOperationsContract.AssertReviewAge(new Probe(checkpoint, fault)));
    /// <summary>Gets each protected branch/review corruption, plus unsuccessful unrelated publication responses.</summary>
    public static IEnumerable<object[]> PublicationFaults
    {
        get
        {
            for (var checkpoint = 0; checkpoint < 4; checkpoint++)
                foreach (var fault in new[] { "MissingRefs", "LocalMain", "ReviewId", "ReviewState", "Main", "Feature", "Other", "RefMissing", "RefExtra" }) yield return [checkpoint, fault];
            yield return [-1, "FeatureDenied"]; yield return [-1, "OtherDenied"];
        }
    }
    /// <summary>Blocked unrelated branches, accidental main updates and erased reviews must fail the same reusable contract.</summary>
    /// <param name="checkpoint">Corrupted ref/reopen read.</param><param name="fault">One publication outcome corruption.</param>
    [Theory, MemberData(nameof(PublicationFaults))]
    public void WrongPublicationIsRejected(int checkpoint, string fault)
        => Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => Phase0ReviewOperationsContract.AssertUnrelatedPublication(new Probe(checkpoint, fault)));

    /// <summary>Scripted observations with independent fixture values and captured operations.</summary>
    private sealed class Probe(int checkpoint = -1, string fault = "", bool noRepeats = false, bool reverse = false) : IPhase0ReviewOperationsProbe
    {
        private static readonly Guid ReviewId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private long elapsed;
        private int ageReads;
        private int refReads;
        private int publications;
        private bool prepared;
        internal List<string> Calls { get; } = [];
        /// <inheritdoc />
        public void Prepare() { prepared = true; Calls.Add("prepare"); }
        /// <inheritdoc />
        public void AdvanceClock(long elapsedSeconds)
        {
            Assert.True(prepared); Assert.Contains(elapsedSeconds, new long[] { 900, 86400, 86460, 90000 }); elapsed = elapsedSeconds; Calls.Add("clock:" + elapsedSeconds);
        }
        /// <inheritdoc />
        public void Restart() => Calls.Add("restart");
        /// <inheritdoc />
        public Phase0ReviewVisibility? ReadVisibility()
        {
            Calls.Add("visibility"); var read = ageReads++;
            var views = new[] { "status", "plugin", "inbox" }.Select(surface => new Phase0ReviewView(surface, ReviewId, "Atlas", "main", "needs_review", elapsed, elapsed == 900 ? "warning" : "critical")).ToArray();
            long[] repeats = noRepeats || elapsed == 900 ? [] : elapsed == 90000 ? [4500, 90000] : [4500];
            var result = new Phase0ReviewVisibility(reverse ? views.Reverse().ToArray() : views, repeats, 0, 0);
            if (read != checkpoint) return result;
            if (fault.Contains(':'))
            {
                var parts = fault.Split(':');
                views = views.Select(view => view.Surface != parts[0] ? view : parts[1] switch
                {
                    "Id" => view with { ReviewId = Guid.Empty }, "Project" => view with { Project = "Other" }, "Branch" => view with { Branch = "feature" },
                    "State" => view with { State = "resolved" }, "Age" => view with { AgeSeconds = elapsed + 1 }, "Severity" => view with { Severity = "normal" }, _ => view
                }).ToArray();
                result = result with { Views = views };
            }
            return fault switch
            {
                "Missing" => null, "SurfaceMissing" => result with { Views = views[..2] }, "SurfaceExtra" => result with { Views = [..views, views[0] with { Surface = "unexpected" }] },
                "Email" => result with { Emails = 1 }, "Slack" => result with { SlackMessages = 1 }, "RepeatNegative" => result with { RepeatSeconds = [-1] },
                "RepeatFuture" => result with { RepeatSeconds = [elapsed + 1] }, "RepeatTooSoon" => result with { RepeatSeconds = [0, 1] },
                "RepeatDuplicate" => result with { RepeatSeconds = [0, 0] }, "RepeatUnsorted" => result with { RepeatSeconds = [elapsed, 0] },
                "HistoryLost" => result with { RepeatSeconds = [] }, "HistoryChanged" => result with { RepeatSeconds = [4501] }, _ => result
            };
        }
        /// <inheritdoc />
        public bool Publish(string project, string branch, string head)
        {
            Assert.True(prepared); Calls.Add("publish:" + project + "/" + branch + ":" + head);
            if (publications++ == 0) { Assert.Equal("Atlas", project); Assert.Equal("feature", branch); Assert.Equal("F2", head); return fault != "FeatureDenied"; }
            Assert.Equal("Other", project); Assert.Equal("main", branch); Assert.Equal("X1", head); return fault != "OtherDenied";
        }
        /// <inheritdoc />
        public Phase0ReviewPublicationState? ReadPublicationState()
        {
            Assert.True(prepared); Calls.Add("refs"); var read = refReads++;
            var refs = new Dictionary<string, string> { ["Atlas/main"] = "A1", ["Atlas/feature"] = publications >= 1 ? "F2" : "F1", ["Other/main"] = publications >= 2 ? "X1" : "X0" };
            var result = new Phase0ReviewPublicationState("B1", refs, ReviewId, "needs_review");
            if (read != checkpoint) return result;
            switch (fault)
            {
                case "Main": refs["Atlas/main"] = "M1"; break;
                case "Feature": refs["Atlas/feature"] = "wrong"; break;
                case "Other": refs["Other/main"] = "wrong"; break;
                case "RefMissing": refs.Remove("Other/main"); break;
                case "RefExtra": refs["Other/extra"] = "bad"; break;
            }
            return fault switch { "MissingRefs" => null, "LocalMain" => result with { LocalMain = "M1" }, "ReviewId" => result with { ReviewId = Guid.Empty }, "ReviewState" => result with { State = "resolved" }, _ => result };
        }
    }
}
