using System;
using System.Windows.Controls;
using McpServer.UI;
using Microsoft.VisualStudio.Shell;

namespace McpServer.VsExtension.McpTodo;

/// <summary>
/// Thin WPF code-behind for the MCP TODO tool window.
/// All logic is delegated to <see cref="TodoToolWindowViewModel"/>.
/// </summary>
public partial class McpServerMcpTodoToolWindowControl : UserControl
{
    /// <summary>Initializes the control with the given ViewModel.</summary>
    /// <param name="viewModel">ViewModel that drives the tool window.</param>
    internal McpServerMcpTodoToolWindowControl(TodoToolWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        Loaded += (s, e) => viewModel.RefreshCommand.Execute(null);
    }

    /// <summary>
    /// Shows a VS completion InfoBar and opens a log file in the editor.
    /// Invoked by the ViewModel via a delegate.
    /// </summary>
#pragma warning disable VSTHRD010 // Caller ensures main thread via SwitchToMainThreadAsync
    internal static void ShowCompletionInfoBar(string message, string filePath)
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
            // InfoBar is best-effort
        }
    }
#pragma warning restore VSTHRD010

    /// <summary>
    /// Opens a file in the VS editor. Used as a delegate callback from the ViewModel.
    /// </summary>
#pragma warning disable VSTHRD010 // Main thread verified by SwitchToMainThreadAsync in caller
    internal static void OpenFileInEditor(string filePath)
    {
        try
        {
            var dte = (EnvDTE.DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(EnvDTE.DTE));
            dte?.ItemOperations?.OpenFile(filePath);
        }
        catch { /* best-effort */ }
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
