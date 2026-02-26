using System.CommandLine;
using McpServer.Client;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.Director.Auth;
using McpServer.Director.Screens;
using McpServer.UI.Core;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Services;
using McpServer.UI.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Director.Commands;

/// <summary>
/// FR-MCP-030: Interactive TUI command that launches Terminal.Gui with ViewModel-bound screens.
/// All Director functions are available as Terminal.Gui tabs including auth.
/// </summary>
internal static class InteractiveCommand
{
    private static readonly Option<string?> WorkspaceOption = new("--workspace", "Workspace path (defaults to current directory)");

    /// <summary>Registers the interactive command on the root command.</summary>
    public static void Register(RootCommand root)
    {
        WorkspaceOption.AddAlias("-w");

        var cmd = new Command("interactive", "Launch interactive Terminal UI for workspace management")
        {
            WorkspaceOption,
        };
        cmd.AddAlias("tui");
        cmd.AddAlias("ui");

        cmd.SetHandler((string? workspace) =>
        {
            var activeWorkspaceClient = McpHttpClient.FromMarkerOnly(workspace);
            activeWorkspaceClient?.TrySetCachedBearerToken();

            var controlClient = McpHttpClient.FromDefaultUrlOrMarker(workspace);
            controlClient?.TrySetCachedBearerToken();

            var directorContext = new DirectorMcpContext(controlClient, activeWorkspaceClient);

            // Build DI container with CQRS + UI Core
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            services.AddCqrs(typeof(Program).Assembly);
            services.AddUiCore();
            services.RemoveAll<IRoleContext>();
            services.RemoveAll<IAuthorizationPolicyService>();
            services.AddSingleton<IRoleContext, DirectorRoleContext>();
            services.AddSingleton<IAuthorizationPolicyService, DirectorAuthorizationPolicyService>();
            services.AddSingleton(directorContext);
            if (controlClient is not null)
                services.AddSingleton(controlClient);
            services.AddSingleton<IHealthApiClient>(_ => new HealthApiClientAdapter(directorContext.ControlClient));
            services.AddSingleton<ISessionLogApiClient>(_ => new SessionLogApiClientAdapter(directorContext));
            services.AddSingleton<IWorkspaceApiClient>(_ => new WorkspaceApiClientAdapter(directorContext));
            services.AddSingleton<ISyncApiClient>(_ => new SyncApiClientAdapter(directorContext));
            services.AddSingleton<IRepoApiClient>(_ => new RepoApiClientAdapter(directorContext));
            services.AddSingleton<IContextApiClient>(_ => new ContextApiClientAdapter(directorContext));
            services.AddSingleton<IAuthConfigApiClient>(_ => new AuthConfigApiClientAdapter(directorContext));
            services.AddSingleton<IDiagnosticApiClient>(_ => new DiagnosticApiClientAdapter(directorContext));
            services.AddSingleton<ITodoApiClient>(_ => new TodoApiClientAdapter(directorContext));
        services.AddSingleton<ITunnelApiClient>(_ => new TunnelApiClientAdapter(directorContext));
            using var sp = services.BuildServiceProvider();

            // Resolve ViewModels
            var workspaceListVm = sp.GetRequiredService<WorkspaceListViewModel>();
            var workspaceDetailVm = sp.GetRequiredService<WorkspaceDetailViewModel>();
            var workspacePolicyVm = sp.GetRequiredService<WorkspacePolicyViewModel>();
            var healthVm = sp.GetRequiredService<HealthSnapshotsViewModel>();
            var dispatcherLogsVm = sp.GetRequiredService<DispatcherLogsViewModel>();
            var sessionLogVm = sp.GetRequiredService<SessionLogListViewModel>();
            var syncStatusVm = sp.GetRequiredService<SyncStatusViewModel>();
            var runSyncVm = sp.GetRequiredService<RunSyncViewModel>();
            var todoVm = sp.GetRequiredService<TodoListViewModel>();
            var todoDetailVm = sp.GetRequiredService<TodoDetailViewModel>();
            var tunnelListVm = sp.GetRequiredService<TunnelListViewModel>();
            var roleContext = sp.GetRequiredService<IRoleContext>();
            var authorizationPolicy = sp.GetRequiredService<IAuthorizationPolicyService>();

            // Initialize Terminal.Gui
            Terminal.Gui.Application.Init();

            try
            {
                var mainScreen = new MainScreen(
                    workspaceListVm,
                    workspaceDetailVm,
                    workspacePolicyVm,
                    healthVm,
                    dispatcherLogsVm,
                    sessionLogVm,
                    syncStatusVm,
                    runSyncVm,
                    todoVm,
                    todoDetailVm,
                    tunnelListVm,
                    authorizationPolicy,
                    roleContext,
                    directorContext);
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
        }, WorkspaceOption);

        root.AddCommand(cmd);
    }
}
