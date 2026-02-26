using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for loading, editing, creating, and deleting TODO item details.
/// </summary>
[ViewModelCommand("get-todo", Description = "Get TODO item detail")]
public sealed partial class TodoDetailViewModel : AreaDetailViewModelBase<TodoDetail>
{
    private readonly CqrsQueryCommand<TodoDetail?> _loadCommand;
    private readonly CqrsRelayCommand<TodoMutationOutcome> _createCommand;
    private readonly CqrsRelayCommand<TodoMutationOutcome> _updateCommand;
    private readonly CqrsRelayCommand<TodoMutationOutcome> _deleteCommand;
    private readonly CqrsRelayCommand<TodoRequirementsAnalysis> _analyzeRequirementsCommand;
    private readonly CqrsQueryCommand<TodoPromptOutput> _statusPromptCommand;
    private readonly CqrsQueryCommand<TodoPromptOutput> _implementPromptCommand;
    private readonly CqrsQueryCommand<TodoPromptOutput> _planPromptCommand;
    private readonly ILogger<TodoDetailViewModel> _logger;


    /// <summary>Initializes a new instance of the TODO detail ViewModel.</summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="logger">Logger instance.</param>
    public TodoDetailViewModel(Dispatcher dispatcher,
        ILogger<TodoDetailViewModel> logger)
        : base(McpArea.Todo)
    {
        _logger = logger;
        _loadCommand = new CqrsQueryCommand<TodoDetail?>(dispatcher, BuildQuery);
        _createCommand = new CqrsRelayCommand<TodoMutationOutcome>(dispatcher, BuildCreateCommand);
        _updateCommand = new CqrsRelayCommand<TodoMutationOutcome>(dispatcher, BuildUpdateCommand);
        _deleteCommand = new CqrsRelayCommand<TodoMutationOutcome>(dispatcher, BuildDeleteCommand);
        _analyzeRequirementsCommand = new CqrsRelayCommand<TodoRequirementsAnalysis>(dispatcher, BuildAnalyzeRequirementsCommand);
        _statusPromptCommand = new CqrsQueryCommand<TodoPromptOutput>(dispatcher, BuildStatusPromptQuery);
        _implementPromptCommand = new CqrsQueryCommand<TodoPromptOutput>(dispatcher, BuildImplementPromptQuery);
        _planPromptCommand = new CqrsQueryCommand<TodoPromptOutput>(dispatcher, BuildPlanPromptQuery);
    }

    /// <summary>TODO item ID to load.</summary>
    [ObservableProperty]
    private string _todoId = string.Empty;

    /// <summary>Editor item ID (used for create/update/delete).</summary>
    [ObservableProperty]
    private string _editorId = string.Empty;

    /// <summary>Editor title.</summary>
    [ObservableProperty]
    private string _editorTitle = string.Empty;

    /// <summary>Editor section.</summary>
    [ObservableProperty]
    private string _editorSection = "mvp-mcp";

    /// <summary>Editor priority.</summary>
    [ObservableProperty]
    private string _editorPriority = "medium";

    /// <summary>Editor done flag.</summary>
    [ObservableProperty]
    private bool _editorDone;

    /// <summary>Editor estimate string.</summary>
    [ObservableProperty]
    private string? _editorEstimate;

    /// <summary>Editor note string.</summary>
    [ObservableProperty]
    private string? _editorNote;

    /// <summary>Editor description lines as multi-line text.</summary>
    [ObservableProperty]
    private string? _editorDescriptionText;

    /// <summary>Editor technical details as multi-line text.</summary>
    [ObservableProperty]
    private string? _editorTechnicalDetailsText;

    /// <summary>Editor implementation tasks as multi-line text.</summary>
    [ObservableProperty]
    private string? _editorImplementationTasksText;

    /// <summary>Whether the editor is currently a new-item draft.</summary>
    [ObservableProperty]
    private bool _isNewDraft = true;

    /// <summary>Last mutation status message for create/update/delete.</summary>
    [ObservableProperty]
    private string? _mutationMessage;

    /// <summary>Last requirements analysis result.</summary>
    [ObservableProperty]
    private TodoRequirementsAnalysis? _requirementsAnalysis;

    /// <summary>Last aggregated prompt output (status/implement/plan).</summary>
    [ObservableProperty]
    private TodoPromptOutput? _promptOutput;

    /// <summary>Load command (also primary command for exec).</summary>
    public IAsyncRelayCommand LoadCommand => _loadCommand;

    /// <summary>Create command for TODO create handler.</summary>
    public IAsyncRelayCommand CreateCommand => _createCommand;

    /// <summary>Save command for TODO update handler.</summary>
    public IAsyncRelayCommand SaveCommand => _updateCommand;

    /// <summary>Delete command for TODO delete handler.</summary>
    public IAsyncRelayCommand DeleteCommand => _deleteCommand;

    /// <summary>Analyze requirements command for TODO requirements handler.</summary>
    public IAsyncRelayCommand AnalyzeRequirementsCommand => _analyzeRequirementsCommand;

    /// <summary>Generate status prompt command.</summary>
    public IAsyncRelayCommand GenerateStatusPromptCommand => _statusPromptCommand;

    /// <summary>Generate implement prompt command.</summary>
    public IAsyncRelayCommand GenerateImplementPromptCommand => _implementPromptCommand;

    /// <summary>Generate plan prompt command.</summary>
    public IAsyncRelayCommand GeneratePlanPromptCommand => _planPromptCommand;

    /// <summary>Primary command alias for registry execution.</summary>
    public IAsyncRelayCommand PrimaryCommand => LoadCommand;

    /// <summary>Last query result.</summary>
    public Result<TodoDetail?>? LastResult => _loadCommand.LastResult;

    /// <summary>Last create result.</summary>
    public Result<TodoMutationOutcome>? LastCreateResult => _createCommand.LastResult;

    /// <summary>Last update result.</summary>
    public Result<TodoMutationOutcome>? LastUpdateResult => _updateCommand.LastResult;

    /// <summary>Last delete result.</summary>
    public Result<TodoMutationOutcome>? LastDeleteResult => _deleteCommand.LastResult;

    /// <summary>Last requirements-analysis result.</summary>
    public Result<TodoRequirementsAnalysis>? LastRequirementsResult => _analyzeRequirementsCommand.LastResult;

    /// <summary>Last status-prompt result.</summary>
    public Result<TodoPromptOutput>? LastStatusPromptResult => _statusPromptCommand.LastResult;

    /// <summary>Last implement-prompt result.</summary>
    public Result<TodoPromptOutput>? LastImplementPromptResult => _implementPromptCommand.LastResult;

    /// <summary>Last plan-prompt result.</summary>
    public Result<TodoPromptOutput>? LastPlanPromptResult => _planPromptCommand.LastResult;

    /// <summary>Begins a new TODO draft in the editor.</summary>
    /// <param name="defaultSection">Optional default section.</param>
    public void BeginNewDraft(string? defaultSection = null)
    {
        IsNewDraft = true;
        MutationMessage = null;
        ErrorMessage = null;
        Detail = null;
        EditorId = string.Empty;
        EditorTitle = string.Empty;
        EditorSection = Normalize(defaultSection) ?? EditorSection;
        EditorPriority = "medium";
        EditorDone = false;
        EditorEstimate = null;
        EditorNote = null;
        EditorDescriptionText = null;
        EditorTechnicalDetailsText = null;
        EditorImplementationTasksText = null;
        RequirementsAnalysis = null;
        PromptOutput = null;
        IsDirty = true;
        StatusMessage = "New TODO draft.";
    }

    /// <summary>Loads the TODO detail.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        ErrorMessage = null;
        MutationMessage = null;
        StatusMessage = "Loading TODO detail...";

        try
        {
            var result = await _loadCommand.DispatchAsync(ct).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                Detail = null;
                ErrorMessage = result.Error ?? "Unknown error loading TODO detail.";
                StatusMessage = "TODO detail load failed.";
                return;
            }

            Detail = result.Value;
            if (result.Value is not null)
            {
                ApplyDetailToEditor(result.Value);
                IsNewDraft = false;
                IsDirty = false;
            }

            LastUpdatedAt = DateTimeOffset.UtcNow;
            StatusMessage = result.Value is null
                ? "TODO item not found."
                : $"Loaded TODO detail for {result.Value.Id}.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            Detail = null;
            ErrorMessage = ex.Message;
            StatusMessage = "TODO detail load failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Creates a TODO item from the current editor fields.</summary>
    public async Task CreateAsync(CancellationToken ct = default)
    {
        await RunMutationAsync(_createCommand, "Creating TODO...", "TODO created.", ct).ConfigureAwait(false);
    }

    /// <summary>Updates the current TODO item from the editor fields.</summary>
    public async Task SaveAsync(CancellationToken ct = default)
    {
        if (IsNewDraft)
        {
            await CreateAsync(ct).ConfigureAwait(false);
            return;
        }

        await RunMutationAsync(_updateCommand, "Saving TODO...", "TODO saved.", ct).ConfigureAwait(false);
    }

    /// <summary>Deletes the current TODO item.</summary>
    public async Task DeleteAsync(CancellationToken ct = default)
    {
        await RunMutationAsync(_deleteCommand, "Deleting TODO...", "TODO deleted.", ct, clearOnDelete: true).ConfigureAwait(false);
    }

    /// <summary>Runs requirements analysis for the active TODO item.</summary>
    public async Task AnalyzeRequirementsAsync(CancellationToken ct = default)
    {
        await RunRequirementsAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Generates a status prompt for the active TODO item.</summary>
    public Task GenerateStatusPromptAsync(CancellationToken ct = default)
        => RunPromptAsync(_statusPromptCommand, "Generating status prompt...", ct);

    /// <summary>Generates an implementation prompt for the active TODO item.</summary>
    public Task GenerateImplementPromptAsync(CancellationToken ct = default)
        => RunPromptAsync(_implementPromptCommand, "Generating implement prompt...", ct);

    /// <summary>Generates a plan prompt for the active TODO item.</summary>
    public Task GeneratePlanPromptAsync(CancellationToken ct = default)
        => RunPromptAsync(_planPromptCommand, "Generating plan prompt...", ct);

    private async Task RunMutationAsync(
        CqrsRelayCommand<TodoMutationOutcome> command,
        string busyMessage,
        string successMessage,
        CancellationToken ct,
        bool clearOnDelete = false)
    {
        IsBusy = true;
        ErrorMessage = null;
        MutationMessage = null;
        StatusMessage = busyMessage;

        try
        {
            var result = await command.DispatchAsync(ct).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Unknown TODO mutation error.";
                StatusMessage = "TODO mutation failed.";
                return;
            }

            if (result.Value is null || !result.Value.Success)
            {
                ErrorMessage = result.Value?.Error ?? "TODO mutation failed.";
                StatusMessage = "TODO mutation failed.";
                return;
            }

            if (clearOnDelete)
            {
                var deletedId = EditorId;
                BeginNewDraft(EditorSection);
                MutationMessage = $"{successMessage} ({deletedId})";
                StatusMessage = MutationMessage;
                LastUpdatedAt = DateTimeOffset.UtcNow;
                return;
            }

            if (result.Value.Item is not null)
            {
                Detail = result.Value.Item;
                ApplyDetailToEditor(result.Value.Item);
                TodoId = result.Value.Item.Id;
                IsNewDraft = false;
                IsDirty = false;
                LastUpdatedAt = DateTimeOffset.UtcNow;
            }

            MutationMessage = successMessage;
            StatusMessage = successMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "TODO mutation failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RunRequirementsAsync(CancellationToken ct)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = "Analyzing TODO requirements...";

        try
        {
            var result = await _analyzeRequirementsCommand.DispatchAsync(ct).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "TODO requirements analysis failed.";
                StatusMessage = "TODO requirements analysis failed.";
                return;
            }

            RequirementsAnalysis = result.Value;
            if (result.Value is not null && result.Value.Success)
            {
                StatusMessage = $"Requirements analysis completed for {GetActiveTodoId()}.";
                LastUpdatedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                ErrorMessage = result.Value?.Error ?? "TODO requirements analysis failed.";
                StatusMessage = "TODO requirements analysis failed.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "TODO requirements analysis failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RunPromptAsync(CqrsQueryCommand<TodoPromptOutput> command, string busyMessage, CancellationToken ct)
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = busyMessage;

        try
        {
            var result = await command.DispatchAsync(ct).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "TODO prompt generation failed.";
                StatusMessage = "TODO prompt generation failed.";
                return;
            }

            PromptOutput = result.Value;
            LastUpdatedAt = DateTimeOffset.UtcNow;
            StatusMessage = result.Value is null
                ? "Prompt output not available."
                : $"Generated {result.Value.PromptType} prompt for {result.Value.TodoId}.";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "TODO prompt generation failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private GetTodoQuery BuildQuery() => new(TodoId.Trim());

    private CreateTodoCommand BuildCreateCommand() => new()
    {
        Id = RequireTrimmed(EditorId),
        Title = RequireTrimmed(EditorTitle),
        Section = RequireTrimmed(EditorSection),
        Priority = RequireTrimmed(EditorPriority),
        Estimate = Normalize(EditorEstimate),
        Note = Normalize(EditorNote),
        Description = ParseLines(EditorDescriptionText),
        TechnicalDetails = ParseLines(EditorTechnicalDetailsText),
        ImplementationTasks = ParseTasks(EditorImplementationTasksText),
    };

    private UpdateTodoCommand BuildUpdateCommand() => new()
    {
        TodoId = RequireTrimmed(EditorId),
        Title = Normalize(EditorTitle),
        Section = Normalize(EditorSection),
        Priority = Normalize(EditorPriority),
        Done = EditorDone,
        Estimate = Normalize(EditorEstimate),
        Note = Normalize(EditorNote),
        Description = ParseLines(EditorDescriptionText),
        TechnicalDetails = ParseLines(EditorTechnicalDetailsText),
        ImplementationTasks = ParseTasks(EditorImplementationTasksText),
    };

    private DeleteTodoCommand BuildDeleteCommand() => new(RequireTrimmed(EditorId));

    private AnalyzeTodoRequirementsCommand BuildAnalyzeRequirementsCommand() => new(GetActiveTodoId());

    private GenerateTodoStatusPromptQuery BuildStatusPromptQuery() => new(GetActiveTodoId());

    private GenerateTodoImplementPromptQuery BuildImplementPromptQuery() => new(GetActiveTodoId());

    private GenerateTodoPlanPromptQuery BuildPlanPromptQuery() => new(GetActiveTodoId());

    private void ApplyDetailToEditor(TodoDetail detail)
    {
        TodoId = detail.Id;
        EditorId = detail.Id;
        EditorTitle = detail.Title;
        EditorSection = detail.Section;
        EditorPriority = detail.Priority;
        EditorDone = detail.Done;
        EditorEstimate = detail.Estimate;
        EditorNote = detail.Note;
        EditorDescriptionText = FormatLines(detail.Description);
        EditorTechnicalDetailsText = FormatLines(detail.TechnicalDetails);
        EditorImplementationTasksText = FormatTasks(detail.ImplementationTasks);
    }

    private string GetActiveTodoId()
    {
        var editorId = RequireTrimmed(EditorId);
        if (!string.IsNullOrEmpty(editorId))
            return editorId;

        return RequireTrimmed(TodoId);
    }

    private static IReadOnlyList<string>? ParseLines(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var lines = text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        return lines.Count == 0 ? null : lines;
    }

    private static IReadOnlyList<TodoTaskDetail>? ParseTasks(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var tasks = new List<TodoTaskDetail>();
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var done = false;
            if (line.StartsWith("[x]", StringComparison.OrdinalIgnoreCase))
            {
                done = true;
                line = line.Substring(3).Trim();
            }
            else if (line.StartsWith("[ ]", StringComparison.OrdinalIgnoreCase))
            {
                line = line.Substring(3).Trim();
            }
            else if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                line = line.Substring(2).Trim();
            }

            if (!string.IsNullOrWhiteSpace(line))
                tasks.Add(new TodoTaskDetail(line, done));
        }

        return tasks.Count == 0 ? null : tasks;
    }

    private static string? FormatLines(IReadOnlyList<string> values)
        => values.Count == 0 ? null : string.Join(Environment.NewLine, values);

    private static string? FormatTasks(IReadOnlyList<TodoTaskDetail> tasks)
        => tasks.Count == 0
            ? null
            : string.Join(Environment.NewLine, tasks.Select(t => $"{(t.Done ? "[x]" : "[ ]")} {t.Task}"));

    private static string RequireTrimmed(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
