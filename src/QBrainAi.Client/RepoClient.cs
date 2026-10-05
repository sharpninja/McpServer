using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using QBrainAi.Client.Models;

namespace QBrainAi.Client;

/// <summary>
/// Client for repository file endpoints (<c>/qbrainai/repo</c>). Supports reading file content,
/// writing files, and listing directory entries in the workspace repository.
/// </summary>
/// <seealso cref="QBrainAiClient.Repo"/>
public sealed class RepoClient : McpClientBase
{
    /// <inheritdoc />
    public RepoClient(HttpClient http, QBrainAiClientOptions options)
        : base(http, options) { }

    internal RepoClient(HttpClient http, QBrainAiClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Read a file from the repository.</summary>
    public async Task<RepoFileReadResult> ReadFileAsync(string path, CancellationToken cancellationToken = default)
    {
        return await GetAsync<RepoFileReadResult>($"qbrainai/repo/file?path={Uri.EscapeDataString(path)}", cancellationToken);
    }

    /// <summary>Write a file to the repository.</summary>
    public async Task<RepoWriteResult> WriteFileAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        var request = new RepoWriteRequest { Path = path, Content = content };
        return await PostAsync<RepoWriteResult>("qbrainai/repo/file", request, cancellationToken);
    }

    /// <summary>FR-MCP-QBTOOLS-006: Apply a targeted string replacement to a repository file.</summary>
    /// <param name="path">File path relative to repo root.</param>
    /// <param name="oldString">Exact text to find.</param>
    /// <param name="newString">Replacement text.</param>
    /// <param name="replaceAll">When true, replaces every occurrence instead of requiring a unique match.</param>
    /// <param name="expectedOccurrences">Optional expected match-count guard.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The edit result.</returns>
    public async Task<RepoEditResult> EditFileAsync(
        string path,
        string oldString,
        string newString,
        bool replaceAll = false,
        int? expectedOccurrences = null,
        CancellationToken cancellationToken = default)
    {
        var request = new RepoEditRequest
        {
            Path = path,
            OldString = oldString,
            NewString = newString,
            ReplaceAll = replaceAll,
            ExpectedOccurrences = expectedOccurrences,
        };
        return await PostAsync<RepoEditResult>("qbrainai/repo/edit", request, cancellationToken);
    }

    /// <summary>List files and directories under a path.</summary>
    public async Task<RepoListResult> ListAsync(string? path = null, CancellationToken cancellationToken = default)
    {
        var qs = path is not null ? $"?path={Uri.EscapeDataString(path)}" : string.Empty;
        return await GetAsync<RepoListResult>($"qbrainai/repo/list{qs}", cancellationToken);
    }
}
