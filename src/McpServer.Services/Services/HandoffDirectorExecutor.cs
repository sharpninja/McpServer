using McpServer.Cqrs.Mvvm;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-HANDOFF-SURFACE-001: Director exec adapter that delegates to
/// <see cref="IHandoffIngestionService"/>.
/// </summary>
public sealed class HandoffDirectorExecutor : IHandoffDirectorExecutor
{
    private readonly IHandoffIngestionService _handoffIngestionService;

    /// <summary>Creates a Director executor over the shared handoff service.</summary>
    public HandoffDirectorExecutor(IHandoffIngestionService handoffIngestionService)
    {
        _handoffIngestionService = handoffIngestionService ?? throw new ArgumentNullException(nameof(handoffIngestionService));
    }

    /// <inheritdoc />
    public Task<object?> IngestAsync(
        string? sourceKind,
        string? path,
        string? content,
        string? artifactId,
        string? mode,
        bool force,
        string? agentName,
        string? promptTemplateId,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<HandoffSourceKind>(sourceKind, ignoreCase: true, out var kind))
        {
            return Task.FromResult<object?>(new HandoffIngestionResult
            {
                Success = false,
                Error = "sourceKind must be Path, Content, or Artifact.",
                ErrorCode = "invalid_source_kind",
            });
        }

        if (!TryParseMode(mode, out var parsedMode, out var modeError))
            return Task.FromResult<object?>(modeError);

        var request = new HandoffIngestionRequest
        {
            SourceKind = kind,
            Path = path,
            Content = content,
            ArtifactId = artifactId,
            Mode = parsedMode,
            Force = force,
            AgentName = agentName,
            PromptTemplateId = promptTemplateId,
        };
        return BoxAsync(_handoffIngestionService.IngestAsync(request, cancellationToken));
    }

    /// <inheritdoc />
    public Task<object?> GetAsync(string? runId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(runId))
        {
            return Task.FromResult<object?>(new HandoffIngestionResult
            {
                Success = false,
                Error = "Run id is required.",
            });
        }

        return BoxAsync(_handoffIngestionService.GetRunAsync(runId, cancellationToken));
    }

    /// <inheritdoc />
    public Task<object?> ApproveAsync(
        string? runId,
        bool approved,
        string? reviewer,
        string? notes,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(runId))
        {
            return Task.FromResult<object?>(new HandoffIngestionResult
            {
                Success = false,
                Error = "Run id is required.",
                ErrorCode = "run_id_required",
            });
        }

        return BoxAsync(_handoffIngestionService.ApproveAsync(
            runId,
            new HandoffApprovalRequest { Approved = approved, Reviewer = reviewer, Notes = notes },
            cancellationToken));
    }

    private static bool TryParseMode(string? mode, out HandoffIngestionMode parsed, out HandoffIngestionResult? error)
    {
        parsed = HandoffIngestionMode.DraftOnly;
        error = null;
        if (string.IsNullOrWhiteSpace(mode))
            return true;
        if (Enum.TryParse(mode, ignoreCase: true, out parsed) && Enum.IsDefined(parsed))
            return true;

        error = new HandoffIngestionResult
        {
            Success = false,
            Error = "mode must be DraftOnly, RequireReview, or CreateWhenConfident.",
            ErrorCode = "invalid_mode",
        };
        return false;
    }

    private static async Task<object?> BoxAsync(Task<HandoffIngestionResult> work)
        => await work.ConfigureAwait(false);
}
