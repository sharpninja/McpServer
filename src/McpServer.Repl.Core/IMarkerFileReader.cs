// FR-MCP-REPL-004: Trust Bootstrap and Auth Rotation - Marker file trust verification
// TR-MCP-REPL-006: Trust Bootstrap and Token Validation - Bootstrap and auth interfaces
// TEST-MCP-REPL-004: Bootstrap invocation validates marker signature and health nonce

namespace McpServer.Repl.Core;

/// <summary>
/// Provides read access to workspace marker files (AGENTS-README-FIRST.yaml)
/// with trust verification and validation semantics.
/// </summary>
public interface IMarkerFileReader
{
    /// <summary>
    /// Reads and parses a marker file from the specified workspace path.
    /// Validates the file structure and required fields.
    /// </summary>
    /// <param name="workspacePath">The absolute path to the workspace root directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parsed marker file data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="workspacePath"/> is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the marker file does not exist.</exception>
    /// <exception cref="FormatException">Thrown when the marker file is malformed or missing required fields.</exception>
    Task<IMarkerFileData> ReadAsync(string workspacePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to read and parse a marker file without throwing exceptions on failure.
    /// </summary>
    /// <param name="workspacePath">The absolute path to the workspace root directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing Success (true if successful) and Data (the parsed marker file data if successful; otherwise, null).</returns>
    Task<(bool Success, IMarkerFileData? Data)> TryReadAsync(
        string workspacePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies that a workspace is trusted for REPL operations.
    /// Trust is established through:
    /// <list type="bullet">
    /// <item><description>Presence of a valid marker file</description></item>
    /// <item><description>Signature verification (if configured)</description></item>
    /// <item><description>User-initiated trust confirmation</description></item>
    /// <item><description>Persistent trust registry check</description></item>
    /// </list>
    /// </summary>
    /// <param name="workspacePath">The absolute path to the workspace root directory.</param>
    /// <param name="requireUserConfirmation">
    /// If true, prompts the user to confirm trust if not already established.
    /// If false, only checks existing trust state.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A trust verification result.</returns>
    Task<ITrustVerificationResult> VerifyTrustAsync(
        string workspacePath,
        bool requireUserConfirmation = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Watches a marker file for changes and invokes a callback when updates occur.
    /// This enables real-time auth token rotation and connection parameter updates.
    /// </summary>
    /// <param name="workspacePath">The absolute path to the workspace root directory.</param>
    /// <param name="onChange">The callback to invoke when the marker file changes.</param>
    /// <param name="cancellationToken">Cancellation token to stop watching.</param>
    /// <returns>A task that completes when watching is stopped.</returns>
    Task WatchAsync(
        string workspacePath,
        Func<IMarkerFileData, Task> onChange,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents parsed data from a workspace marker file.
/// </summary>
public interface IMarkerFileData
{
    /// <summary>
    /// Gets the workspace path this marker file belongs to.
    /// </summary>
    string WorkspacePath { get; }

    /// <summary>
    /// Gets the MCP server base URL.
    /// Example: "http://localhost:5177".
    /// </summary>
    string ServerUrl { get; }

    /// <summary>
    /// Gets the workspace-specific API authentication key.
    /// This key rotates on server restart.
    /// </summary>
    string ApiKey { get; }

    /// <summary>
    /// Gets the workspace identifier used in the X-Workspace-Path header.
    /// Typically an absolute or normalized path string.
    /// </summary>
    string WorkspaceId { get; }

    /// <summary>
    /// Gets optional agent instructions embedded in the marker file.
    /// </summary>
    string? AgentInstructions { get; }

    /// <summary>
    /// Gets optional metadata fields from the marker file.
    /// </summary>
    IReadOnlyDictionary<string, object?>? Metadata { get; }

    /// <summary>
    /// Gets the timestamp when the marker file was last modified.
    /// </summary>
    DateTimeOffset LastModified { get; }
}

/// <summary>
/// Represents the result of a workspace trust verification operation.
/// </summary>
public interface ITrustVerificationResult
{
    /// <summary>
    /// Gets a value indicating whether the workspace is trusted for REPL operations.
    /// </summary>
    bool IsTrusted { get; }

    /// <summary>
    /// Gets the trust establishment method.
    /// Examples: "user_confirmed", "registry_cached", "signature_verified", "not_trusted".
    /// </summary>
    string TrustMethod { get; }

    /// <summary>
    /// Gets optional additional details about the trust verification.
    /// May include timestamp, user identity, or verification metadata.
    /// </summary>
    IReadOnlyDictionary<string, object?>? Details { get; }

    /// <summary>
    /// Gets an optional message explaining why trust was denied.
    /// Null if <see cref="IsTrusted"/> is true.
    /// </summary>
    string? DenialReason { get; }
}
