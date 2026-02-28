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
        return await GetAsync<RepoFileReadResult>($"mcpserver/repo/file?path={Uri.EscapeDataString(path)}", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Write a file to the repository.</summary>
    public async Task<RepoWriteResult> WriteFileAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        var request = new RepoWriteRequest { Path = path, Content = content };
        return await PostAsync<RepoWriteResult>("mcpserver/repo/file", request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>List files and directories under a path.</summary>
    public async Task<RepoListResult> ListAsync(string? path = null, CancellationToken cancellationToken = default)
    {
        var qs = path is not null ? $"?path={Uri.EscapeDataString(path)}" : string.Empty;
        return await GetAsync<RepoListResult>($"mcpserver/repo/list{qs}", cancellationToken).ConfigureAwait(false);
    }
}
