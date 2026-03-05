namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Applies natural-language workspace policy directives to one or more workspaces.
/// </summary>
public interface IWorkspacePolicyService
{
    /// <summary>
    /// Parses and applies a policy directive.
    /// </summary>
    /// <param name="request">Directive request payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Structured parse/apply result.</returns>
    Task<WorkspacePolicyApplyResult> ApplyAsync(WorkspacePolicyApplyRequest request, CancellationToken ct = default);
}

/// <summary>
/// Parses natural-language directives into a structured policy intent.
/// </summary>
public interface IWorkspacePolicyDirectiveParser
{
    /// <summary>
    /// Parses a directive string into a structured policy intent.
    /// </summary>
    /// <param name="directive">Natural-language policy directive.</param>
    /// <param name="workspacePathHint">Optional workspace-path hint for scope resolution.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Structured parse result.</returns>
    Task<WorkspacePolicyParseResult> ParseAsync(string directive, string? workspacePathHint, CancellationToken ct = default);
}

/// <summary>
/// Request payload for applying a natural-language policy directive.
/// </summary>
public sealed record WorkspacePolicyApplyRequest
{
    /// <summary>
    /// Natural-language directive text (for example, "Ban GPL-3.0 in this workspace").
    /// </summary>
    public required string Directive { get; init; }

    /// <summary>
    /// Optional workspace path hint used for "current workspace" resolution.
    /// </summary>
    public string? WorkspacePath { get; init; }
}

/// <summary>
/// Structured directive returned by the parser.
/// </summary>
public sealed record WorkspacePolicyDirective
{
    /// <summary>
    /// Mutation action: <c>add</c>, <c>remove</c>, or <c>clear</c>.
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Target category: <c>license</c>, <c>country_of_origin</c>, <c>organization</c>, or <c>individual</c>.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Values to mutate (empty for <c>clear</c>).
    /// </summary>
    public required IReadOnlyList<string> Values { get; init; }

    /// <summary>
    /// Scope: <c>current</c>, <c>workspace</c>, or <c>all</c>.
    /// </summary>
    public required string Scope { get; init; }

    /// <summary>
    /// Explicit workspace path when <see cref="Scope"/> is <c>workspace</c>.
    /// </summary>
    public string? ScopeWorkspacePath { get; init; }

    /// <summary>
    /// Parser label used to produce this directive (for example, <c>copilot</c> or <c>fallback</c>).
    /// </summary>
    public string? Parser { get; init; }
}

/// <summary>
/// Parser result for a policy directive.
/// </summary>
public sealed record WorkspacePolicyParseResult
{
    /// <summary>
    /// True when parsing succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Parse error message when <see cref="Success"/> is false.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Structured directive when <see cref="Success"/> is true.
    /// </summary>
    public WorkspacePolicyDirective? Directive { get; init; }
}

/// <summary>
/// Per-workspace mutation outcome.
/// </summary>
public sealed record WorkspacePolicyMutationResult
{
    /// <summary>
    /// Workspace root path.
    /// </summary>
    public required string WorkspacePath { get; init; }

    /// <summary>
    /// Workspace name.
    /// </summary>
    public required string WorkspaceName { get; init; }

    /// <summary>
    /// True when the mutation succeeded for this workspace.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error when <see cref="Success"/> is false.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Pre-mutation category values.
    /// </summary>
    public required IReadOnlyList<string> BeforeValues { get; init; }

    /// <summary>
    /// Post-mutation category values.
    /// </summary>
    public required IReadOnlyList<string> AfterValues { get; init; }
}

/// <summary>
/// Aggregate result for a policy-apply request.
/// </summary>
public sealed record WorkspacePolicyApplyResult
{
    /// <summary>
    /// True when parsing succeeded and every targeted workspace mutation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error summary when <see cref="Success"/> is false.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Structured directive parsed from the request.
    /// </summary>
    public WorkspacePolicyDirective? ParsedDirective { get; init; }

    /// <summary>
    /// Per-workspace mutation outcomes.
    /// </summary>
    public IReadOnlyList<WorkspacePolicyMutationResult> WorkspaceResults { get; init; } = [];
}
