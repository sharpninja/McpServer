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
            // Resolve McpHttpClient from marker file
            var client = McpHttpClient.FromMarkerFile(workspace);
            client?.TrySetCachedBearerToken();

            McpServerClient? workspaceApi = null;
            if (client is not null)
            {
                workspaceApi = McpServerClientFactory.Create(new McpServerClientOptions
                {
                    BaseUrl = new Uri(client.BaseUrl),
                    ApiKey = client.ApiKey,
                    Timeout = TimeSpan.FromMinutes(10),
                });
            }

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
            if (client is not null)
                services.AddSingleton(client);
            services.AddSingleton<IHealthApiClient>(_ => new HealthApiClientAdapter(client));
            services.AddSingleton<ISessionLogApiClient>(_ => new SessionLogApiClientAdapter(client));
            if (workspaceApi is not null)
                services.AddSingleton(workspaceApi);
            services.AddSingleton<IWorkspaceApiClient>(_ => new WorkspaceApiClientAdapter(workspaceApi));
            services.AddSingleton<ITodoApiClient>(_ => new TodoApiClientAdapter(workspaceApi));
            using var sp = services.BuildServiceProvider();

            // Resolve ViewModels
            var workspaceListVm = sp.GetRequiredService<WorkspaceListViewModel>();
            var workspacePolicyVm = sp.GetRequiredService<WorkspacePolicyViewModel>();
            var healthVm = sp.GetRequiredService<HealthSnapshotsViewModel>();
            var sessionLogVm = sp.GetRequiredService<SessionLogListViewModel>();
            var todoVm = sp.GetRequiredService<TodoListViewModel>();
            var todoDetailVm = sp.GetRequiredService<TodoDetailViewModel>();
            var roleContext = sp.GetRequiredService<IRoleContext>();
            var authorizationPolicy = sp.GetRequiredService<IAuthorizationPolicyService>();

            // Initialize Terminal.Gui
            Terminal.Gui.Application.Init();

            try
            {
                var mainScreen = new MainScreen(
                    workspaceListVm,
                    workspacePolicyVm,
                    healthVm,
                    sessionLogVm,
                    todoVm,
                    todoDetailVm,
                    authorizationPolicy,
                    roleContext,
                    client);
                Terminal.Gui.Application.Run(mainScreen);
            }
            finally
            {
                Terminal.Gui.Application.Shutdown();
            }
        }, WorkspaceOption);

        root.AddCommand(cmd);
    }
}
