using System.Collections.Concurrent;
using McpServer.QBAgent;

namespace McpServer.QBAgent.Tests;

/// <summary>
/// TEST-MCP-QBAGENT-001: Verifies the interactive run loop (FR-MCP-QBAGENT-001) - each non-empty prompt is run
/// through the bound agent and the assistant text is written; blank lines and exit commands are handled; end of
/// input stops the loop; a runner failure is reported without aborting.
/// </summary>
public sealed class QBAgentRunLoopTests
{
    /// <summary>A prompt is run through the agent and the assistant text is written.</summary>
    [Fact]
    public async Task RunAsync_DispatchesPromptAndWritesResult()
    {
        var prompts = new ConcurrentQueue<string>();
        QBAgentPromptRunner runner = (prompt, _) =>
        {
            prompts.Enqueue(prompt);
            return Task.FromResult("done: wrote the file");
        };
        using var input = new StringReader("implement the thing\nexit\n");
        using var output = new StringWriter();

        var processed = await QBAgentRunLoop.RunAsync(runner, input, output, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(1, processed);
        Assert.Equal("implement the thing", Assert.Single(prompts));
        Assert.Contains("done: wrote the file", output.ToString(), StringComparison.Ordinal);
    }

    /// <summary>Blank lines are skipped and an exit command ends the loop with nothing dispatched.</summary>
    [Fact]
    public async Task RunAsync_BlankLinesThenExit_DispatchesNothing()
    {
        var calls = 0;
        QBAgentPromptRunner runner = (_, _) => { calls++; return Task.FromResult("x"); };
        using var input = new StringReader("\n   \nexit\n");
        using var output = new StringWriter();

        var processed = await QBAgentRunLoop.RunAsync(runner, input, output, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(0, processed);
        Assert.Equal(0, calls);
    }

    /// <summary>End of input (no exit command) stops the loop.</summary>
    [Fact]
    public async Task RunAsync_EndOfInput_Stops()
    {
        QBAgentPromptRunner runner = (_, _) => Task.FromResult("ok");
        using var input = new StringReader("only prompt\n");
        using var output = new StringWriter();

        var processed = await QBAgentRunLoop.RunAsync(runner, input, output, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(1, processed);
    }

    /// <summary>A runner failure is reported and the loop continues to the next prompt.</summary>
    [Fact]
    public async Task RunAsync_RunnerThrows_ReportsErrorAndContinues()
    {
        QBAgentPromptRunner runner = (prompt, _) =>
            prompt == "boom"
                ? throw new InvalidOperationException("quadbrain unavailable")
                : Task.FromResult("ok");
        using var input = new StringReader("boom\nok\nexit\n");
        using var output = new StringWriter();

        var processed = await QBAgentRunLoop.RunAsync(runner, input, output, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(2, processed);
        var text = output.ToString();
        Assert.Contains("[error]", text, StringComparison.Ordinal);
        Assert.Contains("quadbrain unavailable", text, StringComparison.Ordinal);
        Assert.Contains("ok", text, StringComparison.Ordinal);
    }
}
