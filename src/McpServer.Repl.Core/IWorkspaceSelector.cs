namespace McpServer.Repl.Core;

/// <summary>
/// Provides workspace discovery, selection, and switching logic for the REPL session.
/// Manages the active workspace context and enforces selection rules.
/// </summary>
public interface IWorkspaceSelector
{
    /// <summary>
    /// Gets the currently active workspace path.
    /// Null if no workspace has been selected.
    /// </summary>
    string? ActiveWorkspace { get; }

    /// <summary>
    /// Discovers available workspaces by scanning for marker files.
    /// Searches common locations (current directory, user profile, recent workspaces, etc.).
    /// </summary>
    /// <param name="searchPaths">
    /// Optional list of additional paths to search.
    /// If null, uses default discovery heuristics.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of discovered workspace paths with marker file data.</returns>
    Task<IReadOnlyList<IWorkspaceCandidate>> DiscoverWorkspacesAsync(
        IEnumerable<string>? searchPaths = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects a workspace for the current REPL session.
    /// This triggers trust verification, auth state initialization, and context switching.
    /// </summary>
    /// <param name="workspacePath">The absolute path to the workspace root directory.</param>
    /// <param name="forceReselect">
    /// If true, allows re-selecting the same workspace to refresh auth state.
    /// If false, throws if the workspace is already active.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A workspace selection result with connection details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="workspacePath"/> is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the workspace or marker file does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the workspace is not trusted.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the workspace is already active and <paramref name="forceReselect"/> is false.</exception>
    Task<IWorkspaceSelectionResult> SelectWorkspaceAsync(
        string workspacePath,
        bool forceReselect = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Switches to a different workspace, disconnecting from the current one if active.
    /// This is a convenience method combining deselect and select operations.
    /// </summary>
    /// <param name="workspacePath">The absolute path to the new workspace root directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A workspace selection result for the new workspace.</returns>
    Task<IWorkspaceSelectionResult> SwitchWorkspaceAsync(
        string workspacePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deselects the currently active workspace and disconnects from the server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no workspace is currently active.</exception>
    Task DeselectWorkspaceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the marker file data for the currently active workspace.
    /// </summary>
    /// <returns>The marker file data for the active workspace.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no workspace is currently active.</exception>
    IMarkerFileData GetActiveMarkerData();

    /// <summary>
    /// Validates that a workspace path points to a valid workspace with a marker file.
    /// Does not perform trust verification or selection.
    /// </summary>
    /// <param name="workspacePath">The absolute path to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the path is a valid workspace; otherwise, false.</returns>
    Task<bool> ValidateWorkspacePathAsync(string workspacePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a discovered workspace candidate during workspace discovery.
/// </summary>
public interface IWorkspaceCandidate
{
    /// <summary>
    /// Gets the absolute path to the workspace root directory.
    /// </summary>
    string WorkspacePath { get; }

    /// <summary>
    /// Gets the parsed marker file data.
    /// </summary>
    IMarkerFileData MarkerData { get; }

    /// <summary>
    /// Gets a value indicating whether this workspace is currently trusted.
    /// </summary>
    bool IsTrusted { get; }

    /// <summary>
    /// Gets a value indicating whether this workspace is currently active in the REPL session.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Gets optional metadata about the workspace candidate.
    /// May include last accessed time, display name, or tags.
    /// </summary>
    IReadOnlyDictionary<string, object?>? Metadata { get; }
}

/// <summary>
/// Represents the result of a workspace selection operation.
/// </summary>
public interface IWorkspaceSelectionResult
{
    /// <summary>
    /// Gets the selected workspace path.
    /// </summary>
    string WorkspacePath { get; }

    /// <summary>
    /// Gets the marker file data for the selected workspace.
    /// </summary>
    IMarkerFileData MarkerData { get; }

    /// <summary>
    /// Gets the auth state initialized for this workspace.
    /// </summary>
    IAuthState AuthState { get; }

    /// <summary>
    /// Gets a value indicating whether the selection was successful.
    /// False if selection was attempted but failed due to trust or auth issues.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets an optional error message if selection failed.
    /// Null if <see cref="Success"/> is true.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    /// Gets the timestamp when the workspace was selected.
    /// </summary>
    DateTimeOffset SelectedAt { get; }
}
