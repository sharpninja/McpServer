using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using McpServer.UI.Core.Tests.TestInfrastructure;
using McpServer.UI.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace McpServer.UI.Core.Tests.ViewModels;

public sealed class WebUiPhase5ViewModelTests
{
    [Fact]
    public async Task DashboardViewModel_LoadAsync_SetsCounters_OnSuccess()
    {
        var workspaceApi = Substitute.For<IWorkspaceApiClient>();
        workspaceApi.ListWorkspacesAsync(Arg.Any<CancellationToken>())
            .Returns(new ListWorkspacesResult(
                [new WorkspaceSummary("ws", "c:/repo", true, true)],
                1));

        var todoApi = Substitute.For<ITodoApiClient>();
        todoApi.ListTodosAsync(Arg.Any<ListTodosQuery>(), Arg.Any<CancellationToken>())
            .Returns(new ListTodosResult([new TodoListItem("TODO-001", "Task", "Architecture", "high", false, null)], 1));

        var sessionApi = Substitute.For<ISessionLogApiClient>();
        sessionApi.ListSessionLogsAsync(Arg.Any<ListSessionLogsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new ListSessionLogsResult(
                [new SessionLogSummary("session-1", "Cursor", "title", "completed", "model", null, null, 1)],
                1, 20, 0));

        var templateApi = Substitute.For<ITemplateApiClient>();
        templateApi.ListTemplatesAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ListTemplatesResult(
                [new TemplateListItem("tpl-1", "Template", "web", [], null)],
                1));

        var healthApi = Substitute.For<IHealthApiClient>();
        healthApi.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new HealthSnapshot(DateTimeOffset.UtcNow, "healthy", """{"status":"healthy"}"""));

        using var host = UiCoreTestHost.Create(services =>
        {
            services.AddSingleton(workspaceApi);
            services.AddSingleton(todoApi);
            services.AddSingleton(sessionApi);
            services.AddSingleton(templateApi);
            services.AddSingleton(healthApi);
        });

        var viewModel = host.GetRequiredService<DashboardViewModel>();
        await viewModel.LoadAsync();

        Assert.Null(viewModel.ErrorMessage);
        Assert.False(viewModel.IsLoading);
        Assert.Equal(1, viewModel.WorkspaceCount);
        Assert.Equal(1, viewModel.TodoCount);
        Assert.Equal(1, viewModel.SessionLogCount);
        Assert.Equal(1, viewModel.TemplateCount);
        Assert.Equal("healthy", viewModel.HealthStatus);
    }

    [Fact]
    public async Task DashboardViewModel_LoadAsync_ResetsState_OnFailure()
    {
        var workspaceApi = Substitute.For<IWorkspaceApiClient>();
        workspaceApi.ListWorkspacesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ListWorkspacesResult>(new InvalidOperationException("workspace failure")));

        using var host = UiCoreTestHost.Create(services =>
        {
            services.AddSingleton(workspaceApi);
            services.AddSingleton(Substitute.For<ITodoApiClient>());
            services.AddSingleton(Substitute.For<ISessionLogApiClient>());
            services.AddSingleton(Substitute.For<ITemplateApiClient>());
            services.AddSingleton(Substitute.For<IHealthApiClient>());
        });

        var viewModel = host.GetRequiredService<DashboardViewModel>();
        await viewModel.LoadAsync();

        Assert.False(viewModel.IsLoading);
        Assert.Equal(0, viewModel.WorkspaceCount);
        Assert.Equal(0, viewModel.TodoCount);
        Assert.Equal(0, viewModel.SessionLogCount);
        Assert.Equal(0, viewModel.TemplateCount);
        Assert.Equal("unknown", viewModel.HealthStatus);
        Assert.True(string.IsNullOrWhiteSpace(viewModel.ErrorMessage));
    }

    [Fact]
    public async Task TodoListViewModel_LoadAsync_NormalizesFilters_AndMapsResults()
    {
        ListTodosQuery? capturedQuery = null;
        var todoApi = Substitute.For<ITodoApiClient>();
        todoApi.ListTodosAsync(Arg.Any<ListTodosQuery>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                capturedQuery = call.ArgAt<ListTodosQuery>(0);
                return new ListTodosResult(
                    [new TodoListItem("TODO-123", "Title", "Architecture", "high", false, "2h")],
                    1);
            });

        using var host = UiCoreTestHost.Create(services => services.AddSingleton(todoApi));
        var viewModel = host.GetRequiredService<TodoListViewModel>();
        viewModel.Keyword = "   ";
        viewModel.Section = " Architecture ";
        viewModel.Priority = " high ";
        viewModel.TodoId = " TODO-123 ";
        viewModel.Done = false;

        await viewModel.LoadAsync();

        Assert.NotNull(capturedQuery);
        Assert.Null(capturedQuery!.Keyword);
        Assert.Equal("Architecture", capturedQuery.Section);
        Assert.Equal("high", capturedQuery.Priority);
        Assert.Equal("TODO-123", capturedQuery.Id);
        Assert.False(capturedQuery.Done);
        Assert.Single(viewModel.Items);
        Assert.Equal("TODO-123", viewModel.Items[0].Id);
    }

    [Fact]
    public async Task TodoDetailViewModel_LoadAsync_UsesTodoId_AndSetsErrorOnFailure()
    {
        var todoApi = Substitute.For<ITodoApiClient>();
        todoApi.GetTodoAsync("TODO-100", Arg.Any<CancellationToken>())
            .Returns(new TodoDetail(
                "TODO-100",
                "Fix test",
                "Architecture",
                "high",
                false,
                null,
                null,
                [],
                [],
                [],
                null,
                null,
                null,
                null,
                null,
                [],
                [],
                []));

        using var host = UiCoreTestHost.Create(services => services.AddSingleton(todoApi));
        var viewModel = host.GetRequiredService<TodoDetailViewModel>();
        viewModel.TodoId = "TODO-100";
        await viewModel.LoadAsync();

        Assert.NotNull(viewModel.Detail);
        Assert.Equal("TODO-100", viewModel.Detail!.Id);
        Assert.Null(viewModel.ErrorMessage);

        todoApi.GetTodoAsync("TODO-100", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TodoDetail?>(new InvalidOperationException("todo lookup failed")));

        await viewModel.LoadAsync();
        Assert.False(string.IsNullOrWhiteSpace(viewModel.ErrorMessage));
    }

    [Fact]
    public async Task SessionLogListViewModel_LoadAsync_NormalizesPagingAndFilters()
    {
        ListSessionLogsQuery? captured = null;
        var sessionApi = Substitute.For<ISessionLogApiClient>();
        sessionApi.ListSessionLogsAsync(Arg.Any<ListSessionLogsQuery>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured = call.ArgAt<ListSessionLogsQuery>(0);
                return new ListSessionLogsResult([], 0, 20, 0);
            });

        using var host = UiCoreTestHost.Create(services => services.AddSingleton(sessionApi));
        var viewModel = host.GetRequiredService<SessionLogListViewModel>();
        viewModel.Agent = "Cursor";
        viewModel.Model = "gpt";
        viewModel.Text = "audit";
        viewModel.Limit = 0;
        viewModel.Offset = -10;

        await viewModel.LoadAsync();

        Assert.NotNull(captured);
        Assert.Equal("Cursor", captured!.Agent);
        Assert.Equal("gpt", captured.Model);
        Assert.Equal("audit", captured.Text);
        Assert.Equal(20, captured.Limit);
        Assert.Equal(0, captured.Offset);
    }

    [Fact]
    public async Task SessionLogDetailViewModel_LoadAsync_PopulatesEntryContracts()
    {
        var sessionApi = Substitute.For<ISessionLogApiClient>();
        sessionApi.GetSessionLogAsync("session-1", Arg.Any<CancellationToken>())
            .Returns(new SessionLogDetail(
                "session-1",
                "Cursor",
                "Audit",
                "completed",
                "gpt",
                "2026-01-01T00:00:00Z",
                "2026-01-01T00:10:00Z",
                1,
                123,
                null,
                null,
                null,
                [
                    new SessionLogEntryDetail(
                        "req-1",
                        "2026-01-01T00:00:00Z",
                        "Query title",
                        "Query text",
                        "Response",
                        "Interpretation",
                        "completed",
                        "gpt",
                        null,
                        50,
                        null,
                        null,
                        null,
                        ["audit"],
                        ["src/file.cs"],
                        ["decision"],
                        [],
                        [],
                        [],
                        [new SessionLogActionDetail(1, "desc", "edit", "completed", "src/file.cs")],
                        [],
                        [])
                ]));

        using var host = UiCoreTestHost.Create(services => services.AddSingleton(sessionApi));
        var viewModel = host.GetRequiredService<SessionLogDetailViewModel>();
        viewModel.SessionId = "session-1";
        await viewModel.LoadAsync();

        Assert.NotNull(viewModel.Detail);
        Assert.Single(viewModel.Detail!.Entries);
        Assert.Equal("req-1", viewModel.Detail.Entries[0].RequestId);
        Assert.Single(viewModel.Detail.Entries[0].Actions);
    }

    [Fact]
    public async Task TemplateListViewModel_LoadAsync_UsesFilters()
    {
        string? capturedCategory = null;
        string? capturedTag = null;
        string? capturedKeyword = null;
        var templateApi = Substitute.For<ITemplateApiClient>();
        templateApi.ListTemplatesAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                capturedCategory = call.ArgAt<string?>(0);
                capturedTag = call.ArgAt<string?>(1);
                capturedKeyword = call.ArgAt<string?>(2);
                return new ListTemplatesResult([], 0);
            });

        using var host = UiCoreTestHost.Create(services => services.AddSingleton(templateApi));
        var viewModel = host.GetRequiredService<TemplateListViewModel>();
        viewModel.Category = "cat";
        viewModel.Tag = "tag";
        viewModel.Keyword = "keyword";
        await viewModel.LoadAsync();

        Assert.Equal("cat", capturedCategory);
        Assert.Equal("tag", capturedTag);
        Assert.Equal("keyword", capturedKeyword);
    }

    [Fact]
    public async Task TemplateDetailViewModel_LoadAsync_HandlesFoundAndMissingTemplates()
    {
        var templateApi = Substitute.For<ITemplateApiClient>();
        templateApi.GetTemplateAsync("tpl-1", Arg.Any<CancellationToken>())
            .Returns(new TemplateDetail("tpl-1", "Title", "web", [], null, "handlebars", [], "content"));

        using var host = UiCoreTestHost.Create(services => services.AddSingleton(templateApi));
        var viewModel = host.GetRequiredService<TemplateDetailViewModel>();
        await viewModel.LoadAsync("tpl-1");
        Assert.NotNull(viewModel.Detail);
        Assert.Equal("tpl-1", viewModel.Detail!.Id);

        templateApi.GetTemplateAsync("tpl-2", Arg.Any<CancellationToken>())
            .Returns((TemplateDetail?)null);
        await viewModel.LoadAsync("tpl-2");
        Assert.Null(viewModel.Detail);
        Assert.Equal("Template not found.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task TemplateTestViewModel_RunAsync_HandlesGatingAndSuccess()
    {
        var templateApi = Substitute.For<ITemplateApiClient>();
        templateApi.TestTemplateAsync(Arg.Any<TestTemplateQuery>(), Arg.Any<CancellationToken>())
            .Returns(new TemplateTestOutcome(true, "rendered", null, null));

        using var host = UiCoreTestHost.Create(services => services.AddSingleton(templateApi));
        var viewModel = host.GetRequiredService<TemplateTestViewModel>();

        await viewModel.RunAsync();
        Assert.Null(viewModel.Result);
        Assert.Null(viewModel.ErrorMessage);
        Assert.False(viewModel.IsLoading);

        viewModel.TemplateId = "tpl-1";
        await viewModel.RunAsync();
        Assert.NotNull(viewModel.Result);
        Assert.True(viewModel.Result!.Success);
        Assert.Equal("rendered", viewModel.Result.RenderedContent);
    }

    [Fact]
    public async Task ContextSearchViewModel_LoadAsync_NormalizesLimitAndSourceFilter()
    {
        SearchContextQuery? captured = null;
        var contextApi = Substitute.For<IContextApiClient>();
        contextApi.ListSourcesAsync(Arg.Any<CancellationToken>())
            .Returns(new ContextSourcesPayload(
                [new ContextSourceView("one", "todo", null), new ContextSourceView("two", "sessionlog", null)]));
        contextApi.SearchAsync(Arg.Any<SearchContextQuery>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured = call.ArgAt<SearchContextQuery>(0);
                return new ContextSearchPayload("query", [], []);
            });

        using var host = UiCoreTestHost.Create(services => services.AddSingleton(contextApi));
        var viewModel = host.GetRequiredService<ContextSearchViewModel>();
        viewModel.Query = " test ";
        viewModel.SourceType = " todo ";
        viewModel.Limit = -1;
        await viewModel.LoadAsync();

        Assert.NotNull(captured);
        Assert.Equal(" test ", captured!.Query);
        Assert.Equal("todo", captured.SourceType);
        Assert.Equal(20, captured.Limit);
        Assert.NotEmpty(viewModel.SourceTypes);
    }

    [Fact]
    public async Task HealthSnapshotsViewModel_CheckAsync_UpdatesHistoryAndStatus()
    {
        var healthApi = Substitute.For<IHealthApiClient>();
        healthApi.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(
                new HealthSnapshot(DateTimeOffset.UtcNow.AddMinutes(-1), "degraded", """{"status":"degraded"}"""),
                new HealthSnapshot(DateTimeOffset.UtcNow, "healthy", """{"status":"healthy"}"""));

        using var host = UiCoreTestHost.Create(services => services.AddSingleton(healthApi));
        var viewModel = host.GetRequiredService<HealthSnapshotsViewModel>();

        await viewModel.CheckAsync();
        await viewModel.CheckAsync();

        Assert.Equal(2, viewModel.Items.Count);
        Assert.Equal(2, viewModel.TotalCount);
        Assert.Equal(0, viewModel.SelectedIndex);
        Assert.Contains("Health check recorded", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("healthy", viewModel.Items[0].Status);
    }

    [Fact]
    public async Task AuthConfigViewModel_LoadAsync_PopulatesSnapshot()
    {
        var authApi = Substitute.For<IAuthConfigApiClient>();
        authApi.GetAuthConfigAsync(Arg.Any<CancellationToken>())
            .Returns(new AuthConfigSnapshot(
                true,
                "https://issuer",
                "client",
                "scope",
                "https://issuer/device",
                "https://issuer/token",
                DateTimeOffset.UtcNow));

        using var host = UiCoreTestHost.Create(services => services.AddSingleton(authApi));
        var viewModel = host.GetRequiredService<AuthConfigViewModel>();
        await viewModel.LoadAsync();

        Assert.NotNull(viewModel.Snapshot);
        Assert.True(viewModel.Snapshot!.Enabled);
        Assert.Equal("Auth config loaded.", viewModel.StatusMessage);
        Assert.Null(viewModel.ErrorMessage);
    }
}
