using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Models;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using UiCoreTodoDetailViewModel = McpServer.UI.Core.ViewModels.TodoDetailViewModel;
using UiCoreTodoListViewModel = McpServer.UI.Core.ViewModels.TodoListViewModel;
using UiCoreWorkspaceContextViewModel = McpServer.UI.Core.ViewModels.WorkspaceContextViewModel;

namespace McpServer.UI.Core.ViewModels;

#pragma warning disable CS1591

public partial class TodoListHostViewModel : ViewModelBase
{
    private readonly ILogger<TodoListHostViewModel> _logger;

    private readonly IClipboardService _clipboardService;
    private readonly UiCoreTodoListViewModel _listVm;
    private readonly UiCoreTodoDetailViewModel _detailVm;
    private readonly UiCoreWorkspaceContextViewModel _workspaceContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITimerService _timerService;
    private List<TodoListEntry> _allEntries = new();
    private CancellationTokenSource? _activeCts;

    [ObservableProperty] private ObservableCollection<TodoListGroup> _groupedItems = new();
    [ObservableProperty] private TodoListEntry? _selectedEntry;
    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private int _selectedPriorityIndex;
    [ObservableProperty] private int _selectedScopeIndex;
    [ObservableProperty] private string _filterText = "";
    [ObservableProperty] private bool _includeCompleted;

    [ObservableProperty] private bool _isCreatingNew;
    [ObservableProperty] private string _newTodoTitle = "";
    [ObservableProperty] private int _newTodoPriorityIndex = 1;

    [ObservableProperty] private string _editorText = "";
    [ObservableProperty] private string _editorTitle = "";
    [ObservableProperty] private double _editorFontSize = 13;
    [ObservableProperty] private bool _isCopilotRunning;
    [ObservableProperty] private McpTodoFlatItem? _currentTodoDetail;
    [ObservableProperty] private ObservableCollection<EditorTab> _editorTabs = new();
    [ObservableProperty] private EditorTab? _selectedEditorTab;

    public Func<string>? GetEditorText { get; set; }

    public event Action<string>? GlobalStatusChanged;

    public event EventHandler? OpenAiChatRequested;

    public static IReadOnlyList<string> PriorityOptions { get; } = ["All", "High", "Medium", "Low"];
    public static IReadOnlyList<string> ScopeOptions { get; } = ["Title", "ID", "All Fields"];
    public static IReadOnlyList<string> NewPriorityOptions { get; } = ["High", "Medium", "Low"];

    public TodoListHostViewModel(
        IClipboardService clipboardService,
        UiCoreTodoListViewModel listVm,
        UiCoreTodoDetailViewModel detailVm,
        UiCoreWorkspaceContextViewModel workspaceContext,
        IServiceProvider serviceProvider,
        ITimerService timerService,
        ILogger<TodoListHostViewModel>? logger = null)
    {
        _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        _listVm = listVm ?? throw new ArgumentNullException(nameof(listVm));
        _listVm.Done = false; // default to open items only
        _detailVm = detailVm ?? throw new ArgumentNullException(nameof(detailVm));
        _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _timerService = timerService ?? throw new ArgumentNullException(nameof(timerService));
        _logger = logger ?? NullLogger<TodoListHostViewModel>.Instance;
    }

    public void ApplyWorkspacePath(string? workspacePath)
    {
        _workspaceContext.ActiveWorkspacePath = workspacePath ?? string.Empty;
        CurrentTodoDetail = null;
        EditorText = "";
        EditorTitle = "";
        EditorTabs.Clear();
    }

    public Task RefreshForConnectionChangeAsync() => LoadTodosAsync();

    partial void OnSelectedPriorityIndexChanged(int value) => ApplyFilters();
    partial void OnSelectedScopeIndexChanged(int value) => ApplyFilters();
    partial void OnFilterTextChanged(string value) => ApplyFilters();
    partial void OnIncludeCompletedChanged(bool value) => _ = LoadTodosAsync();

    protected async Task LoadTodosAsync() => await LoadTodosCoreAsync(forceEditorReload: false);

    protected async Task RefreshAsync() => await LoadTodosCoreAsync(forceEditorReload: true);

    protected void ClearFilters()
    {
        SelectedPriorityIndex = 0;
        SelectedScopeIndex = 0;
        FilterText = "";
        IncludeCompleted = false;
    }

    protected async Task CopySelectedIdAsync()
    {
        if (SelectedEntry?.Item is { } item)
        {
            await _clipboardService.SetTextAsync(item.Id);
            StatusText = "Copied " + item.Id;
        }
    }

    protected async Task ToggleDoneAsync()
    {
        if (SelectedEntry?.Item is not { } item)
            return;

        try
        {
            var vm = CreateScratchDetailVm();
            vm.EditorId = item.Id;
            vm.EditorDone = !item.Done;
            await vm.SaveAsync();

            if (!string.IsNullOrWhiteSpace(vm.ErrorMessage))
            {
                StatusText = "Failed: " + vm.ErrorMessage;
                return;
            }

            StatusText = item.Done ? $"Reopened {item.Id}" : $"Completed {item.Id}";
            await LoadTodosAsync();
            if (string.Equals(CurrentTodoDetail?.Id, item.Id, StringComparison.OrdinalIgnoreCase))
                await TryRefreshEditorByIdAsync(item.Id, updateStatus: false);
        }
        catch (Exception ex)
        {
            StatusText = "Error: " + ex.Message;
        }
    }

    protected async Task DeleteSelectedAsync()
    {
        if (SelectedEntry?.Item is not { } item)
            return;

        try
        {
            var vm = CreateScratchDetailVm();
            vm.EditorId = item.Id;
            await vm.DeleteAsync();

            if (!string.IsNullOrWhiteSpace(vm.ErrorMessage))
            {
                StatusText = "Delete failed: " + vm.ErrorMessage;
                return;
            }

            StatusText = $"Deleted {item.Id}";
            if (string.Equals(CurrentTodoDetail?.Id, item.Id, StringComparison.OrdinalIgnoreCase))
                ClearEditor();

            await LoadTodosAsync();
        }
        catch (Exception ex)
        {
            StatusText = "Error: " + ex.Message;
        }
    }

    protected async Task AnalyzeRequirementsAsync()
    {
        if (SelectedEntry?.Item is not { } item)
            return;

        StatusText = $"Analyzing {item.Id}...";
        ReplaceActiveCancellation();

        try
        {
            var vm = CreateScratchDetailVm();
            vm.EditorId = item.Id;
            await vm.AnalyzeRequirementsAsync(_activeCts!.Token);

            if (!string.IsNullOrWhiteSpace(vm.ErrorMessage))
            {
                StatusText = "Analysis failed: " + vm.ErrorMessage;
                return;
            }

            var result = vm.RequirementsAnalysis;
            StatusText = $"Requirements for {item.Id}: {(result?.FunctionalRequirements.Count ?? 0)} functional, {(result?.TechnicalRequirements.Count ?? 0)} technical";
        }
        catch (OperationCanceledException)
        {
            StatusText = $"Canceled analysis for {item.Id}";
        }
        catch (Exception ex)
        {
            StatusText = "Error: " + ex.Message;
        }
    }

    protected void StopAction()
    {
        _activeCts?.Cancel();
        StatusText = "Stopped";
    }

    protected void NewTodo()
    {
        EditorText = TodoMarkdown.BlankTemplate();
        EditorTitle = "NEW-TODO";
        ResetEditorTabs("NEW-TODO", EditorText);
    }

    protected void CancelNewTodo()
    {
        IsCreatingNew = false;
        NewTodoTitle = "";
    }

    protected async Task SaveNewTodoAsync()
    {
        var title = (NewTodoTitle ?? "").Trim();
        if (string.IsNullOrEmpty(title))
        {
            StatusText = "Title is required";
            return;
        }

        var id = $"TODO-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var priority = NewPriorityOptions[Math.Clamp(NewTodoPriorityIndex, 0, NewPriorityOptions.Count - 1)].ToLowerInvariant();

        try
        {
            var vm = CreateScratchDetailVm();
            vm.BeginNewDraft("general");
            vm.EditorId = id;
            vm.EditorTitle = title;
            vm.EditorSection = "general";
            vm.EditorPriority = priority;
            await vm.CreateAsync();

            if (!string.IsNullOrWhiteSpace(vm.ErrorMessage))
            {
                StatusText = "Create failed: " + vm.ErrorMessage;
                return;
            }

            StatusText = $"Created {id}";
            IsCreatingNew = false;
            NewTodoTitle = "";
            await LoadTodosAsync();
        }
        catch (Exception ex)
        {
            StatusText = "Error: " + ex.Message;
        }
    }

    protected async Task OpenSelectedTodoAsync()
    {
        if (SelectedEntry?.Item is not { } item)
            return;

        GlobalStatusChanged?.Invoke($"Opening {item.Id}...");
        var opened = await TryRefreshEditorByIdAsync(item.Id, updateStatus: false);
        if (opened)
        {
            GlobalStatusChanged?.Invoke($"Opened {item.Id}.");
            StatusText = $"Opened {item.Id}";
        }
        else
        {
            StatusText = $"Todo {item.Id} not found";
            GlobalStatusChanged?.Invoke($"Todo {item.Id} not found.");
        }
    }

    protected async Task SaveEditorAsync()
    {
        var text = GetEditorText?.Invoke() ?? EditorText;
        if (string.IsNullOrWhiteSpace(text))
            return;

        var parsed = TodoMarkdown.FromMarkdown(text);
        var parsedId = TodoMarkdown.ExtractId(text);
        var isNew = string.IsNullOrWhiteSpace(parsedId) ||
                    string.Equals(parsedId, "NEW-TODO", StringComparison.OrdinalIgnoreCase);
        var effectiveId = isNew ? $"TODO-{DateTime.UtcNow:yyyyMMddHHmmss}" : parsedId!.Trim();
        var effectiveTitle = parsed.Title ?? "Untitled";

        ReplaceActiveCancellation();
        GlobalStatusChanged?.Invoke(isNew ? "Creating todo..." : $"Saving {effectiveId}...");

        try
        {
            PrepareDetailEditorFromMarkdown(_detailVm, effectiveId, parsed, isNew);
            if (isNew)
                await _detailVm.CreateAsync(_activeCts!.Token);
            else
                await _detailVm.SaveAsync(_activeCts!.Token);

            if (!string.IsNullOrWhiteSpace(_detailVm.ErrorMessage))
            {
                StatusText = (isNew ? "Create failed: " : "Save failed: ") + _detailVm.ErrorMessage;
                GlobalStatusChanged?.Invoke(StatusText);
                return;
            }

            var savedDetail = _detailVm.Detail;
            if (savedDetail is null)
            {
                StatusText = isNew
                    ? $"Create failed: {effectiveTitle}"
                    : $"Save failed: {effectiveId}";
                GlobalStatusChanged?.Invoke(StatusText);
                return;
            }

            var savedId = savedDetail.Id;
            StatusText = isNew ? $"Created {savedId}" : $"Saved {savedId}";
            GlobalStatusChanged?.Invoke(StatusText);

            await LoadTodosCoreAsync(forceEditorReload: false);
            await TryRefreshEditorByIdAsync(savedId, updateStatus: false);
        }
        catch (OperationCanceledException)
        {
            StatusText = isNew ? "Create cancelled" : "Save cancelled";
            GlobalStatusChanged?.Invoke(StatusText);
        }
        catch (Exception ex)
        {
            StatusText = (isNew ? "Error creating: " : "Error saving: ") + ex.Message;
            GlobalStatusChanged?.Invoke(StatusText);
        }
    }

    protected void ClearEditor()
    {
        EditorText = "";
        EditorTitle = "";
        CurrentTodoDetail = null;
        EditorTabs.Clear();
    }

    protected async Task RefreshEditorAsync()
    {
        var editorTodoId = GetCurrentEditorTodoId();
        if (string.IsNullOrWhiteSpace(editorTodoId))
            return;

        await TryRefreshEditorByIdAsync(editorTodoId, updateStatus: true);
    }

    protected void EditorZoomIn() => EditorFontSize = Math.Min(EditorFontSize + 2, 48);

    protected void EditorZoomOut() => EditorFontSize = Math.Max(EditorFontSize - 2, 8);

    protected void OpenAiChat() => OpenAiChatRequested?.Invoke(this, EventArgs.Empty);

    public async Task CopilotStatusAsync()
    {
        if (SelectedEntry?.Item is not { } item)
            return;

        await RunTodoPromptCommandAsync(item, "status",
            static (vm, ct) => vm.GenerateStatusPromptAsync(ct));
    }

    public async Task CopilotPlanAsync()
    {
        if (SelectedEntry?.Item is not { } item)
            return;

        await RunTodoPromptCommandAsync(item, "plan",
            static (vm, ct) => vm.GeneratePlanPromptAsync(ct));
    }

    public async Task CopilotImplementAsync()
    {
        if (SelectedEntry?.Item is not { } item)
            return;

        await RunTodoPromptCommandAsync(item, "implement",
            static (vm, ct) => vm.GenerateImplementPromptAsync(ct));
    }

    private async Task RunTodoPromptCommandAsync(
        McpTodoFlatItem item,
        string action,
        Func<UiCoreTodoDetailViewModel, CancellationToken, Task> generateAsync)
    {
        ReplaceActiveCancellation();

        var status = StatusViewModel.Instance;
        IsCopilotRunning = true;
        status.IsCopilotRunning = true;
        status.CopilotActivityText = "Connecting to Copilot...";
        status.CopilotHeartbeatState = "connecting";
        StatusText = $"{Capitalize(action)} prompt: {item.Id}...";

        // Create or reuse a copilot tab for this action
        var tabHeader = char.ToUpper(action[0]) + action[1..];
        var copilotTab = EditorTabs.FirstOrDefault(t => t.Header == tabHeader);
        if (copilotTab is null)
        {
            copilotTab = EditorTab.CreateCopilotTab(action, $"Requesting {action} prompt for {item.Id}...");
            EditorTabs.Add(copilotTab);
        }
        else
        {
            copilotTab.Content = $"Requesting {action} prompt for {item.Id}...";
        }
        SelectedEditorTab = copilotTab;

        ITimerHandle? watchdog = null;
        try
        {
            var vm = CreateScratchDetailVm();
            vm.EditorId = item.Id;

            // Forward streaming text and heartbeat state to the copilot tab as lines arrive
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(UiCoreTodoDetailViewModel.StreamingPromptText)
                    && vm.StreamingPromptText is { } text)
                {
                    copilotTab.Content = text;
                    status.CopilotActivityText = "Copilot is responding";
                    status.CopilotHeartbeatState = "receiving";
                }
                else if (e.PropertyName == nameof(UiCoreTodoDetailViewModel.LastHeartbeatUtc)
                         && vm.LastHeartbeatUtc is not null)
                {
                    status.CopilotActivityText = "Copilot is thinking\u2026";
                    status.CopilotHeartbeatState = "active";
                }
            };

            // Watchdog timer: check if heartbeats have stopped
            watchdog = _timerService.CreateRecurring(TimeSpan.FromSeconds(5), _ =>
            {
                if (!IsCopilotRunning)
                    return Task.CompletedTask;
                var last = vm.LastHeartbeatUtc;
                if (last is null)
                    return Task.CompletedTask;

                var elapsed = DateTimeOffset.UtcNow - last.Value;
                if (elapsed.TotalSeconds > 30)
                {
                    status.CopilotActivityText = $"No heartbeat for {(int)elapsed.TotalSeconds}s \u2014 Copilot may have stalled";
                    status.CopilotHeartbeatState = "stalled";
                }
                else if (elapsed.TotalSeconds > 15)
                {
                    status.CopilotActivityText = $"Last heartbeat {(int)elapsed.TotalSeconds}s ago";
                    status.CopilotHeartbeatState = "warning";
                }
                return Task.CompletedTask;
            });

            await generateAsync(vm, _activeCts!.Token);

            if (!string.IsNullOrWhiteSpace(vm.ErrorMessage))
            {
                copilotTab.Content = $"Error: {vm.ErrorMessage}";
                StatusText = $"{Capitalize(action)} prompt error: {vm.ErrorMessage}";
                return;
            }

            copilotTab.Content = vm.PromptOutput?.Text ?? vm.StreamingPromptText ?? string.Empty;
            StatusText = $"{Capitalize(action)} prompt done: {item.Id}";
        }
        catch (OperationCanceledException)
        {
            copilotTab.Content = $"{copilotTab.Content}{Environment.NewLine}{Environment.NewLine}Cancelled.";
            StatusText = $"{Capitalize(action)} prompt cancelled";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TODO prompt {Action} failed for {Id}", action, item.Id);
            copilotTab.Content = $"Error: {ex.Message}";
            StatusText = $"{Capitalize(action)} prompt error: {ex.Message}";
        }
        finally
        {
            watchdog?.Dispose();
            IsCopilotRunning = false;
            status.ClearCopilotState();
        }
    }

    public string GetTodoContextForAgent()
    {
        var sb = new StringBuilder();
        sb.AppendLine("--- Todo context ---");

        var editorContent = GetEditorText?.Invoke() ?? EditorText;
        if (!string.IsNullOrWhiteSpace(editorContent))
        {
            sb.AppendLine();
            sb.AppendLine("## Currently open todo (YAML):");
            sb.AppendLine(editorContent.Trim());
        }

        if (_allEntries.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Todo list summary:");
            foreach (var entry in _allEntries)
            {
                var item = entry.Item;
                if (item == null)
                    continue;

                var doneTag = item.Done ? " [DONE]" : "";
                sb.AppendLine($"- {item.Id} | {item.Priority} | {item.Title}{doneTag}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("--- End todo context ---");
        return sb.ToString();
    }

    private void ApplyFilters()
    {
        IEnumerable<TodoListEntry> source = _allEntries;

        var priorityTag = SelectedPriorityIndex switch
        {
            1 => "high",
            2 => "medium",
            3 => "low",
            _ => ""
        };

        if (!string.IsNullOrEmpty(priorityTag))
        {
            source = source.Where(e =>
                string.Equals(e.Item?.Priority, priorityTag, StringComparison.OrdinalIgnoreCase));
        }

        var text = (FilterText ?? "").Trim();
        if (!string.IsNullOrEmpty(text))
        {
            var scopeTag = SelectedScopeIndex switch
            {
                1 => "id",
                2 => "all",
                _ => "title"
            };
            var matcher = BooleanSearchParser.Parse(text);
            source = source.Where(e => MatchesTextFilter(e.Item, matcher, scopeTag));
        }

        var groups = source
            .ToList()
            .GroupBy(e => e.PriorityGroup)
            .OrderBy(g => PrioritySortKey(g.First().Item?.Priority))
            .Select(g => new TodoListGroup(
                g.Key,
                new ObservableCollection<TodoListEntry>(
                    g.OrderBy(e => e.Item?.Id, StringComparer.OrdinalIgnoreCase))))
            .ToList();

        GroupedItems = new ObservableCollection<TodoListGroup>(groups);
    }

    private static bool MatchesTextFilter(McpTodoFlatItem? item, Func<string, bool> matcher, string scope)
    {
        if (item == null)
            return false;

        var searchable = scope switch
        {
            "id" => item.Id ?? "",
            "title" => item.Title ?? "",
            _ => string.Join(
                " ",
                new[] { item.Id, item.Title, item.Section, item.Priority, item.Note, item.Estimate, item.Remaining }
                    .Concat(item.Description ?? Enumerable.Empty<string>())
                    .Concat(item.TechnicalDetails ?? Enumerable.Empty<string>())
                    .Where(s => !string.IsNullOrEmpty(s)))
        };

        return matcher(searchable);
    }

    private async Task LoadTodosCoreAsync(bool forceEditorReload)
    {
        var previouslySelectedId = SelectedEntry?.Item?.Id;
        var editorTodoId = forceEditorReload ? GetCurrentEditorTodoId() : null;

        IsLoading = true;
        StatusText = forceEditorReload ? "Refreshing..." : "Loading...";
        GlobalStatusChanged?.Invoke(forceEditorReload ? "Refreshing todos..." : "Loading todos...");

        try
        {
            _listVm.Keyword = null;
            _listVm.Priority = null;
            _listVm.Section = null;
            _listVm.TodoId = null;
            _listVm.Done = IncludeCompleted ? null : false;
            await _listVm.LoadAsync();

            if (!string.IsNullOrWhiteSpace(_listVm.ErrorMessage))
            {
                _allEntries = [];
                ApplyFilters();
                SelectedEntry = null;
                StatusText = "Error: " + _listVm.ErrorMessage;
                GlobalStatusChanged?.Invoke($"Todo load failed: {_listVm.ErrorMessage}");
                return;
            }

            _allEntries = BuildEntries(_listVm.Items);
            ApplyFilters();
            RestoreSelectionById(previouslySelectedId);

            var refreshNote = "";
            if (forceEditorReload && !string.IsNullOrWhiteSpace(editorTodoId))
            {
                var refreshed = await TryRefreshEditorByIdAsync(editorTodoId, updateStatus: false);
                refreshNote = refreshed ? " • editor refreshed" : " • editor not found";
            }

            StatusText = $"{_listVm.TotalCount} item(s){refreshNote}";
            GlobalStatusChanged?.Invoke(forceEditorReload
                ? $"Refreshed {_listVm.TotalCount} todo(s)."
                : $"Loaded {_listVm.TotalCount} todo(s).");
        }
        catch (Exception ex)
        {
            _allEntries = [];
            ApplyFilters();
            SelectedEntry = null;
            StatusText = "Error: " + ex.Message;
            GlobalStatusChanged?.Invoke($"Todo load failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void RestoreSelectionById(string? todoId)
    {
        if (string.IsNullOrWhiteSpace(todoId))
        {
            SelectedEntry = null;
            return;
        }

        SelectedEntry = _allEntries.FirstOrDefault(e =>
            string.Equals(e.Item?.Id, todoId, StringComparison.OrdinalIgnoreCase));
    }

    private string? GetCurrentEditorTodoId()
    {
        var editorContent = GetEditorText?.Invoke() ?? EditorText;
        var markdownId = TodoMarkdown.ExtractId(editorContent);
        if (!string.IsNullOrWhiteSpace(markdownId) &&
            !string.Equals(markdownId, "NEW-TODO", StringComparison.OrdinalIgnoreCase))
        {
            return markdownId;
        }

        if (string.IsNullOrWhiteSpace(EditorTitle) ||
            string.Equals(EditorTitle, "NEW-TODO", StringComparison.OrdinalIgnoreCase) ||
            EditorTitle.Contains(" — ", StringComparison.Ordinal))
        {
            return null;
        }

        return EditorTitle.Trim();
    }

    private async Task<bool> TryRefreshEditorByIdAsync(string todoId, bool updateStatus)
    {
        try
        {
            _detailVm.TodoId = todoId;
            await _detailVm.LoadAsync();
            if (!string.IsNullOrWhiteSpace(_detailVm.ErrorMessage))
            {
                if (updateStatus)
                    StatusText = "Error refreshing: " + _detailVm.ErrorMessage;
                return false;
            }

            if (_detailVm.Detail is null)
            {
                CurrentTodoDetail = null;
                if (string.Equals(EditorTitle, todoId, StringComparison.OrdinalIgnoreCase))
                {
                    EditorText = "";
                    EditorTitle = "";
                    EditorTabs.Clear();
                }

                if (updateStatus)
                    StatusText = $"Todo {todoId} not found";
                return false;
            }

            ApplyDetailToHost(_detailVm.Detail);
            RestoreSelectionById(_detailVm.Detail.Id);
            if (updateStatus)
                StatusText = $"Refreshed {_detailVm.Detail.Id}";
            return true;
        }
        catch (Exception ex)
        {
            if (updateStatus)
                StatusText = "Error refreshing: " + ex.Message;
            return false;
        }
    }

    private void ApplyDetailToHost(TodoDetail detail)
    {
        CurrentTodoDetail = ToMcpTodoFlatItem(detail);
        EditorText = TodoMarkdown.ToMarkdown(CurrentTodoDetail);
        EditorTitle = detail.Id;
        ResetEditorTabs(detail.Id, EditorText);
    }

    private static void PrepareDetailEditorFromMarkdown(
        UiCoreTodoDetailViewModel viewModel,
        string todoId,
        McpTodoUpdateRequest request,
        bool isNew)
    {
        if (isNew)
            viewModel.BeginNewDraft(request.Section ?? "general");

        viewModel.EditorId = todoId;
        viewModel.EditorTitle = request.Title ?? string.Empty;
        viewModel.EditorSection = request.Section ?? (isNew ? "general" : string.Empty);
        viewModel.EditorPriority = request.Priority ?? (isNew ? "medium" : string.Empty);
        viewModel.EditorDone = request.Done ?? false;
        viewModel.EditorEstimate = request.Estimate;
        viewModel.EditorNote = request.Note;
        viewModel.EditorCompletedDate = request.CompletedDate;
        viewModel.EditorDoneSummary = request.DoneSummary;
        viewModel.EditorRemaining = request.Remaining;
        viewModel.EditorDescriptionText = FormatLines(request.Description);
        viewModel.EditorTechnicalDetailsText = FormatLines(request.TechnicalDetails);
        viewModel.EditorImplementationTasksText = FormatTasks(request.ImplementationTasks);
        viewModel.EditorDependsOnText = FormatLines(request.DependsOn);
        viewModel.EditorFunctionalRequirementsText = FormatLines(request.FunctionalRequirements);
        viewModel.EditorTechnicalRequirementsText = FormatLines(request.TechnicalRequirements);
    }

    private void ReplaceActiveCancellation()
    {
        _activeCts?.Cancel();
        _activeCts?.Dispose();
        _activeCts = new CancellationTokenSource();
    }

    /// <summary>Resets editor tabs to a single primary editor tab.</summary>
    private void ResetEditorTabs(string todoId, string content)
    {
        EditorTabs.Clear();
        var tab = EditorTab.CreateEditorTab(todoId, content);
        EditorTabs.Add(tab);
        SelectedEditorTab = tab;
    }

    /// <summary>Syncs the primary editor tab content when EditorText changes externally.</summary>
    partial void OnEditorTextChanged(string value)
    {
        var primary = EditorTabs.FirstOrDefault(t => !t.IsMarkdown);
        if (primary is not null && primary.Content != value)
            primary.Content = value;
    }

    private UiCoreTodoDetailViewModel CreateScratchDetailVm()
    {
        return _serviceProvider.GetService(typeof(UiCoreTodoDetailViewModel)) as UiCoreTodoDetailViewModel
            ?? throw new InvalidOperationException("Unable to resolve TodoDetailViewModel for scratch operations.");
    }

    private static McpTodoFlatItem ToMcpTodoFlatItem(TodoDetail detail)
    {
        return new McpTodoFlatItem
        {
            Id = detail.Id,
            Title = detail.Title,
            Section = detail.Section,
            Priority = detail.Priority,
            Done = detail.Done,
            Estimate = detail.Estimate,
            Note = detail.Note,
            Description = detail.Description.ToList(),
            TechnicalDetails = detail.TechnicalDetails.ToList(),
            ImplementationTasks = detail.ImplementationTasks
                .Select(static task => new McpTodoFlatTask { Task = task.Task, Done = task.Done })
                .ToList(),
            CompletedDate = detail.CompletedDate,
            DoneSummary = detail.DoneSummary,
            Remaining = detail.Remaining,
            PriorityNote = detail.PriorityNote,
            Reference = detail.Reference,
            DependsOn = detail.DependsOn.ToList(),
            FunctionalRequirements = detail.FunctionalRequirements.ToList(),
            TechnicalRequirements = detail.TechnicalRequirements.ToList()
        };
    }

    private static List<TodoListEntry> BuildEntries(IEnumerable<TodoListItem> items)
    {
        return items
            .Select(static item =>
            {
                var flat = new McpTodoFlatItem
                {
                    Id = item.Id,
                    Title = item.Title,
                    Section = item.Section,
                    Priority = item.Priority,
                    Done = item.Done,
                    Estimate = item.Estimate
                };

                return new TodoListEntry
                {
                    PriorityGroup = "Priority: " + FormatPriority(flat.Priority),
                    DisplayLine = $"{flat.Id} · {flat.Priority} · {flat.Title}",
                    Item = flat
                };
            })
            .OrderBy(e => PrioritySortKey(e.Item?.Priority))
            .ThenBy(e => e.Item?.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string FormatPriority(string? priority)
    {
        if (string.IsNullOrWhiteSpace(priority))
            return "Other";

        return char.ToUpperInvariant(priority[0]) + priority.Substring(1).ToLowerInvariant();
    }

    private static int PrioritySortKey(string? priority) => (priority?.Trim().ToUpperInvariant()) switch
    {
        "HIGH" => 0,
        "MEDIUM" => 1,
        "LOW" => 2,
        _ => 3
    };

    private static string Capitalize(string value)
        => string.IsNullOrEmpty(value) ? value : char.ToUpperInvariant(value[0]) + value[1..];

    private static string? FormatLines(IEnumerable<string>? values)
    {
        var lines = values?.Where(static value => !string.IsNullOrWhiteSpace(value)).ToList();
        return lines is { Count: > 0 } ? string.Join(Environment.NewLine, lines) : null;
    }

    private static string? FormatTasks(IEnumerable<McpTodoFlatTask>? tasks)
    {
        var entries = tasks?
            .Where(static task => !string.IsNullOrWhiteSpace(task.Task))
            .Select(static task => $"{(task.Done ? "[x]" : "[ ]")} {task.Task}")
            .ToList();
        return entries is { Count: > 0 } ? string.Join(Environment.NewLine, entries) : null;
    }

    public static string GetTaskProgress(McpTodoFlatItem? item)
    {
        if (item?.ImplementationTasks is not { Count: > 0 } tasks)
            return string.Empty;

        var done = tasks.Count(static task => task.Done);
        return $"{done}/{tasks.Count}";
    }
}

public sealed class TodoListEntry
{
    public string PriorityGroup { get; set; } = "";
    public string DisplayLine { get; set; } = "";
    public McpTodoFlatItem? Item { get; set; }
}

public sealed class TodoListGroup
{
    public TodoListGroup(string name, ObservableCollection<TodoListEntry> items)
    {
        Name = name;
        Items = items;
    }

    public string Name { get; }

    public ObservableCollection<TodoListEntry> Items { get; }
}

#pragma warning restore CS1591
