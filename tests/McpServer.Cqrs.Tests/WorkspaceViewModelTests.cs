using McpServer.UI.Core;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using McpServer.UI.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Cqrs.Tests;

/// <summary>Focused ViewModel tests for the shared workspace relay surfaces.</summary>
public sealed class WorkspaceViewModelTests
{
    [Fact]
    public async Task WorkspaceGlobalPromptViewModel_LoadAsync_PopulatesEditor()
    {
        var apiClient = Substitute.For<IWorkspaceApiClient>();
        apiClient.GetWorkspaceGlobalPromptAsync(Arg.Any<CancellationToken>())
            .Returns(new WorkspaceGlobalPromptState("Global prompt", false));

        using var sp = BuildProvider(apiClient);
        var vm = sp.GetRequiredService<WorkspaceGlobalPromptViewModel>();

        await vm.LoadAsync();

        Assert.Null(vm.ErrorMessage);
        Assert.Equal("Global prompt", vm.TemplateText);
        Assert.False(vm.IsDefault);
    }

    [Fact]
    public async Task WorkspaceHealthProbeViewModel_CheckHealthAsync_PopulatesHealthState()
    {
        var apiClient = Substitute.For<IWorkspaceApiClient>();
        apiClient.CheckWorkspaceHealthAsync("E:\\github\\RequestTracker", Arg.Any<CancellationToken>())
            .Returns(new WorkspaceHealthState(true, 200, "http://localhost:7147/health", "{\"status\":\"Healthy\"}", null));

        using var sp = BuildProvider(apiClient);
        var context = sp.GetRequiredService<WorkspaceContextViewModel>();
        context.ActiveWorkspacePath = "E:\\github\\RequestTracker";

        var vm = sp.GetRequiredService<WorkspaceHealthProbeViewModel>();
        await vm.CheckHealthAsync();

        Assert.Null(vm.ErrorMessage);
        Assert.NotNull(vm.LastHealthState);
        Assert.True(vm.LastHealthState!.Success);
        Assert.Contains("Healthy", vm.HealthStatusText);
    }

    [Fact]
    public async Task WorkspaceDetailViewModel_CreateAsync_SetsLoadedDetail()
    {
        var apiClient = Substitute.For<IWorkspaceApiClient>();
        apiClient.CreateWorkspaceAsync(Arg.Any<CreateWorkspaceCommand>(), Arg.Any<CancellationToken>())
            .Returns(new WorkspaceMutationOutcome(
                true,
                null,
                new WorkspaceDetail(
                    WorkspacePath: "E:\\github\\RequestTracker",
                    Name: "RequestTracker",
                    TodoPath: "docs\\todo.yaml",
                    DataDirectory: "data",
                    TunnelProvider: "ngrok",
                    IsPrimary: false,
                    IsEnabled: true,
                    RunAs: null,
                    PromptTemplate: "Prompt template",
                    StatusPrompt: "Status prompt",
                    ImplementPrompt: "Implement prompt",
                    PlanPrompt: "Plan prompt",
                    DateTimeCreated: DateTimeOffset.Parse("2026-03-01T00:00:00Z"),
                    DateTimeModified: DateTimeOffset.Parse("2026-03-02T00:00:00Z"),
                    BannedLicenses: [],
                    BannedCountriesOfOrigin: [],
                    BannedOrganizations: [],
                    BannedIndividuals: [])));

        using var sp = BuildProvider(apiClient);
        var vm = sp.GetRequiredService<WorkspaceDetailViewModel>();
        vm.BeginNewDraft();
        vm.EditorWorkspacePath = "E:\\github\\RequestTracker";
        vm.EditorName = "RequestTracker";
        vm.EditorTodoPath = "docs\\todo.yaml";

        await vm.CreateAsync();

        Assert.Null(vm.ErrorMessage);
        Assert.NotNull(vm.Detail);
        Assert.Equal("E:\\github\\RequestTracker", vm.WorkspacePath);
        Assert.False(vm.IsNewDraft);
    }

    private static ServiceProvider BuildProvider(IWorkspaceApiClient apiClient)
    {
        var auth = Substitute.For<IAuthorizationPolicyService>();
        auth.CanExecuteAction(Arg.Any<string>()).Returns(true);

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(apiClient);
        services.AddSingleton(auth);
        services.AddCqrs(typeof(WorkspaceViewModelTests).Assembly);
        services.AddUiCore();
        return services.BuildServiceProvider();
    }
}
