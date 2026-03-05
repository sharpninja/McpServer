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
    string? PromptTemplate,
    string StatusPrompt,
    string ImplementPrompt,
    string PlanPrompt,
    DateTimeOffset DateTimeCreated,
    DateTimeOffset DateTimeModified,
    IReadOnlyList<string> BannedLicenses,
    IReadOnlyList<string> BannedCountriesOfOrigin,
    IReadOnlyList<string> BannedOrganizations,
    IReadOnlyList<string> BannedIndividuals);

/// <summary>Typed result of a workspace create/update/delete mutation.</summary>
public sealed record WorkspaceMutationOutcome(
    bool Success,
    string? Error,
    WorkspaceDetail? Item);

/// <summary>Typed process-state snapshot for a workspace host.</summary>
public sealed record WorkspaceProcessState(
    bool IsRunning,
    int? Pid,
    string? Uptime,
    int? Port,
    string? Error);

/// <summary>Typed result of reading or updating the shared global marker prompt.</summary>
public sealed record WorkspaceGlobalPromptState(
    string Template,
    bool IsDefault);

/// <summary>Typed health-probe result for a workspace host.</summary>
public sealed record WorkspaceHealthState(
    bool Success,
    int StatusCode,
    string? Url,
    string? Body,
    string? Error);

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

/// <summary>Command to create a workspace registration.</summary>
public sealed record CreateWorkspaceCommand : ICommand<WorkspaceMutationOutcome>
{
    /// <summary>Absolute workspace path.</summary>
    public required string WorkspacePath { get; init; }

    /// <summary>Optional human-readable name.</summary>
    public string? Name { get; init; }

    /// <summary>Optional TODO file path.</summary>
    public string? TodoPath { get; init; }

    /// <summary>Optional workspace data directory.</summary>
    public string? DataDirectory { get; init; }

    /// <summary>Optional tunnel provider.</summary>
    public string? TunnelProvider { get; init; }

    /// <summary>Optional Windows identity.</summary>
    public string? RunAs { get; init; }

    /// <summary>Whether the workspace should become primary.</summary>
    public bool IsPrimary { get; init; }

    /// <summary>Whether the workspace should be enabled.</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>Optional workspace prompt template.</summary>
    public string? PromptTemplate { get; init; }

    /// <summary>Optional status prompt override.</summary>
    public string? StatusPrompt { get; init; }

    /// <summary>Optional implement prompt override.</summary>
    public string? ImplementPrompt { get; init; }

    /// <summary>Optional plan prompt override.</summary>
    public string? PlanPrompt { get; init; }

    /// <summary>Optional banned-license list.</summary>
    public IReadOnlyList<string>? BannedLicenses { get; init; }

    /// <summary>Optional banned-country list.</summary>
    public IReadOnlyList<string>? BannedCountriesOfOrigin { get; init; }

    /// <summary>Optional banned-organization list.</summary>
    public IReadOnlyList<string>? BannedOrganizations { get; init; }

    /// <summary>Optional banned-individual list.</summary>
    public IReadOnlyList<string>? BannedIndividuals { get; init; }
}

/// <summary>Command to update an existing workspace registration.</summary>
public sealed record UpdateWorkspaceCommand : ICommand<WorkspaceMutationOutcome>
{
    /// <summary>Absolute workspace path used as the immutable workspace key.</summary>
    public required string WorkspacePath { get; init; }

    /// <summary>Updated human-readable name.</summary>
    public string? Name { get; init; }

    /// <summary>Updated TODO file path.</summary>
    public string? TodoPath { get; init; }

    /// <summary>Updated workspace data directory.</summary>
    public string? DataDirectory { get; init; }

    /// <summary>Updated tunnel provider.</summary>
    public string? TunnelProvider { get; init; }

    /// <summary>Updated Windows identity.</summary>
    public string? RunAs { get; init; }

    /// <summary>Updated primary flag.</summary>
    public bool? IsPrimary { get; init; }

    /// <summary>Updated enabled flag.</summary>
    public bool? IsEnabled { get; init; }

    /// <summary>Updated workspace prompt template (empty string resets).</summary>
    public string? PromptTemplate { get; init; }

    /// <summary>Updated status prompt override (empty string resets).</summary>
    public string? StatusPrompt { get; init; }

    /// <summary>Updated implement prompt override (empty string resets).</summary>
    public string? ImplementPrompt { get; init; }

    /// <summary>Updated plan prompt override (empty string resets).</summary>
    public string? PlanPrompt { get; init; }

    /// <summary>Updated banned-license list.</summary>
    public IReadOnlyList<string>? BannedLicenses { get; init; }

    /// <summary>Updated banned-country list.</summary>
    public IReadOnlyList<string>? BannedCountriesOfOrigin { get; init; }

    /// <summary>Updated banned-organization list.</summary>
    public IReadOnlyList<string>? BannedOrganizations { get; init; }

    /// <summary>Updated banned-individual list.</summary>
    public IReadOnlyList<string>? BannedIndividuals { get; init; }
}

/// <summary>Command to delete a workspace registration.</summary>
public sealed record DeleteWorkspaceCommand(string WorkspacePath) : ICommand<WorkspaceMutationOutcome>;

/// <summary>Query to read process state for a single workspace host.</summary>
public sealed record GetWorkspaceStatusQuery(string WorkspacePath) : IQuery<WorkspaceProcessState>;

/// <summary>Command to start the workspace host.</summary>
public sealed record StartWorkspaceCommand(string WorkspacePath) : ICommand<WorkspaceProcessState>;

/// <summary>Command to stop the workspace host.</summary>
public sealed record StopWorkspaceCommand(string WorkspacePath) : ICommand<WorkspaceProcessState>;

/// <summary>Query to probe the health endpoint for a workspace host.</summary>
public sealed record CheckWorkspaceHealthQuery(string WorkspacePath) : IQuery<WorkspaceHealthState>;

/// <summary>Query to load the shared global marker prompt template.</summary>
public sealed record GetWorkspaceGlobalPromptQuery : IQuery<WorkspaceGlobalPromptState>;

/// <summary>Command to update the shared global marker prompt template.</summary>
public sealed record UpdateWorkspaceGlobalPromptCommand(string? Template) : ICommand<WorkspaceGlobalPromptState>;

/// <summary>
/// Command to initialize a workspace for Director agent-management usage.
/// Hosts may implement this as a composite operation (for example, seeding definitions and writing init events).
/// </summary>
public sealed record InitWorkspaceCommand(string WorkspacePath) : ICommand<WorkspaceInitInfo>;

/// <summary>Result of a successful Director workspace initialization workflow.</summary>
/// <param name="WorkspacePath">Workspace path that was initialized.</param>
/// <param name="SeededDefinitions">Optional count of seeded definitions when available.</param>
public sealed record WorkspaceInitInfo(string WorkspacePath, int? SeededDefinitions);
