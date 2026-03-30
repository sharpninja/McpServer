namespace McpServer.Repl.Core;

/// <summary>
/// Manages workspace trust establishment and persistence.
/// Handles user prompts, trust registry storage, and revocation.
/// </summary>
public interface ITrustBootstrapService
{
    /// <summary>
    /// Prompts the user to confirm trust for a workspace.
    /// This operation is typically interactive (console, GUI, or callback-based).
    /// </summary>
    /// <param name="workspacePath">The absolute path to the workspace root directory.</param>
    /// <param name="markerData">The parsed marker file data for the workspace.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user confirmed trust; otherwise, false.</returns>
    Task<bool> PromptUserTrustAsync(
        string workspacePath,
        IMarkerFileData markerData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a user's trust decision in the persistent trust registry.
    /// </summary>
    /// <param name="workspacePath">The absolute path to the workspace root directory.</param>
    /// <param name="trusted">True if the workspace is trusted; false if explicitly denied.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RecordTrustDecisionAsync(
        string workspacePath,
        bool trusted,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the persistent trust registry for an existing trust decision.
    /// </summary>
    /// <param name="workspacePath">The absolute path to the workspace root directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    /// <item><description>HasDecision: true if a trust decision exists; otherwise, false.</description></item>
    /// <item><description>IsTrusted: true if the workspace is trusted; false if denied or no decision exists.</description></item>
    /// </list>
    /// </returns>
    Task<(bool HasDecision, bool IsTrusted)> GetTrustDecisionAsync(
        string workspacePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes trust for a workspace, removing it from the trust registry.
    /// </summary>
    /// <param name="workspacePath">The absolute path to the workspace root directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RevokeTrustAsync(string workspacePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all trusted workspaces from the persistent trust registry.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of trusted workspace paths with metadata.</returns>
    Task<IReadOnlyList<ITrustedWorkspace>> ListTrustedWorkspacesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all trust decisions from the registry.
    /// This is a destructive operation that requires user confirmation.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearAllTrustAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a trusted workspace in the trust registry.
/// </summary>
public interface ITrustedWorkspace
{
    /// <summary>
    /// Gets the absolute path to the workspace root directory.
    /// </summary>
    string WorkspacePath { get; }

    /// <summary>
    /// Gets the timestamp when trust was granted.
    /// </summary>
    DateTimeOffset TrustedAt { get; }

    /// <summary>
    /// Gets the method by which trust was established.
    /// Examples: "user_confirmed", "signature_verified", "admin_override".
    /// </summary>
    string TrustMethod { get; }

    /// <summary>
    /// Gets optional metadata associated with the trust decision.
    /// May include user identity, machine name, or verification details.
    /// </summary>
    IReadOnlyDictionary<string, object?>? Metadata { get; }
}
