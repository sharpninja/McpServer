using McpServer.UI.Core;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Cqrs.Tests;

/// <summary>Tests for the shared workspace lifecycle and global-prompt handlers via <see cref="Dispatcher"/>.</summary>
public sealed class WorkspaceLifecycleHandlerTests
{
    [Fact]
    public async Task SendAsync_CreateWorkspace_EmptyPath_ReturnsFailure()
    {
        using var sp = BuildProvider(Substitute.For<IWorkspaceApiClient>(), AllowAllAuth());
        var dispatcher = sp.GetRequiredService<Dispatcher>();

        var result = await dispatcher.SendAsync(new CreateWorkspaceCommand { WorkspacePath = string.Empty });

        Assert.True(result.IsFailure);
        Assert.Equal("WorkspacePath is required.", result.Error);
    }

    [Fact]
    public async Task SendAsync_CreateWorkspace_Unauthorized_ReturnsPermissionFailure()
    {
        var apiClient = Substitute.For<IWorkspaceApiClient>();
        var auth = Deny(McpActionKeys.WorkspaceCreate, "director");
        using var sp = BuildProvider(apiClient, auth);
        var dispatcher = sp.GetRequiredService<Dispatcher>();

        var result = await dispatcher.SendAsync(new CreateWorkspaceCommand { WorkspacePath = "E:\\github\\RequestTracker" });

        Assert.True(result.IsFailure);
        Assert.Equal("Permission denied: requires director.", result.Error);
        await apiClient.DidNotReceiveWithAnyArgs().CreateWorkspaceAsync(default!, default);
    }

    [Fact]
    public async Task SendAsync_CreateWorkspace_Authorized_ReturnsMutationOutcome()
    {
        var apiClient = Substitute.For<IWorkspaceApiClient>();
        apiClient.CreateWorkspaceAsync(Arg.Any<CreateWorkspaceCommand>(), Arg.Any<CancellationToken>())
            .Returns(new WorkspaceMutationOutcome(true, null, BuildWorkspaceDetail()));

        using var sp = BuildProvider(apiClient, AllowAllAuth());
        var dispatcher = sp.GetRequiredService<Dispatcher>();

        var result = await dispatcher.SendAsync(new CreateWorkspaceCommand { WorkspacePath = "E:\\github\\RequestTracker" });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value!.Success);
        Assert.Equal("RequestTracker", result.Value.Item!.Name);
    }

    [Fact]
    public async Task SendAsync_UpdateWorkspace_Authorized_ReturnsMutationOutcome()
    {
        var apiClient = Substitute.For<IWorkspaceApiClient>();
        apiClient.UpdateWorkspaceAsync(Arg.Any<UpdateWorkspaceCommand>(), Arg.Any<CancellationToken>())
            .Returns(new WorkspaceMutationOutcome(true, null, BuildWorkspaceDetail() with { Name = "Updated Workspace" }));

        using var sp = BuildProvider(apiClient, AllowAllAuth());
        var dispatcher = sp.GetRequiredService<Dispatcher>();

        var result = await dispatcher.SendAsync(new UpdateWorkspaceCommand { WorkspacePath = "E:\\github\\RequestTracker", Name = "Updated Workspace" });

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Workspace", result.Value!.Item!.Name);
    }

    [Fact]
    public async Task SendAsync_DeleteWorkspace_Authorized_ReturnsMutationOutcome()
    {
        var apiClient = Substitute.For<IWorkspaceApiClient>();
        apiClient.DeleteWorkspaceAsync(Arg.Any<DeleteWorkspaceCommand>(), Arg.Any<CancellationToken>())
            .Returns(new WorkspaceMutationOutcome(true, null, null));

        using var sp = BuildProvider(apiClient, AllowAllAuth());
        var dispatcher = sp.GetRequiredService<Dispatcher>();

        var result = await dispatcher.SendAsync(new DeleteWorkspaceCommand("E:\\github\\RequestTracker"));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Success);
    }

    [Fact]
    public async Task QueryAsync_GetWorkspaceStatus_Authorized_ReturnsProcessState()
    {
        var apiClient = Substitute.For<IWorkspaceApiClient>();
        apiClient.GetWorkspaceStatusAsync("E:\\github\\RequestTracker", Arg.Any<CancellationToken>())
            .Returns(new WorkspaceProcessState(true, 7147, "00:01:00", 7147, null));

        using var sp = BuildProvider(apiClient, AllowAllAuth());
        var dispatcher = sp.GetRequiredService<Dispatcher>();

        var result = await dispatcher.QueryAsync(new GetWorkspaceStatusQuery("E:\\github\\RequestTracker"));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsRunning);
        Assert.Equal(7147, result.Value.Port);
    }

    [Fact]
    public async Task SendAsync_StartWorkspace_ClientThrows_ReturnsFailure()
    {
        var apiClient = Substitute.For<IWorkspaceApiClient>();
        apiClient.StartWorkspaceAsync("E:\\github\\RequestTracker", Arg.Any<CancellationToken>())
            .Returns<Task<WorkspaceProcessState>>(_ => throw new InvalidOperationException("boom"));

        using var sp = BuildProvider(apiClient, AllowAllAuth());
        var dispatcher = sp.GetRequiredService<Dispatcher>();

        var result = await dispatcher.SendAsync(new StartWorkspaceCommand("E:\\github\\RequestTracker"));

        Assert.True(result.IsFailure);
        Assert.Equal("boom", result.Error);
    }

    [Fact]
    public async Task SendAsync_StopWorkspace_Authorized_ReturnsProcessState()
    {
        var apiClient = Substitute.For<IWorkspaceApiClient>();
        apiClient.StopWorkspaceAsync("E:\\github\\RequestTracker", Arg.Any<CancellationToken>())
            .Returns(new WorkspaceProcessState(false, null, null, 7147, null));

        using var sp = BuildProvider(apiClient, AllowAllAuth());
        var dispatcher = sp.GetRequiredService<Dispatcher>();

        var result = await dispatcher.SendAsync(new StopWorkspaceCommand("E:\\github\\RequestTracker"));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsRunning);
    }

    [Fact]
    public async Task QueryAsync_CheckWorkspaceHealth_Authorized_ReturnsHealthState()
    {
        var apiClient = Substitute.For<IWorkspaceApiClient>();
        apiClient.CheckWorkspaceHealthAsync("E:\\github\\RequestTracker", Arg.Any<CancellationToken>())
            .Returns(new WorkspaceHealthState(true, 200, "http://localhost:7147/health", "{\"status\":\"Healthy\"}", null));

        using var sp = BuildProvider(apiClient, AllowAllAuth());
        var dispatcher = sp.GetRequiredService<Dispatcher>();

        var result = await dispatcher.QueryAsync(new CheckWorkspaceHealthQuery("E:\\github\\RequestTracker"));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Success);
        Assert.Equal(200, result.Value.StatusCode);
    }

    [Fact]
    public async Task QueryAsync_GetWorkspaceGlobalPrompt_Unauthorized_ReturnsPermissionFailure()
    {
        var apiClient = Substitute.For<IWorkspaceApiClient>();
        var auth = Deny(McpActionKeys.WorkspaceGlobalPromptGet, "operator");

        using var sp = BuildProvider(apiClient, auth);
        var dispatcher = sp.GetRequiredService<Dispatcher>();

        var result = await dispatcher.QueryAsync(new GetWorkspaceGlobalPromptQuery());

        Assert.True(result.IsFailure);
        Assert.Equal("Permission denied: requires operator.", result.Error);
        await apiClient.DidNotReceiveWithAnyArgs().GetWorkspaceGlobalPromptAsync(default);
    }

    [Fact]
    public async Task SendAsync_UpdateWorkspaceGlobalPrompt_Authorized_ReturnsPromptState()
    {
        var apiClient = Substitute.For<IWorkspaceApiClient>();
        apiClient.UpdateWorkspaceGlobalPromptAsync(Arg.Any<UpdateWorkspaceGlobalPromptCommand>(), Arg.Any<CancellationToken>())
            .Returns(new WorkspaceGlobalPromptState("Prompt text", false));

        using var sp = BuildProvider(apiClient, AllowAllAuth());
        var dispatcher = sp.GetRequiredService<Dispatcher>();

        var result = await dispatcher.SendAsync(new UpdateWorkspaceGlobalPromptCommand("Prompt text"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Prompt text", result.Value!.Template);
        Assert.False(result.Value.IsDefault);
    }

    private static ServiceProvider BuildProvider(IWorkspaceApiClient apiClient, IAuthorizationPolicyService auth)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(apiClient);
        services.AddSingleton(auth);
        services.AddCqrs(typeof(WorkspaceLifecycleHandlerTests).Assembly);
        services.AddUiCore();
        return services.BuildServiceProvider();
    }

    private static IAuthorizationPolicyService AllowAllAuth()
    {
        var auth = Substitute.For<IAuthorizationPolicyService>();
        auth.CanExecuteAction(Arg.Any<string>()).Returns(true);
        return auth;
    }

    private static IAuthorizationPolicyService Deny(string actionKey, string requiredRole)
    {
        var auth = Substitute.For<IAuthorizationPolicyService>();
        auth.CanExecuteAction(Arg.Any<string>()).Returns(true);
        auth.CanExecuteAction(actionKey).Returns(false);
        auth.GetRequiredRole(actionKey).Returns(requiredRole);
        return auth;
    }

    private static WorkspaceDetail BuildWorkspaceDetail()
        => new(
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
            BannedIndividuals: []);
}
