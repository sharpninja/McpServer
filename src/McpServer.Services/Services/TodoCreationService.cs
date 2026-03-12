using System.Text;
using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TODO-003, TR-MCP-GH-005: Orchestrates TODO creation flows that may require
/// immediate GitHub issue creation before the local TODO is persisted.
/// </summary>
public sealed class TodoCreationService
{
    /// <summary>
    /// Special create-time TODO identifier that instructs the server to create a GitHub issue first,
    /// then persist the TODO with the canonical <c>ISSUE-{number}</c> identifier returned by GitHub.
    /// </summary>
    public const string NewGitHubIssueTodoId = "ISSUE-NEW";

    private const string IssueIdPrefix = "ISSUE-";

    private readonly WorkspaceServiceAccessor _workspaceAccessor;
    private readonly IGitHubCliService _gitHubCliService;
    private readonly ILogger<TodoCreationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TodoCreationService"/> class.
    /// </summary>
    /// <param name="workspaceAccessor">Workspace-aware accessor for the active TODO service.</param>
    /// <param name="gitHubCliService">GitHub CLI wrapper used to create issue-backed TODO items.</param>
    /// <param name="logger">Logger for create-flow diagnostics.</param>
    public TodoCreationService(
        WorkspaceServiceAccessor workspaceAccessor,
        IGitHubCliService gitHubCliService,
        ILogger<TodoCreationService> logger)
    {
        _workspaceAccessor = workspaceAccessor ?? throw new ArgumentNullException(nameof(workspaceAccessor));
        _gitHubCliService = gitHubCliService ?? throw new ArgumentNullException(nameof(gitHubCliService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a TODO item in the active workspace. When the request id is <c>ISSUE-NEW</c>,
    /// the method first creates a GitHub issue, rewrites the TODO id to the canonical
    /// <c>ISSUE-{number}</c> form, and then persists the local TODO item.
    /// </summary>
    /// <param name="request">The TODO create request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The mutation result returned by the underlying TODO store.</returns>
    public async Task<TodoMutationResult> CreateAsync(TodoCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsNewGitHubIssueRequestId(request.Id))
            return await _workspaceAccessor.GetTodoService().CreateAsync(request, cancellationToken).ConfigureAwait(false);

        var issueBody = BuildIssueBody(request);
        var issueResult = await _gitHubCliService.CreateIssueAsync(request.Title, issueBody, cancellationToken).ConfigureAwait(false);
        if (!issueResult.Success)
            return new TodoMutationResult(false, issueResult.Error ?? "GitHub issue creation failed.");

        if (!issueResult.Number.HasValue)
            return new TodoMutationResult(false, "GitHub issue creation succeeded but did not return a canonical issue number.");

        var canonicalId = $"{IssueIdPrefix}{issueResult.Number.Value}";
        var rewrittenRequest = request with
        {
            Id = canonicalId,
            Note = BuildIssueNote(request.Note, issueResult.Url)
        };

        var result = await _workspaceAccessor.GetTodoService().CreateAsync(rewrittenRequest, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
        {
            var failure = result.Error ?? $"Local TODO persistence failed after creating GitHub issue #{issueResult.Number.Value}.";
            if (!string.IsNullOrWhiteSpace(issueResult.Url))
                failure = $"{failure} GitHub issue: {issueResult.Url}";

            _logger.LogWarning(
                "ISSUE-NEW create flow created GitHub issue {IssueNumber} but failed to persist TODO {TodoId}: {Failure}",
                issueResult.Number.Value,
                canonicalId,
                failure);

            return new TodoMutationResult(false, failure);
        }

        _logger.LogInformation(
            "Created GitHub-backed TODO {TodoId} from ISSUE-NEW request in workspace {WorkspacePath}.",
            canonicalId,
            _workspaceAccessor.GetWorkspacePath());

        return result;
    }

    /// <summary>
    /// Returns <see langword="true"/> when the supplied TODO id requests GitHub-backed creation.
    /// </summary>
    /// <param name="id">The requested TODO id.</param>
    /// <returns><see langword="true"/> when the id is <c>ISSUE-NEW</c>; otherwise <see langword="false"/>.</returns>
    public static bool IsNewGitHubIssueRequestId(string? id)
        => string.Equals(id, NewGitHubIssueTodoId, StringComparison.OrdinalIgnoreCase);

    private static string BuildIssueNote(string? existingNote, string? issueUrl)
    {
        var frontmatter = new IssueNoteFrontmatter
        {
            Status = "OPEN",
            GitHubUrl = issueUrl
        }.Serialize();

        if (string.IsNullOrWhiteSpace(existingNote))
            return frontmatter;

        if (string.IsNullOrWhiteSpace(frontmatter))
            return existingNote.Trim();

        return $"{frontmatter}{Environment.NewLine}{Environment.NewLine}{existingNote.Trim()}";
    }

    private static string BuildIssueBody(TodoCreateRequest request)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Created from an MCP TODO request.");
        builder.AppendLine();
        builder.AppendLine("## Metadata");
        builder.AppendLine($"- Requested section: {request.Section}");
        builder.AppendLine($"- Requested priority: {request.Priority}");
        if (!string.IsNullOrWhiteSpace(request.Estimate))
            builder.AppendLine($"- Estimate: {request.Estimate.Trim()}");

        AppendTextListSection(builder, "Description", request.Description);
        AppendTextListSection(builder, "Technical Details", request.TechnicalDetails);
        AppendImplementationTaskSection(builder, request.ImplementationTasks);
        AppendParagraphSection(builder, "Remaining", request.Remaining);
        AppendParagraphSection(builder, "Note", request.Note);
        AppendIdListSection(builder, "Functional Requirements", request.FunctionalRequirements);
        AppendIdListSection(builder, "Technical Requirements", request.TechnicalRequirements);

        return builder.ToString().TrimEnd();
    }

    private static void AppendTextListSection(StringBuilder builder, string heading, IReadOnlyList<string>? values)
    {
        if (values is not { Count: > 0 })
            return;

        builder.AppendLine();
        builder.AppendLine($"## {heading}");
        foreach (var value in values.Where(static value => !string.IsNullOrWhiteSpace(value)))
            builder.AppendLine($"- {value.Trim()}");
    }

    private static void AppendImplementationTaskSection(StringBuilder builder, IReadOnlyList<TodoFlatTask>? tasks)
    {
        if (tasks is not { Count: > 0 })
            return;

        builder.AppendLine();
        builder.AppendLine("## Implementation Tasks");
        foreach (var task in tasks.Where(static task => !string.IsNullOrWhiteSpace(task.Task)))
        {
            var checkbox = task.Done ? "x" : " ";
            builder.AppendLine($"- [{checkbox}] {task.Task.Trim()}");
        }
    }

    private static void AppendParagraphSection(StringBuilder builder, string heading, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        builder.AppendLine();
        builder.AppendLine($"## {heading}");
        builder.AppendLine(value.Trim());
    }

    private static void AppendIdListSection(StringBuilder builder, string heading, IReadOnlyList<string>? values)
    {
        if (values is not { Count: > 0 })
            return;

        builder.AppendLine();
        builder.AppendLine($"## {heading}");
        foreach (var value in values.Where(static value => !string.IsNullOrWhiteSpace(value)))
            builder.AppendLine($"- {value.Trim()}");
    }
}
