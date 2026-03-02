using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.Director;

/// <summary>Director adapter for <see cref="IRepoApiClient"/> backed by <see cref="McpServer.Client.McpServerClient"/>.</summary>
internal sealed class RepoApiClientAdapter : IRepoApiClient
{
    private readonly DirectorMcpContext _context;

    public RepoApiClientAdapter(DirectorMcpContext context)
    {
        _context = context;
    }

    public async Task<RepoListResultView> ListAsync(ListRepoEntriesQuery query, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Repo.ListAsync(query.Path, cancellationToken).ConfigureAwait(false);
        return new RepoListResultView(
            result.Path,
            result.Entries.Select(e => new RepoEntrySummary(e.Name, e.IsDirectory)).ToList());
    }

    public async Task<RepoFileDetail> ReadFileAsync(string path, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Repo.ReadFileAsync(path, cancellationToken).ConfigureAwait(false);
        return new RepoFileDetail(result.Path, result.Content, result.Exists);
    }

    public async Task<RepoWriteOutcome> WriteFileAsync(WriteRepoFileCommand command, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Repo.WriteFileAsync(command.Path, command.Content, cancellationToken).ConfigureAwait(false);
        return new RepoWriteOutcome(result.Path, result.Written);
    }
}
