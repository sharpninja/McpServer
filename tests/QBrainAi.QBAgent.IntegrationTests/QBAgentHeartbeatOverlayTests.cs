using QBrainAi.QBAgent;

namespace QBrainAi.QBAgent.IntegrationTests;

/// <summary>
/// Heartbeat paints a rotating glyph in the top-right cell and does not consume a line.
/// When the turn ends or the runner fails (server not working), the glyph is hidden.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of QBAgentHeartbeatOverlayTests, split from its four unit cases in QBrainAi.QBAgent.Tests into the integration-owned QBrainAi.QBAgent.IntegrationTests so the unit-owned project holds only unit cases (slice S3, hostile validation finding R14-F4; operator 2026-11-06: "Per-owner projects (Recommended)"); the case, its documentation and its earlier change-log entry moved unchanged and the halves share no private helper; only the namespace changed.</remarks>
public sealed class QBAgentHeartbeatOverlayTests
{
    /// <summary>
    /// The run loop paints rotating heartbeat frames in the top-right cell while a turn runs, writes no "still working"
    /// line, and hides the glyph when the turn ends. An integration case: it expects the real 20 ms heartbeat timer to
    /// tick at least once during an 80 ms real-time wait, so its outcome depends on how much real time passes.
    /// </summary>
    /// <returns>The test task.</returns>
    /// <remarks>2026-11-03, TR-MCP-USECASE-018 AC-006 (appended "A wall-clock bound is an assertion whose outcome depends on how much real time passes ... such a test is an integration test"): tagged Category=Integration with its assertions unchanged, so it runs in the MigrationIntegrationTests lane and not in the unit suite, because the holistic read before round 11 found the second frame depends on scheduling within an 80 ms wait.</remarks>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task RunAsync_Heartbeat_RotatesGlyphWithoutStillWorkingLines()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        QBAgentPromptRunner runner = async (_, cancellationToken) =>
        {
            started.TrySetResult();
            await release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return "done";
        };
        using var input = new StringReader("go\nexit\n");
        using var output = new StringWriter();

        var loop = QBAgentRunLoop.RunAsync(
            runner,
            input,
            output,
            options: new QBAgentRunLoopOptions
            {
                HeartbeatInterval = TimeSpan.FromMilliseconds(20),
                TerminalWidth = static () => 40,
            },
            cancellationToken: TestContext.Current.CancellationToken);

        await started.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken).ConfigureAwait(true);
        await Task.Delay(80, TestContext.Current.CancellationToken).ConfigureAwait(true);
        release.TrySetResult();
        await loop.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken).ConfigureAwait(true);

        var text = output.ToString();
        Assert.DoesNotContain("still working", text, StringComparison.Ordinal);
        Assert.Contains("\u001b[1;40H", text, StringComparison.Ordinal);
        Assert.Contains(QBAgentHeartbeatOverlay.FrameAt(0), text);
        Assert.Contains(QBAgentHeartbeatOverlay.FrameAt(1), text);
        Assert.Contains("\u001b[1;40H ", text, StringComparison.Ordinal);
        Assert.Contains("done", text, StringComparison.Ordinal);
    }
}
