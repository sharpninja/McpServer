namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Generates agent-consumable prompts for TODO items and invokes Copilot CLI
/// in the workspace directory, streaming output line by line.
/// Extracted from VS2026 extension copilot functions (MVP-MCP-002).
/// </summary>
public interface ITodoPromptService
{
    /// <summary>
    /// Invokes Copilot to produce a status report for a TODO item, streaming
    /// output lines as they are produced.
    /// </summary>
    /// <param name="id">The TODO item id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of output lines.</returns>
    IAsyncEnumerable<string> StreamStatusAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes Copilot with an implementation prompt for a TODO item, streaming
    /// output lines as they are produced.
    /// </summary>
    /// <param name="id">The TODO item id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of output lines.</returns>
    IAsyncEnumerable<string> StreamImplementAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes Copilot with a planning prompt for a TODO item, streaming
    /// output lines as they are produced.
    /// </summary>
    /// <param name="id">The TODO item id.</param>
    /// <param name="additionalPrompt">Optional prompt text from the client (e.g. extension); appended to the template prompt when provided.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of output lines.</returns>
    IAsyncEnumerable<string> StreamPlanAsync(string id, string? additionalPrompt = null, CancellationToken cancellationToken = default);
}
