using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using McpServer.VsExtension.McpTodo.Models;
using Microsoft.VisualStudio.Shell;

namespace McpServer.VsExtension.McpTodo;

public partial class McpServerMcpTodoToolWindowControl : UserControl
{
    private readonly McpTodoClient _client;
    private readonly TodoEditorService _editorService;
    private List<TodoListEntry> _entries = new();
    private string _filterPriority = "";
    private string _filterText = "";
    private string _filterTextScope = "title";
    private CancellationTokenSource? _copilotCts;

    public McpServerMcpTodoToolWindowControl()
    {
        InitializeComponent();
        _client = new McpTodoClient();
        _editorService = TodoEditorService.Instance ?? new TodoEditorService(_client);
        _editorService.TodoSaved += () => _ = LoadTodosAsync();
        Loaded += (s, e) => _ = LoadTodosAsync();
        PriorityFilter.Items.Add(new ComboBoxItem { Content = "All", Tag = "" });
        PriorityFilter.Items.Add(new ComboBoxItem { Content = "High", Tag = "high" });
        PriorityFilter.Items.Add(new ComboBoxItem { Content = "Medium", Tag = "medium" });
        PriorityFilter.Items.Add(new ComboBoxItem { Content = "Low", Tag = "low" });
        PriorityFilter.SelectedIndex = 0;
        TextScopeFilter.Items.Add(new ComboBoxItem { Content = "TITLE", Tag = "title" });
        TextScopeFilter.Items.Add(new ComboBoxItem { Content = "ID", Tag = "id" });
        TextScopeFilter.Items.Add(new ComboBoxItem { Content = "ALL", Tag = "all" });
        TextScopeFilter.SelectedIndex = 0;
        TextFilter.TextChanged += (s, e) => ApplyFilters();
    }

    private async System.Threading.Tasks.Task LoadTodosAsync()
    {
        StatusText.Text = "Loading…";
        try
        {
            var result = await _client.GetTodoListAsync(done: false).ConfigureAwait(true);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _entries = BuildEntries(result.Items ?? new List<TodoFlatItem>());
            ApplyFilters();
            StatusText.Text = $"{result.TotalCount} item(s)";
        }
        catch (System.Exception ex)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            StatusText.Text = "Error: " + ex.Message;
            _entries = new List<TodoListEntry>();
            ApplyFilters();
        }
    }

    private static int PrioritySortKey(string? p)
    {
        if (string.IsNullOrWhiteSpace(p)) return 3;
        return p!.Trim().ToUpperInvariant() switch { "HIGH" => 0, "MEDIUM" => 1, "LOW" => 2, _ => 3 };
    }

    private List<TodoListEntry> BuildEntries(List<TodoFlatItem> items)
    {
        return items
            .Select(i => new TodoListEntry
            {
                PriorityGroup = "Priority: " + (string.IsNullOrWhiteSpace(i.Priority) ? "Other" : (i.Priority.Length > 1 ? char.ToUpperInvariant(i.Priority[0]) + i.Priority.Substring(1).ToUpperInvariant() : i.Priority.ToUpperInvariant())),
                DisplayLine = $"{i.Id} · {i.Priority} · {i.Title}",
                Item = i
            })
            .OrderBy(e => PrioritySortKey(e.Item?.Priority))
            .ThenBy(e => e.Item?.Id)
            .ToList();
    }

    private void ApplyFilters()
    {
        _filterText = TextFilter?.Text?.Trim() ?? "";
        var priorityItem = PriorityFilter?.SelectedItem as ComboBoxItem;
        _filterPriority = (priorityItem?.Tag as string) ?? "";
        var scopeItem = TextScopeFilter?.SelectedItem as ComboBoxItem;
        _filterTextScope = (scopeItem?.Tag as string) ?? "title";

        var filtered = _entries.AsEnumerable();
        if (!string.IsNullOrEmpty(_filterPriority))
            filtered = filtered.Where(e => string.Equals(e.Item?.Priority, _filterPriority, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(_filterText))
        {
            var q = _filterText.ToUpperInvariant();
            filtered = filtered.Where(e =>
            {
                var i = e.Item;
                if (i == null) return false;
                string search;
                switch (_filterTextScope.ToLowerInvariant())
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

        var list = filtered.ToList();
        var view = (CollectionView)CollectionViewSource.GetDefaultView(list);
        view.GroupDescriptions.Clear();
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(TodoListEntry.PriorityGroup)));
        TodoList.ItemsSource = view;
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e) => _ = LoadTodosAsync();

    private void NewTodoButton_Click(object sender, RoutedEventArgs e)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        _editorService.OpenNewTodo();
    }

    private void PriorityFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();

    private void TextScopeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();

    private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
    {
        PriorityFilter.SelectedIndex = 0;
        TextScopeFilter.SelectedIndex = 0;
        TextFilter.Text = "";
        ApplyFilters();
    }

    private void TodoList_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void CopyIdButton_Click(object sender, RoutedEventArgs e)
    {
        if (TodoList.SelectedItem is TodoListEntry entry && entry.Item != null)
        {
            CopyIdToClipboard(entry.Item.Id);
            StatusText.Text = $"Copied {entry.Item.Id}";
        }
    }

#pragma warning disable VSTHRD100 // Avoid async void — required for WPF event handler signature
    private async void TodoList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
#pragma warning restore VSTHRD100
    {
        if (TodoList.SelectedItem is not TodoListEntry entry || entry.Item == null) return;
        await _editorService.OpenTodoAsync(entry.Item.Id).ConfigureAwait(true);
    }

    private static void CopyIdToClipboard(string id)
    {
        try { Clipboard.SetText(id); } catch { }
    }

    private void ContextMenu_Open_Click(object sender, RoutedEventArgs e)
    {
        if (TodoList.SelectedItem is TodoListEntry entry && entry.Item != null)
            TodoList_MouseDoubleClick(sender, null!);
    }

#pragma warning disable VSTHRD100 // Avoid async void — required for WPF event handler signature
    private async void ContextMenu_Status_Click(object sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100
    {
        if (TodoList.SelectedItem is not TodoListEntry entry || entry.Item == null) return;
        var id = entry.Item.Id;
        var prompt = $"Get the current status of TODO {id} from the local MCP server at http://localhost:7147. Use: curl http://localhost:7147/mcp/todo/{id} to retrieve the item. Report the title, priority, section, done status, description, technical details, implementation tasks with completion status, and any blockers or next steps.";
        await InvokeCopilotPromptAsync(id, "Status", prompt).ConfigureAwait(true);
    }

#pragma warning disable VSTHRD100 // Avoid async void — required for WPF event handler signature
    private async void ContextMenu_Implement_Click(object sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100
    {
        if (TodoList.SelectedItem is not TodoListEntry entry || entry.Item == null) return;
        var id = entry.Item.Id;
        var prompt = $@"Implement TODO {id}. Follow this procedure:

1. RETRIEVE: Fetch the full TODO from the local MCP server:
   curl http://localhost:7147/mcp/todo/{id}
   Note the implementationTasks array — each entry has {{ task, done }}.

2. IMPLEMENT TASKS: Work through each implementationTask that has done=false.
   After completing each task, immediately update the TODO via PUT to mark
   that specific task done. Send the FULL implementationTasks array with the
   completed task's done field set to true:
   curl -X PUT http://localhost:7147/mcp/todo/{id} \
     -H ""Content-Type: application/json"" \
     -d '{{""implementationTasks"": [ ...full array with updated done flags... ]}}'
   This makes progress visible in the tree view in real time.

3. UPDATE DEPENDENTS: After all tasks are complete, query all TODOs:
   curl http://localhost:7147/mcp/todo
   Find any TODO whose dependsOn array contains ""{id}"". For each dependent:
   - Update its technicalDetails or note to reflect that {id} is now complete.
   - If all of the dependent's own dependencies are satisfied, update its
     remaining estimate and note accordingly.

4. MARK DONE: When all implementationTasks are done, mark the TODO itself done:
   curl -X PUT http://localhost:7147/mcp/todo/{id} \
     -H ""Content-Type: application/json"" \
     -d '{{""done"": true}}'

5. Update the session log throughout. Run to completion, do not wait for user.
   The project is at E:\github\FunWasHad.";
        await InvokeCopilotPromptAsync(id, "Implement", prompt).ConfigureAwait(true);
    }

#pragma warning disable VSTHRD100 // Avoid async void — required for WPF event handler signature
    private async void ContextMenu_Plan_Click(object sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100
    {
        if (TodoList.SelectedItem is not TodoListEntry entry || entry.Item == null) return;
        var id = entry.Item.Id;
        var prompt = $"Create an implementation plan in excruciating detail as a new TODO that TODO {id} depends on. First retrieve the full details of {id} from the local MCP server using: curl http://localhost:7147/mcp/todo/{id}. Then create a new TODO via POST http://localhost:7147/mcp/todo with the detailed plan. Finally update {id} via PUT http://localhost:7147/mcp/todo/{id} to add the new plan TODO as a dependency.";
        await InvokeCopilotPromptAsync(id, "Plan", prompt).ConfigureAwait(true);
    }

    private void ContextMenu_CopyId_Click(object sender, RoutedEventArgs e)
    {
        CopyIdButton_Click(sender, e);
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        _copilotCts?.Cancel();
    }

    private async System.Threading.Tasks.Task InvokeCopilotPromptAsync(string id, string action, string prompt)
    {
        StatusText.Text = $"{action} {id}…";
        _copilotCts?.Dispose();
        _copilotCts = new CancellationTokenSource();
        StopButton.IsEnabled = true;
        try
        {
            // Create a temp markdown file for live output
            var tempDir = Path.Combine(Path.GetTempPath(), "McpServer-McpTodo");
            Directory.CreateDirectory(tempDir);
            var mdPath = Path.Combine(tempDir, $"{action}-{id}-{DateTime.Now:yyyyMMdd-HHmmss}.md");
            File.WriteAllText(mdPath, $"# {action}: {id}\n\n_Running…_\n");

            // Open the file in VS editor
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
#pragma warning disable VSTHRD010 // Main thread verified by SwitchToMainThreadAsync above
            var dte = (EnvDTE.DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE));
            dte?.ItemOperations?.OpenFile(mdPath);
#pragma warning restore VSTHRD010

            // Stream output line-by-line into the file
            var firstLine = true;
            void OnLine(string line)
            {
                try
                {
                    if (firstLine)
                    {
                        // Replace the "Running…" placeholder with the first real output
                        File.WriteAllText(mdPath, $"# {action}: {id}\n\n{line}\n");
                        firstLine = false;
                    }
                    else
                    {
                        File.AppendAllText(mdPath, line + "\n");
                    }
                }
                catch { /* file write race — swallow */ }
            }

            var result = await CopilotCliHelper.InvokeAsync(prompt, OnLine, _copilotCts.Token).ConfigureAwait(true);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (result.State == "cancelled")
            {
                StatusText.Text = $"{action} {id} stopped";
            }
            else if (result.State == "success")
            {
                StatusText.Text = $"{action} {id} complete";
                ShowCompletionInfoBar($"{action} {id} complete", mdPath);
            }
            else
            {
                StatusText.Text = $"{action} {id}: {result.State}";
                CopilotOutputPane.Log($"Copilot CLI returned {result.State} for {action} {id}: {result.Stderr ?? result.Body}");
            }
        }
        catch (System.Exception ex)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            StatusText.Text = $"Copilot unavailable for {action} {id}";
            CopilotOutputPane.Log($"Copilot CLI failed ({action} {id}): {ex.Message}");
        }
        finally
        {
            StopButton.IsEnabled = false;
            _copilotCts?.Dispose();
            _copilotCts = null;
        }
    }

#pragma warning disable VSTHRD010 // Caller ensures main thread via SwitchToMainThreadAsync
    private static void ShowCompletionInfoBar(string message, string filePath)
    {
        try
        {
            var shell = (Microsoft.VisualStudio.Shell.Interop.IVsShell)
                Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(Microsoft.VisualStudio.Shell.Interop.SVsShell));
            if (shell == null) return;

            shell.GetProperty((int)Microsoft.VisualStudio.Shell.Interop.__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var hostObj);
            if (hostObj is not Microsoft.VisualStudio.Shell.Interop.IVsInfoBarHost host) return;

            var factory = (Microsoft.VisualStudio.Shell.Interop.IVsInfoBarUIFactory)
                Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(Microsoft.VisualStudio.Shell.Interop.SVsInfoBarUIFactory));
            if (factory == null) return;

            var actionItems = new[]
            {
                new Microsoft.VisualStudio.Shell.InfoBarHyperlink("Show Log", filePath)
            };
            var model = new Microsoft.VisualStudio.Shell.InfoBarModel(
                message,
                actionItems,
                Microsoft.VisualStudio.Imaging.KnownMonikers.StatusInformation,
                isCloseButtonVisible: true);
            var uiElement = factory.CreateInfoBar(model);
            if (uiElement == null) return;

            uiElement.Advise(new InfoBarActionHandler(uiElement, host), out _);
            host.AddInfoBar(uiElement);
        }
        catch
        {
            // InfoBar is best-effort — don't break functionality
        }
    }
#pragma warning restore VSTHRD010

#pragma warning disable VSTHRD010 // InfoBar events are fired on the UI thread
    private sealed class InfoBarActionHandler(
        Microsoft.VisualStudio.Shell.Interop.IVsInfoBarUIElement uiElement,
        Microsoft.VisualStudio.Shell.Interop.IVsInfoBarHost host) : Microsoft.VisualStudio.Shell.Interop.IVsInfoBarUIEvents
    {
        public void OnClosed(Microsoft.VisualStudio.Shell.Interop.IVsInfoBarUIElement infoBarUIElement)
        {
            host.RemoveInfoBar(uiElement);
        }

        public void OnActionItemClicked(
            Microsoft.VisualStudio.Shell.Interop.IVsInfoBarUIElement infoBarUIElement,
            Microsoft.VisualStudio.Shell.Interop.IVsInfoBarActionItem actionItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (actionItem.ActionContext is string path)
            {
                try
                {
                    var dte = (EnvDTE.DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE));
                    dte?.ItemOperations?.OpenFile(path);
                }
                catch { /* best-effort */ }
            }
            host.RemoveInfoBar(uiElement);
        }
    }
#pragma warning restore VSTHRD010
}
