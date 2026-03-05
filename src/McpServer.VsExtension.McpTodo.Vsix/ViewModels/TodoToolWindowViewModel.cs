using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using McpServer.VsExtension.McpTodo;
using McpServer.VsExtension.McpTodo.Models;
using Microsoft.VisualStudio.Shell;

namespace McpServer.UI;

/// <summary>
/// ViewModel for the MCP TODO tool window, following the MVVM pattern
/// from McpServer.UI.Core with CommunityToolkit.Mvvm RelayCommands.
/// </summary>
internal sealed partial class TodoToolWindowViewModel : ObservableObject
{
    private readonly McpTodoClient _client;
    private readonly TodoEditorService _editorService;
    private List<TodoListEntry> _allEntries = new();
    private CancellationTokenSource? _copilotCts;

    /// <summary>Delegate invoked to open a file in the VS editor.</summary>
    private readonly Action<string>? _openFileInEditor;

    /// <summary>Delegate invoked to show a completion InfoBar in VS.</summary>
    private readonly Action<string, string>? _showCompletionInfoBar;

    /// <summary>Initializes a new instance of the TODO tool window ViewModel.</summary>
    /// <param name="client">MCP server HTTP client.</param>
    /// <param name="editorService">Service for opening/saving TODO items.</param>
    /// <param name="openFileInEditor">Delegate to open a file path in the VS editor.</param>
    /// <param name="showCompletionInfoBar">Delegate to show a VS InfoBar (message, filePath).</param>
    public TodoToolWindowViewModel(
        McpTodoClient client,
        TodoEditorService editorService,
        Action<string>? openFileInEditor = null,
        Action<string, string>? showCompletionInfoBar = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
        _openFileInEditor = openFileInEditor;
        _showCompletionInfoBar = showCompletionInfoBar;

        _editorService.TodoSaved += OnTodoSaved;
    }

    // ────── Observable properties ──────

    /// <summary>Filtered items displayed in the ListView.</summary>
    public ObservableCollection<TodoListEntry> FilteredItems { get; } = new();

    /// <summary>Status bar text.</summary>
    [ObservableProperty]
    private string _statusText = "";

    /// <summary>Selected priority filter value (empty string = all).</summary>
    [ObservableProperty]
    private string _filterPriority = "";

    /// <summary>Free-text filter string.</summary>
    [ObservableProperty]
    private string _filterText = "";

    /// <summary>Text filter scope: title, id, or all.</summary>
    [ObservableProperty]
    private string _filterTextScope = "title";

    /// <summary>Whether the Stop button is enabled.</summary>
    [ObservableProperty]
    private bool _isStopEnabled;

    /// <summary>Currently selected entry in the list.</summary>
    [ObservableProperty]
    private TodoListEntry? _selectedEntry;

    // ────── Filter change hooks ──────

    partial void OnFilterPriorityChanged(string value) => ApplyFilters();
    partial void OnFilterTextChanged(string value) => ApplyFilters();
    partial void OnFilterTextScopeChanged(string value) => ApplyFilters();

    // ────── Commands ──────

    /// <summary>Reload TODO items from the MCP server.</summary>
    [RelayCommand]
    private async System.Threading.Tasks.Task RefreshAsync()
    {
        StatusText = "Loading\u2026";
        try
        {
            var result = await _client.GetTodoListAsync(done: false).ConfigureAwait(true);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _allEntries = BuildEntries(result.Items ?? new List<TodoFlatItem>());
            ApplyFilters();
            StatusText = $"{result.TotalCount} item(s)";
        }
        catch (Exception ex)
        {
            CopilotOutputPane.Log($"LoadTodos error: {ex}");
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            StatusText = "Error: " + ex.Message;
            _allEntries = new List<TodoListEntry>();
            ApplyFilters();
        }
    }

    /// <summary>Open a new-todo editor template.</summary>
    [RelayCommand]
    private void NewTodo()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        _editorService.OpenNewTodo();
    }

    /// <summary>Copy the selected item's ID to the clipboard.</summary>
    [RelayCommand]
    private void CopyId()
    {
        if (SelectedEntry?.Item == null) return;
        try { Clipboard.SetText(SelectedEntry.Item.Id); } catch { /* clipboard race */ }
        StatusText = $"Copied {SelectedEntry.Item.Id}";
    }

    /// <summary>Cancel the running Copilot CLI operation.</summary>
    [RelayCommand]
    private void Stop()
    {
        _copilotCts?.Cancel();
    }

    /// <summary>Reset all filter controls to defaults.</summary>
    [RelayCommand]
    private void ClearFilters()
    {
        // Suppress re-filtering until all values are set
        FilterPriority = "";
        FilterTextScope = "title";
        FilterText = "";
    }

    /// <summary>Open the selected TODO item in the VS editor.</summary>
    [RelayCommand]
    private async System.Threading.Tasks.Task OpenItemAsync()
    {
        if (SelectedEntry?.Item == null) return;
        await _editorService.OpenTodoAsync(SelectedEntry.Item.Id).ConfigureAwait(true);
    }

    /// <summary>Invoke Copilot CLI to report the status of the selected TODO.</summary>
    [RelayCommand]
    private async System.Threading.Tasks.Task StatusPromptAsync()
    {
        if (SelectedEntry?.Item == null) return;
        var id = SelectedEntry.Item.Id;
        var prompt = $"Get the current status of TODO {id} from the local MCP server at http://localhost:7147. "
                   + $"Use: curl http://localhost:7147/mcpserver/todo/{id} to retrieve the item. "
                   + "Report the title, priority, section, done status, description, technical details, "
                   + "implementation tasks with completion status, and any blockers or next steps.";
        await InvokeCopilotPromptAsync(id, "Status", prompt).ConfigureAwait(true);
    }

    /// <summary>Invoke Copilot CLI to implement the selected TODO.</summary>
    [RelayCommand]
    private async System.Threading.Tasks.Task ImplementAsync()
    {
        if (SelectedEntry?.Item == null) return;
        var id = SelectedEntry.Item.Id;
        var prompt = $@"Implement TODO {id}. Follow this procedure:

1. RETRIEVE: Fetch the full TODO from the local MCP server:
   curl http://localhost:7147/mcpserver/todo/{id}
   Note the implementationTasks array — each entry has {{ task, done }}.

2. IMPLEMENT TASKS: Work through each implementationTask that has done=false.
   After completing each task, immediately update the TODO via PUT to mark
   that specific task done. Send the FULL implementationTasks array with the
   completed task's done field set to true:
   curl -X PUT http://localhost:7147/mcpserver/todo/{id} \
     -H ""Content-Type: application/json"" \
     -d '{{""implementationTasks"": [ ...full array with updated done flags... ]}}'
   This makes progress visible in the tree view in real time.

3. UPDATE DEPENDENTS: After all tasks are complete, query all TODOs:
   curl http://localhost:7147/mcpserver/todo
   Find any TODO whose dependsOn array contains ""{id}"". For each dependent:
   - Update its technicalDetails or note to reflect that {id} is now complete.
   - If all of the dependent's own dependencies are satisfied, update its
     remaining estimate and note accordingly.

4. MARK DONE: When all implementationTasks are done, mark the TODO itself done:
   curl -X PUT http://localhost:7147/mcpserver/todo/{id} \
     -H ""Content-Type: application/json"" \
     -d '{{""done"": true}}'

5. Update the session log throughout. Run to completion, do not wait for user.
   The project is at E:\github\FunWasHad.";
        await InvokeCopilotPromptAsync(id, "Implement", prompt).ConfigureAwait(true);
    }

    /// <summary>Invoke Copilot CLI to create an implementation plan for the selected TODO.</summary>
    [RelayCommand]
    private async System.Threading.Tasks.Task PlanAsync()
    {
        if (SelectedEntry?.Item == null) return;
        var id = SelectedEntry.Item.Id;
        var prompt = $"Create an implementation plan in excruciating detail as a new TODO that TODO {id} depends on. "
                   + $"First retrieve the full details of {id} from the local MCP server using: "
                   + $"curl http://localhost:7147/mcpserver/todo/{id}. "
                   + $"Then create a new TODO via POST http://localhost:7147/mcpserver/todo with the detailed plan. "
                   + $"Finally update {id} via PUT http://localhost:7147/mcpserver/todo/{id} "
                   + "to add the new plan TODO as a dependency.";
        await InvokeCopilotPromptAsync(id, "Plan", prompt).ConfigureAwait(true);
    }

    // ────── Private helpers ──────

    private void OnTodoSaved() => RefreshCommand.Execute(null);

    private static int PrioritySortKey(string? p)
    {
        if (string.IsNullOrWhiteSpace(p)) return 3;
        return p!.Trim().ToUpperInvariant() switch
        {
            "HIGH" => 0,
            "MEDIUM" => 1,
            "LOW" => 2,
            _ => 3,
        };
    }

    private static List<TodoListEntry> BuildEntries(List<TodoFlatItem> items)
    {
        return items
            .Select(i => new TodoListEntry
            {
                PriorityGroup = "Priority: " + (string.IsNullOrWhiteSpace(i.Priority)
                    ? "Other"
                    : (i.Priority.Length > 1
                        ? char.ToUpperInvariant(i.Priority[0]) + i.Priority.Substring(1).ToUpperInvariant()
                        : i.Priority.ToUpperInvariant())),
                DisplayLine = $"{i.Id} \u00B7 {i.Priority} \u00B7 {i.Title}",
                Item = i,
            })
            .OrderBy(e => PrioritySortKey(e.Item?.Priority))
            .ThenBy(e => e.Item?.Id)
            .ToList();
    }

    private void ApplyFilters()
    {
        var filtered = _allEntries.AsEnumerable();

        if (!string.IsNullOrEmpty(FilterPriority))
            filtered = filtered.Where(e =>
                string.Equals(e.Item?.Priority, FilterPriority, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(FilterText))
        {
            var q = FilterText.Trim().ToUpperInvariant();
            var scope = (FilterTextScope ?? "title").ToLowerInvariant();
            filtered = filtered.Where(e =>
            {
                var i = e.Item;
                if (i == null) return false;
                string search;
                switch (scope)
                {
                    case "id":
                        search = (i.Id ?? "").ToUpperInvariant();
                        break;
                    case "title":
                        search = (i.Title ?? "").ToUpperInvariant();
                        break;
                    default:
                        search = string.Join(" ", new[] { i.Id, i.Title, i.Section, i.Priority, i.Note, i.Estimate, i.Remaining }
                            .Concat(i.Description ?? Array.Empty<string>())
                            .Concat(i.TechnicalDetails ?? Array.Empty<string>())
                            .Where(s => !string.IsNullOrEmpty(s))).ToUpperInvariant();
                        break;
                }
                return search.IndexOf(q, StringComparison.Ordinal) >= 0;
            });
        }

        FilteredItems.Clear();
        foreach (var entry in filtered)
            FilteredItems.Add(entry);
    }

    private async System.Threading.Tasks.Task InvokeCopilotPromptAsync(string id, string action, string prompt)
    {
        StatusText = $"{action} {id}\u2026";
        _copilotCts?.Dispose();
        _copilotCts = new CancellationTokenSource();
        IsStopEnabled = true;
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "McpServer-McpTodo");
            Directory.CreateDirectory(tempDir);
            var mdPath = Path.Combine(tempDir, $"{action}-{id}-{DateTime.Now:yyyyMMdd-HHmmss}.md");
            File.WriteAllText(mdPath, $"# {action}: {id}\n\n_Running\u2026_\n");

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _openFileInEditor?.Invoke(mdPath);

            var firstLine = true;
            void OnLine(string line)
            {
                try
                {
                    if (firstLine)
                    {
                        File.WriteAllText(mdPath, $"# {action}: {id}\n\n{line}\n");
                        firstLine = false;
                    }
                    else
                    {
                        File.AppendAllText(mdPath, line + "\n");
                    }
                }
                catch { /* file write race */ }
            }

            var result = await CopilotCliHelper.InvokeAsync(prompt, OnLine, _copilotCts.Token).ConfigureAwait(true);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (result.State == "cancelled")
            {
                StatusText = $"{action} {id} stopped";
            }
            else if (result.State == "success")
            {
                StatusText = $"{action} {id} complete";
                _showCompletionInfoBar?.Invoke($"{action} {id} complete", mdPath);
            }
            else
            {
                StatusText = $"{action} {id}: {result.State}";
                CopilotOutputPane.Log($"Copilot CLI returned {result.State} for {action} {id}: {result.Stderr ?? result.Body}");
            }
        }
        catch (Exception ex)
        {
            CopilotOutputPane.Log($"Copilot CLI failed ({action} {id}): {ex.Message}");
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            StatusText = $"Copilot unavailable for {action} {id}";
        }
        finally
        {
            IsStopEnabled = false;
            _copilotCts?.Dispose();
            _copilotCts = null;
        }
    }
}
