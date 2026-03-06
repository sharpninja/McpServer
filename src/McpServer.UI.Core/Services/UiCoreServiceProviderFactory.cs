using System;
using McpServer.Cqrs;
using McpServer.UI.Core;
using McpServer.UI.Core.Services;
using McpServer.UI.Core.ViewModels;
using McpServer.UI.Core.Services.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.UI.Core.Services;

internal static class UiCoreServiceProviderFactory
{
    public static ServiceProvider Build(
        Commands.ICommandTarget commandTarget,
        McpTodoService? todoService = null,
        McpWorkspaceService? workspaceService = null,
        McpVoiceConversationService? voiceService = null,
        McpSessionLogService? sessionLogService = null,
        McpAgentEventStreamService? eventStreamService = null,
        IFileSystemService? fileSystemService = null,
        IProcessLauncherService? processLauncherService = null,
        ITimerService? timerService = null,
        IJsonParsingService? jsonParsingService = null,
        IFileSystemWatcherService? fileSystemWatcherService = null,
        WorkspaceContextViewModel? workspaceContext = null)
    {
        if (todoService is null && workspaceService is null)
            throw new ArgumentException("At least one MCP service is required to build the UI.Core host provider.");

        var services = new ServiceCollection();
        services.AddSingleton(AppLogService.Instance);
        services.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(sp => sp.GetRequiredService<AppLogService>());
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(AppLogger<>));

        if (todoService is not null)
            services.AddSingleton<ITodoApiClient>(_ => new UiCoreTodoApiClientAdapter(todoService));

        if (workspaceService is not null)
            services.AddSingleton<IWorkspaceApiClient>(_ => new UiCoreWorkspaceApiClientAdapter(workspaceService));

        if (voiceService is not null)
            services.AddSingleton<IVoiceApiClient>(_ => new UiCoreVoiceApiClientAdapter(voiceService));

        if (sessionLogService is not null)
            services.AddSingleton<ISessionLogApiClient>(_ => new UiCoreSessionLogApiClientAdapter(sessionLogService));

        if (eventStreamService is not null)
            services.AddSingleton<IEventStreamApiClient>(_ => new UiCoreEventStreamApiClientAdapter(eventStreamService));

        // Infrastructure services — use host-provided or default implementations
        services.AddSingleton<IFileSystemService>(fileSystemService ?? new FileSystemService());
        services.AddSingleton<IProcessLauncherService>(processLauncherService ?? new ProcessLauncherService());
        services.AddSingleton<ITimerService>(timerService ?? new TimerService());
        services.AddSingleton<IJsonParsingService>(jsonParsingService ?? new JsonParsingService());
        services.AddSingleton<IFileSystemWatcherService>(fileSystemWatcherService ?? new FileSystemWatcherService());
        services.AddSingleton<McpServer.UI.Core.Services.IClipboardService>(_ => new McpServer.UI.Core.Services.NoOpClipboardService());
        services.AddSingleton<McpServer.UI.Core.Services.IAppLogService>(_ => AppLogService.Instance);
        services.AddSingleton<McpServer.UI.Core.Services.ISpeechFilterService>(_ => new NoOpSpeechFilterService());
        services.AddSingleton<McpServer.UI.Core.Services.IUiDispatcherService>(_ => new ImmediateUiDispatcherService());
        services.AddSingleton<McpServer.UI.Core.Services.IConnectionAuthService>(_ => new NoOpConnectionAuthService());

        // Register ICommandTarget and granular sub-interfaces for CQRS handler DI
        services.AddSingleton<Commands.ICommandTarget>(commandTarget);
        services.AddSingleton<Commands.INavigationTarget>(sp => sp.GetRequiredService<Commands.ICommandTarget>());
        services.AddSingleton<Commands.IRequestDetailsTarget>(sp => sp.GetRequiredService<Commands.ICommandTarget>());
        services.AddSingleton<Commands.IPreviewTarget>(sp => sp.GetRequiredService<Commands.ICommandTarget>());
        services.AddSingleton<Commands.IArchiveTarget>(sp => sp.GetRequiredService<Commands.ICommandTarget>());
        services.AddSingleton<Commands.ISessionDataTarget>(sp => sp.GetRequiredService<Commands.ICommandTarget>());
        services.AddSingleton<Commands.IClipboardTarget>(sp => sp.GetRequiredService<Commands.ICommandTarget>());
        services.AddSingleton<Commands.IConfigTarget>(sp => sp.GetRequiredService<Commands.ICommandTarget>());
        services.AddSingleton<Commands.IUiDispatchTarget>(sp => sp.GetRequiredService<Commands.ICommandTarget>());
        services.AddSingleton<Commands.ITodoCopilotTarget>(sp => sp.GetRequiredService<Commands.ICommandTarget>());

        services.AddCqrsDispatcher();
        // Scan app command handlers for DI-based handler discovery
        services.AddCqrsHandlers(typeof(Commands.NavigateBackCommand).Assembly);
        services.AddUiCore();
        if (workspaceContext is not null)
            services.AddSingleton(workspaceContext);

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>()
            .AddProvider(serviceProvider.GetRequiredService<Dispatcher>());
        return serviceProvider;
    }
}

