namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Service for registering tool definitions and searching them by keyword.
/// Tools can be global (available to all workspaces) or workspace-scoped.
/// Keyword queries return the union of global tools and workspace-specific tools.
/// </summary>
public interface IToolRegistryService
{
    /// <summary>Search tools by keyword. Returns global tools plus tools for the specified workspace.</summary>
    Task<ToolSearchResult> SearchAsync(string keyword, string? workspacePath = null, CancellationToken ct = default);

    /// <summary>Get a single tool by id.</summary>
    Task<ToolDto?> GetAsync(int id, CancellationToken ct = default);

    /// <summary>List all tools, optionally filtered to a workspace (includes global).</summary>
    Task<ToolSearchResult> ListAsync(string? workspacePath = null, CancellationToken ct = default);

    /// <summary>Register a new tool definition.</summary>
    Task<ToolMutationResult> CreateAsync(ToolCreateRequest request, CancellationToken ct = default);

    /// <summary>Update an existing tool definition.</summary>
    Task<ToolMutationResult> UpdateAsync(int id, ToolUpdateRequest request, CancellationToken ct = default);

    /// <summary>Delete a tool definition.</summary>
    Task<ToolMutationResult> DeleteAsync(int id, CancellationToken ct = default);
}

/// <summary>Request to register a new tool definition.</summary>
/// <param name="Name">Unique tool name.</param>
/// <param name="Description">Human-readable description.</param>
/// <param name="Tags">Keyword tags for discovery (e.g. <c>["screenshot", "capture"]</c>).</param>
/// <param name="ParameterSchema">Optional JSON schema for input parameters.</param>
/// <param name="CommandTemplate">Optional command template for invocation.</param>
/// <param name="WorkspacePath">Optional workspace path; <c>null</c> = global tool.</param>
public sealed record ToolCreateRequest(
    string Name,
    string Description,
    IReadOnlyList<string> Tags,
    string? ParameterSchema = null,
    string? CommandTemplate = null,
    string? WorkspacePath = null);

/// <summary>Request to update an existing tool definition. Null fields are left unchanged.</summary>
/// <param name="Name">New name, or null to keep.</param>
/// <param name="Description">New description, or null to keep.</param>
/// <param name="Tags">New tag set (replaces all), or null to keep.</param>
/// <param name="ParameterSchema">New schema, or null to keep.</param>
/// <param name="CommandTemplate">New command template, or null to keep.</param>
/// <param name="WorkspacePath">New workspace scope, or null to keep. Empty string clears to global.</param>
public sealed record ToolUpdateRequest(
    string? Name = null,
    string? Description = null,
    IReadOnlyList<string>? Tags = null,
    string? ParameterSchema = null,
    string? CommandTemplate = null,
    string? WorkspacePath = null);

/// <summary>Read model for a tool definition.</summary>
public sealed record ToolDto(
    int Id,
    string Name,
    string Description,
    IReadOnlyList<string> Tags,
    string? ParameterSchema,
    string? CommandTemplate,
    string? WorkspacePath,
    DateTimeOffset DateTimeCreated,
    DateTimeOffset DateTimeModified);

/// <summary>Result of a tool keyword search or list.</summary>
public sealed record ToolSearchResult(IReadOnlyList<ToolDto> Tools, int TotalCount);

/// <summary>Result of a create/update/delete mutation.</summary>
public sealed record ToolMutationResult(bool Success, string? Error = null, ToolDto? Tool = null);
