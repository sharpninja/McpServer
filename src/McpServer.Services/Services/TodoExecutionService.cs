using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// File-backed Byrd execution workflow service layered on top of the existing TODO providers.
/// </summary>
public sealed class TodoExecutionService : ITodoExecutionService
{
    private const string GeneratedTodoIdPrefix = "EXEC-TODO-";
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> s_stateLocks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly JsonSerializerOptions s_stateJsonOptions = CreateJsonOptions();
    private static readonly Regex s_nonAlphaNumericRegex = new("[^a-z0-9]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex s_currentFocusRegex = new(@"mCurrentFocus=Window\{.*?\s(?<focus>[A-Za-z0-9_.$]+/[A-Za-z0-9_.$]+)\}", RegexOptions.Compiled);
    private static readonly Regex s_focusedAppRegex = new(@"mFocusedApp=.*?\s(?<focus>[A-Za-z0-9_.$]+/[A-Za-z0-9_.$]+)", RegexOptions.Compiled);

    private readonly TodoServiceResolver _todoServiceResolver;
    private readonly IWorkspaceService _workspaceService;
    private readonly IRequirementsDocumentService _requirementsDocumentService;
    private readonly McpDbContext _db;
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<TodoExecutionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TodoExecutionService"/> class.
    /// </summary>
    public TodoExecutionService(
        TodoServiceResolver todoServiceResolver,
        IWorkspaceService workspaceService,
        IRequirementsDocumentService requirementsDocumentService,
        McpDbContext db,
        IProcessRunner processRunner,
        ILogger<TodoExecutionService> logger)
    {
        _todoServiceResolver = todoServiceResolver ?? throw new ArgumentNullException(nameof(todoServiceResolver));
        _workspaceService = workspaceService ?? throw new ArgumentNullException(nameof(workspaceService));
        _requirementsDocumentService = requirementsDocumentService ?? throw new ArgumentNullException(nameof(requirementsDocumentService));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<CreateIterationPhaseResult> CreateIterationPhaseAsync(string workspacePath, CreateIterationPhaseRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalizedWorkspacePath = NormalizeWorkspacePath(workspacePath);
        ValidateRequired(request.Name, nameof(request.Name));
        ValidateRequired(request.Summary, nameof(request.Summary));

        var statePath = GetStatePath(normalizedWorkspacePath);
        var gate = GetStateLock(statePath);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await LoadStateAsync(statePath, cancellationToken).ConfigureAwait(false);
            var phaseId = $"PHASE-{state.NextPhaseNumber++:D3}";
            var now = UtcNow();
            state.Phases.Add(new TodoIterationPhase
            {
                PhaseId = phaseId,
                WorkspacePath = normalizedWorkspacePath,
                Name = request.Name.Trim(),
                Summary = request.Summary.Trim(),
                Status = TodoIterationPhaseStatus.Planning,
                RequirementIds = NormalizeStringList(request.RequirementIds),
                TodoIds = [],
                EntryCriteria = NormalizeStringList(request.EntryCriteria),
                ExitCriteria = NormalizeStringList(request.ExitCriteria),
                CreatedFromPlanId = request.CreatedFromPlanId?.Trim(),
                Branch = request.Branch?.Trim(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });

            await SaveStateAsync(statePath, state, cancellationToken).ConfigureAwait(false);
            return new CreateIterationPhaseResult { PhaseId = phaseId, Status = TodoIterationPhaseStatus.Planning };
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<CreateTodosFromPlanResult> CreateTodosFromPlanAsync(string workspacePath, CreateTodosFromPlanRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalizedWorkspacePath = NormalizeWorkspacePath(workspacePath);
        ValidateRequired(request.PhaseId, nameof(request.PhaseId));
        ValidateRequired(request.PlanId, nameof(request.PlanId));
        if (request.Todos is not { Count: > 0 })
            throw new ArgumentException("At least one plan TODO is required.", nameof(request));

        var todoService = await ResolveTodoServiceAsync(normalizedWorkspacePath, cancellationToken).ConfigureAwait(false);
        var statePath = GetStatePath(normalizedWorkspacePath);
        var gate = GetStateLock(statePath);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await LoadStateAsync(statePath, cancellationToken).ConfigureAwait(false);
            var phase = FindPhase(state, normalizedWorkspacePath, request.PhaseId)
                ?? throw new KeyNotFoundException($"Iteration phase '{request.PhaseId}' was not found.");

            var createdTodoIds = new List<string>();
            var updatedPhaseTodoIds = phase.TodoIds.ToList();
            var updatedTodos = state.Todos.ToList();
            var now = UtcNow();

            foreach (var input in request.Todos)
            {
                ValidateRequired(input.Title, nameof(input.Title));
                ValidateRequired(input.Goal, nameof(input.Goal));
                ValidateRequired(input.Summary, nameof(input.Summary));

                var todoId = await GenerateNextTodoIdAsync(state, todoService, cancellationToken).ConfigureAwait(false);
                var requirementIds = NormalizeStringList(input.RequirementIds);
                var createResult = await todoService.CreateAsync(new TodoCreateRequest
                {
                    Id = todoId,
                    Title = input.Title.Trim(),
                    Section = BuildPhaseSectionName(phase.Name),
                    Priority = MapLegacyPriority(TodoExecutionPriority.Medium),
                    Description = NormalizeStringList([input.Goal.Trim(), input.Summary.Trim()]),
                    Remaining = input.Summary.Trim(),
                    Note = $"Byrd phase: {phase.Name}",
                    DependsOn = NormalizeStringList(input.DependsOnTodoIds),
                    FunctionalRequirements = requirementIds.Where(static id => id.StartsWith("FR-", StringComparison.OrdinalIgnoreCase)).ToList(),
                    TechnicalRequirements = requirementIds.Where(static id => id.StartsWith("TR-", StringComparison.OrdinalIgnoreCase)).ToList(),
                }, cancellationToken).ConfigureAwait(false);

                if (!createResult.Success)
                    throw new InvalidOperationException(createResult.Error ?? $"Failed to create legacy TODO '{todoId}'.");

                var acceptanceCriteria = NormalizeStringList(input.AcceptanceCriteria)
                    .Select((text, index) => new AcceptanceCriterion
                    {
                        Id = $"{todoId}-AC-{index + 1:D2}",
                        Text = text,
                    })
                    .ToList();
                var constraints = NormalizeStringList(input.Constraints)
                    .Select((text, index) => new TodoConstraint
                    {
                        Id = $"{todoId}-CT-{index + 1:D2}",
                        Text = text,
                    })
                    .ToList();
                var dependencies = NormalizeStringList(input.DependsOnTodoIds)
                    .Select(id => new TodoDependency { TodoId = id, Reason = "Plan dependency" })
                    .ToList();

                updatedTodos.Add(new TodoExecutionRecord
                {
                    TodoId = todoId,
                    WorkspacePath = normalizedWorkspacePath,
                    Title = input.Title.Trim(),
                    Goal = input.Goal.Trim(),
                    Summary = input.Summary.Trim(),
                    Status = TodoExecutionStatus.Planned,
                    Priority = TodoExecutionPriority.Medium,
                    IterationPhaseId = phase.PhaseId,
                    DependsOn = dependencies,
                    AcceptanceCriteria = acceptanceCriteria,
                    Constraints = constraints,
                    RequirementIds = requirementIds,
                    RelevantFiles = NormalizeStringList(input.RelevantFiles),
                    ArtifactIds = [],
                    SessionTurnIds = [],
                    NextAction = $"Define unit tests for {input.Title.Trim()}",
                    TestPlan = new TodoTestPlan
                    {
                        UnitTestsDefined = false,
                        UnitTestsPassing = false,
                        IntegrationTestsDefined = false,
                        IntegrationTestsPassing = false,
                        TestFilePaths = [],
                        TestCommands = [],
                    },
                    Validation = new TodoValidationState
                    {
                        LastResult = "not_run",
                        LastValidatedAtUtc = null,
                        ValidationArtifactIds = [],
                        Summary = null,
                    },
                    Pointers = new TodoExecutionPointers(),
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                });

                updatedPhaseTodoIds.Add(todoId);
                createdTodoIds.Add(todoId);
            }

            state.Todos = updatedTodos;
            ReplacePhase(state, phase with
            {
                TodoIds = updatedPhaseTodoIds,
                UpdatedAtUtc = now,
                CreatedFromPlanId = string.IsNullOrWhiteSpace(phase.CreatedFromPlanId) ? request.PlanId.Trim() : phase.CreatedFromPlanId,
            });
            await SaveStateAsync(statePath, state, cancellationToken).ConfigureAwait(false);

            return new CreateTodosFromPlanResult
            {
                PhaseId = phase.PhaseId,
                TodoIds = createdTodoIds,
            };
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ActiveTodoResult?> GetActiveTodoAsync(string workspacePath, CancellationToken cancellationToken = default)
        => await GetNextReadyTodoAsync(workspacePath, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<TodoExecutionRecord?> GetTodoAsync(string workspacePath, string todoId, CancellationToken cancellationToken = default)
    {
        var normalizedWorkspacePath = NormalizeWorkspacePath(workspacePath);
        ValidateRequired(todoId, nameof(todoId));

        var state = await LoadWorkspaceStateAsync(normalizedWorkspacePath, cancellationToken).ConfigureAwait(false);
        return FindTodo(state, normalizedWorkspacePath, todoId);
    }

    /// <inheritdoc />
    public async Task<ActiveTodoResult?> GetNextReadyTodoAsync(string workspacePath, CancellationToken cancellationToken = default)
    {
        var normalizedWorkspacePath = NormalizeWorkspacePath(workspacePath);
        var state = await LoadWorkspaceStateAsync(normalizedWorkspacePath, cancellationToken).ConfigureAwait(false);
        return SelectNextReadyTodo(state, normalizedWorkspacePath);
    }

    /// <inheritdoc />
    public async Task<ActiveTodoContext?> GetExecutionContextAsync(
        string workspacePath,
        string todoId,
        int requirementSnippetLimit = 5,
        int sessionTurnSummaryLimit = 5,
        CancellationToken cancellationToken = default)
    {
        var normalizedWorkspacePath = NormalizeWorkspacePath(workspacePath);
        ValidateRequired(todoId, nameof(todoId));

        var state = await LoadWorkspaceStateAsync(normalizedWorkspacePath, cancellationToken).ConfigureAwait(false);
        var todo = FindTodo(state, normalizedWorkspacePath, todoId);
        if (todo is null)
            return null;

        var requirementSnippets = await GetRequirementSnippetsAsync(todo.RequirementIds, requirementSnippetLimit, cancellationToken).ConfigureAwait(false);
        var recentTurnSummaries = await GetTurnSummariesAsync(normalizedWorkspacePath, todo.SessionTurnIds, sessionTurnSummaryLimit, cancellationToken).ConfigureAwait(false);
        var relevantFiles = MergeDistinct(todo.RelevantFiles, await GetFilesModifiedAsync(normalizedWorkspacePath, todo.SessionTurnIds, sessionTurnSummaryLimit, cancellationToken).ConfigureAwait(false));

        return new ActiveTodoContext
        {
            TodoId = todo.TodoId,
            WorkspacePath = todo.WorkspacePath,
            Title = todo.Title,
            Goal = todo.Goal,
            Summary = todo.Summary,
            Status = todo.Status,
            IterationPhaseId = todo.IterationPhaseId,
            NextAction = DetermineNextAction(todo),
            RequirementIds = todo.RequirementIds,
            RecentRequirementSnippets = requirementSnippets,
            RecentTurnSummaries = recentTurnSummaries,
            RelevantFiles = relevantFiles,
            ArtifactIds = todo.ArtifactIds,
            AcceptanceCriteria = todo.AcceptanceCriteria.Select(static criterion => criterion.Text).ToList(),
            Constraints = todo.Constraints.Select(static constraint => constraint.Text).ToList(),
            TestPlan = todo.TestPlan,
            Validation = todo.Validation,
            Pointers = todo.Pointers,
        };
    }

    /// <inheritdoc />
    public async Task<TodoDeltaContext?> GetDeltaContextAsync(
        string workspacePath,
        string todoId,
        string? sinceCheckpointId,
        CancellationToken cancellationToken = default)
    {
        var normalizedWorkspacePath = NormalizeWorkspacePath(workspacePath);
        ValidateRequired(todoId, nameof(todoId));

        var state = await LoadWorkspaceStateAsync(normalizedWorkspacePath, cancellationToken).ConfigureAwait(false);
        var todo = FindTodo(state, normalizedWorkspacePath, todoId);
        if (todo is null)
            return null;

        var checkpoints = GetCheckpointsForTodo(state, normalizedWorkspacePath, todoId);
        if (!string.IsNullOrWhiteSpace(sinceCheckpointId))
        {
            var checkpointIndex = checkpoints.FindIndex(checkpoint => string.Equals(checkpoint.CheckpointId, sinceCheckpointId, StringComparison.OrdinalIgnoreCase));
            if (checkpointIndex >= 0)
                checkpoints = checkpoints.Skip(checkpointIndex + 1).ToList();
        }

        var newTurnIds = checkpoints.SelectMany(static checkpoint => checkpoint.SessionTurnIds).Where(static id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var newTurnSummaries = await GetTurnSummariesAsync(normalizedWorkspacePath, newTurnIds, 20, cancellationToken).ConfigureAwait(false);
        var newArtifactIds = checkpoints.SelectMany(static checkpoint => checkpoint.ArtifactIds).Where(static id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var newCommitShas = checkpoints.SelectMany(static checkpoint => checkpoint.CommitShas).Where(static sha => !string.IsNullOrWhiteSpace(sha)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        return new TodoDeltaContext
        {
            TodoId = todo.TodoId,
            SinceCheckpointId = sinceCheckpointId,
            NewTurnIds = newTurnIds,
            NewTurnSummaries = newTurnSummaries,
            NewArtifactIds = newArtifactIds,
            NewCommitShas = newCommitShas,
            UpdatedNextAction = DetermineNextAction(todo),
        };
    }

    /// <inheritdoc />
    public async Task<SetTodoTestPlanResult> SetTestPlanAsync(string workspacePath, string todoId, SetTodoTestPlanRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalizedWorkspacePath = NormalizeWorkspacePath(workspacePath);
        ValidateRequired(todoId, nameof(todoId));

        var statePath = GetStatePath(normalizedWorkspacePath);
        var gate = GetStateLock(statePath);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await LoadStateAsync(statePath, cancellationToken).ConfigureAwait(false);
            var todo = FindTodo(state, normalizedWorkspacePath, todoId)
                ?? throw new KeyNotFoundException($"Execution TODO '{todoId}' was not found.");

            var updatedStatus = request.UnitTestsDefined
                ? TodoExecutionStatus.TestReady
                : TodoExecutionStatus.TestDesign;
            var updatedTodo = todo with
            {
                Status = updatedStatus,
                TestPlan = new TodoTestPlan
                {
                    UnitTestsDefined = request.UnitTestsDefined,
                    UnitTestsPassing = request.UnitTestsPassing ?? todo.TestPlan.UnitTestsPassing,
                    IntegrationTestsDefined = request.IntegrationTestsDefined,
                    IntegrationTestsPassing = request.IntegrationTestsPassing ?? todo.TestPlan.IntegrationTestsPassing,
                    TestFilePaths = NormalizeStringList(request.TestFilePaths),
                    TestCommands = NormalizeStringList(request.TestCommands),
                },
                NextAction = request.UnitTestsDefined
                    ? $"Implement {todo.Title}"
                    : $"Define unit tests for {todo.Title}",
                UpdatedAtUtc = UtcNow(),
            };

            ReplaceTodo(state, updatedTodo);
            RefreshPhaseStatus(state, normalizedWorkspacePath, updatedTodo.IterationPhaseId);
            await SaveStateAsync(statePath, state, cancellationToken).ConfigureAwait(false);

            return new SetTodoTestPlanResult
            {
                TodoId = todoId,
                Status = updatedStatus,
            };
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<UpdateTodoStatusResult> UpdateStatusAsync(string workspacePath, string todoId, UpdateTodoStatusRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalizedWorkspacePath = NormalizeWorkspacePath(workspacePath);
        ValidateRequired(todoId, nameof(todoId));

        var statePath = GetStatePath(normalizedWorkspacePath);
        var gate = GetStateLock(statePath);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await LoadStateAsync(statePath, cancellationToken).ConfigureAwait(false);
            var todo = FindTodo(state, normalizedWorkspacePath, todoId)
                ?? throw new KeyNotFoundException($"Execution TODO '{todoId}' was not found.");

            ValidateTransition(state, todo, request.TargetStatus, request.Reason);

            var updatedTodo = todo with
            {
                Status = request.TargetStatus,
                NextAction = DetermineNextAction(todo with { Status = request.TargetStatus }),
                UpdatedAtUtc = UtcNow(),
            };

            ReplaceTodo(state, updatedTodo);
            RefreshPhaseStatus(state, normalizedWorkspacePath, updatedTodo.IterationPhaseId);
            await SaveStateAsync(statePath, state, cancellationToken).ConfigureAwait(false);

            return new UpdateTodoStatusResult
            {
                TodoId = todoId,
                PreviousStatus = todo.Status,
                CurrentStatus = request.TargetStatus,
            };
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<AppendTodoCheckpointResult> AppendCheckpointAsync(string workspacePath, string todoId, AppendTodoCheckpointRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalizedWorkspacePath = NormalizeWorkspacePath(workspacePath);
        ValidateRequired(todoId, nameof(todoId));
        ValidateRequired(request.Summary, nameof(request.Summary));

        var statePath = GetStatePath(normalizedWorkspacePath);
        var gate = GetStateLock(statePath);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await LoadStateAsync(statePath, cancellationToken).ConfigureAwait(false);
            var todo = FindTodo(state, normalizedWorkspacePath, todoId)
                ?? throw new KeyNotFoundException($"Execution TODO '{todoId}' was not found.");

            var checkpointId = $"CHK-{state.NextCheckpointNumber++:D3}";
            var checkpoint = new TodoCheckpoint
            {
                CheckpointId = checkpointId,
                TodoId = todoId,
                WorkspacePath = normalizedWorkspacePath,
                Kind = request.Kind,
                Summary = request.Summary.Trim(),
                NextAction = request.NextAction?.Trim(),
                RequirementIds = NormalizeStringList(request.RequirementIds),
                SessionTurnIds = NormalizeStringList(request.SessionTurnIds),
                ArtifactIds = NormalizeStringList(request.ArtifactIds),
                CommitShas = NormalizeStringList(request.CommitShas),
                CreatedAtUtc = UtcNow(),
            };
            state.Checkpoints.Add(checkpoint);

            var updatedTodo = ApplyCheckpointToTodo(todo, checkpoint);
            ReplaceTodo(state, updatedTodo);
            RefreshPhaseStatus(state, normalizedWorkspacePath, updatedTodo.IterationPhaseId);
            await SaveStateAsync(statePath, state, cancellationToken).ConfigureAwait(false);

            return new AppendTodoCheckpointResult
            {
                CheckpointId = checkpointId,
                TodoId = todoId,
            };
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<RecordTodoValidationResultResult> RecordValidationResultAsync(string workspacePath, string todoId, RecordTodoValidationResultRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalizedWorkspacePath = NormalizeWorkspacePath(workspacePath);
        ValidateRequired(todoId, nameof(todoId));
        ValidateRequired(request.Result, nameof(request.Result));

        var statePath = GetStatePath(normalizedWorkspacePath);
        var gate = GetStateLock(statePath);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await LoadStateAsync(statePath, cancellationToken).ConfigureAwait(false);
            var todo = FindTodo(state, normalizedWorkspacePath, todoId)
                ?? throw new KeyNotFoundException($"Execution TODO '{todoId}' was not found.");

            var normalizedResult = request.Result.Trim().ToLowerInvariant();
            var validationState = new TodoValidationState
            {
                LastResult = normalizedResult,
                LastValidatedAtUtc = UtcNow(),
                ValidationArtifactIds = NormalizeStringList(request.ArtifactIds),
                Summary = request.Summary?.Trim(),
            };

            var updatedTestPlan = todo.TestPlan with
            {
                UnitTestsPassing = request.UnitTestsPassing ?? todo.TestPlan.UnitTestsPassing,
                IntegrationTestsPassing = request.IntegrationTestsPassing ?? todo.TestPlan.IntegrationTestsPassing,
            };

            var updatedAcceptanceCriteria = string.Equals(normalizedResult, "pass", StringComparison.OrdinalIgnoreCase)
                ? todo.AcceptanceCriteria
                    .Select(criterion => criterion with { IsSatisfied = true, Evidence = request.Summary ?? criterion.Evidence })
                    .ToList()
                : todo.AcceptanceCriteria;

            var updatedTodo = todo with
            {
                Validation = validationState,
                TestPlan = updatedTestPlan,
                AcceptanceCriteria = updatedAcceptanceCriteria,
                SessionTurnIds = MergeDistinct(todo.SessionTurnIds, NormalizeStringList(request.SessionTurnIds)),
                ArtifactIds = MergeDistinct(todo.ArtifactIds, NormalizeStringList(request.ArtifactIds)),
                Pointers = todo.Pointers with
                {
                    LastRelevantTurnId = NormalizeStringList(request.SessionTurnIds).LastOrDefault() ?? todo.Pointers.LastRelevantTurnId,
                    LastSuccessfulTurnId = string.Equals(normalizedResult, "pass", StringComparison.OrdinalIgnoreCase)
                        ? NormalizeStringList(request.SessionTurnIds).LastOrDefault() ?? todo.Pointers.LastSuccessfulTurnId
                        : todo.Pointers.LastSuccessfulTurnId,
                    LastFailedTurnId = string.Equals(normalizedResult, "fail", StringComparison.OrdinalIgnoreCase)
                        ? NormalizeStringList(request.SessionTurnIds).LastOrDefault() ?? todo.Pointers.LastFailedTurnId
                        : todo.Pointers.LastFailedTurnId,
                    LastScreenshotArtifactId = NormalizeStringList(request.ArtifactIds)
                        .LastOrDefault(static artifact => artifact.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                        ?? todo.Pointers.LastScreenshotArtifactId,
                },
                NextAction = string.Equals(normalizedResult, "pass", StringComparison.OrdinalIgnoreCase)
                    ? "Mark TODO complete once acceptance criteria are confirmed."
                    : "Address validation failures and rerun the test plan.",
                UpdatedAtUtc = UtcNow(),
            };

            ReplaceTodo(state, updatedTodo);
            RefreshPhaseStatus(state, normalizedWorkspacePath, updatedTodo.IterationPhaseId);
            await SaveStateAsync(statePath, state, cancellationToken).ConfigureAwait(false);

            return new RecordTodoValidationResultResult
            {
                TodoId = todoId,
                ValidationState = validationState,
            };
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<LinkTodoToSessionTurnsResult> LinkTodoToSessionTurnsAsync(string workspacePath, string todoId, LinkTodoToSessionTurnsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalizedWorkspacePath = NormalizeWorkspacePath(workspacePath);
        ValidateRequired(todoId, nameof(todoId));

        var statePath = GetStatePath(normalizedWorkspacePath);
        var gate = GetStateLock(statePath);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = await LoadStateAsync(statePath, cancellationToken).ConfigureAwait(false);
            var todo = FindTodo(state, normalizedWorkspacePath, todoId)
                ?? throw new KeyNotFoundException($"Execution TODO '{todoId}' was not found.");

            var sessionTurnIds = MergeDistinct(todo.SessionTurnIds, NormalizeStringList(request.SessionTurnIds));
            ReplaceTodo(state, todo with
            {
                SessionTurnIds = sessionTurnIds,
                Pointers = todo.Pointers with { LastRelevantTurnId = sessionTurnIds.LastOrDefault() ?? todo.Pointers.LastRelevantTurnId },
                UpdatedAtUtc = UtcNow(),
            });

            await SaveStateAsync(statePath, state, cancellationToken).ConfigureAwait(false);
            return new LinkTodoToSessionTurnsResult { TodoId = todoId, SessionTurnIds = sessionTurnIds };
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<AdbStepResult> AdbStepAsync(string workspacePath, AdbStepRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalizedWorkspacePath = NormalizeWorkspacePath(workspacePath);
        var timestamp = UtcNow();
        try
        {
            var deviceSerial = await ResolveDeviceSerialAsync(request.DeviceSerial, normalizedWorkspacePath, cancellationToken).ConfigureAwait(false);
            string? commandSummary = null;

            switch (request.Action)
            {
                case AdbStepAction.Screenshot:
                    commandSummary = BuildAdbCommandSummary(deviceSerial, "exec-out screencap -p");
                    break;
                case AdbStepAction.Tap:
                    if (!request.X.HasValue || !request.Y.HasValue)
                        throw new ArgumentException("Tap actions require x and y coordinates.", nameof(request));
                    commandSummary = BuildAdbCommandSummary(deviceSerial, $"shell input tap {request.X.Value} {request.Y.Value}");
                    await RunAdbAsync(deviceSerial, $"shell input tap {request.X.Value} {request.Y.Value}", normalizedWorkspacePath, cancellationToken).ConfigureAwait(false);
                    break;
                case AdbStepAction.Swipe:
                    if (!request.StartX.HasValue || !request.StartY.HasValue || !request.EndX.HasValue || !request.EndY.HasValue)
                        throw new ArgumentException("Swipe actions require start and end coordinates.", nameof(request));
                    var swipeDuration = Math.Max(request.DurationMs ?? 300, 0);
                    commandSummary = BuildAdbCommandSummary(deviceSerial, $"shell input swipe {request.StartX.Value} {request.StartY.Value} {request.EndX.Value} {request.EndY.Value} {swipeDuration}");
                    await RunAdbAsync(deviceSerial, $"shell input swipe {request.StartX.Value} {request.StartY.Value} {request.EndX.Value} {request.EndY.Value} {swipeDuration}", normalizedWorkspacePath, cancellationToken).ConfigureAwait(false);
                    break;
                case AdbStepAction.Text:
                    if (string.IsNullOrWhiteSpace(request.Text))
                        throw new ArgumentException("Text actions require a text payload.", nameof(request));
                    var escapedText = EscapeAdbInputText(request.Text);
                    commandSummary = BuildAdbCommandSummary(deviceSerial, $"shell input text {escapedText}");
                    await RunAdbAsync(deviceSerial, $"shell input text {escapedText}", normalizedWorkspacePath, cancellationToken).ConfigureAwait(false);
                    break;
                case AdbStepAction.Keyevent:
                    if (string.IsNullOrWhiteSpace(request.KeyEvent))
                        throw new ArgumentException("Keyevent actions require a keyEvent value.", nameof(request));
                    commandSummary = BuildAdbCommandSummary(deviceSerial, $"shell input keyevent {request.KeyEvent.Trim()}");
                    await RunAdbAsync(deviceSerial, $"shell input keyevent {request.KeyEvent.Trim()}", normalizedWorkspacePath, cancellationToken).ConfigureAwait(false);
                    break;
                case AdbStepAction.Wait:
                    var delay = Math.Max(request.WaitMilliseconds ?? request.DurationMs ?? 500, 0);
                    commandSummary = $"wait {delay}ms";
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    break;
                case AdbStepAction.LaunchApp:
                    if (string.IsNullOrWhiteSpace(request.PackageName))
                        throw new ArgumentException("LaunchApp actions require a packageName.", nameof(request));
                    var launchCommand = string.IsNullOrWhiteSpace(request.ActivityName)
                        ? $"shell monkey -p {request.PackageName.Trim()} -c android.intent.category.LAUNCHER 1"
                        : $"shell am start -n {request.PackageName.Trim()}/{request.ActivityName.Trim()}";
                    commandSummary = BuildAdbCommandSummary(deviceSerial, launchCommand);
                    await RunAdbAsync(deviceSerial, launchCommand, normalizedWorkspacePath, cancellationToken).ConfigureAwait(false);
                    break;
                case AdbStepAction.GetFocus:
                    commandSummary = BuildAdbCommandSummary(deviceSerial, "shell dumpsys window windows");
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported ADB action '{request.Action}'.");
            }

            var currentFocus = await GetCurrentFocusAsync(deviceSerial, normalizedWorkspacePath, cancellationToken).ConfigureAwait(false);
            string? screenshotPath = null;
            if (request.CaptureScreenshot || request.Action == AdbStepAction.Screenshot)
                screenshotPath = await CaptureScreenshotAsync(deviceSerial, normalizedWorkspacePath, cancellationToken).ConfigureAwait(false);

            var hints = new List<string>();
            if (!string.IsNullOrWhiteSpace(request.Instruction))
                hints.Add(request.Instruction.Trim());
            if (!string.IsNullOrWhiteSpace(currentFocus))
                hints.Add($"Current focus: {currentFocus}");

            return new AdbStepResult
            {
                Success = true,
                Action = request.Action,
                DeviceSerial = deviceSerial,
                CommandSummary = commandSummary,
                ScreenshotPath = screenshotPath,
                ScreenshotBase64 = null,
                CurrentFocus = currentFocus,
                ObservationHints = hints,
                Error = null,
                TimestampUtc = timestamp,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ADB step {Action} failed for workspace {WorkspacePath}.", request.Action, normalizedWorkspacePath);
            return new AdbStepResult
            {
                Success = false,
                Action = request.Action,
                DeviceSerial = request.DeviceSerial,
                CommandSummary = null,
                ScreenshotPath = null,
                ScreenshotBase64 = null,
                CurrentFocus = null,
                ObservationHints = string.IsNullOrWhiteSpace(request.Instruction) ? [] : [request.Instruction.Trim()],
                Error = ex.Message,
                TimestampUtc = timestamp,
            };
        }
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static SemaphoreSlim GetStateLock(string statePath)
        => s_stateLocks.GetOrAdd(statePath, static _ => new SemaphoreSlim(1, 1));

    private static string NormalizeWorkspacePath(string workspacePath)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
            throw new ArgumentException("workspacePath is required.", nameof(workspacePath));

        return Path.GetFullPath(workspacePath.Trim());
    }

    private static void ValidateRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} is required.", paramName);
    }

    private static string UtcNow() => DateTimeOffset.UtcNow.ToString("O");

    private static string GetStatePath(string workspacePath)
        => Path.Combine(workspacePath, ".mcpServer", "todo-execution-state.json");

    private static string BuildPhaseSectionName(string phaseName)
    {
        var normalized = s_nonAlphaNumericRegex.Replace(phaseName.Trim().ToLowerInvariant(), "-").Trim('-');
        if (string.IsNullOrWhiteSpace(normalized))
            normalized = "byrd-execution";

        return $"byrd-{normalized}";
    }

    private static string MapLegacyPriority(TodoExecutionPriority priority)
        => priority switch
        {
            TodoExecutionPriority.Low => "low",
            TodoExecutionPriority.Medium => "medium",
            TodoExecutionPriority.High => "high",
            TodoExecutionPriority.Critical => "high",
            _ => "medium",
        };

    private static List<string> NormalizeStringList(IReadOnlyList<string>? values)
        => values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
           ?? [];

    private static List<string> MergeDistinct(IReadOnlyList<string>? left, IReadOnlyList<string>? right)
    {
        var merged = new List<string>();
        foreach (var value in left ?? [])
        {
            if (!string.IsNullOrWhiteSpace(value) && !merged.Contains(value, StringComparer.OrdinalIgnoreCase))
                merged.Add(value);
        }

        foreach (var value in right ?? [])
        {
            if (!string.IsNullOrWhiteSpace(value) && !merged.Contains(value, StringComparer.OrdinalIgnoreCase))
                merged.Add(value);
        }

        return merged;
    }

    private static TodoIterationPhase? FindPhase(TodoExecutionStateDocument state, string workspacePath, string phaseId)
        => state.Phases.FirstOrDefault(phase =>
            string.Equals(phase.WorkspacePath, workspacePath, StringComparison.OrdinalIgnoreCase)
            && string.Equals(phase.PhaseId, phaseId, StringComparison.OrdinalIgnoreCase));

    private static TodoExecutionRecord? FindTodo(TodoExecutionStateDocument state, string workspacePath, string todoId)
        => state.Todos.FirstOrDefault(todo =>
            string.Equals(todo.WorkspacePath, workspacePath, StringComparison.OrdinalIgnoreCase)
            && string.Equals(todo.TodoId, todoId, StringComparison.OrdinalIgnoreCase));

    private static void ReplacePhase(TodoExecutionStateDocument state, TodoIterationPhase updatedPhase)
    {
        var index = state.Phases.FindIndex(phase => string.Equals(phase.PhaseId, updatedPhase.PhaseId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(phase.WorkspacePath, updatedPhase.WorkspacePath, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
            state.Phases[index] = updatedPhase;
    }

    private static void ReplaceTodo(TodoExecutionStateDocument state, TodoExecutionRecord updatedTodo)
    {
        var index = state.Todos.FindIndex(todo => string.Equals(todo.TodoId, updatedTodo.TodoId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(todo.WorkspacePath, updatedTodo.WorkspacePath, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
            state.Todos[index] = updatedTodo;
    }

    private static List<TodoCheckpoint> GetCheckpointsForTodo(TodoExecutionStateDocument state, string workspacePath, string todoId)
        => state.Checkpoints
            .Where(checkpoint =>
                string.Equals(checkpoint.WorkspacePath, workspacePath, StringComparison.OrdinalIgnoreCase)
                && string.Equals(checkpoint.TodoId, todoId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(checkpoint => checkpoint.CreatedAtUtc, StringComparer.Ordinal)
            .ToList();

    private static TodoExecutionRecord ApplyCheckpointToTodo(TodoExecutionRecord todo, TodoCheckpoint checkpoint)
    {
        var sessionTurnIds = MergeDistinct(todo.SessionTurnIds, checkpoint.SessionTurnIds);
        var artifactIds = MergeDistinct(todo.ArtifactIds, checkpoint.ArtifactIds);
        var pointers = todo.Pointers with
        {
            LastCheckpointId = checkpoint.CheckpointId,
            LastRelevantTurnId = checkpoint.SessionTurnIds.LastOrDefault() ?? todo.Pointers.LastRelevantTurnId,
            LastSuccessfulTurnId = checkpoint.Kind == TodoCheckpointKind.ValidationPassed
                ? checkpoint.SessionTurnIds.LastOrDefault() ?? todo.Pointers.LastSuccessfulTurnId
                : todo.Pointers.LastSuccessfulTurnId,
            LastFailedTurnId = checkpoint.Kind is TodoCheckpointKind.ValidationFailed or TodoCheckpointKind.Blocker
                ? checkpoint.SessionTurnIds.LastOrDefault() ?? todo.Pointers.LastFailedTurnId
                : todo.Pointers.LastFailedTurnId,
            LastCommitSha = checkpoint.CommitShas.LastOrDefault() ?? todo.Pointers.LastCommitSha,
            LastScreenshotArtifactId = checkpoint.ArtifactIds.LastOrDefault(static artifact => artifact.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                ?? todo.Pointers.LastScreenshotArtifactId,
        };

        return todo with
        {
            SessionTurnIds = sessionTurnIds,
            ArtifactIds = artifactIds,
            RequirementIds = MergeDistinct(todo.RequirementIds, checkpoint.RequirementIds),
            NextAction = string.IsNullOrWhiteSpace(checkpoint.NextAction) ? todo.NextAction : checkpoint.NextAction,
            Pointers = pointers,
            UpdatedAtUtc = UtcNow(),
        };
    }

    private static TodoExecutionStatus[] s_terminalStatuses => [TodoExecutionStatus.Complete, TodoExecutionStatus.Cancelled];

    private static ActiveTodoResult? SelectNextReadyTodo(TodoExecutionStateDocument state, string workspacePath)
    {
        var activePhaseIds = state.Phases
            .Where(phase =>
                string.Equals(phase.WorkspacePath, workspacePath, StringComparison.OrdinalIgnoreCase)
                && phase.Status is not TodoIterationPhaseStatus.Cancelled and not TodoIterationPhaseStatus.Complete)
            .Select(phase => phase.PhaseId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var todos = state.Todos
            .Where(todo =>
                string.Equals(todo.WorkspacePath, workspacePath, StringComparison.OrdinalIgnoreCase)
                && !s_terminalStatuses.Contains(todo.Status)
                && todo.Status != TodoExecutionStatus.Blocked
                && (string.IsNullOrWhiteSpace(todo.IterationPhaseId) || activePhaseIds.Contains(todo.IterationPhaseId)))
            .Where(todo => DependenciesSatisfied(state, workspacePath, todo))
            .OrderBy(todo => GetStatusOrder(todo.Status))
            .ThenByDescending(todo => todo.Priority)
            .ThenBy(todo => todo.CreatedAtUtc, StringComparer.Ordinal)
            .FirstOrDefault();

        return todos is null
            ? null
            : new ActiveTodoResult
            {
                TodoId = todos.TodoId,
                Title = todos.Title,
                Status = todos.Status,
                NextAction = DetermineNextAction(todos),
            };
    }

    private static bool DependenciesSatisfied(TodoExecutionStateDocument state, string workspacePath, TodoExecutionRecord todo)
    {
        foreach (var dependency in todo.DependsOn)
        {
            var dependencyTodo = FindTodo(state, workspacePath, dependency.TodoId);
            if (dependencyTodo is null || dependencyTodo.Status != TodoExecutionStatus.Complete)
                return false;
        }

        return true;
    }

    private static int GetStatusOrder(TodoExecutionStatus status)
        => status switch
        {
            TodoExecutionStatus.Implementing => 0,
            TodoExecutionStatus.Validating => 1,
            TodoExecutionStatus.TestReady => 2,
            TodoExecutionStatus.TestDesign => 3,
            TodoExecutionStatus.Planned => 4,
            TodoExecutionStatus.Draft => 5,
            _ => 10,
        };

    private static string DetermineNextAction(TodoExecutionRecord todo)
        => todo.Status switch
        {
            TodoExecutionStatus.Draft => todo.NextAction ?? $"Plan {todo.Title}",
            TodoExecutionStatus.Planned => todo.NextAction ?? (todo.TestPlan.UnitTestsDefined ? $"Implement {todo.Title}" : $"Define unit tests for {todo.Title}"),
            TodoExecutionStatus.TestDesign => todo.NextAction ?? $"Define unit tests for {todo.Title}",
            TodoExecutionStatus.TestReady => todo.NextAction ?? $"Implement {todo.Title}",
            TodoExecutionStatus.Implementing => todo.NextAction ?? $"Continue implementation for {todo.Title}",
            TodoExecutionStatus.Validating => todo.NextAction ?? $"Validate {todo.Title} and record the result",
            TodoExecutionStatus.Blocked => todo.NextAction ?? $"Resolve blockers for {todo.Title}",
            TodoExecutionStatus.Complete => "No action required.",
            TodoExecutionStatus.Cancelled => "No action required.",
            _ => todo.NextAction ?? $"Continue {todo.Title}",
        };

    private static void ValidateTransition(TodoExecutionStateDocument state, TodoExecutionRecord todo, TodoExecutionStatus targetStatus, string? reason)
    {
        if (todo.Status == targetStatus)
            return;

        if (todo.Status == TodoExecutionStatus.Complete
            && targetStatus is not TodoExecutionStatus.Planned and not TodoExecutionStatus.TestDesign
            && targetStatus != TodoExecutionStatus.Complete)
        {
            throw new InvalidOperationException("Complete TODOs can only be reopened to Planned or TestDesign.");
        }

        if (targetStatus == TodoExecutionStatus.Implementing && !todo.TestPlan.UnitTestsDefined)
            throw new InvalidOperationException("Implementation cannot start before unit tests are defined.");

        if (todo.Status == TodoExecutionStatus.TestDesign && targetStatus == TodoExecutionStatus.TestReady && !todo.TestPlan.UnitTestsDefined)
            throw new InvalidOperationException("TestDesign TODOs can only move to TestReady after unit tests are defined.");

        if (todo.Status == TodoExecutionStatus.Implementing && targetStatus == TodoExecutionStatus.Validating)
        {
            var hasImplementationEvidence = state.Checkpoints.Any(checkpoint =>
                string.Equals(checkpoint.WorkspacePath, todo.WorkspacePath, StringComparison.OrdinalIgnoreCase)
                && string.Equals(checkpoint.TodoId, todo.TodoId, StringComparison.OrdinalIgnoreCase)
                && checkpoint.Kind is TodoCheckpointKind.ImplementationProgress or TodoCheckpointKind.CommitCreated or TodoCheckpointKind.TestPassing);

            if (!hasImplementationEvidence)
                throw new InvalidOperationException("Implementing TODOs require implementation evidence before validation can begin.");
        }

        if (todo.Status == TodoExecutionStatus.Validating && targetStatus == TodoExecutionStatus.Complete)
        {
            if (!todo.TestPlan.UnitTestsPassing)
                throw new InvalidOperationException("TODOs cannot complete until unit tests are passing.");
            if (todo.TestPlan.IntegrationTestsDefined && !todo.TestPlan.IntegrationTestsPassing)
                throw new InvalidOperationException("TODOs cannot complete until required integration tests are passing.");
            if (!string.Equals(todo.Validation.LastResult, "pass", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("TODOs cannot complete until validation passes.");
            if (todo.AcceptanceCriteria.Any(static criterion => !criterion.IsSatisfied))
                throw new InvalidOperationException("TODOs cannot complete until all acceptance criteria are satisfied or explicitly waived.");
        }

        if (targetStatus == TodoExecutionStatus.Blocked)
            return;

        if (todo.Status == TodoExecutionStatus.Blocked
            && targetStatus is TodoExecutionStatus.Planned or TodoExecutionStatus.TestDesign or TodoExecutionStatus.Implementing or TodoExecutionStatus.Validating
            && string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("Blocked TODOs require an explicit reason before resuming work.");
        }
    }

    private static void RefreshPhaseStatus(TodoExecutionStateDocument state, string workspacePath, string? phaseId)
    {
        if (string.IsNullOrWhiteSpace(phaseId))
            return;

        var phase = FindPhase(state, workspacePath, phaseId);
        if (phase is null)
            return;

        var phaseTodos = state.Todos
            .Where(todo => string.Equals(todo.WorkspacePath, workspacePath, StringComparison.OrdinalIgnoreCase)
                && string.Equals(todo.IterationPhaseId, phaseId, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (phaseTodos.Count == 0)
            return;

        TodoIterationPhaseStatus status;
        if (phaseTodos.All(todo => todo.Status is TodoExecutionStatus.Complete or TodoExecutionStatus.Cancelled))
            status = TodoIterationPhaseStatus.Complete;
        else if (phaseTodos.All(todo => todo.Status == TodoExecutionStatus.Blocked))
            status = TodoIterationPhaseStatus.Blocked;
        else if (phaseTodos.Any(todo => todo.Status == TodoExecutionStatus.Validating))
            status = TodoIterationPhaseStatus.Validating;
        else if (phaseTodos.Any(todo => todo.Status is TodoExecutionStatus.Implementing or TodoExecutionStatus.TestReady or TodoExecutionStatus.TestDesign))
            status = TodoIterationPhaseStatus.Implementing;
        else
            status = TodoIterationPhaseStatus.Planning;

        ReplacePhase(state, phase with { Status = status, UpdatedAtUtc = UtcNow() });
    }

    private async Task<TodoExecutionStateDocument> LoadWorkspaceStateAsync(string workspacePath, CancellationToken cancellationToken)
    {
        var statePath = GetStatePath(workspacePath);
        var gate = GetStateLock(statePath);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await LoadStateAsync(statePath, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    private static async Task<TodoExecutionStateDocument> LoadStateAsync(string statePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(statePath))
            return new TodoExecutionStateDocument();

        await using var stream = File.OpenRead(statePath);
        var state = await JsonSerializer.DeserializeAsync<TodoExecutionStateDocument>(stream, s_stateJsonOptions, cancellationToken).ConfigureAwait(false);
        return state ?? new TodoExecutionStateDocument();
    }

    private static async Task SaveStateAsync(string statePath, TodoExecutionStateDocument state, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(statePath)!);
        await using var stream = File.Create(statePath);
        await JsonSerializer.SerializeAsync(stream, state, s_stateJsonOptions, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<ITodoService> ResolveTodoServiceAsync(string workspacePath, CancellationToken cancellationToken)
    {
        var workspace = await _workspaceService.GetAsync(workspacePath, cancellationToken).ConfigureAwait(false);
        var context = workspace is null
            ? new WorkspaceContext
            {
                WorkspacePath = workspacePath,
                DataDirectory = workspacePath,
                TodoFilePath = "docs/Project/TODO.yaml",
            }
            : new WorkspaceContext
            {
                WorkspacePath = workspace.WorkspacePath,
                WorkspaceName = workspace.Name,
                DataDirectory = workspace.DataDirectory,
                TodoFilePath = workspace.TodoPath,
            };

        return _todoServiceResolver.Resolve(context);
    }

    private async Task<string> GenerateNextTodoIdAsync(TodoExecutionStateDocument state, ITodoService todoService, CancellationToken cancellationToken)
    {
        while (true)
        {
            var todoId = $"{GeneratedTodoIdPrefix}{state.NextTodoNumber++:D3}";
            if (state.Todos.Any(todo => string.Equals(todo.TodoId, todoId, StringComparison.OrdinalIgnoreCase)))
                continue;

            var existing = await todoService.GetByIdAsync(todoId, cancellationToken).ConfigureAwait(false);
            if (existing is null)
                return todoId;
        }
    }

    private async Task<IReadOnlyList<string>> GetRequirementSnippetsAsync(IReadOnlyList<string> requirementIds, int limit, CancellationToken cancellationToken)
    {
        var snippets = new List<string>();
        foreach (var requirementId in requirementIds.Where(static id => !string.IsNullOrWhiteSpace(id)).Take(Math.Max(limit, 0)))
        {
            var snippet = await GetRequirementSnippetAsync(requirementId, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(snippet))
                snippets.Add(snippet);
        }

        return snippets;
    }

    private async Task<string?> GetRequirementSnippetAsync(string requirementId, CancellationToken cancellationToken)
    {
        if (requirementId.StartsWith("FR-", StringComparison.OrdinalIgnoreCase))
        {
            var fr = await _requirementsDocumentService.GetFrAsync(requirementId, cancellationToken).ConfigureAwait(false);
            return fr is null ? null : BuildRequirementSnippet(fr.Id, fr.Title, fr.Body);
        }

        if (requirementId.StartsWith("TR-", StringComparison.OrdinalIgnoreCase))
        {
            var tr = await _requirementsDocumentService.GetTrAsync(requirementId, cancellationToken).ConfigureAwait(false);
            return tr is null ? null : BuildRequirementSnippet(tr.Id, string.IsNullOrWhiteSpace(tr.Title) ? tr.Id : tr.Title, tr.Body);
        }

        if (requirementId.StartsWith("TEST-", StringComparison.OrdinalIgnoreCase))
        {
            var test = await _requirementsDocumentService.GetTestAsync(requirementId, cancellationToken).ConfigureAwait(false);
            return test is null ? null : BuildRequirementSnippet(test.Id, test.Condition, test.Condition);
        }

        return null;
    }

    private static string BuildRequirementSnippet(string id, string title, string body)
    {
        var snippet = string.IsNullOrWhiteSpace(body) ? title : body;
        snippet = snippet.ReplaceLineEndings(" ").Trim();
        if (snippet.Length > 180)
            snippet = snippet[..177] + "...";

        return $"{id}: {snippet}";
    }

    private async Task<IReadOnlyList<string>> GetTurnSummariesAsync(string workspacePath, IReadOnlyList<string> requestIds, int limit, CancellationToken cancellationToken)
    {
        if (requestIds.Count == 0 || limit <= 0)
            return [];

        _db.OverrideWorkspaceId(workspacePath);
        var requestIdSet = requestIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // BUG-SESSIONLOG-WS-001..004: child sets carry no workspace query filter,
        // so isolation comes from the explicit parent-session predicate here.
        var turns = await _db.SessionLogTurns
            .AsNoTracking()
            .Where(turn => turn.RequestId != null
                && requestIdSet.Contains(turn.RequestId)
                && turn.SessionLog!.WorkspaceId == workspacePath)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return turns
            .OrderByDescending(turn => turn.Timestamp)
            .Take(limit)
            .Select(BuildTurnSummary)
            .ToList();
    }

    private async Task<IReadOnlyList<string>> GetFilesModifiedAsync(string workspacePath, IReadOnlyList<string> requestIds, int limit, CancellationToken cancellationToken)
    {
        if (requestIds.Count == 0 || limit <= 0)
            return [];

        _db.OverrideWorkspaceId(workspacePath);
        var requestIdSet = requestIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // BUG-SESSIONLOG-WS-001..004: child sets carry no workspace query filter,
        // so isolation comes from the explicit parent-session predicate here.
        var items = await _db.SessionLogTurnStringLists
            .AsNoTracking()
            .Where(item => item.ListType == "filesModified"
                && item.SessionLogTurn != null
                && item.SessionLogTurn.RequestId != null
                && requestIdSet.Contains(item.SessionLogTurn.RequestId)
                && item.SessionLogTurn.SessionLog!.WorkspaceId == workspacePath)
            .Select(item => new { item.SessionLogTurn!.Timestamp, item.Value })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return items
            .OrderByDescending(item => item.Timestamp)
            .Take(limit * 4)
            .Select(item => item.Value)
            .Where(static file => !string.IsNullOrWhiteSpace(file))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();
    }

    private static string BuildTurnSummary(McpServer.Support.Mcp.Storage.Entities.SessionLogTurnEntity turn)
    {
        var summary = !string.IsNullOrWhiteSpace(turn.QueryTitle)
            ? turn.QueryTitle!
            : !string.IsNullOrWhiteSpace(turn.Interpretation)
                ? turn.Interpretation!
                : !string.IsNullOrWhiteSpace(turn.Response)
                    ? turn.Response!
                    : turn.QueryText ?? string.Empty;
        summary = summary.ReplaceLineEndings(" ").Trim();
        if (summary.Length > 160)
            summary = summary[..157] + "...";

        return summary;
    }

    private async Task<string> ResolveDeviceSerialAsync(string? requestedSerial, string workspacePath, CancellationToken cancellationToken)
    {
        var devicesResult = await RunAdbAsync(requestedSerial, "devices", workspacePath, cancellationToken, allowRequestedSerialOverride: false).ConfigureAwait(false);
        var devices = ParseDeviceList(devicesResult.Stdout);

        if (!string.IsNullOrWhiteSpace(requestedSerial))
        {
            if (!devices.Contains(requestedSerial.Trim(), StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException($"ADB device '{requestedSerial}' is not connected.");

            return requestedSerial.Trim();
        }

        if (devices.Count == 0)
            throw new InvalidOperationException("No connected ADB devices were found.");
        if (devices.Count > 1)
            throw new InvalidOperationException("Multiple ADB devices are connected. Specify deviceSerial explicitly.");

        return devices[0];
    }

    private async Task<string?> GetCurrentFocusAsync(string deviceSerial, string workspacePath, CancellationToken cancellationToken)
    {
        var result = await RunAdbAsync(deviceSerial, "shell dumpsys window windows", workspacePath, cancellationToken).ConfigureAwait(false);
        var focusDump = (result.Stdout ?? string.Empty) + Environment.NewLine + (result.Stderr ?? string.Empty);

        var currentFocusMatch = s_currentFocusRegex.Match(focusDump);
        if (currentFocusMatch.Success)
            return currentFocusMatch.Groups["focus"].Value;

        var focusedAppMatch = s_focusedAppRegex.Match(focusDump);
        if (focusedAppMatch.Success)
            return focusedAppMatch.Groups["focus"].Value;

        return null;
    }

    private async Task<string> CaptureScreenshotAsync(string deviceSerial, string workspacePath, CancellationToken cancellationToken)
    {
        var artifactDirectory = Path.Combine(workspacePath, ".mcpServer", "artifacts", "device");
        Directory.CreateDirectory(artifactDirectory);

        var fileName = $"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmssfff}.png";
        var absolutePath = Path.Combine(artifactDirectory, fileName);
        var remotePath = "/sdcard/mcpserver-screen.png";

        await RunAdbAsync(deviceSerial, $"shell screencap -p {remotePath}", workspacePath, cancellationToken).ConfigureAwait(false);
        await RunAdbAsync(deviceSerial, $"pull {remotePath} {QuoteArgument(absolutePath)}", workspacePath, cancellationToken).ConfigureAwait(false);
        await RunAdbAsync(deviceSerial, $"shell rm {remotePath}", workspacePath, cancellationToken).ConfigureAwait(false);

        return Path.GetRelativePath(workspacePath, absolutePath).Replace(Path.DirectorySeparatorChar, '/');
    }

    private async Task<ProcessRunResult> RunAdbAsync(
        string? deviceSerial,
        string commandArguments,
        string workspacePath,
        CancellationToken cancellationToken,
        bool allowRequestedSerialOverride = true)
    {
        var prefix = allowRequestedSerialOverride && !string.IsNullOrWhiteSpace(deviceSerial)
            ? $"-s {deviceSerial.Trim()} "
            : string.Empty;
        var arguments = prefix + commandArguments;
        var result = await _processRunner.RunAsync(new ProcessRunRequest("adb", arguments, WorkingDirectory: workspacePath), cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            var error = string.IsNullOrWhiteSpace(result.Stderr) ? result.Stdout : result.Stderr;
            throw new InvalidOperationException(error ?? $"ADB command failed: adb {arguments}");
        }

        return result;
    }

    private static List<string> ParseDeviceList(string? output)
    {
        var devices = new List<string>();
        foreach (var line in (output ?? string.Empty).ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith("List of devices attached", StringComparison.OrdinalIgnoreCase))
                continue;

            var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 2 && string.Equals(parts[1], "device", StringComparison.OrdinalIgnoreCase))
                devices.Add(parts[0]);
        }

        return devices;
    }

    private static string EscapeAdbInputText(string text)
        => QuoteShellSafe(text.Trim().Replace(" ", "%s", StringComparison.Ordinal));

    private static string QuoteShellSafe(string value)
        => value.Replace("\"", string.Empty, StringComparison.Ordinal)
            .Replace("'", string.Empty, StringComparison.Ordinal)
            .Replace("&", string.Empty, StringComparison.Ordinal);

    private static string QuoteArgument(string value)
        => $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";

    private static string BuildAdbCommandSummary(string deviceSerial, string command)
        => $"adb -s {deviceSerial} {command}";

    private sealed class TodoExecutionStateDocument
    {
        /// <summary>Next phase number.</summary>
        public int NextPhaseNumber { get; set; } = 1;

        /// <summary>Next TODO number.</summary>
        public int NextTodoNumber { get; set; } = 1;

        /// <summary>Next checkpoint number.</summary>
        public int NextCheckpointNumber { get; set; } = 1;

        /// <summary>Stored phases.</summary>
        public List<TodoIterationPhase> Phases { get; set; } = [];

        /// <summary>Stored execution TODOs.</summary>
        public List<TodoExecutionRecord> Todos { get; set; } = [];

        /// <summary>Stored checkpoints.</summary>
        public List<TodoCheckpoint> Checkpoints { get; set; } = [];
    }
}
