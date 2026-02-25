using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.Services;

/// <summary>Host-provided API abstraction for repository file endpoints.</summary>
public interface IRepoApiClient
{
    /// <summary>Lists repo entries.</summary>
    Task<RepoListResultView> ListAsync(ListRepoEntriesQuery query, CancellationToken cancellationToken = default);

    /// <summary>Reads a repo file.</summary>
    Task<RepoFileDetail> ReadFileAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>Writes a repo file.</summary>
    Task<RepoWriteOutcome> WriteFileAsync(WriteRepoFileCommand command, CancellationToken cancellationToken = default);
}
