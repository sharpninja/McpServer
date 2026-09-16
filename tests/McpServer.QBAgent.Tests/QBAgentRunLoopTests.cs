using System.Collections.Concurrent;
using McpServer.Client.Models;
using McpServer.QBAgent;
using Microsoft.Extensions.AI;

namespace McpServer.QBAgent.Tests;

/// <summary>
/// TEST-MCP-QBAGENT-001: Verifies the interactive run loop (FR-MCP-QBAGENT-001) - each non-empty prompt is run
/// through the bound agent and the assistant text is written; blank lines and exit commands are handled; end of
/// input stops the loop; a runner failure is reported without aborting.
/// </summary>
public sealed class QBAgentRunLoopTests
{
    [Fact]
    public async Task RunAsync_AppendsUserAndAssistantToSessionLog()
    {
        var root = Path.Combine(Path.GetTempPath(), "qbagent-loop-" + Guid.NewGuid().ToString("N"));
        var log = QBAgentSessionLog.CreateNew(root);
        QBAgentPromptRunner runner = (_, _) => Task.FromResult("pong");
        using var input = new StringReader("ping\nexit\n");
        using var output = new StringWriter();

        await QBAgentRunLoop.RunAsync(
                runner,
                input,
                output,
                options: new QBAgentRunLoopOptions
                {
                    HeartbeatInterval = Timeout.InfiniteTimeSpan,
                    SessionLog = log,
                },
                cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Contains($"Session {log.SessionId}", output.ToString(), StringComparison.Ordinal);
        var messages = log.ToChatMessages();
        Assert.Contains(messages, item => item.Role == ChatRole.User && item.Text == "ping");
        Assert.Contains(messages, item => item.Role == ChatRole.Assistant && item.Text == "pong");
    }

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
        var text = output.ToString();
        Assert.Contains("done: wrote the file", text, StringComparison.Ordinal);
        Assert.Contains("running prompt", text, StringComparison.Ordinal);
        Assert.Matches(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}", text);
    }

    [Fact]
    public async Task RunAsync_PrefixesResponseWithLocalSortableTimestamp()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 9, 10, 20, 25, 17, TimeSpan.Zero));
        QBAgentPromptRunner runner = (_, _) => Task.FromResult("PONG");
        using var input = new StringReader("ping\nexit\n");
        using var output = new StringWriter();

        await QBAgentRunLoop.RunAsync(
                runner,
                input,
                output,
                options: new QBAgentRunLoopOptions { Clock = clock, HeartbeatInterval = Timeout.InfiniteTimeSpan },
                cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Contains("2026-09-10 20:25:17", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("PONG", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_ListOpenTodo_DoesNotCallQuadBrain()
    {
        var calls = 0;
        QBAgentPromptRunner runner = (_, _) => { calls++; return Task.FromResult("should-not-run"); };
        var listed = 0;
        using var input = new StringReader("list open todo\nexit\n");
        using var output = new StringWriter();

        var processed = await QBAgentRunLoop.RunAsync(
                runner,
                input,
                output,
                options: new QBAgentRunLoopOptions
                {
                    HeartbeatInterval = Timeout.InfiniteTimeSpan,
                    ListOpenTodos = _ =>
                    {
                        listed++;
                        return Task.FromResult("1 open TODO (done: false):\nPLAN-X\thigh\tTitle");
                    },
                },
                cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal(0, processed);
        Assert.Equal(0, calls);
        Assert.Equal(1, listed);
        Assert.Contains("PLAN-X", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("listing open MCP TODOs", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void FormatOpenTodos_Empty_SaysNone()
        => Assert.Equal("No open TODOs (done: false).", QBAgentOpenTodoList.Format(new TodoQueryResult()));

    [Fact]
    public void FormatOpenTodos_EmptyWithWorkspace_IncludesPath()
        => Assert.Equal(
            "No open TODOs (done: false) in F:\\GitHub\\McpServerManager.",
            QBAgentOpenTodoList.Format(new TodoQueryResult(), @"F:\GitHub\McpServerManager"));

    [Fact]
    public void FormatOpenTodos_IncludesIdPriorityTitle()
    {
        var result = new TodoQueryResult
        {
            TotalCount = 1,
            Items = [new TodoFlatItem { Id = "PLAN-X-001", Title = "Do the thing", Priority = "high", Done = false }],
        };

        var text = QBAgentOpenTodoList.Format(result);
        Assert.Contains("1 open TODO", text, StringComparison.Ordinal);
        Assert.Contains("PLAN-X-001", text, StringComparison.Ordinal);
        Assert.Contains("high", text, StringComparison.Ordinal);
        Assert.Contains("Do the thing", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("exit")]
    [InlineData("/exit")]
    [InlineData("quit")]
    [InlineData("/quit")]
    public async Task RunAsync_ExitAliases_DispatchNothing(string command)
    {
        var calls = 0;
        QBAgentPromptRunner runner = (_, _) => { calls++; return Task.FromResult("x"); };
        using var input = new StringReader(command + "\n");
        using var output = new StringWriter();

        var processed = await QBAgentRunLoop.RunAsync(runner, input, output, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(0, processed);
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData("new session")]
    [InlineData("reset")]
    [InlineData("/new")]
    [InlineData("/reset")]
    public async Task RunAsync_NewSession_DoesNotCallQuadBrain(string command)
    {
        var calls = 0;
        QBAgentPromptRunner runner = (_, _) => { calls++; return Task.FromResult("should-not-run"); };
        var resets = 0;
        QBAgentSessionReset reset = _ =>
        {
            resets++;
            return Task.FromResult("Started a new session.");
        };
        using var input = new StringReader(command + "\nexit\n");
        using var output = new StringWriter();

        var processed = await QBAgentRunLoop.RunAsync(
                runner,
                input,
                output,
                reset,
                cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal(0, processed);
        Assert.Equal(0, calls);
        Assert.Equal(1, resets);
        Assert.Contains("Started a new session.", output.ToString(), StringComparison.Ordinal);
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

    private sealed class FixedUtcTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
