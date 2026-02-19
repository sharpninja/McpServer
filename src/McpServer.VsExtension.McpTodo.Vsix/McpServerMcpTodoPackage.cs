using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

#pragma warning disable VSTHRD010 // Main thread access verified via SwitchToMainThreadAsync in callers
#pragma warning disable VSTHRD110 // Observe result of async calls
#pragma warning disable VSSDK007 // Await/join JoinableTaskFactory.RunAsync

namespace McpServer.VsExtension.McpTodo;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true, RegisterUsing = RegistrationMethod.CodeBase)]
[Guid("E8F0A1B2-3C4D-4E5F-8A9B-0C1D2E3F4A5B")]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideToolWindow(typeof(McpServerMcpTodoToolWindowPane), Style = VsDockStyle.Tabbed, Window = "3ae79031-e1bc-11d0-8f78-00a0c9110057")]
[ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
public sealed class McpServerMcpTodoPackage : AsyncPackage, IVsSolutionEvents
{
    private TodoEditorService? _editorService;
    private uint _solutionEventsCookie;
    private bool _todoEnabled;

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        try
        {
            // Subscribe to solution events so we can react to load/unload
            var solution = (IVsSolution)GetGlobalService(typeof(SVsSolution));
            solution?.AdviseSolutionEvents(this, out _solutionEventsCookie);

            // If a solution is already open, try to activate now
            await TryEnableTodoAsync(cancellationToken).ConfigureAwait(true);

            await McpServerMcpTodoToolWindowCommand.InitializeAsync(this);
            ActivityLog.LogInformation("McpServerMcpTodoPackage", "Package initialized successfully. Command registered.");
        }
        catch (Exception ex)
        {
            ActivityLog.LogError("McpServerMcpTodoPackage", $"InitializeAsync failed: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Checks the currently loaded solution and enables todo functionality if it is FunWasHad.
    /// </summary>
    private async Task TryEnableTodoAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        string? solutionDir = null;
        try
        {
            var dte = (EnvDTE.DTE)GetGlobalService(typeof(EnvDTE.DTE));
            if (dte?.Solution?.FullName is string slnPath && !string.IsNullOrEmpty(slnPath))
            {
                var slnName = System.IO.Path.GetFileNameWithoutExtension(slnPath);
                if (!slnName.StartsWith("FunWasHad", StringComparison.OrdinalIgnoreCase))
                {
                    ActivityLog.LogInformation("McpServerMcpTodoPackage", $"Solution '{slnName}' is not FunWasHad — todo disabled.");
                    return;
                }
                solutionDir = System.IO.Path.GetDirectoryName(slnPath);
            }
            else
            {
                return;
            }
        }
        catch { return; }

        if (_todoEnabled) return;

        var client = new McpTodoClient();
        var serverWasStarted = await client.EnsureServerRunningAsync(solutionDir, cancellationToken).ConfigureAwait(true);

        if (TodoEditorService.Instance == null)
        {
            _editorService = new TodoEditorService(client);
        }

        _todoEnabled = true;
        CopilotCliHelper.WorkingDirectory = solutionDir;
        ActivityLog.LogInformation("McpServerMcpTodoPackage", "FunWasHad solution detected — todo enabled.");

        // If we had to start the server, trigger a refresh so the tool window loads data
        if (serverWasStarted)
            _editorService?.NotifyRefresh();
    }

    private void DisableTodo()
    {
        if (!_todoEnabled) return;

        _editorService?.Dispose();
        _editorService = null;
        _todoEnabled = false;
        CopilotCliHelper.WorkingDirectory = null;
        ActivityLog.LogInformation("McpServerMcpTodoPackage", "Solution closed — todo disabled.");
    }

    /// <summary>Gets whether the todo functionality is currently active.</summary>
    internal bool IsTodoEnabled => _todoEnabled;

    #region IVsSolutionEvents

    int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
    {
        JoinableTaskFactory.RunAsync(async () =>
        {
            await TryEnableTodoAsync(DisposalToken).ConfigureAwait(true);
        });
        return VSConstants.S_OK;
    }

    int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved)
    {
        DisableTodo();
        return VSConstants.S_OK;
    }

    int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved) => VSConstants.S_OK;
    int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) => VSConstants.S_OK;
    int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) => VSConstants.S_OK;
    int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) => VSConstants.S_OK;
    int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => VSConstants.S_OK;
    int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) => VSConstants.S_OK;
    int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) => VSConstants.S_OK;
    int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) => VSConstants.S_OK;

    #endregion

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisableTodo();

            if (_solutionEventsCookie != 0)
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var solution = (IVsSolution)GetGlobalService(typeof(SVsSolution));
                    solution?.UnadviseSolutionEvents(_solutionEventsCookie);
                });
                _solutionEventsCookie = 0;
            }
        }
        base.Dispose(disposing);
    }
}
