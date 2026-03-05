using McpServer.Cqrs;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using McpServer.UI.Core.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace McpServer.UI.Core.Tests.Handlers;

public sealed class WebUiHandlerApiDispatchMappingTests
{
    [Fact]
    public async Task Dispatcher_WebUiBackedRequests_RouteToExpectedApiClients()
    {
        var todoApi = Substitute.For<ITodoApiClient>();
        todoApi.ListTodosAsync(Arg.Any<ListTodosQuery>(), Arg.Any<CancellationToken>())
            .Returns(new ListTodosResult([], 0));
        todoApi.GetTodoAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((TodoDetail?)null);

        var sessionApi = Substitute.For<ISessionLogApiClient>();
        sessionApi.ListSessionLogsAsync(Arg.Any<ListSessionLogsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new ListSessionLogsResult([], 0, 20, 0));
        sessionApi.GetSessionLogAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((SessionLogDetail?)null);

        var templateApi = Substitute.For<ITemplateApiClient>();
        templateApi.ListTemplatesAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ListTemplatesResult([], 0));
        templateApi.GetTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((TemplateDetail?)null);
        templateApi.TestTemplateAsync(Arg.Any<TestTemplateQuery>(), Arg.Any<CancellationToken>())
            .Returns(new TemplateTestOutcome(true, "ok", null, null));

        var contextApi = Substitute.For<IContextApiClient>();
        contextApi.SearchAsync(Arg.Any<SearchContextQuery>(), Arg.Any<CancellationToken>())
            .Returns(new ContextSearchPayload("q", [], []));
        contextApi.ListSourcesAsync(Arg.Any<CancellationToken>())
            .Returns(new ContextSourcesPayload([]));

        var healthApi = Substitute.For<IHealthApiClient>();
        healthApi.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new HealthSnapshot(DateTimeOffset.UtcNow, "healthy", """{"status":"healthy"}"""));

        var authApi = Substitute.For<IAuthConfigApiClient>();
        authApi.GetAuthConfigAsync(Arg.Any<CancellationToken>())
            .Returns(new AuthConfigSnapshot(false, null, null, null, null, null, DateTimeOffset.UtcNow));

        var workspaceApi = Substitute.For<IWorkspaceApiClient>();
        workspaceApi.ListWorkspacesAsync(Arg.Any<CancellationToken>())
            .Returns(new ListWorkspacesResult([], 0));

        using var host = UiCoreTestHost.Create(services =>
        {
            services.AddSingleton(todoApi);
            services.AddSingleton(sessionApi);
            services.AddSingleton(templateApi);
            services.AddSingleton(contextApi);
            services.AddSingleton(healthApi);
            services.AddSingleton(authApi);
            services.AddSingleton(workspaceApi);
        });

        var dispatcher = host.GetRequiredService<Dispatcher>();

        await dispatcher.QueryAsync(new ListTodosQuery());
        await dispatcher.QueryAsync(new GetTodoQuery("TODO-001"));
        await dispatcher.QueryAsync(new ListSessionLogsQuery());
        await dispatcher.QueryAsync(new GetSessionLogQuery("session-1"));
        await dispatcher.QueryAsync(new ListTemplatesQuery());
        await dispatcher.QueryAsync(new GetTemplateQuery("tpl-1"));
        await dispatcher.QueryAsync(new TestTemplateQuery { TemplateId = "tpl-1", VariablesJson = "{}" });
        await dispatcher.QueryAsync(new SearchContextQuery { Query = "audit" });
        await dispatcher.QueryAsync(new ListContextSourcesQuery());
        await dispatcher.QueryAsync(new CheckHealthQuery());
        await dispatcher.QueryAsync(new GetAuthConfigQuery());
        await dispatcher.QueryAsync(new ListWorkspacesQuery());

        await todoApi.Received(1).ListTodosAsync(Arg.Any<ListTodosQuery>(), Arg.Any<CancellationToken>());
        await todoApi.Received(1).GetTodoAsync("TODO-001", Arg.Any<CancellationToken>());
        await sessionApi.Received(1).ListSessionLogsAsync(Arg.Any<ListSessionLogsQuery>(), Arg.Any<CancellationToken>());
        await sessionApi.Received(1).GetSessionLogAsync("session-1", Arg.Any<CancellationToken>());
        await templateApi.Received(1).ListTemplatesAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await templateApi.Received(1).GetTemplateAsync("tpl-1", Arg.Any<CancellationToken>());
        await templateApi.Received(1).TestTemplateAsync(Arg.Any<TestTemplateQuery>(), Arg.Any<CancellationToken>());
        await contextApi.Received(1).SearchAsync(Arg.Any<SearchContextQuery>(), Arg.Any<CancellationToken>());
        await contextApi.Received(1).ListSourcesAsync(Arg.Any<CancellationToken>());
        await healthApi.Received(1).CheckHealthAsync(Arg.Any<CancellationToken>());
        await authApi.Received(1).GetAuthConfigAsync(Arg.Any<CancellationToken>());
        await workspaceApi.Received(1).ListWorkspacesAsync(Arg.Any<CancellationToken>());
    }
}
