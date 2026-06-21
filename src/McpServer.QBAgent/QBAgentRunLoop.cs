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
/// FR-MCP-QBAGENT-001: Interactive run loop that reads coding prompts and runs each through the bound QBAgent
/// (QuadBrain model + Agent Framework tool loop) until an exit command or end of input.
/// </summary>
public static class QBAgentRunLoop
{
    private static readonly HashSet<string> ExitCommands =
        new(StringComparer.OrdinalIgnoreCase) { "exit", "quit", ":q" };

    /// <summary>
    /// Runs the interactive loop: each non-empty line is sent to <paramref name="runner"/> and the assistant
    /// text is written to <paramref name="output"/>. Stops on an exit command or end of input; a runner failure
    /// is reported without aborting the loop.
    /// </summary>
    /// <param name="runner">Runs one prompt through the bound agent and returns the assistant text.</param>
    /// <param name="input">Prompt source.</param>
    /// <param name="output">Result sink.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of prompts dispatched to the runner.</returns>
    public static async Task<int> RunAsync(
        QBAgentPromptRunner runner,
        TextReader input,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        var processed = 0;
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

            processed++;
            try
            {
                var responseText = await runner(prompt, cancellationToken).ConfigureAwait(false);
                await output.WriteLineAsync(string.IsNullOrWhiteSpace(responseText) ? "(no response)" : responseText.Trim())
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await output.WriteLineAsync($"[error] {ex.Message}").ConfigureAwait(false);
            }
        }

        return processed;
    }
}
