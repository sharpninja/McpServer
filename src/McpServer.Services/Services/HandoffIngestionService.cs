using System.Text.Json;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-HANDOFF-001 through FR-HANDOFF-006: Shared handoff ingestion pipeline.
/// AI output never writes TODO storage. Persistence goes through ITodoService only.
/// </summary>
public sealed class HandoffIngestionService : IHandoffIngestionService
{
    private static readonly JsonSerializerOptions DraftJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly IHandoffSourceResolver _sourceResolver;
    private readonly IHandoffOneShotExtractor _extractor;
    private readonly IHandoffTodoDraftParser _parser;
    private readonly IHandoffTodoDraftValidator _validator;
    private readonly IHandoffModePolicy _modePolicy;
    private readonly WorkspaceServiceAccessor _workspaceAccessor;
    private readonly McpDbContext _db;
    private readonly ISessionLogSanitizer _sanitizer;
    private readonly TimeProvider _time;
    private readonly IDbContextFactory<McpDbContext>? _dbFactory;
    private readonly HandoffLeaseOptions _lease;
    private readonly string _instanceId = Guid.NewGuid().ToString("N");

    /// <summary>TR-HANDOFF-TODO-001: Constructor.</summary>
    public HandoffIngestionService(
        IHandoffSourceResolver sourceResolver,
        IHandoffOneShotExtractor extractor,
        IHandoffTodoDraftParser parser,
        IHandoffTodoDraftValidator validator,
        IHandoffModePolicy modePolicy,
        WorkspaceServiceAccessor workspaceAccessor,
        McpDbContext db,
        ISessionLogSanitizer sanitizer,
        TimeProvider? time = null,
        IDbContextFactory<McpDbContext>? dbFactory = null,
        IOptions<HandoffLeaseOptions>? leaseOptions = null)
    {
        _sourceResolver = sourceResolver ?? throw new ArgumentNullException(nameof(sourceResolver));
        _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _modePolicy = modePolicy ?? throw new ArgumentNullException(nameof(modePolicy));
        _workspaceAccessor = workspaceAccessor ?? throw new ArgumentNullException(nameof(workspaceAccessor));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _sanitizer = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));
        _time = time ?? TimeProvider.System;
        _dbFactory = dbFactory;
        _lease = leaseOptions?.Value ?? new HandoffLeaseOptions();
    }

    /// <inheritdoc />
    public async Task<HandoffIngestionResult> IngestAsync(HandoffIngestionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (!Enum.IsDefined(request.SourceKind) || !Enum.IsDefined(request.Mode))
        {
            return new HandoffIngestionResult
            {
                Success = false,
                Error = "Handoff sourceKind and mode must be defined string enum values.",
                ErrorCode = HandoffErrorCodes.InvalidMode,
                Diagnostics =
                [
                    new HandoffDiagnostic
                    {
                        Code = HandoffErrorCodes.InvalidMode,
                        Severity = HandoffDiagnosticSeverity.Error,
                        Field = "mode",
                        Message = "Handoff sourceKind and mode must be defined string enum values.",
                    },
                ],
            };
        }

        if (!string.IsNullOrWhiteSpace(request.PromptTemplateId)
            && !string.Equals(request.PromptTemplateId, HandoffPromptDefaults.TemplateId, StringComparison.Ordinal))
        {
            return new HandoffIngestionResult
            {
                Success = false,
                Error = "Custom prompt templates are not allowed. The immutable canonical handoff prompt is required.",
                ErrorCode = HandoffErrorCodes.InvalidPromptTemplate,
            };
        }

        var workspacePath = HandoffWorkspacePaths.Canonicalize(_workspaceAccessor.GetWorkspacePath());
        _db.OverrideWorkspaceId(workspacePath);
        var diagnostics = new List<HandoffDiagnostic>();
        var resolved = await _sourceResolver.ResolveAsync(request, workspacePath, cancellationToken).ConfigureAwait(false);
        diagnostics.AddRange(resolved.Diagnostics);
        if (!resolved.Success || string.IsNullOrEmpty(resolved.Text) || string.IsNullOrEmpty(resolved.ContentSha256))
        {
            return await PersistAsync(
                request,
                workspacePath,
                resolved,
                draft: null,
                diagnostics,
                created: false,
                replayed: false,
                requiresReview: false,
                reviewState: HandoffReviewState.Failed,
                createdTodoId: null,
                agent: null,
                model: null,
                success: false,
                error: diagnostics.FirstOrDefault(item => item.Severity == HandoffDiagnosticSeverity.Error)?.Message ?? "Handoff source resolution failed.",
                cancellationToken).ConfigureAwait(false);
        }

        var reserved = await TryReserveRunAsync(
            request,
            workspacePath,
            resolved,
            cancellationToken).ConfigureAwait(false);
        if (reserved.InProgress)
            return InProgressResult(reserved.Entity);
        if (reserved.Replayed && reserved.Entity is not null)
            return MapEntity(reserved.Entity, replayed: true);

        var entity = reserved.Entity ?? throw new InvalidOperationException("Handoff run reservation did not produce a row.");

        using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var heartbeat = RenewProcessingLeaseLoopAsync(entity, heartbeatCts.Token);
        try
        {
            var extraction = await _extractor.ExtractAsync(
                workspacePath,
                resolved.Text,
                request.AgentName,
                promptTemplateId: null,
                cancellationToken).ConfigureAwait(false);

            if (!extraction.Success)
            {
                diagnostics.Add(new HandoffDiagnostic
                {
                    Code = "extract_malformed",
                    Severity = HandoffDiagnosticSeverity.Error,
                    Message = extraction.Error ?? "One-shot extraction failed.",
                });
            }

            var parsed = _parser.Parse(extraction.ResponseText);
            diagnostics.AddRange(parsed.Diagnostics);
            var validation = _validator.Validate(parsed.Draft);
            diagnostics.AddRange(validation.Diagnostics);
            await AddMissingReferenceDiagnosticsAsync(validation.Draft, diagnostics, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(extraction.Model))
            {
                diagnostics.Add(new HandoffDiagnostic
                {
                    Code = "provenance_model_missing",
                    Severity = HandoffDiagnosticSeverity.Warning,
                    Field = "model",
                    Message = "The one-shot job did not report a model identifier.",
                });
            }

            var decision = _modePolicy.Decide(request.Mode, validation, diagnostics);
            diagnostics.AddRange(decision.Diagnostics);

            if (!await OwnsProcessingAsync(entity, cancellationToken).ConfigureAwait(false))
            {
                // Still attempt the fenced terminal write. A missing owner+version
                // predicate would clobber the takeover receipt; the fence must no-op.
                return await CompleteReservedRunAsync(
                    entity,
                    request,
                    validation.Draft,
                    diagnostics,
                    created: false,
                    requiresReview: false,
                    reviewState: HandoffReviewState.None,
                    createdTodoId: null,
                    extraction.AgentName,
                    extraction.Model,
                    success: false,
                    error: "Handoff ingestion is already in progress.",
                    errorCode: HandoffErrorCodes.InProgress,
                    cancellationToken).ConfigureAwait(false);
            }

            string? createdTodoId = null;
            var created = false;
            if (decision.CanCreate && validation.Draft is not null)
            {
                var create = await TryCreateTodoAsync(entity, validation.Draft, diagnostics, cancellationToken).ConfigureAwait(false);
                created = create.Created;
                createdTodoId = create.TodoId;
                if (!created && diagnostics.Any(item => item.Code is HandoffErrorCodes.TodoCollision or HandoffErrorCodes.TodoCreateFailed))
                {
                    decision = new HandoffModeDecision
                    {
                        CanCreate = false,
                        RequiresReview = true,
                        ReviewState = HandoffReviewState.PendingReview,
                        Diagnostics = decision.Diagnostics,
                    };
                }
            }

            if (decision.CanCreate && !created && diagnostics.All(item => item.Code is not (HandoffErrorCodes.TodoCollision or HandoffErrorCodes.TodoCreateFailed)))
            {
                diagnostics.Add(new HandoffDiagnostic
                {
                    Code = HandoffErrorCodes.LostOwnership,
                    Severity = HandoffDiagnosticSeverity.Error,
                    Message = "This instance lost the processing fence before TODO creation completed.",
                });
            }

            var reviewState = created ? HandoffReviewState.Created : decision.ReviewState;
            if (!created && reviewState == HandoffReviewState.Created)
                reviewState = HandoffReviewState.None;
            var hasError = diagnostics.Any(item => item.Severity == HandoffDiagnosticSeverity.Error);
            var success = !hasError && reviewState is not HandoffReviewState.Failed;
            var errorDiagnostic = diagnostics.LastOrDefault(item => item.Severity == HandoffDiagnosticSeverity.Error);
            return await CompleteReservedRunAsync(
                entity,
                request,
                validation.Draft,
                diagnostics,
                created,
                decision.RequiresReview && reviewState == HandoffReviewState.PendingReview,
                reviewState,
                createdTodoId,
                extraction.AgentName,
                extraction.Model,
                success,
                success ? null : errorDiagnostic?.Message,
                success ? null : errorDiagnostic?.Code,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (string.IsNullOrWhiteSpace(entity.CreatedTodoId))
            {
                await TerminalizeReservedRunAsync(
                    entity,
                    HandoffErrorCodes.Cancelled,
                    "Handoff ingestion was cancelled.",
                    cancellationToken).ConfigureAwait(false);
            }

            throw;
        }
        catch (Exception ex)
        {
            return await TerminalizeReservedRunAsync(
                entity,
                HandoffErrorCodes.ProcessingFailed,
                ex.Message,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            heartbeatCts.Cancel();
            try
            {
                await heartbeat.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    /// <inheritdoc />
    public async Task<HandoffIngestionResult> GetRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId))
            return new HandoffIngestionResult { Success = false, Error = "Run id is required.", ErrorCode = "run_id_required" };

        var entity = await _db.HandoffIngestionRuns
            .Include(run => run.Diagnostics)
            .FirstOrDefaultAsync(run => run.RunId == runId, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
            return new HandoffIngestionResult { Success = false, Error = $"Handoff run '{runId}' was not found.", ErrorCode = "run_not_found" };

        return MapEntity(entity, replayed: false);
    }

    /// <inheritdoc />
    public async Task<HandoffIngestionResult> ApproveAsync(string runId, HandoffApprovalRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(runId))
            return new HandoffIngestionResult { Success = false, Error = "Run id is required.", ErrorCode = "run_id_required" };

        return await ApproveCoreAsync(runId, request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<HandoffIngestionResult> ApproveCoreAsync(
        string runId,
        HandoffApprovalRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _db.HandoffIngestionRuns
            .Include(run => run.Diagnostics)
            .FirstOrDefaultAsync(run => run.RunId == runId, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
            return new HandoffIngestionResult { Success = false, Error = $"Handoff run '{runId}' was not found.", ErrorCode = "run_not_found" };

        if (entity.ReviewState == nameof(HandoffReviewState.Created) && !string.IsNullOrWhiteSpace(entity.CreatedTodoId))
            return MapEntity(entity, replayed: true);

        if (entity.ReviewState == nameof(HandoffReviewState.Failed))
            return new HandoffIngestionResult { Success = false, Error = "Failed runs cannot be approved.", ErrorCode = "run_not_approvable" };

        var now = _time.GetUtcNow();
        var claimed = await _db.HandoffIngestionRuns
            .Where(run => run.RunId == runId
                && (run.ReviewState == nameof(HandoffReviewState.PendingReview)
                    || (run.ReviewState == nameof(HandoffReviewState.Approving)
                        && (run.ApprovalLeaseExpiresAtUtc == null || run.ApprovalLeaseExpiresAtUtc < now))))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(run => run.ReviewState, nameof(HandoffReviewState.Approving))
                    .SetProperty(run => run.ApprovalOwner, _instanceId)
                    .SetProperty(run => run.ApprovalLeaseExpiresAtUtc, now.Add(_lease.Duration))
                    .SetProperty(run => run.StateVersion, run => run.StateVersion + 1),
                cancellationToken)
            .ConfigureAwait(false);

        if (claimed == 0)
        {
            entity = await _db.HandoffIngestionRuns
                .Include(run => run.Diagnostics)
                .FirstOrDefaultAsync(run => run.RunId == runId, cancellationToken)
                .ConfigureAwait(false);
            if (entity is null)
                return new HandoffIngestionResult { Success = false, Error = $"Handoff run '{runId}' was not found.", ErrorCode = HandoffErrorCodes.RunNotFound };
            if (entity.ReviewState == nameof(HandoffReviewState.Created) && !string.IsNullOrWhiteSpace(entity.CreatedTodoId))
                return MapEntity(entity, replayed: true);
            if (entity.ReviewState == nameof(HandoffReviewState.Approving) && entity.ApprovalLeaseExpiresAtUtc >= now)
                return InProgressResult(entity);
            return new HandoffIngestionResult { Success = false, Error = "Only pending-review runs can be approved.", ErrorCode = HandoffErrorCodes.RunNotApprovable };
        }

        _db.Entry(entity).State = EntityState.Detached;
        entity = await _db.HandoffIngestionRuns
            .Include(run => run.Diagnostics)
            .FirstAsync(run => run.RunId == runId, cancellationToken)
            .ConfigureAwait(false);

        if (!request.Approved)
        {
            if (!await TryCompleteApprovalAsync(
                    entity,
                    nameof(HandoffReviewState.Rejected),
                    succeeded: true,
                    createdTodoId: null,
                    SanitizeText(request.Reviewer),
                    SanitizeText(request.Notes),
                    error: null,
                    errorCode: null,
                    cancellationToken).ConfigureAwait(false))
            {
                return LostOwnershipResult(entity);
            }

            return MapEntity(entity, replayed: false);
        }

        var draft = string.IsNullOrWhiteSpace(entity.DraftJson)
            ? null
            : JsonSerializer.Deserialize<HandoffTodoDraft>(entity.DraftJson, DraftJsonOptions);
        var validation = _validator.Validate(draft);
        var diagnostics = validation.Diagnostics.ToList();
        await AddMissingReferenceDiagnosticsAsync(validation.Draft, diagnostics, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid || validation.Draft is null || diagnostics.Any(item => item.Severity == HandoffDiagnosticSeverity.Error))
        {
            diagnostics.Add(new HandoffDiagnostic
            {
                Code = "approval_invalid",
                Severity = HandoffDiagnosticSeverity.Error,
                Message = "Approval revalidation failed. The stored draft cannot create a TODO.",
            });
            ReplaceDiagnostics(entity, diagnostics);
            if (!await TryCompleteApprovalAsync(
                    entity,
                    nameof(HandoffReviewState.Failed),
                    succeeded: false,
                    createdTodoId: null,
                    SanitizeText(request.Reviewer),
                    SanitizeText(request.Notes),
                    "Approval revalidation failed.",
                    "approval_invalid",
                    cancellationToken).ConfigureAwait(false))
            {
                return LostOwnershipResult(entity);
            }
            var failed = MapEntity(entity, replayed: false);
            failed.Success = false;
            failed.Error = "Approval revalidation failed.";
            failed.ErrorCode = "approval_invalid";
            return failed;
        }

        var create = await TryCreateTodoAsync(entity, validation.Draft, diagnostics, cancellationToken).ConfigureAwait(false);
        if (!create.Created)
        {
            if (entity.ReviewState == nameof(HandoffReviewState.Created))
                return MapEntity(entity, replayed: true);

            var error = diagnostics.LastOrDefault(item => item.Severity == HandoffDiagnosticSeverity.Error);
            ReplaceDiagnostics(entity, diagnostics);
            if (!await TryCompleteApprovalAsync(
                    entity,
                    nameof(HandoffReviewState.PendingReview),
                    succeeded: false,
                    createdTodoId: null,
                    SanitizeText(request.Reviewer),
                    SanitizeText(request.Notes),
                    error?.Message,
                    error?.Code ?? HandoffErrorCodes.TodoCreateFailed,
                    cancellationToken).ConfigureAwait(false))
            {
                return LostOwnershipResult(entity);
            }
            var blocked = MapEntity(entity, replayed: false);
            blocked.RequiresReview = true;
            blocked.Success = false;
            blocked.Error = entity.Error;
            blocked.ErrorCode = entity.ErrorCode;
            return blocked;
        }

        ReplaceDiagnostics(entity, diagnostics);
        if (!await TryCompleteApprovalAsync(
                entity,
                nameof(HandoffReviewState.Created),
                succeeded: true,
                create.TodoId,
                SanitizeText(request.Reviewer),
                SanitizeText(request.Notes),
                error: null,
                errorCode: null,
                cancellationToken).ConfigureAwait(false))
        {
            return LostOwnershipResult(entity);
        }
        var created = MapEntity(entity, replayed: false);
        created.Created = true;
        return created;
    }

    private async Task<(bool Created, string? TodoId)> TryCreateTodoAsync(
        HandoffIngestionRunEntity entity,
        HandoffTodoDraft draft,
        List<HandoffDiagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        var intentId = entity.TodoCreationIntentId;
        if (string.IsNullOrWhiteSpace(intentId))
        {
            intentId = $"handoff-todo:{entity.RunId}";
            using var persistCts = CreateCompensationCts();
            var version = entity.StateVersion;
            var persisted = await _db.HandoffIngestionRuns
                .Where(run => run.RunId == entity.RunId
                    && (run.ProcessingOwner == _instanceId || run.ApprovalOwner == _instanceId)
                    && run.StateVersion == version)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(run => run.TodoCreationIntentId, intentId)
                        .SetProperty(run => run.StateVersion, version + 1),
                    persistCts.Token)
                .ConfigureAwait(false);
            if (persisted == 0)
                return (false, null);
            entity.TodoCreationIntentId = intentId;
            entity.StateVersion = version + 1;
        }

        var todoService = _workspaceAccessor.GetTodoService();
        using var lookupCts = CreateCompensationCts();
        var existing = await todoService.GetByIdAsync(draft.Id!, lookupCts.Token).ConfigureAwait(false);
        if (existing is not null)
        {
            if (IsHeal(existing, intentId, draft))
                return (true, existing.Id);

            diagnostics.Add(CollisionDiagnostic());
            return (false, null);
        }

        try
        {
            var result = await todoService.CreateAsync(ToCreateRequest(draft, intentId), cancellationToken).ConfigureAwait(false);

            if (!result.Success)
            {
                using var conflictCts = CreateCompensationCts();
                var afterConflict = await todoService.GetByIdAsync(draft.Id!, conflictCts.Token).ConfigureAwait(false);
                if (afterConflict is not null && IsHeal(afterConflict, intentId, draft))
                    return (true, afterConflict.Id);

                diagnostics.Add(new HandoffDiagnostic
                {
                    Code = result.FailureKind == TodoMutationFailureKind.Conflict ? HandoffErrorCodes.TodoCollision : HandoffErrorCodes.TodoCreateFailed,
                    Severity = HandoffDiagnosticSeverity.Error,
                    Field = result.FailureKind == TodoMutationFailureKind.Conflict ? "id" : null,
                    Message = result.Error ?? "The TODO service rejected the create request.",
                });
                return (false, null);
            }

            return (true, result.Item?.Id ?? draft.Id);
        }
        catch (OperationCanceledException)
        {
            using var cancelCts = CreateCompensationCts();
            var afterCancel = await todoService.GetByIdAsync(draft.Id!, cancelCts.Token).ConfigureAwait(false);
            if (afterCancel is not null && IsHeal(afterCancel, intentId, draft))
            {
                entity.CreatedTodoId = afterCancel.Id;
                entity.ReviewState = nameof(HandoffReviewState.Created);
                entity.ProcessingState = nameof(HandoffProcessingState.Terminal);
                entity.Succeeded = true;
                entity.Error = null;
                entity.ErrorCode = null;
                if (!await TryCompensateCreatedRunAsync(entity).ConfigureAwait(false))
                    throw new InvalidOperationException(HandoffErrorCodes.CompensationFailed);
            }

            throw;
        }
    }

    private bool IsHeal(TodoFlatItem existing, string intentId, HandoffTodoDraft draft)
        => string.Equals(existing.IdempotencyKey, intentId, StringComparison.Ordinal)
           && TodoPayloadFingerprint.AreEquivalent(ToCreateRequest(draft, intentId), existing);

    private TodoCreateRequest ToCreateRequest(HandoffTodoDraft draft, string intentId)
        => new()
        {
            Id = draft.Id!,
            Title = SanitizeText(draft.Title) ?? draft.Title!,
            Section = SanitizeText(draft.Section) ?? draft.Section!,
            Priority = draft.Priority!,
            Estimate = SanitizeText(draft.Estimate),
            Description = SanitizeLines(draft.Description),
            TechnicalDetails = SanitizeLines(draft.TechnicalDetails),
            ImplementationTasks = draft.ImplementationTasks.Select(task => new TodoFlatTask(SanitizeText(task.Task) ?? task.Task, task.Done)).ToArray(),
            DependsOn = draft.DependsOn,
            FunctionalRequirements = draft.FunctionalRequirements,
            TechnicalRequirements = draft.TechnicalRequirements,
            IdempotencyKey = intentId,
        };

    private static HandoffDiagnostic CollisionDiagnostic()
        => new()
        {
            Code = HandoffErrorCodes.TodoCollision,
            Severity = HandoffDiagnosticSeverity.Error,
            Field = "id",
            Message = "A TODO with this id already exists. The draft requires review and will not be renamed.",
        };

    private async Task<HandoffIngestionResult> PersistAsync(
        HandoffIngestionRequest request,
        string workspacePath,
        HandoffResolvedSource resolved,
        HandoffTodoDraft? draft,
        List<HandoffDiagnostic> diagnostics,
        bool created,
        bool replayed,
        bool requiresReview,
        HandoffReviewState reviewState,
        string? createdTodoId,
        string? agent,
        string? model,
        bool success,
        string? error,
        CancellationToken cancellationToken,
        string? errorCode = null)
    {
        var runId = $"handoff-run-{Guid.NewGuid():N}";
        var sanitizedDraft = SanitizeDraft(draft);
        var entity = new HandoffIngestionRunEntity
        {
            RunId = runId,
            WorkspaceId = workspacePath,
            SourceKind = resolved.SourceKind.ToString(),
            SourceLocator = SanitizeText(resolved.Locator) ?? resolved.Locator,
            ContentSha256 = resolved.ContentSha256 ?? string.Empty,
            ExtractedAtUtc = _time.GetUtcNow(),
            PromptVersion = HandoffPromptDefaults.PromptVersion,
            TemplateVersion = HandoffPromptDefaults.TemplateId,
            Agent = SanitizeText(agent),
            Model = SanitizeText(model),
            Confidence = sanitizedDraft?.Confidence,
            Mode = request.Mode.ToString(),
            ReviewState = reviewState.ToString(),
            CreatedTodoId = createdTodoId,
            DraftJson = sanitizedDraft is null ? null : JsonSerializer.Serialize(sanitizedDraft, DraftJsonOptions),
            Force = request.Force,
            ReplayIdentity = HandoffReplayKeys.Create(
                workspacePath,
                resolved.ContentSha256 ?? string.Empty,
                HandoffPromptDefaults.PromptVersion,
                request.Force || string.IsNullOrEmpty(resolved.ContentSha256),
                runId),
            ProcessingState = nameof(HandoffProcessingState.Terminal),
            Succeeded = success,
            Error = SanitizeText(error),
            ErrorCode = errorCode,
        };
        ReplaceDiagnostics(entity, diagnostics);
        _db.HandoffIngestionRuns.Add(entity);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var result = MapEntity(entity, replayed);
        result.Success = success;
        result.Created = created;
        result.RequiresReview = requiresReview;
        result.Error = error;
        result.ErrorCode = errorCode;
        return result;
    }

    private async Task<(HandoffIngestionRunEntity? Entity, bool Replayed, bool InProgress)> TryReserveRunAsync(
        HandoffIngestionRequest request,
        string workspacePath,
        HandoffResolvedSource resolved,
        CancellationToken cancellationToken)
    {
        var runId = $"handoff-run-{Guid.NewGuid():N}";
        var identity = HandoffReplayKeys.Create(
            workspacePath,
            resolved.ContentSha256 ?? string.Empty,
            HandoffPromptDefaults.PromptVersion,
            request.Force,
            runId);
        if (!request.Force)
        {
            var existing = await _db.HandoffIngestionRuns
                .Include(run => run.Diagnostics)
                .FirstOrDefaultAsync(run => run.ReplayIdentity == identity, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
                return await ResolveExistingReservationAsync(existing, cancellationToken).ConfigureAwait(false);
        }

        var now = _time.GetUtcNow();
        var entity = new HandoffIngestionRunEntity
        {
            RunId = runId,
            WorkspaceId = workspacePath,
            SourceKind = resolved.SourceKind.ToString(),
            SourceLocator = SanitizeText(resolved.Locator) ?? resolved.Locator,
            ContentSha256 = resolved.ContentSha256 ?? string.Empty,
            ExtractedAtUtc = now,
            PromptVersion = HandoffPromptDefaults.PromptVersion,
            TemplateVersion = HandoffPromptDefaults.TemplateId,
            Mode = request.Mode.ToString(),
            ReviewState = nameof(HandoffReviewState.None),
            Force = request.Force,
            ReplayIdentity = identity,
            Succeeded = false,
            ProcessingState = nameof(HandoffProcessingState.Processing),
            ProcessingOwner = _instanceId,
            ProcessingLeaseExpiresAtUtc = now.Add(_lease.Duration),
        };
        _db.HandoffIngestionRuns.Add(entity);
        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return (entity, false, false);
        }
        catch (DbUpdateException ex) when (HandoffDbExceptions.IsUniqueViolation(ex))
        {
            _db.Entry(entity).State = EntityState.Detached;
            var winner = await _db.HandoffIngestionRuns
                .Include(run => run.Diagnostics)
                .FirstAsync(run => run.ReplayIdentity == identity, cancellationToken)
                .ConfigureAwait(false);
            return await ResolveExistingReservationAsync(winner, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<(HandoffIngestionRunEntity Entity, bool Replayed, bool InProgress)> ResolveExistingReservationAsync(
        HandoffIngestionRunEntity existing,
        CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow();
        var isTerminal = existing.ProcessingState == nameof(HandoffProcessingState.Terminal)
            || existing.ReviewState is nameof(HandoffReviewState.Created)
                or nameof(HandoffReviewState.Failed)
                or nameof(HandoffReviewState.Rejected)
                or nameof(HandoffReviewState.PendingReview);
        if (isTerminal && existing.ProcessingState != nameof(HandoffProcessingState.Processing))
            return (existing, true, false);

        var leaseLive = existing.ProcessingState == nameof(HandoffProcessingState.Processing)
            && existing.ProcessingLeaseExpiresAtUtc is { } expires
            && expires >= now
            && !string.Equals(existing.ProcessingOwner, _instanceId, StringComparison.Ordinal);
        if (leaseLive)
            return (existing, false, true);

        var taken = await _db.HandoffIngestionRuns
            .Where(run => run.RunId == existing.RunId
                && run.ProcessingState == nameof(HandoffProcessingState.Processing)
                && run.StateVersion == existing.StateVersion
                && (run.ProcessingLeaseExpiresAtUtc == null || run.ProcessingLeaseExpiresAtUtc < now))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(run => run.ProcessingOwner, _instanceId)
                    .SetProperty(run => run.ProcessingLeaseExpiresAtUtc, now.Add(_lease.Duration))
                    .SetProperty(run => run.StateVersion, run => run.StateVersion + 1),
                cancellationToken)
            .ConfigureAwait(false);
        if (taken == 0)
        {
            var current = await _db.HandoffIngestionRuns
                .AsNoTracking()
                .Include(run => run.Diagnostics)
                .FirstAsync(run => run.RunId == existing.RunId, cancellationToken)
                .ConfigureAwait(false);
            if (current.ProcessingState == nameof(HandoffProcessingState.Terminal))
                return (current, true, false);
            return (current, false, true);
        }

        _db.Entry(existing).State = EntityState.Detached;
        var owned = await _db.HandoffIngestionRuns
            .Include(run => run.Diagnostics)
            .FirstAsync(run => run.RunId == existing.RunId, cancellationToken)
            .ConfigureAwait(false);
        return (owned, false, false);
    }

    private async Task<HandoffIngestionResult> CompleteReservedRunAsync(
        HandoffIngestionRunEntity entity,
        HandoffIngestionRequest request,
        HandoffTodoDraft? draft,
        List<HandoffDiagnostic> diagnostics,
        bool created,
        bool requiresReview,
        HandoffReviewState reviewState,
        string? createdTodoId,
        string? agent,
        string? model,
        bool success,
        string? error,
        string? errorCode,
        CancellationToken cancellationToken)
    {
        var sanitizedDraft = SanitizeDraft(draft);
        var claimedVersion = entity.StateVersion;
        var fenced = await _db.HandoffIngestionRuns
            .Where(run => run.RunId == entity.RunId
                && run.ProcessingOwner == _instanceId
                && run.StateVersion == claimedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(run => run.TemplateVersion, HandoffPromptDefaults.TemplateId)
                    .SetProperty(run => run.Agent, SanitizeText(agent))
                    .SetProperty(run => run.Model, SanitizeText(model))
                    .SetProperty(run => run.Confidence, sanitizedDraft?.Confidence)
                    .SetProperty(run => run.Mode, request.Mode.ToString())
                    .SetProperty(run => run.ReviewState, reviewState.ToString())
                    .SetProperty(run => run.ProcessingState, nameof(HandoffProcessingState.Terminal))
                    .SetProperty(run => run.ProcessingOwner, (string?)null)
                    .SetProperty(run => run.ProcessingLeaseExpiresAtUtc, (DateTimeOffset?)null)
                    .SetProperty(run => run.CreatedTodoId, createdTodoId)
                    .SetProperty(run => run.DraftJson, sanitizedDraft is null ? null : JsonSerializer.Serialize(sanitizedDraft, DraftJsonOptions))
                    .SetProperty(run => run.Succeeded, success)
                    .SetProperty(run => run.Error, SanitizeText(error))
                    .SetProperty(run => run.ErrorCode, errorCode)
                    .SetProperty(run => run.StateVersion, claimedVersion + 1),
                cancellationToken)
            .ConfigureAwait(false);
        if (fenced == 0)
            return InProgressResult(entity);

        entity.TemplateVersion = HandoffPromptDefaults.TemplateId;
        entity.Agent = SanitizeText(agent);
        entity.Model = SanitizeText(model);
        entity.Confidence = sanitizedDraft?.Confidence;
        entity.Mode = request.Mode.ToString();
        entity.ReviewState = reviewState.ToString();
        entity.ProcessingState = nameof(HandoffProcessingState.Terminal);
        entity.ProcessingOwner = null;
        entity.ProcessingLeaseExpiresAtUtc = null;
        entity.CreatedTodoId = createdTodoId;
        entity.DraftJson = sanitizedDraft is null ? null : JsonSerializer.Serialize(sanitizedDraft, DraftJsonOptions);
        entity.Succeeded = success;
        entity.Error = SanitizeText(error);
        entity.ErrorCode = errorCode;
        entity.StateVersion = claimedVersion + 1;
        ReplaceDiagnostics(entity, diagnostics);
        try
        {
            if (!await SaveRunAfterTodoAsync(entity, cancellationToken).ConfigureAwait(false))
                return created ? CompensationFailedResult(entity) : FailedPersistResult(entity, error, errorCode);
        }
        catch (OperationCanceledException) when (created && !cancellationToken.IsCancellationRequested)
        {
            return CompensationFailedResult(entity);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception) when (created)
        {
            return CompensationFailedResult(entity);
        }

        var result = MapEntity(entity, replayed: false);
        result.Success = success;
        result.Created = created;
        result.RequiresReview = requiresReview;
        result.Error = error;
        result.ErrorCode = errorCode;
        return result;
    }

    private async Task<bool> SaveRunAfterTodoAsync(HandoffIngestionRunEntity entity, CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (!string.IsNullOrWhiteSpace(entity.CreatedTodoId))
        {
            if (!await TryCompensateCreatedRunAsync(entity).ConfigureAwait(false))
                throw;
            throw;
        }
        catch (Exception ex) when (!string.IsNullOrWhiteSpace(entity.CreatedTodoId) && HandoffDbExceptions.IsCommitAmbiguous(ex))
        {
            if (!await TryCompensateCreatedRunAsync(entity).ConfigureAwait(false))
                throw;
            return true;
        }
    }

    private async Task<bool> TryCompensateCreatedRunAsync(HandoffIngestionRunEntity entity)
    {
        using var cts = new CancellationTokenSource(_lease.CompensationTimeout);
        var token = cts.Token;
        var todo = await _workspaceAccessor.GetTodoService()
            .GetByIdAsync(entity.CreatedTodoId!, token)
            .ConfigureAwait(false);
        if (todo is null)
            return false;

        await using var fresh = CreateFreshContext();
        fresh.OverrideWorkspaceId(entity.WorkspaceId);
        var updated = await fresh.HandoffIngestionRuns
            .Where(run => run.RunId == entity.RunId
                && (run.ProcessingOwner == _instanceId || run.ApprovalOwner == _instanceId || run.CreatedTodoId == entity.CreatedTodoId))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(run => run.ReviewState, nameof(HandoffReviewState.Created))
                    .SetProperty(run => run.ProcessingState, nameof(HandoffProcessingState.Terminal))
                    .SetProperty(run => run.ProcessingOwner, (string?)null)
                    .SetProperty(run => run.ProcessingLeaseExpiresAtUtc, (DateTimeOffset?)null)
                    .SetProperty(run => run.CreatedTodoId, entity.CreatedTodoId)
                    .SetProperty(run => run.TodoCreationIntentId, entity.TodoCreationIntentId)
                    .SetProperty(run => run.Succeeded, true)
                    .SetProperty(run => run.Error, (string?)null)
                    .SetProperty(run => run.ErrorCode, (string?)null)
                    .SetProperty(run => run.DraftJson, entity.DraftJson)
                    .SetProperty(run => run.Agent, entity.Agent)
                    .SetProperty(run => run.Model, entity.Model)
                    .SetProperty(run => run.Confidence, entity.Confidence),
                token)
            .ConfigureAwait(false);
        if (updated == 0)
            return false;

        entity.ReviewState = nameof(HandoffReviewState.Created);
        entity.ProcessingState = nameof(HandoffProcessingState.Terminal);
        entity.Succeeded = true;
        return true;
    }

    private async Task<HandoffIngestionResult> TerminalizeReservedRunAsync(
        HandoffIngestionRunEntity entity,
        string errorCode,
        string error,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        using var cts = CreateCompensationCts();
        var token = cts.Token;
        if (!await OwnsProcessingAsync(entity, token).ConfigureAwait(false))
            return InProgressResult(entity);

        var version = entity.StateVersion;
        var fenced = await _db.HandoffIngestionRuns
            .Where(run => run.RunId == entity.RunId
                && run.ProcessingOwner == _instanceId
                && run.StateVersion == version)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(run => run.ReviewState, nameof(HandoffReviewState.Failed))
                    .SetProperty(run => run.ProcessingState, nameof(HandoffProcessingState.Terminal))
                    .SetProperty(run => run.ProcessingOwner, (string?)null)
                    .SetProperty(run => run.ProcessingLeaseExpiresAtUtc, (DateTimeOffset?)null)
                    .SetProperty(run => run.Succeeded, false)
                    .SetProperty(run => run.Error, SanitizeText(error))
                    .SetProperty(run => run.ErrorCode, errorCode)
                    .SetProperty(run => run.StateVersion, version + 1),
                token)
            .ConfigureAwait(false);
        if (fenced == 0)
            return InProgressResult(entity);

        entity.ReviewState = nameof(HandoffReviewState.Failed);
        entity.ProcessingState = nameof(HandoffProcessingState.Terminal);
        entity.ProcessingOwner = null;
        entity.ProcessingLeaseExpiresAtUtc = null;
        entity.Succeeded = false;
        entity.Error = SanitizeText(error);
        entity.ErrorCode = errorCode;
        entity.StateVersion = version + 1;
        var result = MapEntity(entity, replayed: false);
        result.Success = false;
        result.Error = entity.Error;
        result.ErrorCode = errorCode;
        return result;
    }

    private static HandoffIngestionResult InProgressResult(HandoffIngestionRunEntity? entity)
    {
        var result = entity is null
            ? new HandoffIngestionResult()
            : MapEntity(entity, replayed: false);
        result.Success = false;
        result.Replayed = false;
        result.Created = false;
        result.Error = "Handoff ingestion is already in progress.";
        result.ErrorCode = HandoffErrorCodes.InProgress;
        return result;
    }

    private McpDbContext CreateFreshContext()
    {
        if (_dbFactory is not null)
            return _dbFactory.CreateDbContext();

        var options = (DbContextOptions<McpDbContext>)_db.GetService(typeof(DbContextOptions<McpDbContext>));
        var fresh = new McpDbContext(options, new WorkspaceContext { WorkspacePath = _db.CurrentWorkspaceId });
        fresh.OverrideWorkspaceId(_db.CurrentWorkspaceId);
        return fresh;
    }

    private async Task RenewProcessingLeaseLoopAsync(HandoffIngestionRunEntity entity, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(_lease.HeartbeatInterval, cancellationToken).ConfigureAwait(false);
                var now = _time.GetUtcNow();
                await using var db = CreateFreshContext();
                var renewed = await db.HandoffIngestionRuns
                    .Where(run => run.RunId == entity.RunId
                        && run.ProcessingOwner == _instanceId)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(run => run.ProcessingLeaseExpiresAtUtc, now.Add(_lease.Duration)),
                        cancellationToken)
                    .ConfigureAwait(false);
                if (renewed == 0)
                    return;
                entity.ProcessingLeaseExpiresAtUtc = now.Add(_lease.Duration);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task<bool> OwnsProcessingAsync(HandoffIngestionRunEntity entity, CancellationToken cancellationToken)
    {
        var current = await _db.HandoffIngestionRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(run => run.RunId == entity.RunId, cancellationToken)
            .ConfigureAwait(false);
        return current is not null
            && string.Equals(current.ProcessingOwner, _instanceId, StringComparison.Ordinal)
            && current.ProcessingState == nameof(HandoffProcessingState.Processing)
            && current.StateVersion == entity.StateVersion;
    }

    private async Task<bool> TryCompleteApprovalAsync(
        HandoffIngestionRunEntity entity,
        string reviewState,
        bool succeeded,
        string? createdTodoId,
        string? reviewer,
        string? notes,
        string? error,
        string? errorCode,
        CancellationToken cancellationToken)
    {
        var version = entity.StateVersion;
        var updated = await _db.HandoffIngestionRuns
            .Where(run => run.RunId == entity.RunId
                && run.ApprovalOwner == _instanceId
                && run.StateVersion == version)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(run => run.ReviewState, reviewState)
                    .SetProperty(run => run.ProcessingState, reviewState == nameof(HandoffReviewState.PendingReview)
                        ? nameof(HandoffProcessingState.None)
                        : nameof(HandoffProcessingState.Terminal))
                    .SetProperty(run => run.ApprovalOwner, (string?)null)
                    .SetProperty(run => run.ApprovalLeaseExpiresAtUtc, (DateTimeOffset?)null)
                    .SetProperty(run => run.CreatedTodoId, createdTodoId)
                    .SetProperty(run => run.Reviewer, reviewer)
                    .SetProperty(run => run.ReviewNotes, notes)
                    .SetProperty(run => run.Succeeded, succeeded)
                    .SetProperty(run => run.Error, SanitizeText(error))
                    .SetProperty(run => run.ErrorCode, errorCode)
                    .SetProperty(run => run.StateVersion, version + 1),
                cancellationToken)
            .ConfigureAwait(false);
        if (updated == 0)
            return false;

        entity.ReviewState = reviewState;
        entity.Succeeded = succeeded;
        entity.CreatedTodoId = createdTodoId;
        entity.Reviewer = reviewer;
        entity.ReviewNotes = notes;
        entity.Error = SanitizeText(error);
        entity.ErrorCode = errorCode;
        entity.StateVersion = version + 1;
        entity.ApprovalOwner = null;
        entity.ApprovalLeaseExpiresAtUtc = null;
        return true;
    }

    private static HandoffIngestionResult LostOwnershipResult(HandoffIngestionRunEntity entity)
    {
        var result = MapEntity(entity, replayed: false);
        result.Success = false;
        result.Created = false;
        result.Error = "This instance lost the approval or processing fence.";
        result.ErrorCode = HandoffErrorCodes.LostOwnership;
        return result;
    }

    private static HandoffIngestionResult CompensationFailedResult(HandoffIngestionRunEntity entity)
    {
        var result = MapEntity(entity, replayed: false);
        result.Success = false;
        result.Created = false;
        result.Error = "The TODO was created but the durable handoff receipt could not be confirmed.";
        result.ErrorCode = HandoffErrorCodes.CompensationFailed;
        return result;
    }

    private static HandoffIngestionResult FailedPersistResult(HandoffIngestionRunEntity entity, string? error, string? errorCode)
    {
        var result = MapEntity(entity, replayed: false);
        result.Success = false;
        result.Created = false;
        result.Error = error ?? "Handoff run persistence failed.";
        result.ErrorCode = errorCode ?? HandoffErrorCodes.ProcessingFailed;
        return result;
    }

    private CancellationTokenSource CreateCompensationCts()
        => new(_lease.CompensationTimeout);

    private async Task AddMissingReferenceDiagnosticsAsync(
        HandoffTodoDraft? draft,
        List<HandoffDiagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        if (draft is null)
            return;

        var requirementIds = draft.FunctionalRequirements.Concat(draft.TechnicalRequirements).Distinct(StringComparer.Ordinal).ToArray();
        if (requirementIds.Length > 0)
        {
            var existing = await _db.Requirements
                .AsNoTracking()
                .Where(item => requirementIds.Contains(item.Id))
                .Select(item => item.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            foreach (var id in requirementIds.Except(existing, StringComparer.Ordinal))
            {
                diagnostics.Add(new HandoffDiagnostic
                {
                    Code = "draft_missing_requirement",
                    Severity = HandoffDiagnosticSeverity.Error,
                    Field = id.StartsWith("TR-", StringComparison.Ordinal) ? "technicalRequirements" : "functionalRequirements",
                    Message = $"Referenced requirement '{id}' does not exist.",
                });
            }
        }

        foreach (var dependency in draft.DependsOn)
        {
            var todo = await _workspaceAccessor.GetTodoService().GetByIdAsync(dependency, cancellationToken).ConfigureAwait(false);
            if (todo is not null)
                continue;
            diagnostics.Add(new HandoffDiagnostic
            {
                Code = "draft_missing_dependency",
                Severity = HandoffDiagnosticSeverity.Error,
                Field = "dependsOn",
                Message = $"Referenced dependency '{dependency}' does not exist.",
            });
        }
    }

    private void ReplaceDiagnostics(HandoffIngestionRunEntity entity, IReadOnlyList<HandoffDiagnostic> diagnostics)
    {
        var existing = entity.Diagnostics.ToList();
        if (existing.Count > 0)
            _db.RemoveRange(existing);

        var ordinal = 0;
        foreach (var diagnostic in diagnostics)
        {
            _db.Set<HandoffDiagnosticEntity>().Add(new HandoffDiagnosticEntity
            {
                WorkspaceId = entity.WorkspaceId,
                RunId = entity.RunId,
                Code = diagnostic.Code,
                Severity = diagnostic.Severity.ToString(),
                Field = diagnostic.Field,
                Message = SanitizeText(diagnostic.Message) ?? diagnostic.Message,
                Ordinal = ordinal++,
            });
        }
    }

    private static HandoffIngestionResult MapEntity(HandoffIngestionRunEntity entity, bool replayed)
    {
        var draft = string.IsNullOrWhiteSpace(entity.DraftJson)
            ? null
            : JsonSerializer.Deserialize<HandoffTodoDraft>(entity.DraftJson, DraftJsonOptions);
        Enum.TryParse<HandoffSourceKind>(entity.SourceKind, out var sourceKind);
        Enum.TryParse<HandoffIngestionMode>(entity.Mode, out var mode);
        Enum.TryParse<HandoffReviewState>(entity.ReviewState, out var reviewState);
        return new HandoffIngestionResult
        {
            Success = entity.Succeeded,
            Created = reviewState == HandoffReviewState.Created && !replayed,
            Replayed = replayed,
            RequiresReview = reviewState == HandoffReviewState.PendingReview,
            Error = entity.Error,
            ErrorCode = entity.ErrorCode,
            Draft = draft,
            CreatedTodoId = entity.CreatedTodoId,
            Provenance = new HandoffProvenance
            {
                RunId = entity.RunId,
                SourceKind = sourceKind,
                SourceLocator = entity.SourceLocator,
                ContentSha256 = entity.ContentSha256,
                ExtractedAtUtc = entity.ExtractedAtUtc,
                PromptVersion = entity.PromptVersion,
                TemplateVersion = entity.TemplateVersion,
                Agent = entity.Agent,
                Model = entity.Model,
                Confidence = entity.Confidence,
                Mode = mode,
                ReviewState = replayed ? HandoffReviewState.Replayed : reviewState,
                CreatedTodoId = entity.CreatedTodoId,
            },
            Diagnostics = entity.Diagnostics
                .OrderBy(item => item.Ordinal)
                .Select(item => new HandoffDiagnostic
                {
                    Code = item.Code,
                    Severity = Enum.TryParse<HandoffDiagnosticSeverity>(item.Severity, out var severity)
                        ? severity
                        : HandoffDiagnosticSeverity.Info,
                    Field = item.Field,
                    Message = item.Message,
                })
                .ToArray(),
        };
    }

    private HandoffTodoDraft? SanitizeDraft(HandoffTodoDraft? draft)
    {
        if (draft is null)
            return null;

        return new HandoffTodoDraft
        {
            Id = draft.Id,
            Title = SanitizeText(draft.Title),
            Section = SanitizeText(draft.Section),
            Priority = draft.Priority,
            Estimate = SanitizeText(draft.Estimate),
            Description = SanitizeLines(draft.Description),
            TechnicalDetails = SanitizeLines(draft.TechnicalDetails),
            ImplementationTasks = draft.ImplementationTasks
                .Select(task => new HandoffTodoDraftTask { Task = SanitizeText(task.Task) ?? task.Task, Done = task.Done })
                .ToArray(),
            DependsOn = draft.DependsOn,
            FunctionalRequirements = draft.FunctionalRequirements,
            TechnicalRequirements = draft.TechnicalRequirements,
            Confidence = draft.Confidence,
            UnknownSourceNotes = SanitizeLines(draft.UnknownSourceNotes),
        };
    }

    private IReadOnlyList<string> SanitizeLines(IReadOnlyList<string>? values)
        => (values ?? []).Select(item => SanitizeText(item) ?? string.Empty).ToArray();

    private string? SanitizeText(string? value)
        => value is null ? null : _sanitizer.SanitizeString(value);
}
