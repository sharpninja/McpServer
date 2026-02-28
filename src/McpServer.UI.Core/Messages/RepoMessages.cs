using McpServer.Cqrs;

namespace McpServer.UI.Core.Messages;

/// <summary>Query for <c>/mcpserver/repo/list</c>.</summary>
public sealed record ListRepoEntriesQuery : IQuery<RepoListResultView>
{
    /// <summary>Optional relative path to list.</summary>
    public string? Path { get; init; }
}

/// <summary>Query for <c>/mcpserver/repo/file</c> (read).</summary>
public sealed record GetRepoFileQuery(string Path) : IQuery<RepoFileDetail>;

/// <summary>Command for <c>POST /mcpserver/repo/file</c> (write).</summary>
public sealed record WriteRepoFileCommand(string Path, string Content) : ICommand<RepoWriteOutcome>;

/// <summary>Repo list result model used by UI.Core.</summary>
public sealed record RepoListResultView(string? Path, IReadOnlyList<RepoEntrySummary> Entries);

/// <summary>List-friendly repo entry.</summary>
public sealed record RepoEntrySummary(string Name, bool IsDirectory);

/// <summary>Detailed repo file read result.</summary>
public sealed record RepoFileDetail(string Path, string? Content, bool Exists);

/// <summary>Repo file write result.</summary>
public sealed record RepoWriteOutcome(string? Path, bool Written);
