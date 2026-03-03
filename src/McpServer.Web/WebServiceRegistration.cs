using McpServer.Cqrs;
using McpServer.UI.Core;
using McpServer.UI.Core.Services;
using McpServer.Web.Adapters;
using McpServer.Web.Services;

namespace McpServer.Web;

internal static class WebServiceRegistration
{
    public static IServiceCollection AddWebServices(this IServiceCollection services)
    {
        // Avoid registering Dispatcher as an ILoggerProvider (AddCqrs) to prevent startup DI/logger cycles.
        services.AddSingleton<Dispatcher>();
        services.AddUiCore(typeof(WebServiceRegistration).Assembly);

        services.AddScoped<WebMcpContext>();
        services.AddScoped<ITodoApiClient, TodoApiClientAdapter>();
        services.AddScoped<IWorkspaceApiClient, WorkspaceApiClientAdapter>();
        services.AddScoped<ISessionLogApiClient, SessionLogApiClientAdapter>();
        services.AddScoped<IHealthApiClient, HealthApiClientAdapter>();
        services.AddScoped<ITemplateApiClient, TemplateApiClientAdapter>();
        services.AddScoped<IContextApiClient, ContextApiClientAdapter>();
        services.AddScoped<IAuthConfigApiClient, AuthConfigApiClientAdapter>();
        services.AddScoped<ISseSubscriptionService, SseSubscriptionService>();

        return services;
    }
}
