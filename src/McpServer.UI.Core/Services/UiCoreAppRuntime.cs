using System;
using McpServer.UI.Core.Services;
using McpServer.UI.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.UI.Core.Services;

public sealed class UiCoreAppRuntime : IDisposable
{
    public UiCoreAppRuntime(
        McpServer.UI.Core.Commands.ICommandTarget commandTarget,
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
        WorkspaceContext = workspaceContext ?? new WorkspaceContextViewModel();
        Services = UiCoreServiceProviderFactory.Build(
            commandTarget,
            todoService, workspaceService, voiceService,
            sessionLogService, eventStreamService,
            fileSystemService, processLauncherService,
            timerService, jsonParsingService, fileSystemWatcherService,
            WorkspaceContext);
    }

    public WorkspaceContextViewModel WorkspaceContext { get; }

    public ServiceProvider Services { get; }

    public T GetRequiredService<T>()
        where T : notnull
        => Services.GetRequiredService<T>();

    public void Dispose() => Services.Dispose();
}

