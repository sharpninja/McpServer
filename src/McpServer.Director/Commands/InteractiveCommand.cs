using System.CommandLine;
using McpServer.Cqrs.Mvvm;
using McpServer.Director.Helpers;
using McpServer.Director.Screens;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace McpServer.Director.Commands;

/// <summary>
/// FR-MCP-030: Interactive TUI command that launches Terminal.Gui with ViewModel-bound screens.
/// All Director functions are available as Terminal.Gui tabs including auth.
/// </summary>
internal static class InteractiveCommand
{
    private static readonly Option<string?> s_workspaceOption = new("--workspace", "Workspace path (defaults to current directory)");

    /// <summary>Registers the interactive command on the root command.</summary>
    public static void Register(RootCommand root)
    {
        s_workspaceOption.AddAlias("-w");

        var cmd = new Command("interactive", "Launch interactive Terminal UI for workspace management")
        {
            s_workspaceOption,
        };
        cmd.AddAlias("tui");
        cmd.AddAlias("ui");

        cmd.SetHandler((string? workspace) =>
        {
            // Build DI container with shared registration
            var services = new ServiceCollection();
            var directorContext = DirectorServiceRegistration.Configure(services, workspace);
            using var sp = DirectorServiceRegistration.BuildAndFinalize(services);

            // Resolve ViewModels
            var workspaceListVm = sp.GetRequiredService<WorkspaceListViewModel>();
            var workspaceDetailVm = sp.GetRequiredService<WorkspaceDetailViewModel>();
            var workspacePolicyVm = sp.GetRequiredService<WorkspacePolicyViewModel>();
            var healthVm = sp.GetRequiredService<HealthSnapshotsViewModel>();
            var dispatcherLogsVm = sp.GetRequiredService<DispatcherLogsViewModel>();
            var sessionLogVm = sp.GetRequiredService<SessionLogListViewModel>();
            var sessionLogDetailVm = sp.GetRequiredService<SessionLogDetailViewModel>();
            var todoVm = sp.GetRequiredService<TodoListViewModel>();
            var todoDetailVm = sp.GetRequiredService<TodoDetailViewModel>();
            var tunnelListVm = sp.GetRequiredService<TunnelListViewModel>();
            var templateListVm = sp.GetRequiredService<TemplateListViewModel>();
            var templateDetailVm = sp.GetRequiredService<TemplateDetailViewModel>();
            var agentPoolVm = sp.GetRequiredService<AgentPoolViewModel>();
            var workspaceContextVm = sp.GetRequiredService<WorkspaceContextViewModel>();
            var roleContext = sp.GetRequiredService<IRoleContext>();
            var authorizationPolicy = sp.GetRequiredService<IAuthorizationPolicyService>();

            // Initialize Terminal.Gui
            Terminal.Gui.Application.Init();
            ApplyDarculaTheme();

            try
            {
                var mainScreen = new MainScreen(
                    workspaceListVm,
                    workspaceDetailVm,
                    workspacePolicyVm,
                    healthVm,
                    dispatcherLogsVm,
                    sessionLogVm,
                    sessionLogDetailVm,
                    todoVm,
                    todoDetailVm,
                    tunnelListVm,
                    templateListVm,
                    templateDetailVm,
                    agentPoolVm,
                    workspaceContextVm,
                    authorizationPolicy,
                    roleContext,
                    directorContext,
                    sp.GetRequiredService<ILoggerFactory>(),
                    sp.GetRequiredService<IBrowserLauncher>());
                Terminal.Gui.Application.Run(mainScreen);
            }
            finally
            {
                Terminal.Gui.Application.Shutdown();
                try
                {
                    Console.Clear();
                }
                catch
                {
                    // Best-effort terminal cleanup on exit.
                }
            }
        }, s_workspaceOption);

        root.AddCommand(cmd);
    }

    /// <summary>Applies a Darcula-inspired dark color scheme to all Terminal.Gui color scheme slots.</summary>
    private static void ApplyDarculaTheme()
    {
        // Darcula palette — text brighter, borders dimmer
        var bg = new Terminal.Gui.Color(40, 40, 40);
        var fg = new Terminal.Gui.Color(210, 210, 210);
        var accent = new Terminal.Gui.Color(120, 170, 210);
        var hotKey = new Terminal.Gui.Color(220, 140, 65);
        var focusBg = new Terminal.Gui.Color(48, 48, 48);
        var dialogBg = new Terminal.Gui.Color(55, 57, 59);
        var menuBg = new Terminal.Gui.Color(45, 47, 49);
        var errorFg = new Terminal.Gui.Color(255, 120, 115);

        var baseScheme = new Terminal.Gui.ColorScheme(
            normal: new Terminal.Gui.Attribute(fg, bg),
            focus: new Terminal.Gui.Attribute(accent, focusBg),
            hotNormal: new Terminal.Gui.Attribute(hotKey, bg),
            hotFocus: new Terminal.Gui.Attribute(hotKey, focusBg),
            disabled: new Terminal.Gui.Attribute(fg, bg));

        var dialogScheme = new Terminal.Gui.ColorScheme(
            normal: new Terminal.Gui.Attribute(fg, dialogBg),
            focus: new Terminal.Gui.Attribute(accent, focusBg),
            hotNormal: new Terminal.Gui.Attribute(hotKey, dialogBg),
            hotFocus: new Terminal.Gui.Attribute(hotKey, focusBg),
            disabled: new Terminal.Gui.Attribute(fg, dialogBg));

        var menuScheme = new Terminal.Gui.ColorScheme(
            normal: new Terminal.Gui.Attribute(fg, menuBg),
            focus: new Terminal.Gui.Attribute(accent, focusBg),
            hotNormal: new Terminal.Gui.Attribute(hotKey, menuBg),
            hotFocus: new Terminal.Gui.Attribute(hotKey, focusBg),
            disabled: new Terminal.Gui.Attribute(fg, menuBg));

        var errorScheme = new Terminal.Gui.ColorScheme(
            normal: new Terminal.Gui.Attribute(errorFg, bg),
            focus: new Terminal.Gui.Attribute(errorFg, focusBg),
            hotNormal: new Terminal.Gui.Attribute(hotKey, bg),
            hotFocus: new Terminal.Gui.Attribute(hotKey, focusBg),
            disabled: new Terminal.Gui.Attribute(errorFg, bg));

        Terminal.Gui.Colors.ColorSchemes["Base"] = baseScheme;
        Terminal.Gui.Colors.ColorSchemes["TopLevel"] = baseScheme;
        Terminal.Gui.Colors.ColorSchemes["Dialog"] = dialogScheme;
        Terminal.Gui.Colors.ColorSchemes["Menu"] = menuScheme;
        Terminal.Gui.Colors.ColorSchemes["Error"] = errorScheme;

        // Editable text fields use accent blue for normal text to distinguish from labels
        var editableScheme = new Terminal.Gui.ColorScheme(
            normal: new Terminal.Gui.Attribute(accent, bg),
            focus: new Terminal.Gui.Attribute(accent, focusBg),
            hotNormal: new Terminal.Gui.Attribute(hotKey, bg),
            hotFocus: new Terminal.Gui.Attribute(hotKey, focusBg),
            disabled: new Terminal.Gui.Attribute(fg, bg));
        Terminal.Gui.Colors.ColorSchemes["Editable"] = editableScheme;
    }
}
