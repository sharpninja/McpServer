namespace FWH.Common.Copilot;

/// <summary>TR-CLI-001: Interface for invoking the Copilot CLI agent.</summary>
public interface ICopilotClient
{
    /// <summary>
    /// Invoke the Copilot CLI agent with a prompt and return a structured result.
    /// </summary>
    /// <param name="prompt">The prompt text to send to the agent.</param>
    /// <param name="options">Optional per-call configuration overrides.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A structured <see cref="CopilotResult"/> with state, body, and optional parsed object.</returns>
    Task<CopilotResult> InvokeAsync(
        string prompt,
        CopilotClientOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invoke the Copilot CLI agent with a prompt and deserialize the result as <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected deserialized type.</typeparam>
    /// <param name="prompt">The prompt text to send to the agent.</param>
    /// <param name="options">Optional per-call configuration overrides.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A strongly-typed <see cref="CopilotResult{T}"/>.</returns>
    Task<CopilotResult<T>> InvokeAsync<T>(
        string prompt,
        CopilotClientOptions? options = null,
        CancellationToken cancellationToken = default);
}
