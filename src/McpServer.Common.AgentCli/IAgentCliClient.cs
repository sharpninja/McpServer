using System.Diagnostics.CodeAnalysis;

namespace McpServer.Common.AgentCli;

/// <summary>TR-CLI-001: Interface for invoking the CLI agent agent.</summary>
public interface IAgentCliClient
{
    /// <summary>
    /// Invoke the CLI agent agent with a prompt and return a structured result.
    /// </summary>
    /// <param name="prompt">The prompt text to send to the agent.</param>
    /// <param name="options">Optional per-call configuration overrides.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A structured <see cref="AgentCliResult"/> with state, body, and optional parsed object.</returns>
    Task<AgentCliResult> InvokeAsync(
        string prompt,
        AgentCliClientOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invoke the CLI agent agent with a prompt and deserialize the result as <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected deserialized type.</typeparam>
    /// <param name="prompt">The prompt text to send to the agent.</param>
    /// <param name="options">Optional per-call configuration overrides.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A strongly-typed <see cref="AgentCliResult{T}"/>.</returns>
    [RequiresUnreferencedCode("Generic CLI output parsing requires runtime serializer metadata for arbitrary caller-supplied types.")]
    Task<AgentCliResult<T>> InvokeAsync<T>(
        string prompt,
        AgentCliClientOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invoke the CLI agent agent and stream stdout lines as they are produced.
    /// </summary>
    /// <param name="prompt">The prompt text to send to the agent.</param>
    /// <param name="options">Optional per-call configuration overrides.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of stdout lines.</returns>
    IAsyncEnumerable<string> InvokeStreamingAsync(
        string prompt,
        AgentCliClientOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a persistent interactive CLI agent session using <c>-i</c>.
    /// The <paramref name="initialPrompt"/> is passed as the <c>-i</c> argument value.
    /// Subsequent prompts are written to the process's stdin.
    /// </summary>
    /// <param name="initialPrompt">The seed prompt passed to <c>-i</c>.</param>
    /// <param name="options">Optional per-call configuration overrides.</param>
    /// <returns>An interactive session that must be disposed when no longer needed.</returns>
    AgentCliInteractiveSession CreateInteractiveSession(
        string initialPrompt,
        AgentCliClientOptions? options = null);
}
