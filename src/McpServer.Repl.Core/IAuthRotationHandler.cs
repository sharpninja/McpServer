namespace McpServer.Repl.Core;

/// <summary>
/// Handles mutable authentication state and connection parameter updates.
/// Responds to marker-file changes, server restarts, and auth token rotation events.
/// </summary>
public interface IAuthRotationHandler
{
    /// <summary>
    /// Gets the current authentication state.
    /// </summary>
    IAuthState CurrentAuthState { get; }

    /// <summary>
    /// Updates the authentication state when a marker file changes.
    /// This is typically triggered by a file watcher or polling mechanism.
    /// </summary>
    /// <param name="newMarkerData">The updated marker file data with new auth credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous update operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="newMarkerData"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the auth state cannot be updated.</exception>
    Task UpdateAuthStateAsync(IMarkerFileData newMarkerData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a callback to be invoked when authentication state changes.
    /// Multiple callbacks can be registered.
    /// </summary>
    /// <param name="onAuthChanged">The callback to invoke with the new auth state.</param>
    void RegisterAuthChangeCallback(Func<IAuthState, Task> onAuthChanged);

    /// <summary>
    /// Unregisters a previously registered auth change callback.
    /// </summary>
    /// <param name="onAuthChanged">The callback to remove.</param>
    void UnregisterAuthChangeCallback(Func<IAuthState, Task> onAuthChanged);

    /// <summary>
    /// Forces an immediate re-read and update of auth state from the marker file.
    /// This is useful when the application suspects the auth token has rotated.
    /// </summary>
    /// <param name="workspacePath">The workspace path to refresh auth state for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated auth state.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the marker file no longer exists.</exception>
    /// <exception cref="FormatException">Thrown when the marker file is malformed.</exception>
    Task<IAuthState> RefreshAuthStateAsync(string workspacePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the current auth state is still valid against the server.
    /// Makes a lightweight API call to verify the token has not expired or been revoked.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the current auth state is valid; otherwise, false.</returns>
    Task<bool> ValidateAuthStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the current auth state, forcing re-authentication on the next operation.
    /// </summary>
    void ClearAuthState();
}

/// <summary>
/// Represents the current authentication state for a workspace connection.
/// </summary>
public interface IAuthState
{
    /// <summary>
    /// Gets the workspace path this auth state belongs to.
    /// </summary>
    string WorkspacePath { get; }

    /// <summary>
    /// Gets the MCP server base URL.
    /// </summary>
    string ServerUrl { get; }

    /// <summary>
    /// Gets the current API key.
    /// </summary>
    string ApiKey { get; }

    /// <summary>
    /// Gets the workspace identifier used in the X-Workspace-Path header.
    /// </summary>
    string WorkspaceId { get; }

    /// <summary>
    /// Gets a value indicating whether this auth state is currently valid.
    /// False if the token has expired, been revoked, or the server is unreachable.
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    /// Gets the timestamp when this auth state was last updated.
    /// </summary>
    DateTimeOffset LastUpdated { get; }

    /// <summary>
    /// Gets the timestamp when this auth state was last validated against the server.
    /// Null if never validated.
    /// </summary>
    DateTimeOffset? LastValidated { get; }

    /// <summary>
    /// Gets optional metadata associated with this auth state.
    /// </summary>
    IReadOnlyDictionary<string, object?>? Metadata { get; }
}
