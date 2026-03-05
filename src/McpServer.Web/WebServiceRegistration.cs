using McpServer.Cqrs;
using McpServer.UI.Core;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Services;
using McpServer.Web.Adapters;
using McpServer.Web.Authorization;
using McpServer.Web.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace McpServer.Web;

internal static class WebServiceRegistration
{
    public static IServiceCollection AddWebServices(this IServiceCollection services)
    {
        // IHttpContextAccessor required by WebRoleContext and BearerTokenAccessor.
        services.AddHttpContextAccessor();

        // Register WebRoleContext BEFORE AddUiCore so it wins over the AllowAllRoleContext TryAdd fallback.
        services.TryAddSingleton<IRoleContext, WebRoleContext>();

        // BearerTokenAccessor reads the OIDC access_token from the current HttpContext for API forwarding.
        services.AddScoped<BearerTokenAccessor>();

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
        services.AddScoped<IAgentApiClient, AgentApiClientAdapter>();
        services.AddScoped<ISseSubscriptionService, SseSubscriptionService>();

        return services;
    }
}
