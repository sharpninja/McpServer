using QBrainAi.QBAgent;

namespace QBrainAi.QBAgent.Tests;

/// <summary>
/// Heartbeat paints a rotating glyph in the top-right cell and does not consume a line.
/// When the turn ends or the runner fails (server not working), the glyph is hidden.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class; its one Category=Integration case, RunAsync_Heartbeat_RotatesGlyphWithoutStillWorkingLines, moved unchanged to the class of the same name in the integration-owned QBrainAi.QBAgent.IntegrationTests, so this unit-owned project holds only unit cases (slice S3, hostile validation finding R14-F4); the four unit cases are unchanged and the halves share no private helper.</remarks>
public sealed class QBAgentHeartbeatOverlayTests
{
    /// <summary>
    /// No requirement id applies: no FR, TR or TEST record names the QBAgent heartbeat overlay. The case proves that
    /// QBAgentHeartbeatOverlay.Paint for column 80 and glyph | writes no line feed or carriage return, moves the cursor
    /// to row 1, column 80 (<c>ESC[1;80H</c>) and writes the glyph. Data: the literal column 80 and glyph |; no
    /// fixture.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, TR-MCP-NUKE-019 AC-8: quarantined with the trait Quarantine=NoMatchingCriterion (operator 2026-11-07: "If none match, quarantine them (keep, but descope them) for later manual investigation."): no acceptance criterion in the requirement store matches the behavior. The FR, TR and TEST criteria, titles and descriptions were searched for heartbeat, glyph, still working, spinner, overlay, top right and qbagent (uncited-matches.txt, test 1 of 23); the nearest, FR-MCP-QBAGENT-001 criterion 4 (the interactive run loop), states no overlay paint text. Test and MigrationIntegrationTests run no quarantined case; TestGroups runs it when this class is named; the test code is unchanged.</para>
    /// </remarks>
    [Fact]
    [Trait("Quarantine", "NoMatchingCriterion")]
    public void Paint_DoesNotContainNewline()
    {
        var text = QBAgentHeartbeatOverlay.Paint(80, '|');
        Assert.DoesNotContain('\n', text);
        Assert.DoesNotContain('\r', text);
        Assert.Contains("\u001b[1;80H", text, StringComparison.Ordinal);
        Assert.Contains('|', text);
    }

    /// <summary>
    /// No requirement id applies: no FR, TR or TEST record names the QBAgent heartbeat overlay. The case proves that
    /// QBAgentHeartbeatOverlay.Hide for column 80 writes no line feed and writes a space in the cell Paint uses
    /// (<c>ESC[1;80H</c> followed by a space). Data: the literal column 80; no fixture.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, TR-MCP-NUKE-019 AC-8: quarantined with the trait Quarantine=NoMatchingCriterion (operator 2026-11-07: "If none match, quarantine them (keep, but descope them) for later manual investigation."): no acceptance criterion in the requirement store matches the behavior. The FR, TR and TEST criteria, titles and descriptions were searched for heartbeat, glyph, still working, spinner, overlay, top right and qbagent (uncited-matches.txt, test 2 of 23); the nearest, FR-MCP-QBAGENT-001 criterion 4 (the interactive run loop), states nothing about how the overlay erases its glyph. Test and MigrationIntegrationTests run no quarantined case; TestGroups runs it when this class is named; the test code is unchanged.</para>
    /// </remarks>
    [Fact]
    [Trait("Quarantine", "NoMatchingCriterion")]
    public void Hide_OverwritesGlyphWithSpace_SameCell()
    {
        var text = QBAgentHeartbeatOverlay.Hide(80);
        Assert.DoesNotContain('\n', text);
        Assert.Contains("\u001b[1;80H ", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// No requirement id applies: no FR, TR or TEST record names the QBAgent heartbeat overlay. The case proves that
    /// QBAgentHeartbeatOverlay.FrameAt gives different glyphs for beats 0 and 1 and wraps around, giving the same glyph
    /// for beat 0 and for the beat equal to the frame count. Data: the overlay Frames array; no fixture.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, TR-MCP-NUKE-019 AC-8: quarantined with the trait Quarantine=NoMatchingCriterion (operator 2026-11-07: "If none match, quarantine them (keep, but descope them) for later manual investigation."): no acceptance criterion in the requirement store matches the behavior. The FR, TR and TEST criteria, titles and descriptions were searched for heartbeat, glyph, still working, spinner, overlay, top right, qbagent and rotate wording (uncited-matches.txt, test 3 of 23); no criterion describes heartbeat frames. Test and MigrationIntegrationTests run no quarantined case; TestGroups runs it when this class is named; the test code is unchanged.</para>
    /// </remarks>
    [Fact]
    [Trait("Quarantine", "NoMatchingCriterion")]
    public void Frames_Rotate()
    {
        Assert.NotEqual(QBAgentHeartbeatOverlay.FrameAt(0), QBAgentHeartbeatOverlay.FrameAt(1));
        Assert.Equal(QBAgentHeartbeatOverlay.FrameAt(0), QBAgentHeartbeatOverlay.FrameAt(QBAgentHeartbeatOverlay.Frames.Length));
    }

    /// <summary>
    /// The run loop hides the heartbeat glyph and reports the error when the runner fails after a scripted 80 ms of
    /// work, and writes no "still working" line. The run is bounded by a hang guard only.
    /// </summary>
    /// <returns>The test task.</returns>
    /// <remarks>2026-11-03, TR-MCP-USECASE-018 AC-006 (appended for finding B4: "Far above" means at least 100 times the longest real or scripted delay of the awaited step): the hang guard on the run loop rises from 2 s to 10 s, because the awaited run includes the runner's 80 ms delay, so a hang guard must be at least 8 s; the holistic read before hostile validation round 12 found 2 s, 25 times the delay (operator 2026-11-03: "Strengthen + margin (Recommended)").</remarks>
    [Fact]
    public async Task RunAsync_RunnerThrows_HidesGlyph()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        QBAgentPromptRunner runner = async (_, cancellationToken) =>
        {
            started.TrySetResult();
            await Task.Delay(80, cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException("quadbrain unavailable");
        };
        using var input = new StringReader("boom\nexit\n");
        using var output = new StringWriter();

        await QBAgentRunLoop.RunAsync(
                runner,
                input,
                output,
                options: new QBAgentRunLoopOptions
                {
                    HeartbeatInterval = TimeSpan.FromMilliseconds(20),
                    TerminalWidth = static () => 24,
                },
                cancellationToken: TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var text = output.ToString();
        Assert.DoesNotContain("still working", text, StringComparison.Ordinal);
        Assert.Contains("\u001b[1;24H ", text, StringComparison.Ordinal);
        Assert.Contains("quadbrain unavailable", text, StringComparison.Ordinal);
    }
}
