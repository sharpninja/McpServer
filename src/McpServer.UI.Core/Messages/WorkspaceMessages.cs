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

/// <summary>
/// Command to initialize a workspace for Director agent-management usage.
/// Hosts may implement this as a composite operation (for example, seeding definitions and writing init events).
/// </summary>
public sealed record InitWorkspaceCommand(string WorkspacePath) : ICommand<WorkspaceInitInfo>;

/// <summary>Result of a successful Director workspace initialization workflow.</summary>
/// <param name="WorkspacePath">Workspace path that was initialized.</param>
/// <param name="SeededDefinitions">Optional count of seeded definitions when available.</param>
public sealed record WorkspaceInitInfo(string WorkspacePath, int? SeededDefinitions);

/// <summary>Command to create/register a workspace.</summary>
public sealed record CreateWorkspaceCommand : ICommand<WorkspaceMutationOutcome>
{
    /// <summary>Absolute workspace path.</summary>
    public required string WorkspacePath { get; init; }

    /// <summary>Workspace display name.</summary>
    public string? Name { get; init; }

    /// <summary>Todo file path.</summary>
    public string? TodoPath { get; init; }

    /// <summary>Optional data directory override.</summary>
    public string? DataDirectory { get; init; }

    /// <summary>Optional tunnel provider.</summary>
    public string? TunnelProvider { get; init; }

    /// <summary>Optional run-as account.</summary>
    public string? RunAs { get; init; }

    /// <summary>Whether workspace is primary.</summary>
    public bool IsPrimary { get; init; }

    /// <summary>Whether workspace is enabled.</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>Workspace-level prompt template.</summary>
    public string? PromptTemplate { get; init; }

    /// <summary>Status prompt override.</summary>
    public string? StatusPrompt { get; init; }

    /// <summary>Implement prompt override.</summary>
    public string? ImplementPrompt { get; init; }

    /// <summary>Plan prompt override.</summary>
    public string? PlanPrompt { get; init; }

    /// <summary>Initial banned licenses.</summary>
    public List<string>? BannedLicenses { get; init; }

    /// <summary>Initial banned countries.</summary>
    public List<string>? BannedCountriesOfOrigin { get; init; }

    /// <summary>Initial banned organizations.</summary>
    public List<string>? BannedOrganizations { get; init; }

    /// <summary>Initial banned individuals.</summary>
    public List<string>? BannedIndividuals { get; init; }
}

/// <summary>Command to update a workspace.</summary>
public sealed record UpdateWorkspaceCommand : ICommand<WorkspaceMutationOutcome>
{
    /// <summary>Absolute workspace path.</summary>
    public required string WorkspacePath { get; init; }

    /// <summary>Updated workspace display name.</summary>
    public string? Name { get; init; }

    /// <summary>Updated todo path.</summary>
    public string? TodoPath { get; init; }

    /// <summary>Updated data directory.</summary>
    public string? DataDirectory { get; init; }

    /// <summary>Updated tunnel provider.</summary>
    public string? TunnelProvider { get; init; }

    /// <summary>Updated run-as account.</summary>
    public string? RunAs { get; init; }

    /// <summary>Updated primary flag.</summary>
    public bool? IsPrimary { get; init; }

    /// <summary>Updated enabled flag.</summary>
    public bool? IsEnabled { get; init; }

    /// <summary>Updated workspace prompt template.</summary>
    public string? PromptTemplate { get; init; }

    /// <summary>Updated status prompt.</summary>
    public string? StatusPrompt { get; init; }

    /// <summary>Updated implement prompt.</summary>
    public string? ImplementPrompt { get; init; }

    /// <summary>Updated plan prompt.</summary>
    public string? PlanPrompt { get; init; }

    /// <summary>Updated banned licenses.</summary>
    public List<string>? BannedLicenses { get; init; }

    /// <summary>Updated banned countries.</summary>
    public List<string>? BannedCountriesOfOrigin { get; init; }

    /// <summary>Updated banned organizations.</summary>
    public List<string>? BannedOrganizations { get; init; }

    /// <summary>Updated banned individuals.</summary>
    public List<string>? BannedIndividuals { get; init; }
}

/// <summary>Command to delete a workspace.</summary>
public sealed record DeleteWorkspaceCommand(string WorkspacePath) : ICommand<WorkspaceMutationOutcome>;

/// <summary>Command to start a workspace host.</summary>
public sealed record StartWorkspaceCommand(string WorkspacePath) : ICommand<WorkspaceRuntimeStatus>;

/// <summary>Command to stop a workspace host.</summary>
public sealed record StopWorkspaceCommand(string WorkspacePath) : ICommand<WorkspaceRuntimeStatus>;

/// <summary>Query to retrieve workspace host status.</summary>
public sealed record GetWorkspaceStatusQuery(string WorkspacePath) : IQuery<WorkspaceRuntimeStatus>;

/// <summary>Query to retrieve the global marker prompt template.</summary>
public sealed record GetGlobalPromptQuery : IQuery<GlobalPromptInfo>;

/// <summary>Command to update the global marker prompt template.</summary>
public sealed record UpdateGlobalPromptCommand(string? Template) : ICommand<GlobalPromptInfo>;

/// <summary>Workspace mutation result.</summary>
public sealed record WorkspaceMutationOutcome(bool Success, string? Error, WorkspaceDetail? Workspace);

/// <summary>Workspace host runtime status.</summary>
public sealed record WorkspaceRuntimeStatus(bool IsRunning, int? Pid, string? Uptime, int? Port, string? Error);

/// <summary>Global prompt template payload.</summary>
public sealed record GlobalPromptInfo(string Template, bool IsDefault);
