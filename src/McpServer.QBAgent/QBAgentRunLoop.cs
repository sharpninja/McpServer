using System.Globalization;

namespace McpServer.QBAgent;

/// <summary>
/// FR-MCP-QBAGENT-001: Runs one coding prompt through the bound QBAgent (the Microsoft Agent Framework agent
/// whose model is QuadBrain) and returns the assistant's final text. The agent itself executes any tool calls
/// QuadBrain emits during the turn.
/// </summary>
/// <param name="prompt">The user coding prompt.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The assistant's final response text.</returns>
public delegate Task<string> QBAgentPromptRunner(string prompt, CancellationToken cancellationToken);

/// <summary>
/// FR-MCP-QBAGENT-001: Optional callback that starts a fresh Agent Framework session without sending
/// a prompt to QuadBrain.
/// </summary>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>Confirmation text written to the console.</returns>
public delegate Task<string> QBAgentSessionReset(CancellationToken cancellationToken);

/// <summary>
/// FR-MCP-QBAGENT-001: Interactive run loop that reads coding prompts and runs each through the bound QBAgent
/// (QuadBrain model + Agent Framework tool loop) until an exit command or end of input.
/// </summary>
public static class QBAgentRunLoop
{
    private static readonly HashSet<string> ExitCommands =
        new(StringComparer.OrdinalIgnoreCase) { "exit", "quit", ":q", "/exit", "/quit" };

    private static readonly HashSet<string> NewSessionCommands =
        new(StringComparer.OrdinalIgnoreCase) { "new session", "reset", "/new", "/reset" };

    private static readonly HashSet<string> ListOpenTodoCommands =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "list open todo",
            "list open todos",
            "list todos",
            "list todo",
        };

    /// <summary>
    /// Runs the interactive loop: each non-empty line is sent to <paramref name="runner"/> and the assistant
    /// text is written to <paramref name="output"/>. Stops on an exit command or end of input; a runner failure
    /// is reported without aborting the loop.
    /// </summary>
    /// <param name="runner">Runs one prompt through the bound agent and returns the assistant text.</param>
    /// <param name="input">Prompt source.</param>
    /// <param name="output">Result sink.</param>
    /// <param name="resetSession">Optional callback for local <c>new session</c> / <c>reset</c> commands.</param>
    /// <param name="options">Timestamps, progress heartbeat, and local <c>list open todo</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of prompts dispatched to the runner.</returns>
    public static async Task<int> RunAsync(
        QBAgentPromptRunner runner,
        TextReader input,
        TextWriter output,
        QBAgentSessionReset? resetSession = null,
        QBAgentRunLoopOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        options ??= new QBAgentRunLoopOptions();
        var clock = options.Clock ?? TimeProvider.System;
        var processed = 0;
        if (options.SessionLog is not null)
        {
            await output.WriteLineAsync($"Session {options.SessionLog.SessionId}").ConfigureAwait(false);
            await output.WriteLineAsync($"Log {options.SessionLog.FilePath}").ConfigureAwait(false);
        }

        await output.WriteLineAsync("QBAgent ready. Enter a coding prompt, or 'exit' to quit.").ConfigureAwait(false);

        while (!cancellationToken.IsCancellationRequested)
        {
            await output.WriteAsync("qbagent> ").ConfigureAwait(false);
            var line = await input.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
                break; // end of input

            var prompt = line.Trim();
            if (prompt.Length == 0)
                continue;
            if (ExitCommands.Contains(prompt))
                break;
            if (NewSessionCommands.Contains(prompt))
            {
                var message = resetSession is null
                    ? "Started a new session."
                    : await resetSession(cancellationToken).ConfigureAwait(false);
                await WriteResponseAsync(
                        output,
                        clock,
                        string.IsNullOrWhiteSpace(message) ? "Started a new session." : message.Trim())
                    .ConfigureAwait(false);
                continue;
            }

            if (ListOpenTodoCommands.Contains(prompt))
            {
                await WriteProgressAsync(output, clock, "listing open MCP TODOs (done: false)").ConfigureAwait(false);
                if (options.ListOpenTodos is null)
                {
                    await WriteResponseAsync(
                            output,
                            clock,
                            "list open todo is not bound in this host. Ask QuadBrain, or restart qbagent.")
                        .ConfigureAwait(false);
                    continue;
                }

                try
                {
                    var listing = await options.ListOpenTodos(cancellationToken).ConfigureAwait(false);
                    await WriteResponseAsync(
                            output,
                            clock,
                            string.IsNullOrWhiteSpace(listing) ? "No open TODOs (done: false)." : listing.Trim())
                        .ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    await WriteResponseAsync(output, clock, $"[error] {ex.Message}").ConfigureAwait(false);
                }

                continue;
            }

            processed++;
            options.SessionLog?.Append("user", prompt);
            try
            {
                await WriteProgressAsync(output, clock, "running prompt").ConfigureAwait(false);
                using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var heartbeat = HeartbeatAsync(output, options, heartbeatCts.Token);
                string responseText;
                try
                {
                    responseText = await runner(prompt, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    await heartbeatCts.CancelAsync().ConfigureAwait(false);
                    try
                    {
                        await heartbeat.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }

                options.SessionLog?.Append("assistant", responseText);
                await WriteResponseAsync(
                        output,
                        clock,
                        FormatResponseBody(responseText, options.ShowIntent))
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                options.SessionLog?.Append("assistant", $"[error] {ex.Message}");
                await WriteResponseAsync(output, clock, $"[error] {ex.Message}").ConfigureAwait(false);
            }
        }

        return processed;
    }

    /// <summary>Local sortable timestamp <c>yyyy-MM-dd HH:mm:ss</c>.</summary>
    public static string FormatLocalTimestamp(TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        return clock.GetLocalNow().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private static Task WriteProgressAsync(TextWriter output, TimeProvider clock, string message)
    {
        lock (output)
        {
            output.WriteLine($"[{FormatLocalTimestamp(clock)}] {message}");
            output.Flush();
        }

        return Task.CompletedTask;
    }

    private static Task WriteResponseAsync(TextWriter output, TimeProvider clock, string body)
    {
        lock (output)
        {
            output.WriteLine(FormatLocalTimestamp(clock));
            output.WriteLine(body);
            output.Flush();
        }

        return Task.CompletedTask;
    }

    private static string FormatResponseBody(string? responseText, bool showIntent)
    {
        if (string.IsNullOrWhiteSpace(responseText))
            return "(no response)";
        var trimmed = responseText.Trim();
        var display = QBAgentIntentDisplay.FormatForDisplay(trimmed, showIntent);
        return string.IsNullOrWhiteSpace(display) ? trimmed : display;
    }

    private static async Task HeartbeatAsync(
        TextWriter output,
        QBAgentRunLoopOptions options,
        CancellationToken cancellationToken)
    {
        var interval = options.HeartbeatInterval;
        if (interval <= TimeSpan.Zero || interval == Timeout.InfiniteTimeSpan)
            return;

        var column = ResolveHeartbeatColumn(options);
        var beat = 0;
        var painted = false;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                WriteOverlay(output, QBAgentHeartbeatOverlay.Paint(column, QBAgentHeartbeatOverlay.FrameAt(beat++)));
                painted = true;
                await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (painted)
                WriteOverlay(output, QBAgentHeartbeatOverlay.Hide(column));
        }
    }

    private static int ResolveHeartbeatColumn(QBAgentRunLoopOptions options)
    {
        try
        {
            var width = options.TerminalWidth is not null
                ? options.TerminalWidth()
                : Console.WindowWidth;
            return Math.Max(1, width);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentOutOfRangeException)
        {
            return 80;
        }
    }

    private static void WriteOverlay(TextWriter output, string sequence)
    {
        lock (output)
        {
            output.Write(sequence);
            output.Flush();
        }
    }
}
