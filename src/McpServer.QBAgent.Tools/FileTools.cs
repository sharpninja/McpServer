using McpServer.Client;
using McpServer.Client.Models;

namespace McpServer.QBAgent.Tools;

/// <summary>
/// FR-MCP-QBTOOLS-001 / TR-MCP-QBTOOLS-000: Agent-side file tools (read/write/list/edit). These are transport
/// adapters only: every operation is delegated to the MCP Server through <see cref="McpServerClient"/>, so the
/// server-side <c>RepoFileService</c> remains the single gate for path-traversal safety, allowlist enforcement,
/// and transactional rollback. The tool classes carry no filesystem logic of their own.
/// </summary>
public sealed class FileTools
{
    private readonly McpServerClient _client;

    /// <summary>Initializes a new instance of the <see cref="FileTools"/> class.</summary>
    /// <param name="client">The MCP transport client whose <see cref="McpServerClient.Repo"/> surface is used.</param>
    public FileTools(McpServerClient client)
        => _client = client ?? throw new ArgumentNullException(nameof(client));

    /// <summary>Reads a repository file by its workspace-relative path.</summary>
    /// <param name="path">The workspace-relative file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file read result from the server.</returns>
    public Task<RepoFileReadResult> ReadFileAsync(string path, CancellationToken cancellationToken = default)
        => _client.Repo.ReadFileAsync(path, cancellationToken);

    /// <summary>Writes a repository file by its workspace-relative path, creating or overwriting it.</summary>
    /// <param name="path">The workspace-relative file path.</param>
    /// <param name="content">The full file content to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The write result from the server.</returns>
    public Task<RepoWriteResult> WriteFileAsync(string path, string content, CancellationToken cancellationToken = default)
        => _client.Repo.WriteFileAsync(path, content, cancellationToken);

    /// <summary>Lists repository files and directories under an optional workspace-relative path.</summary>
    /// <param name="path">The workspace-relative directory path, or null for the workspace root.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The directory listing from the server.</returns>
    public Task<RepoListResult> ListFilesAsync(string? path = null, CancellationToken cancellationToken = default)
        => _client.Repo.ListAsync(path, cancellationToken);

    /// <summary>
    /// FR-MCP-QBTOOLS-006: Applies a targeted string replacement to a repository file by routing through the
    /// server <c>RepoFileService.EditAsync</c> (one core per capability), so path-safety, audit, and transactional
    /// rollback are server-owned.
    /// </summary>
    /// <param name="path">The workspace-relative file path.</param>
    /// <param name="oldString">The exact text to find.</param>
    /// <param name="newString">The replacement text (must differ from <paramref name="oldString"/>).</param>
    /// <param name="replaceAll">When true, replaces every occurrence instead of requiring a unique match.</param>
    /// <param name="expectedOccurrences">Optional expected match count guard.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The edit result.</returns>
    public async Task<FileEditResult> EditFileAsync(
        string path,
        string oldString,
        string newString,
        bool replaceAll = false,
        int? expectedOccurrences = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _client.Repo.EditFileAsync(path, oldString, newString, replaceAll, expectedOccurrences, cancellationToken)
            .ConfigureAwait(false);
        return new FileEditResult(result.Written, result.Replacements, result.Error);
    }
}
