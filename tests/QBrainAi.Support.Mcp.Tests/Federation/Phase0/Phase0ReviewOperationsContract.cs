namespace QBrainAi.Support.Mcp.Tests.Federation.Phase0;

/// <summary>One independently queried review display, never a Boolean visibility assertion.</summary>
/// <param name="Surface">status, selected plugin, or inbox.</param><param name="ReviewId">Original durable UUID.</param>
/// <param name="Project">Project scope.</param><param name="Branch">Blocked branch.</param><param name="State">Review state.</param>
/// <param name="AgeSeconds">Observed age.</param><param name="Severity">Visible warning or critical severity.</param>
/// <remarks>2026-11-17: FR-MCP-180 ac-ReviewAgeVisible; TR-MCP-FEDERATION-007; TEST-MCP-228.</remarks>
internal sealed record Phase0ReviewView(string Surface, Guid ReviewId, string Project, string Branch, string State, long AgeSeconds, string Severity);

/// <summary>Actual review displays and independent emitted-repeat history at a clock checkpoint.</summary>
/// <param name="Views">All three selected fixture surfaces.</param><param name="RepeatSeconds">Durably emitted repeat timestamps relative to creation.</param>
/// <param name="Emails">Implicit email sends.</param><param name="SlackMessages">Implicit Slack sends.</param>
/// <remarks>2026-11-17: FR-MCP-180 ac-ReviewAgeVisible; TR-MCP-FEDERATION-007; TEST-MCP-228.</remarks>
internal sealed record Phase0ReviewVisibility(Phase0ReviewView[] Views, long[] RepeatSeconds, int Emails, int SlackMessages);

/// <summary>Independent branch/ref state while one branch remains blocked.</summary>
/// <param name="LocalMain">Local blocked head.</param><param name="PublicRefs">Complete fixture public map.</param>
/// <param name="ReviewId">Original blocked review.</param><param name="State">Original review state.</param>
/// <remarks>2026-11-17: FR-MCP-180 ac-UnrelatedPublicationContinues; TR-MCP-FEDERATION-007; TEST-MCP-228.</remarks>
internal sealed record Phase0ReviewPublicationState(string LocalMain, IReadOnlyDictionary<string, string> PublicRefs, Guid ReviewId, string State);

/// <summary>Disposable clock, selected-plugin/inbox and branch-publication seam.</summary>
/// <remarks>2026-11-17: TASK-FEDPH0-006/007; FR-MCP-180 ac-ReviewAgeVisible/ac-UnrelatedPublicationContinues; TR-MCP-FEDERATION-007; TEST-MCP-228.</remarks>
internal interface IPhase0ReviewOperationsProbe
{
    /// <summary>Prepares original Atlas/main review and isolated publication refs.</summary>
    void Prepare();
    /// <summary>Advances the fake clock and evaluates due review visibility/notifications.</summary><param name="elapsedSeconds">Absolute elapsed time since the12:00 fixture creation.</param>
    void AdvanceClock(long elapsedSeconds);
    /// <summary>Reads each selected surface and actual durable emission records independently.</summary><returns>Observed displays and repeat history.</returns>
    Phase0ReviewVisibility? ReadVisibility();
    /// <summary>Closes and reopens owning stores/processes without clearing durable review evidence.</summary>
    void Restart();
    /// <summary>Publishes an unrelated fixture branch through its normal publication boundary.</summary>
    /// <param name="project">Atlas or Other.</param><param name="branch">feature or main.</param><param name="head">F2 or X1.</param>
    /// <returns>Whether publication actually reports success, checked separately from refs.</returns>
    bool Publish(string project, string branch, string head);
    /// <summary>Reads all protected refs, local blocked main and original review independently.</summary><returns>Actual persisted branch state.</returns>
    Phase0ReviewPublicationState? ReadPublicationState();
}

/// <summary>Requirements-derived review ageing, durable repeat suppression and unrelated publication assertions.</summary>
/// <remarks>
/// 2026-11-17: TASK-FEDPH0-006/007; FR-MCP-180 ac-ReviewAgeVisible/ac-UnrelatedPublicationContinues; TR-MCP-FEDERATION-007; TEST-MCP-228.
/// Scripted controls do not establish a real scheduler, all-plugin conformance or gateway publication safety.
/// </remarks>
internal static class Phase0ReviewOperationsContract
{
    private static readonly Guid ReviewId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    /// <summary>Checks fifteen-minute and twenty-four-hour boundaries plus durable at-most-hourly repeat history.</summary>
    /// <param name="probe">Scripted or later real disposable operations binding.</param>
    internal static void AssertReviewAge(IPhase0ReviewOperationsProbe probe)
    {
        probe.Prepare(); long[] prior = [];
        foreach (var elapsed in new long[] { 900, 86400, 86460, 90000 })
        {
            probe.AdvanceClock(elapsed); var current = probe.ReadVisibility(); Assert.NotNull(current);
            AssertVisibility(current, elapsed); AssertPrefix(prior, current.RepeatSeconds); prior = current.RepeatSeconds.ToArray();
            if (elapsed != 86460) continue;
            probe.Restart(); var reopened = probe.ReadVisibility(); Assert.NotNull(reopened); AssertVisibility(reopened, elapsed);
            Assert.Equal(prior, reopened.RepeatSeconds);
        }
    }
    /// <summary>Checks each unrelated publication and reopen without changing the blocked branch or review.</summary>
    /// <param name="probe">Scripted or later real disposable operations binding.</param>
    internal static void AssertUnrelatedPublication(IPhase0ReviewOperationsProbe probe)
    {
        probe.Prepare(); AssertPublication(probe.ReadPublicationState(), "F1", "X0");
        Assert.True(probe.Publish("Atlas", "feature", "F2")); AssertPublication(probe.ReadPublicationState(), "F2", "X0");
        Assert.True(probe.Publish("Other", "main", "X1")); AssertPublication(probe.ReadPublicationState(), "F2", "X1");
        probe.Restart(); AssertPublication(probe.ReadPublicationState(), "F2", "X1");
    }
    /// <summary>Compares every surface identity and exact elapsed-age/severity value, then validates actual repeats.</summary>
    /// <param name="observed">Independent visibility/emission read.</param><param name="elapsed">Controlled fixture time.</param>
    private static void AssertVisibility(Phase0ReviewVisibility observed, long elapsed)
    {
        Assert.Equal(new[] { "inbox", "plugin", "status" }, observed.Views.Select(view => view.Surface).OrderBy(value => value));
        foreach (var view in observed.Views)
        {
            Assert.Equal(ReviewId, view.ReviewId); Assert.Equal("Atlas", view.Project); Assert.Equal("main", view.Branch);
            Assert.Equal("needs_review", view.State); Assert.Equal(elapsed, view.AgeSeconds); Assert.Equal(elapsed < 86400 ? "warning" : "critical", view.Severity);
        }
        Assert.Equal(0, observed.Emails); Assert.Equal(0, observed.SlackMessages);
        long? previous = null;
        foreach (var emitted in observed.RepeatSeconds)
        {
            Assert.InRange(emitted, 0, elapsed);
            if (previous.HasValue) Assert.True(emitted - previous.Value >= 3600, "Repeat history must be ordered with at least one hour between emissions.");
            previous = emitted;
        }
    }
    /// <summary>Ensures previously observed durable emissions cannot disappear as the clock advances.</summary>
    /// <param name="prior">Prior independently observed history.</param><param name="current">Current history.</param>
    private static void AssertPrefix(long[] prior, long[] current)
    {
        Assert.True(current.Length >= prior.Length); Assert.Equal(prior, current.Take(prior.Length));
    }
    /// <summary>Checks complete protected refs and original blocked review after one operation.</summary>
    /// <param name="state">Independent branch state.</param><param name="feature">Expected Atlas feature head.</param><param name="other">Expected Other main head.</param>
    private static void AssertPublication(Phase0ReviewPublicationState? state, string feature, string other)
    {
        Assert.NotNull(state); Assert.Equal("B1", state.LocalMain); Assert.Equal(ReviewId, state.ReviewId); Assert.Equal("needs_review", state.State);
        var expected = new Dictionary<string, string> { ["Atlas/main"] = "A1", ["Atlas/feature"] = feature, ["Other/main"] = other };
        Assert.Equal(expected.OrderBy(pair => pair.Key), state.PublicRefs.OrderBy(pair => pair.Key));
    }
}
