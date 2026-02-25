using McpServer.Cqrs;

namespace McpServer.UI.Core.Messages;

/// <summary>Query to list all workspaces.</summary>
public sealed record ListWorkspacesQuery : IQuery<ListWorkspacesResult>;

/// <summary>Result of listing workspaces.</summary>
public sealed record ListWorkspacesResult(IReadOnlyList<WorkspaceSummary> Items, int TotalCount);

/// <summary>Lightweight workspace summary for list views.</summary>
public sealed record WorkspaceSummary(
    string WorkspacePath,
    string Name,
    int WorkspacePort,
    bool IsPrimary,
    bool IsEnabled);

/// <summary>Query to get a single workspace by path.</summary>
public sealed record GetWorkspaceQuery(string WorkspacePath) : IQuery<WorkspaceDetail?>;

/// <summary>Detailed workspace view.</summary>
public sealed record WorkspaceDetail(
    string WorkspacePath,
    string Name,
    string TodoPath,
    string? DataDirectory,
    int WorkspacePort,
    string? TunnelProvider,
    bool IsPrimary,
    bool IsEnabled,
    string? RunAs,
    DateTimeOffset DateTimeCreated,
    DateTimeOffset DateTimeModified,
    IReadOnlyList<string> BannedLicenses,
    IReadOnlyList<string> BannedCountriesOfOrigin,
    IReadOnlyList<string> BannedOrganizations,
    IReadOnlyList<string> BannedIndividuals);

/// <summary>Command to update workspace policy (ban lists).</summary>
public sealed record UpdateWorkspacePolicyCommand : ICommand<bool>
{
    /// <summary>Workspace path to update.</summary>
    public required string WorkspacePath { get; init; }

    /// <summary>Updated banned licenses (null = no change).</summary>
    public List<string>? BannedLicenses { get; init; }

    /// <summary>Updated banned countries (null = no change).</summary>
    public List<string>? BannedCountriesOfOrigin { get; init; }

    /// <summary>Updated banned organizations (null = no change).</summary>
    public List<string>? BannedOrganizations { get; init; }

    /// <summary>Updated banned individuals (null = no change).</summary>
    public List<string>? BannedIndividuals { get; init; }
}
