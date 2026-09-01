using CommunityToolkit.Mvvm.Input;

namespace McpServer.Cqrs.Mvvm;

/// <summary>
/// TR-HANDOFF-SURFACE-001: Director-facing operations that must delegate to
/// <c>IHandoffIngestionService</c> in the host that registers these ViewModels.
/// </summary>
public interface IHandoffDirectorExecutor
{
    /// <summary>Ingest a handoff document and return the shared result contract.</summary>
    Task<object?> IngestAsync(
        string? sourceKind,
        string? path,
        string? content,
        string? artifactId,
        string? mode,
        bool force,
        string? agentName,
        string? promptTemplateId,
        CancellationToken cancellationToken);

    /// <summary>Inspect a persisted handoff run.</summary>
    Task<object?> GetAsync(string? runId, CancellationToken cancellationToken);

    /// <summary>Approve or reject a stored handoff run after revalidation.</summary>
    Task<object?> ApproveAsync(
        string? runId,
        bool approved,
        string? reviewer,
        string? notes,
        CancellationToken cancellationToken);
}

/// <summary>TR-HANDOFF-SURFACE-001: Director exec alias for handoff ingest.</summary>
[ViewModelCommand("handoff-ingest", Description = "Ingest a workspace-scoped handoff document and return the shared result contract.")]
public sealed class HandoffIngestDirectorCommand
{
    private readonly IHandoffDirectorExecutor _executor;

    /// <summary>Creates a Director ingest command that delegates to the shared handoff service.</summary>
    public HandoffIngestDirectorCommand(IHandoffDirectorExecutor executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        PrimaryCommand = new AsyncRelayCommand(ExecuteAsync);
    }

    /// <summary>Primary Director exec command.</summary>
    public IAsyncRelayCommand PrimaryCommand { get; }

    /// <summary>Last execution result, consumed by <see cref="IViewModelRegistry.GetResult"/>.</summary>
    public object? Result { get; private set; }

    /// <summary>Source kind: Path, Content, or Artifact.</summary>
    public string? SourceKind { get; set; }

    /// <summary>Workspace-contained path when SourceKind is Path.</summary>
    public string? Path { get; set; }

    /// <summary>Caller-supplied content when SourceKind is Content.</summary>
    public string? Content { get; set; }

    /// <summary>Artifact identifier when SourceKind is Artifact.</summary>
    public string? ArtifactId { get; set; }

    /// <summary>DraftOnly, RequireReview, or CreateWhenConfident.</summary>
    public string? Mode { get; set; }

    /// <summary>When true, skip deterministic replay.</summary>
    public bool Force { get; set; }

    /// <summary>Optional pooled agent name.</summary>
    public string? AgentName { get; set; }

    /// <summary>Optional prompt template identifier.</summary>
    public string? PromptTemplateId { get; set; }

    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Result = await _executor.IngestAsync(
                SourceKind,
                Path,
                Content,
                ArtifactId,
                Mode,
                Force,
                AgentName,
                PromptTemplateId,
                cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>TR-HANDOFF-SURFACE-001: Director exec alias for handoff run inspection.</summary>
[ViewModelCommand("handoff-get", Description = "Inspect a persisted handoff ingestion run.")]
public sealed class HandoffGetDirectorCommand
{
    private readonly IHandoffDirectorExecutor _executor;

    /// <summary>Creates a Director get command that delegates to the shared handoff service.</summary>
    public HandoffGetDirectorCommand(IHandoffDirectorExecutor executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        PrimaryCommand = new AsyncRelayCommand(ExecuteAsync);
    }

    /// <summary>Primary Director exec command.</summary>
    public IAsyncRelayCommand PrimaryCommand { get; }

    /// <summary>Last execution result, consumed by <see cref="IViewModelRegistry.GetResult"/>.</summary>
    public object? Result { get; private set; }

    /// <summary>Handoff run identifier.</summary>
    public string? RunId { get; set; }

    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Result = await _executor.GetAsync(RunId, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>TR-HANDOFF-SURFACE-001: Director exec alias for handoff approval.</summary>
[ViewModelCommand("handoff-approve", Description = "Approve or reject a stored handoff run after revalidation.")]
public sealed class HandoffApproveDirectorCommand
{
    private readonly IHandoffDirectorExecutor _executor;

    /// <summary>Creates a Director approve command that delegates to the shared handoff service.</summary>
    public HandoffApproveDirectorCommand(IHandoffDirectorExecutor executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        PrimaryCommand = new AsyncRelayCommand(ExecuteAsync);
    }

    /// <summary>Primary Director exec command.</summary>
    public IAsyncRelayCommand PrimaryCommand { get; }

    /// <summary>Last execution result, consumed by <see cref="IViewModelRegistry.GetResult"/>.</summary>
    public object? Result { get; private set; }

    /// <summary>Handoff run identifier.</summary>
    public string? RunId { get; set; }

    /// <summary>True to approve and create the TODO.</summary>
    public bool Approved { get; set; }

    /// <summary>Optional reviewer identity.</summary>
    public string? Reviewer { get; set; }

    /// <summary>Optional review notes.</summary>
    public string? Notes { get; set; }

    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Result = await _executor.ApproveAsync(RunId, Approved, Reviewer, Notes, cancellationToken).ConfigureAwait(false);
    }
}
