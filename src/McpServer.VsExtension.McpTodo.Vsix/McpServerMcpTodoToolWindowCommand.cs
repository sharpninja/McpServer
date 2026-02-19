using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace McpServer.VsExtension.McpTodo;

internal static class McpServerMcpTodoToolWindowCommand
{
    public static readonly Guid CommandSetGuid = Guid.Parse("B1C2D3E4-F5A6-4B7C-8D9E-0F1A2B3C4D5E");
    public const int CommandId = 0x0100;
    private static AsyncPackage? s_package;

    private const string LogSource = "McpServerMcpTodoCommand";

    public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
    {
        s_package = package;
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
        var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)).ConfigureAwait(true) as OleMenuCommandService;
        if (commandService == null)
        {
            ActivityLog.LogError(LogSource, "OleMenuCommandService is null — command not registered.");
            return;
        }

        var cmdId = new CommandID(CommandSetGuid, CommandId);
        var cmd = new OleMenuCommand(Execute, cmdId);
        cmd.BeforeQueryStatus += OnBeforeQueryStatus;
        commandService.AddCommand(cmd);
        ActivityLog.LogInformation(LogSource, $"Command registered: {CommandSetGuid}:{CommandId:X4}");
    }

    private static void OnBeforeQueryStatus(object sender, EventArgs e)
    {
        if (sender is OleMenuCommand cmd)
        {
            cmd.Visible = true;
            cmd.Enabled = true;
        }
    }

    private static void Execute(object sender, EventArgs e)
    {
        ActivityLog.LogInformation(LogSource, "Execute invoked — opening tool window.");
        if (s_package == null)
        {
            ActivityLog.LogError(LogSource, "Execute: s_package is null.");
            return;
        }

        _ = s_package.JoinableTaskFactory.RunAsync(async () =>
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(s_package!.DisposalToken);
                var window = await s_package!.ShowToolWindowAsync(typeof(McpServerMcpTodoToolWindowPane), 0, true, s_package.DisposalToken).ConfigureAwait(true);
                ActivityLog.LogInformation(LogSource, window != null
                    ? "Tool window opened successfully."
                    : "ShowToolWindowAsync returned null.");
            }
            catch (Exception ex)
            {
                ActivityLog.LogError(LogSource, $"Execute failed: {ex}");
            }
        });
    }
}
