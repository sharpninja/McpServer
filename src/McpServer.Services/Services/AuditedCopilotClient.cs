using System.Globalization;
using System.Runtime.CompilerServices;
using McpServer.Common.Copilot;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Decorates <see cref="ICopilotClient"/> and records copilot invocation audit entries in session logs.
/// </summary>
public sealed class AuditedCopilotClient : ICopilotClient
{
    private readonly ICopilotClient _inner;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<IngestionOptions> _ingestionOptions;
    private readonly ILogger<AuditedCopilotClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditedCopilotClient"/> class.
    /// </summary>
    public AuditedCopilotClient(
        ICopilotClient inner,
        IServiceScopeFactory scopeFactory,
        IHttpContextAccessor httpContextAccessor,
        IOptions<IngestionOptions> ingestionOptions,
        ILogger<AuditedCopilotClient> logger)
    {
        _inner = inner;
        _scopeFactory = scopeFactory;
        _httpContextAccessor = httpContextAccessor;
        _ingestionOptions = ingestionOptions;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CopilotResult> InvokeAsync(
        string prompt,
        CopilotClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var audit = await BeginAuditAsync("invoke", prompt, options, cancellationToken).ConfigureAwait(false);
        try
        {
            var result = await _inner.InvokeAsync(prompt, options, cancellationToken).ConfigureAwait(false);
            var completed = result.State == CopilotResultState.Success;
            await CompleteAuditAsync(
                    audit,
                    prompt,
                    options,
                    completed ? "completed" : "failed",
                    $"state={result.State}; exitCode={result.ExitCode}; stderr={Truncate(result.Stderr, 400)}",
                    result.State.ToString(),
                    cancellationToken)
                .ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            await CompleteAuditAsync(
                    audit,
                    prompt,
                    options,
                    "failed",
                    ex.Message,
                    "exception",
                    CancellationToken.None)
                .ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<CopilotResult<T>> InvokeAsync<T>(
        string prompt,
        CopilotClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var audit = await BeginAuditAsync("invoke_typed", prompt, options, cancellationToken).ConfigureAwait(false);
        try
        {
            var result = await _inner.InvokeAsync<T>(prompt, options, cancellationToken).ConfigureAwait(false);
            var completed = result.State == CopilotResultState.Success;
            await CompleteAuditAsync(
                    audit,
                    prompt,
                    options,
                    completed ? "completed" : "failed",
                    $"state={result.State}; exitCode={result.ExitCode}; contentType={result.ContentType}; stderr={Truncate(result.Stderr, 400)}",
                    result.State.ToString(),
                    cancellationToken)
                .ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            await CompleteAuditAsync(
                    audit,
                    prompt,
                    options,
                    "failed",
                    ex.Message,
                    "exception",
                    CancellationToken.None)
                .ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerable<string> InvokeStreamingAsync(
        string prompt,
        CopilotClientOptions? options = null,
        CancellationToken cancellationToken = default)
        => InvokeStreamingWithAuditAsync(prompt, options, cancellationToken);

    /// <inheritdoc />
    public CopilotInteractiveSession CreateInteractiveSession(
        string initialPrompt,
        CopilotClientOptions? options = null)
    {
        var audit = BeginAuditAsync("create_interactive_session", initialPrompt, options, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        try
        {
            var session = _inner.CreateInteractiveSession(initialPrompt, options);
            CompleteAuditAsync(
                    audit,
                    initialPrompt,
                    options,
                    "completed",
                    "Interactive session created.",
                    "success",
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            return session;
        }
        catch (Exception ex)
        {
            CompleteAuditAsync(
                    audit,
                    initialPrompt,
                    options,
                    "failed",
                    ex.Message,
                    "exception",
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            throw;
        }
    }

    private async IAsyncEnumerable<string> InvokeStreamingWithAuditAsync(
        string prompt,
        CopilotClientOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var audit = await BeginAuditAsync("invoke_streaming", prompt, options, cancellationToken).ConfigureAwait(false);
        var lineCount = 0;
        var status = "completed";
        var resultState = "success";
        var response = string.Empty;

        var succeeded = false;
        try
        {
            await foreach (var line in _inner.InvokeStreamingAsync(prompt, options, cancellationToken))
            {
                lineCount++;
                yield return line;
            }

            response = $"Streaming invocation completed with {lineCount} lines.";
            succeeded = true;
        }
        finally
        {
            if (!succeeded)
            {
                status = "failed";
                resultState = "exception";
                response = $"Streaming invocation failed after {lineCount} lines.";
            }

            await CompleteAuditAsync(
                    audit,
                    prompt,
                    options,
                    status,
                    response,
                    resultState,
                    CancellationToken.None)
                .ConfigureAwait(false);
        }
    }

    private async Task<IReadOnlyList<AuditTarget>> BeginAuditAsync(
        string operation,
        string prompt,
        CopilotClientOptions? options,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var idTimestamp = now.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
        var workspaces = ResolveWorkspacePaths(options).ToList();
        var targets = new List<AuditTarget>(workspaces.Count);

        foreach (var workspacePath in workspaces)
        {
            var nonce = Guid.NewGuid().ToString("N")[..8];
            targets.Add(new AuditTarget(
                workspacePath,
                $"Copilot-{idTimestamp}-copilot-invocation-{nonce}",
                $"req-{idTimestamp}-{SanitizeSlug(operation)}-{nonce}",
                operation));
        }

        foreach (var target in targets)
        {
            await SubmitAuditAsync(
                    target,
                    operation,
                    prompt,
                    options,
                    status: "in_progress",
                    response: $"Copilot invocation started for operation '{operation}'.",
                    resultState: "in_progress",
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return targets;
    }

    private async Task CompleteAuditAsync(
        IReadOnlyList<AuditTarget> targets,
        string prompt,
        CopilotClientOptions? options,
        string status,
        string response,
        string resultState,
        CancellationToken cancellationToken)
    {
        foreach (var target in targets)
        {
            await SubmitAuditAsync(
                    target,
                    target.Operation,
                    prompt,
                    options,
                    status,
                    response,
                    resultState,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task SubmitAuditAsync(
        AuditTarget target,
        string operation,
        string prompt,
        CopilotClientOptions? options,
        string status,
        string response,
        string resultState,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sessionLogService = scope.ServiceProvider.GetRequiredService<ISessionLogService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<McpDbContext>();

            dbContext.OverrideWorkspaceId(target.WorkspacePath);

            var now = DateTimeOffset.UtcNow;
            var workspaceName = Path.GetFileName(target.WorkspacePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (string.IsNullOrWhiteSpace(workspaceName))
                workspaceName = "workspace";

            var dto = new UnifiedSessionLogDto
            {
                SourceType = "Copilot",
                SessionId = target.SessionId,
                Title = $"Copilot invocation audit - {workspaceName}",
                Model = options?.Model ?? "auto",
                Started = now.ToString("o", CultureInfo.InvariantCulture),
                LastUpdated = now.ToString("o", CultureInfo.InvariantCulture),
                Status = status,
                TurnCount = 1,
                Workspace = new WorkspaceInfoDto
                {
                    Project = workspaceName,
                    Repository = target.WorkspacePath,
                },
                Turns =
                [
                    new UnifiedRequestEntryDto
                    {
                        RequestId = target.RequestId,
                        Timestamp = now.ToString("o", CultureInfo.InvariantCulture),
                        QueryTitle = $"Copilot {operation}",
                        QueryText = Truncate(prompt, 8000),
                        Interpretation = $"workspace={target.WorkspacePath}; operation={operation}",
                        Response = Truncate(response, 4000),
                        Status = status,
                        Model = options?.Model,
                        Actions =
                        [
                            new UnifiedActionDto
                            {
                                Order = 1,
                                Type = "copilot_invocation",
                                Status = status,
                                FilePath = target.WorkspacePath,
                                Description = $"operation={operation}; result={resultState}",
                            }
                        ],
                        Tags = ["copilot", "audit", "copilot_invocation"],
                        ContextList = [target.WorkspacePath],
                    }
                ],
            };

            await sessionLogService.SubmitAsync(dto, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write copilot invocation audit for workspace {WorkspacePath}", target.WorkspacePath);
        }
    }

    private IReadOnlyList<string> ResolveWorkspacePaths(CopilotClientOptions? options)
    {
        var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(options?.WorkingDirectory))
            resolved.Add(Path.GetFullPath(options.WorkingDirectory));

        var workspaceContext = _httpContextAccessor.HttpContext?.RequestServices.GetService<WorkspaceContext>();
        if (!string.IsNullOrWhiteSpace(workspaceContext?.WorkspacePath))
            resolved.Add(Path.GetFullPath(workspaceContext.WorkspacePath));

        if (resolved.Count == 0)
        {
            var fallback = _ingestionOptions.Value.RepoRoot;
            if (!string.IsNullOrWhiteSpace(fallback))
                resolved.Add(Path.GetFullPath(fallback));
        }

        return resolved.Count == 0 ? [Path.GetFullPath(Environment.CurrentDirectory)] : resolved.ToList();
    }

    private static string Truncate(string? text, int maxChars)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        return text.Length <= maxChars ? text : text[..maxChars];
    }

    private static string SanitizeSlug(string raw)
    {
        var normalized = new string(raw
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray())
            .Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? "invoke" : normalized;
    }

    private sealed record AuditTarget(string WorkspacePath, string SessionId, string RequestId, string Operation);
}
