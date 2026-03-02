using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using McpServer.VsExtension.McpTodo.Models;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Extensions.Logging;

namespace McpServer.VsExtension.McpTodo;

/// <summary>
/// Opens TODO items in VS editor document windows (temp .md files) instead of a TextBox.
/// Intercepts saves via RunningDocumentTable to push changes back to MCP.
/// </summary>
#pragma warning disable VSTHRD010, VSTHRD110, VSSDK007 // Threading handled via SwitchToMainThreadAsync
internal sealed class TodoEditorService : IVsRunningDocTableEvents3, IDisposable
{
    private readonly McpTodoClient _client;

    /// <summary>Maps temp file path → TODO id (null for new todo).</summary>
    private readonly Dictionary<string, string?> _openFiles = new(StringComparer.OrdinalIgnoreCase);

    private IVsRunningDocumentTable4? _rdt4;
    private uint _rdtCookie;
    private bool _disposed;

    /// <summary>Shared instance created at package init so RDT events fire before the tool window opens.</summary>
    internal static TodoEditorService? Instance { get; private set; }

    /// <summary>Raised after a successful MCP update or new-todo creation so the list can refresh.</summary>
    internal event Action? TodoSaved;

    /// <summary>Triggers a refresh of the todo list (e.g. after MCP server becomes healthy).</summary>
    internal void NotifyRefresh() => TodoSaved?.Invoke();
    private readonly ILogger<TodoEditorService> _logger;


    internal TodoEditorService(McpTodoClient client,
        ILogger<TodoEditorService> logger)
    {
        _logger = logger;
        _client = client;
        Instance = this;
        SubscribeToRdt();
    }

    private void SubscribeToRdt()
    {
        ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var rdt = (IVsRunningDocumentTable)Package.GetGlobalService(typeof(SVsRunningDocumentTable));
            if (rdt != null)
            {
                rdt.AdviseRunningDocTableEvents(this, out _rdtCookie);
                _rdt4 = rdt as IVsRunningDocumentTable4;
                RecoverOpenFiles();
            }
        });
    }

    /// <summary>
    /// Re-registers temp .md files that VS restored from a previous session
    /// so that saves are detected even after an IDE restart.
    /// </summary>
    private void RecoverOpenFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "McpServer-McpTodo");
        if (!Directory.Exists(tempDir)) return;

        foreach (var file in Directory.GetFiles(tempDir, "*.md"))
        {
            if (_openFiles.ContainsKey(file)) continue;

            var name = Path.GetFileNameWithoutExtension(file);
            if (name.StartsWith("NEW-TODO-", StringComparison.OrdinalIgnoreCase))
            {
                _openFiles[file] = null;
            }
            else
            {
                _openFiles[file] = name; // filename is the TODO id
            }
        }
    }

    /// <summary>Open an existing TODO item in a VS editor tab.</summary>
    internal async Task OpenTodoAsync(string todoId)
    {
        try
        {
            var item = await _client.GetTodoByIdAsync(todoId).ConfigureAwait(true);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (item == null)
            {
                CopilotOutputPane.Log($"TODO {todoId} not found.");
                return;
            }

            var markdown = TodoMarkdown.ToMarkdown(item);
            var tempPath = GetTempPath(todoId);
            File.WriteAllText(tempPath, markdown);
            _openFiles[tempPath] = todoId;

            VsShellUtilities.OpenDocument(ServiceProvider.GlobalProvider, tempPath);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            CopilotOutputPane.Log($"Failed to open {todoId}: {ex.Message}");
        }
    }

    /// <summary>Open a blank template for creating a new TODO item.</summary>
    internal void OpenNewTodo()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var markdown = TodoMarkdown.BlankTemplate();
        var tempPath = GetTempPath("NEW-TODO-" + Guid.NewGuid().ToString("N").Substring(0, 6));
        File.WriteAllText(tempPath, markdown);
        _openFiles[tempPath] = null; // null signals new-todo

        VsShellUtilities.OpenDocument(ServiceProvider.GlobalProvider, tempPath);
    }

    // --- IVsRunningDocTableEvents3 ---

    public int OnAfterSave(uint docCookie)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        try
        {
            string? path = null;
            if (_rdt4 != null)
            {
                path = _rdt4.GetDocumentMoniker(docCookie);
            }

            if (path != null && _openFiles.TryGetValue(path, out var todoId))
            {
                var text = File.ReadAllText(path);
                ThreadHelper.JoinableTaskFactory.RunAsync(() => HandleSaveAsync(path, todoId, text));
            }
        }
        catch
        {
            // Don't break VS on logging failures
        }
        return VSConstants.S_OK;
    }

    private async Task HandleSaveAsync(string filePath, string? todoId, string text)
    {
        if (todoId == null)
        {
            // New todo — send to Copilot CLI
            var fields = TodoMarkdown.ToNewTodoFields(text);
            if (string.IsNullOrWhiteSpace(fields.Title))
            {
                CopilotOutputPane.Log("New todo save skipped: title is required.");
                return;
            }

            var priority = string.IsNullOrWhiteSpace(fields.Priority) ? "low" : fields.Priority;
            var command = $"add {priority} todo: {fields.Title.Trim()}";

            try
            {
                var result = await CopilotCliHelper.InvokeAsync(command).ConfigureAwait(true);
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (result.State == "success")
                {
                    CopilotOutputPane.Log("TODO created: " + result.Body.Substring(0, Math.Min(120, result.Body.Length)));
                    CleanupFile(filePath);
                    TodoSaved?.Invoke();
                }
                else
                {
                    CopilotOutputPane.Log($"Copilot CLI returned {result.State}: {result.Stderr ?? result.Body}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{ExceptionDetail}", ex.ToString());
                CopilotOutputPane.Log($"Copilot CLI failed (New Todo): {ex.Message}");
            }
        }
        else
        {
            // Existing todo — push update to MCP
            var body = TodoMarkdown.FromMarkdown(text);
            try
            {
                var result = await _client.UpdateTodoAsync(todoId, body).ConfigureAwait(true);
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (result.Success)
                {
                    CopilotOutputPane.Log($"Updated {todoId}.");
                    TodoSaved?.Invoke();
                }
                else
                {
                    CopilotOutputPane.Log($"Update failed for {todoId}: " + (result.Error ?? "unknown"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("{ExceptionDetail}", ex.ToString());
                CopilotOutputPane.Log($"Failed to update {todoId}: {ex.Message}");
            }
        }
    }

    private void CleanupFile(string filePath)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        try
        {
            // Close the document if still open
            if (VsShellUtilities.IsDocumentOpen(ServiceProvider.GlobalProvider, filePath,
                    Guid.Empty, out _, out _, out var windowFrame))
            {
                windowFrame?.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
            }

            _openFiles.Remove(filePath);
            TryDeleteFile(filePath);
        }
        catch
        {
            // Best-effort cleanup
        }
    }

    public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame) => VSConstants.S_OK;
    public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame) => VSConstants.S_OK;
    public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;
    public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) => VSConstants.S_OK;
    public int OnAfterAttributeChange(uint docCookie, uint grfAttribs) => VSConstants.S_OK;
    public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew) => VSConstants.S_OK;
    public int OnBeforeSave(uint docCookie) => VSConstants.S_OK;

    private static string GetTempPath(string name)
    {
        var dir = Path.Combine(Path.GetTempPath(), "McpServer-McpTodo");
        Directory.CreateDirectory(dir);
        // Sanitize the name for use as a filename
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return Path.Combine(dir, name + ".md");
    }

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (Instance == this) Instance = null;

        if (_rdtCookie != 0)
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var rdt = (IVsRunningDocumentTable)Package.GetGlobalService(typeof(SVsRunningDocumentTable));
                rdt?.UnadviseRunningDocTableEvents(_rdtCookie);
            });
        }

        // Clean up temp files
        foreach (var path in _openFiles.Keys)
            TryDeleteFile(path);
        _openFiles.Clear();
    }
}
#pragma warning restore VSTHRD010, VSTHRD110, VSSDK007
