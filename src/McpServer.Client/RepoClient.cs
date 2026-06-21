using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for repository file endpoints (<c>/mcpserver/repo</c>). Supports reading file content,
/// writing files, and listing directory entries in the workspace repository.
/// </summary>
/// <seealso cref="McpServerClient.Repo"/>
public sealed class RepoClient : McpClientBase
{
    /// <inheritdoc />
    public RepoClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal RepoClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Read a file from the repository.</summary>
    public async Task<RepoFileReadResult> ReadFileAsync(string path, CancellationToken cancellationToken = default)
    {
        return await GetAsync<RepoFileReadResult>($"mcpserver/repo/file?path={Uri.EscapeDataString(path)}", cancellationToken);
    }

    /// <summary>Write a file to the repository.</summary>
    public async Task<RepoWriteResult> WriteFileAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        var request = new RepoWriteRequest { Path = path, Content = content };
        return await PostAsync<RepoWriteResult>("mcpserver/repo/file", request, cancellationToken);
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
        return await PostAsync<RepoEditResult>("mcpserver/repo/edit", request, cancellationToken);
    }

    /// <summary>List files and directories under a path.</summary>
    public async Task<RepoListResult> ListAsync(string? path = null, CancellationToken cancellationToken = default)
    {
        var qs = path is not null ? $"?path={Uri.EscapeDataString(path)}" : string.Empty;
        return await GetAsync<RepoListResult>($"mcpserver/repo/list{qs}", cancellationToken);
    }
}
