namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Service for managing workspace registrations, initialization, and lifecycle.
/// </summary>
public interface IWorkspaceService
{
    /// <summary>List all registered workspaces.</summary>
    Task<WorkspaceListResult> ListAsync(CancellationToken ct = default);

    /// <summary>Get a single workspace by its path.</summary>
    Task<WorkspaceDto?> GetAsync(string workspacePath, CancellationToken ct = default);

    /// <summary>Create (register) a new workspace.</summary>
    Task<WorkspaceMutationResult> CreateAsync(WorkspaceCreateRequest request, CancellationToken ct = default);

    /// <summary>Update an existing workspace by its path.</summary>
    Task<WorkspaceMutationResult> UpdateAsync(string workspacePath, WorkspaceUpdateRequest request, CancellationToken ct = default);

    /// <summary>Delete a workspace registration by its path.</summary>
    Task<WorkspaceMutationResult> DeleteAsync(string workspacePath, CancellationToken ct = default);

    /// <summary>Initialize data files in a workspace (scaffold dirs, todo.yaml, mcp.db).</summary>
    Task<WorkspaceInitResult> InitAsync(string workspacePath, CancellationToken ct = default);
}

/// <summary>Request to create a new workspace.</summary>
public sealed record WorkspaceCreateRequest
{
    /// <summary>Absolute path to the workspace root folder. Required.</summary>
    public required string WorkspacePath { get; init; }

    /// <summary>Human-readable workspace name. Default: last segment of WorkspacePath.</summary>
    public string? Name { get; init; }

    /// <summary>Relative path to todo file within workspace. Default: docs/todo.yaml.</summary>
    public string? TodoPath { get; init; }

    /// <summary>
    /// Override directory for <c>mcp.db</c> and related data files.
    /// Useful when <see cref="WorkspacePath"/> is a symlink to a non-Windows filesystem (e.g. WSL).
    /// Null = use <see cref="WorkspacePath"/>.
    /// </summary>
    public string? DataDirectory { get; init; }

    /// <summary>Tunnel provider key (ngrok, cloudflare, frp) or null = no tunnel.</summary>
    public string? TunnelProvider { get; init; }

    /// <summary>Identity for child process. Null = current Windows user.</summary>
    public string? RunAs { get; init; }

    /// <summary>Mark this workspace as the primary instance served by the host process. Default: false.</summary>
    public bool IsPrimary { get; init; }

    /// <summary>Whether the workspace is started during auto-start. Default: true.</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Optional markdown prompt template appended to the global marker prompt.
    /// Supports <c>{baseUrl}</c> placeholder.
    /// </summary>
    public string? PromptTemplate { get; init; }

    /// <summary>Override for the Copilot status prompt. Null = use built-in default.</summary>
    public string? StatusPrompt { get; init; }

    /// <summary>Override for the Copilot implement prompt. Null = use built-in default.</summary>
    public string? ImplementPrompt { get; init; }

    /// <summary>Override for the Copilot plan prompt. Null = use built-in default.</summary>
    public string? PlanPrompt { get; init; }

    /// <summary>SPDX license identifiers banned in this workspace (e.g. "GPL-3.0", "AGPL-3.0").</summary>
    public List<string>? BannedLicenses { get; init; }

    /// <summary>ISO 3166-1 alpha-2 country codes banned as dependency origin (e.g. "CN", "RU").</summary>
    public List<string>? BannedCountriesOfOrigin { get; init; }

    /// <summary>Organization/company names whose code and libraries are banned.</summary>
    public List<string>? BannedOrganizations { get; init; }

    /// <summary>Individual names/handles whose code and libraries are banned.</summary>
    public List<string>? BannedIndividuals { get; init; }
}

/// <summary>Request to update a workspace. Null fields are not changed.</summary>
public sealed record WorkspaceUpdateRequest
{
    /// <summary>Updated name (null = no change).</summary>
    public string? Name { get; init; }

    /// <summary>Updated todo path (null = no change).</summary>
    public string? TodoPath { get; init; }

    /// <summary>
    /// Override directory for <c>mcp.db</c> (null = no change, empty string = revert to WorkspacePath).
    /// </summary>
    public string? DataDirectory { get; init; }

    /// <summary>Updated tunnel provider (null = no change, empty string = disable tunnel).</summary>
    public string? TunnelProvider { get; init; }

    /// <summary>Updated RunAs identity (null = no change, empty string = default).</summary>
    public string? RunAs { get; init; }

    /// <summary>Updated primary flag (null = no change).</summary>
    public bool? IsPrimary { get; init; }

    /// <summary>Updated enabled flag (null = no change).</summary>
    public bool? IsEnabled { get; init; }

    /// <summary>Updated workspace prompt template (null = no change, empty string = remove).</summary>
    public string? PromptTemplate { get; init; }

    /// <summary>Updated status prompt (null = no change, empty string = revert to default).</summary>
    public string? StatusPrompt { get; init; }

    /// <summary>Updated implement prompt (null = no change, empty string = revert to default).</summary>
    public string? ImplementPrompt { get; init; }

    /// <summary>Updated plan prompt (null = no change, empty string = revert to default).</summary>
    public string? PlanPrompt { get; init; }

    /// <summary>Updated banned licenses (null = no change, empty list = clear all).</summary>
    public List<string>? BannedLicenses { get; init; }

    /// <summary>Updated banned countries of origin (null = no change, empty list = clear all).</summary>
    public List<string>? BannedCountriesOfOrigin { get; init; }

    /// <summary>Updated banned organizations (null = no change, empty list = clear all).</summary>
    public List<string>? BannedOrganizations { get; init; }

    /// <summary>Updated banned individuals (null = no change, empty list = clear all).</summary>
    public List<string>? BannedIndividuals { get; init; }
}

/// <summary>Read-only workspace view.</summary>
public sealed record WorkspaceDto
{
    /// <summary>Absolute path to workspace root folder.</summary>
    public required string WorkspacePath { get; init; }

    /// <summary>Human-readable workspace name.</summary>
    public required string Name { get; init; }

    /// <summary>Relative path to todo file.</summary>
    public required string TodoPath { get; init; }

    /// <summary>
    /// Override directory for <c>mcp.db</c> and related data files.
    /// Null = <see cref="WorkspacePath"/> is used as the data directory.
    /// </summary>
    public string? DataDirectory { get; init; }

    /// <summary>Tunnel provider key or null.</summary>
    public string? TunnelProvider { get; init; }

    /// <summary>True if this workspace is served by the primary host process (no child app).</summary>
    public bool IsPrimary { get; init; }

    /// <summary>Whether the workspace is started during auto-start.</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>When the workspace was registered.</summary>
    public DateTimeOffset DateTimeCreated { get; init; }

    /// <summary>When the workspace was last updated.</summary>
    public DateTimeOffset DateTimeModified { get; init; }

    /// <summary>Identity for child process.</summary>
    public string? RunAs { get; init; }

    /// <summary>
    /// Optional markdown prompt template appended to the global marker prompt for this workspace.
    /// Supports <c>{baseUrl}</c> placeholder.
    /// </summary>
    public string? PromptTemplate { get; init; }

    /// <summary>Effective Copilot status prompt (custom override or built-in default).</summary>
    public required string StatusPrompt { get; init; }

    /// <summary>Effective Copilot implement prompt (custom override or built-in default).</summary>
    public required string ImplementPrompt { get; init; }

    /// <summary>Effective Copilot plan prompt (custom override or built-in default).</summary>
    public required string PlanPrompt { get; init; }

    /// <summary>SPDX license identifiers banned in this workspace (e.g. "GPL-3.0", "AGPL-3.0").</summary>
    public List<string> BannedLicenses { get; init; } = [];

    /// <summary>ISO 3166-1 alpha-2 country codes banned as dependency origin (e.g. "CN", "RU").</summary>
    public List<string> BannedCountriesOfOrigin { get; init; } = [];

    /// <summary>Organization/company names whose code and libraries are banned.</summary>
    public List<string> BannedOrganizations { get; init; } = [];

    /// <summary>Individual names/handles whose code and libraries are banned.</summary>
    public List<string> BannedIndividuals { get; init; } = [];
}

/// <summary>Result of listing workspaces.</summary>
public sealed record WorkspaceListResult(IReadOnlyList<WorkspaceDto> Items, int TotalCount);

/// <summary>Result of a workspace mutation (create/update/delete).</summary>
public sealed record WorkspaceMutationResult(bool Success, string? Error = null, WorkspaceDto? Workspace = null);

/// <summary>Result of workspace initialization.</summary>
public sealed record WorkspaceInitResult(bool Success, string? Error = null, IReadOnlyList<string>? FilesCreated = null);

/// <summary>Result of reading the global marker prompt template.</summary>
public sealed record GlobalPromptResult(string Template, bool IsDefault);

/// <summary>Request to update the global marker prompt template.</summary>
public sealed record GlobalPromptUpdateRequest
{
    /// <summary>
    /// The new global prompt template. Supports <c>{baseUrl}</c> placeholder.
    /// Send null or empty to revert to the built-in default.
    /// </summary>
    public string? Template { get; init; }
}
