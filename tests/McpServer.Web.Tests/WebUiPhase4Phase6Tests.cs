using Bunit;
using McpServer.Cqrs;
using McpServer.UI.Core;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using McpServer.UI.Core.ViewModels;
using McpServer.Web;
using McpServer.Web.Components.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Web.Tests;

public sealed class WebUiPhase4Phase6Tests
{
    [Fact]
    public async Task WebMcpContext_TracksWorkspaceContext_AndUpdatesActiveClientWorkspacePath()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["McpServer:BaseUrl"] = "http://localhost:7147",
                ["McpServer:ApiKey"] = "test-api-key",
                ["McpServer:WorkspacePath"] = "E:/ws/default",
            })
            .Build();

        var workspaceContext = new WorkspaceContextViewModel();
        var webContext = new WebMcpContext(config, workspaceContext);

        var initialClient = await webContext.GetRequiredActiveWorkspaceApiClientAsync();
        Assert.Equal("E:/ws/default", webContext.ActiveWorkspacePath);
        Assert.Equal("E:/ws/default", initialClient.WorkspacePath);

        workspaceContext.ActiveWorkspacePath = "E:/ws/changed";

        var updatedClient = await webContext.GetRequiredActiveWorkspaceApiClientAsync();
        Assert.Equal("E:/ws/changed", webContext.ActiveWorkspacePath);
        Assert.Equal("E:/ws/changed", updatedClient.WorkspacePath);
    }

    [Fact]
    public void TodoListPage_ShowsLoadingBranch_WhenRequestInFlight()
    {
        var gate = new TaskCompletionSource<ListTodosResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var api = new TodoApiClientStub
        {
            OnListTodosAsync = (_, _) => gate.Task
        };

        using var ctx = CreateTestContext(services => services.AddSingleton<ITodoApiClient>(api));
        var cut = ctx.Render<McpServer.Web.Pages.Todos.TodoList>();

        cut.WaitForAssertion(() => Assert.Contains("Loading todos...", cut.Markup, StringComparison.Ordinal));
        gate.SetResult(new ListTodosResult([], 0));
    }

    [Fact]
    public void SessionLogListPage_ShowsErrorBranch_OnApiFailure()
    {
        var api = new SessionLogApiClientStub
        {
            OnListSessionLogsAsync = (_, _) => Task.FromException<ListSessionLogsResult>(new InvalidOperationException("session log failure"))
        };

        using var ctx = CreateTestContext(services => services.AddSingleton<ISessionLogApiClient>(api));
        var cut = ctx.Render<McpServer.Web.Pages.Sessions.SessionLogList>();

        cut.WaitForAssertion(() => Assert.Contains("Failed to load session logs", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void TemplateListPage_ShowsEmptyBranch_WhenNoItems()
    {
        var api = new TemplateApiClientStub
        {
            OnListTemplatesAsync = (_, _, _, _) => Task.FromResult(new ListTemplatesResult([], 0))
        };

        using var ctx = CreateTestContext(services => services.AddSingleton<ITemplateApiClient>(api));
        var cut = ctx.Render<McpServer.Web.Pages.Templates.TemplateList>();

        cut.WaitForAssertion(() => Assert.Contains("No templates found", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void RepresentativePages_ShowDataBranch_WhenItemsPresent()
    {
        var todoApi = new TodoApiClientStub
        {
            OnListTodosAsync = (_, _) => Task.FromResult(
                new ListTodosResult([new TodoListItem("TODO-001", "Task", "Architecture", "high", false, "2h")], 1))
        };

        var sessionApi = new SessionLogApiClientStub
        {
            OnListSessionLogsAsync = (_, _) => Task.FromResult(
                new ListSessionLogsResult(
                    [new SessionLogSummary("session-1", "Cursor", "Audit", "completed", "gpt", null, null, 1)],
                    1,
                    20,
                    0))
        };

        var templateApi = new TemplateApiClientStub
        {
            OnListTemplatesAsync = (_, _, _, _) => Task.FromResult(
                new ListTemplatesResult(
                    [new TemplateListItem("tpl-1", "Template One", "web", ["tag"], "desc")],
                    1))
        };

        using var ctx = CreateTestContext(services =>
        {
            services.AddSingleton<ITodoApiClient>(todoApi);
            services.AddSingleton<ISessionLogApiClient>(sessionApi);
            services.AddSingleton<ITemplateApiClient>(templateApi);
        });

        var todoCut = ctx.Render<McpServer.Web.Pages.Todos.TodoList>();
        todoCut.WaitForAssertion(() => Assert.Contains("TODO-001", todoCut.Markup, StringComparison.Ordinal));

        var sessionCut = ctx.Render<McpServer.Web.Pages.Sessions.SessionLogList>();
        sessionCut.WaitForAssertion(() => Assert.Contains("session-1", sessionCut.Markup, StringComparison.Ordinal));

        var templateCut = ctx.Render<McpServer.Web.Pages.Templates.TemplateList>();
        templateCut.WaitForAssertion(() => Assert.Contains("tpl-1", templateCut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void WorkspacePicker_PropagatesSelection_ToWorkspaceContext_AndCallback()
    {
        var workspaceApi = new WorkspaceApiClientStub
        {
            OnListWorkspacesAsync = _ => Task.FromResult(
                new ListWorkspacesResult(
                    [
                        new WorkspaceSummary("E:/ws/default", "Default", true, true),
                        new WorkspaceSummary("E:/ws/secondary", "Secondary", false, true)
                    ],
                    2))
        };

        using var ctx = CreateTestContext(services => services.AddSingleton<IWorkspaceApiClient>(workspaceApi));
        var workspaceContext = ctx.Services.GetRequiredService<WorkspaceContextViewModel>();
        workspaceContext.ActiveWorkspacePath = "E:/ws/default";

        string? callbackValue = null;
        var cut = ctx.Render<WorkspacePicker>(parameters => parameters
            .Add(x => x.SelectedWorkspaceChanged, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<string?>(this, value => callbackValue = value)));

        var select = cut.Find("select");
        select.Change("E:/ws/secondary");

        Assert.Equal("E:/ws/secondary", workspaceContext.ActiveWorkspacePath);
        Assert.Equal("E:/ws/secondary", callbackValue);
    }

    private static Bunit.BunitContext CreateTestContext(Action<IServiceCollection>? configureServices = null)
    {
        var ctx = new Bunit.BunitContext();
        ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        ctx.Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        ctx.Services.AddSingleton<McpServer.Cqrs.Dispatcher>();
        ctx.Services.AddUiCore();

        configureServices?.Invoke(ctx.Services);
        return ctx;
    }

    private sealed class TodoApiClientStub : ITodoApiClient
    {
        public Func<ListTodosQuery, CancellationToken, Task<ListTodosResult>>? OnListTodosAsync { get; init; }

        public Task<ListTodosResult> ListTodosAsync(ListTodosQuery query, CancellationToken cancellationToken = default)
            => OnListTodosAsync?.Invoke(query, cancellationToken) ?? Task.FromResult(new ListTodosResult([], 0));

        public Task<TodoDetail?> GetTodoAsync(string todoId, CancellationToken cancellationToken = default) => Task.FromResult<TodoDetail?>(null);
        public Task<TodoMutationOutcome> CreateTodoAsync(CreateTodoCommand command, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TodoMutationOutcome> UpdateTodoAsync(UpdateTodoCommand command, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TodoMutationOutcome> DeleteTodoAsync(DeleteTodoCommand command, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TodoRequirementsAnalysis> AnalyzeTodoRequirementsAsync(string todoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TodoPromptOutput> GenerateTodoStatusPromptAsync(string todoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TodoPromptOutput> GenerateTodoImplementPromptAsync(string todoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TodoPromptOutput> GenerateTodoPlanPromptAsync(string todoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class WorkspaceApiClientStub : IWorkspaceApiClient
    {
        public Func<CancellationToken, Task<ListWorkspacesResult>>? OnListWorkspacesAsync { get; init; }

        public Task<ListWorkspacesResult> ListWorkspacesAsync(CancellationToken ct = default)
            => OnListWorkspacesAsync?.Invoke(ct) ?? Task.FromResult(new ListWorkspacesResult([], 0));

        public Task<WorkspaceDetail?> GetWorkspaceAsync(string workspacePath, CancellationToken ct = default) => Task.FromResult<WorkspaceDetail?>(null);
        public Task<bool> UpdateWorkspacePolicyAsync(UpdateWorkspacePolicyCommand command, CancellationToken ct = default) => Task.FromResult(false);
        public Task<WorkspaceMutationOutcome> CreateWorkspaceAsync(CreateWorkspaceCommand command, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<WorkspaceMutationOutcome> UpdateWorkspaceAsync(UpdateWorkspaceCommand command, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<WorkspaceMutationOutcome> DeleteWorkspaceAsync(DeleteWorkspaceCommand command, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<WorkspaceProcessState> GetWorkspaceStatusAsync(string workspacePath, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<WorkspaceProcessState> StartWorkspaceAsync(string workspacePath, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<WorkspaceProcessState> StopWorkspaceAsync(string workspacePath, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<WorkspaceHealthState> CheckWorkspaceHealthAsync(string workspacePath, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<WorkspaceGlobalPromptState> GetWorkspaceGlobalPromptAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public Task<WorkspaceGlobalPromptState> UpdateWorkspaceGlobalPromptAsync(UpdateWorkspaceGlobalPromptCommand command, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<WorkspaceInitInfo> InitWorkspaceAsync(string workspacePath, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class SessionLogApiClientStub : ISessionLogApiClient
    {
        public Func<ListSessionLogsQuery, CancellationToken, Task<ListSessionLogsResult>>? OnListSessionLogsAsync { get; init; }

        public Task<ListSessionLogsResult> ListSessionLogsAsync(ListSessionLogsQuery query, CancellationToken cancellationToken = default)
            => OnListSessionLogsAsync?.Invoke(query, cancellationToken) ?? Task.FromResult(new ListSessionLogsResult([], 0, 20, 0));

        public Task<SessionLogDetail?> GetSessionLogAsync(string sessionId, CancellationToken cancellationToken = default) => Task.FromResult<SessionLogDetail?>(null);
        public Task<SessionLogSubmitOutcome> SubmitSessionLogAsync(SubmitSessionLogCommand command, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<SessionLogDialogAppendOutcome> AppendSessionLogDialogAsync(AppendSessionLogDialogCommand command, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class TemplateApiClientStub : ITemplateApiClient
    {
        public Func<string?, string?, string?, CancellationToken, Task<ListTemplatesResult>>? OnListTemplatesAsync { get; init; }

        public Task<ListTemplatesResult> ListTemplatesAsync(string? category, string? tag, string? keyword, CancellationToken cancellationToken = default)
            => OnListTemplatesAsync?.Invoke(category, tag, keyword, cancellationToken) ?? Task.FromResult(new ListTemplatesResult([], 0));

        public Task<TemplateDetail?> GetTemplateAsync(string templateId, CancellationToken cancellationToken = default) => Task.FromResult<TemplateDetail?>(null);
        public Task<TemplateMutationOutcome> CreateTemplateAsync(CreateTemplateCommand command, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TemplateMutationOutcome> UpdateTemplateAsync(UpdateTemplateCommand command, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TemplateMutationOutcome> DeleteTemplateAsync(string templateId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TemplateTestOutcome> TestTemplateAsync(TestTemplateQuery query, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class HealthApiClientStub : IHealthApiClient
    {
        public Task<HealthSnapshot> CheckHealthAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new HealthSnapshot(DateTimeOffset.UtcNow, "healthy", """{"status":"healthy"}"""));
    }

    private sealed class ContextApiClientStub : IContextApiClient
    {
        public Task<ContextSearchPayload> SearchAsync(SearchContextQuery query, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ContextPackPayload> PackAsync(PackContextQuery query, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ContextSourcesPayload> ListSourcesAsync(CancellationToken cancellationToken = default) => Task.FromResult(new ContextSourcesPayload([]));
        public Task<ContextRebuildResult> RebuildIndexAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class AuthConfigApiClientStub : IAuthConfigApiClient
    {
        public Task<AuthConfigSnapshot> GetAuthConfigAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AuthConfigSnapshot(false, null, null, null, null, null, DateTimeOffset.UtcNow));
    }
}
