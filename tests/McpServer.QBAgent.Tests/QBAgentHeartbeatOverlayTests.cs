using McpServer.QBAgent;

namespace McpServer.QBAgent.Tests;

/// <summary>
/// Heartbeat paints a rotating glyph in the top-right cell and does not consume a line.
/// When the turn ends or the runner fails (server not working), the glyph is hidden.
/// </summary>
public sealed class QBAgentHeartbeatOverlayTests
{
    [Fact]
    public void Paint_DoesNotContainNewline()
    {
        var text = QBAgentHeartbeatOverlay.Paint(80, '|');
        Assert.DoesNotContain('\n', text);
        Assert.DoesNotContain('\r', text);
        Assert.Contains("\u001b[1;80H", text, StringComparison.Ordinal);
        Assert.Contains('|', text);
    }

    [Fact]
    public void Hide_OverwritesGlyphWithSpace_SameCell()
    {
        var text = QBAgentHeartbeatOverlay.Hide(80);
        Assert.DoesNotContain('\n', text);
        Assert.Contains("\u001b[1;80H ", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Frames_Rotate()
    {
        Assert.NotEqual(QBAgentHeartbeatOverlay.FrameAt(0), QBAgentHeartbeatOverlay.FrameAt(1));
        Assert.Equal(QBAgentHeartbeatOverlay.FrameAt(0), QBAgentHeartbeatOverlay.FrameAt(QBAgentHeartbeatOverlay.Frames.Length));
    }

    [Fact]
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
            .WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var text = output.ToString();
        Assert.DoesNotContain("still working", text, StringComparison.Ordinal);
        Assert.Contains("\u001b[1;24H ", text, StringComparison.Ordinal);
        Assert.Contains("quadbrain unavailable", text, StringComparison.Ordinal);
    }
}
