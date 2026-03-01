using McpServer.Client;
using McpServer.Cqrs;
using McpServer.Director.Auth;
using McpServer.Director.Helpers;
using McpServer.UI.Core;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace McpServer.Director;

/// <summary>
/// TR-MCP-DRY-001: Shared DI registration for all Director entry points (exec CLI and interactive TUI).
/// Eliminates duplicated service wiring between <see cref="Program"/> and
/// <see cref="Commands.InteractiveCommand"/>.
/// </summary>
internal static class DirectorServiceRegistration
{
    /// <summary>
    /// Configures all Director services: logging, CQRS, UI Core, authorization, HTTP clients, and API adapters.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
    /// <param name="workspace">Optional workspace path override.</param>
    /// <returns>
    /// The <see cref="DirectorMcpContext"/> created during registration, needed by callers
    /// that wire up screens or resolve ViewModels before calling <see cref="BuildAndFinalize"/>.
    /// </returns>
    public static DirectorMcpContext Configure(IServiceCollection services, string? workspace = null)
    {
        services.AddDirectorLogging();
        services.AddCqrs(typeof(Program).Assembly);
        services.AddUiCore();

        services.AddSingleton<IBrowserLauncher, BrowserLauncher>();

        // Override default permissive auth with Director-specific implementations
        services.RemoveAll<IRoleContext>();
        services.RemoveAll<IAuthorizationPolicyService>();
        services.AddSingleton<IRoleContext, DirectorRoleContext>();
        services.AddSingleton<IAuthorizationPolicyService, DirectorAuthorizationPolicyService>();

        // Build HTTP clients for control-plane and active workspace
        var activeWorkspaceClient = McpHttpClient.FromMarkerOnly(workspace);
        activeWorkspaceClient?.TrySetCachedBearerToken();

        var controlClient = McpHttpClient.FromDefaultUrlOrMarker(workspace);
        controlClient?.TrySetCachedBearerToken();

        var directorContext = new DirectorMcpContext(controlClient, activeWorkspaceClient);
        services.AddSingleton(directorContext);
        if (controlClient is not null)
            services.AddSingleton(controlClient);

        // Register API client adapters — all delegate to DirectorMcpContext for workspace switching
        services.AddSingleton<IHealthApiClient>(_ => new HealthApiClientAdapter(directorContext.ControlClient));
        services.AddSingleton<ISessionLogApiClient>(_ => new SessionLogApiClientAdapter(directorContext));
        services.AddSingleton<IWorkspaceApiClient>(_ => new WorkspaceApiClientAdapter(directorContext));
        services.AddSingleton<IRepoApiClient>(_ => new RepoApiClientAdapter(directorContext));
        services.AddSingleton<IContextApiClient>(_ => new ContextApiClientAdapter(directorContext));
        services.AddSingleton<IAuthConfigApiClient>(_ => new AuthConfigApiClientAdapter(directorContext));
        services.AddSingleton<IDiagnosticApiClient>(_ => new DiagnosticApiClientAdapter(directorContext));
        services.AddSingleton<ITodoApiClient>(_ => new TodoApiClientAdapter(directorContext));
        services.AddSingleton<ITunnelApiClient>(_ => new TunnelApiClientAdapter(directorContext));
        services.AddSingleton<ITemplateApiClient>(_ => new TemplateApiClientAdapter(directorContext));

        return directorContext;
    }

    /// <summary>
    /// Builds the <see cref="ServiceProvider"/> and attaches the CQRS <see cref="Dispatcher"/>
    /// as an <see cref="ILoggerProvider"/> post-construction to break the circular dependency.
    /// </summary>
    /// <param name="services">The fully configured service collection.</param>
    /// <returns>The built service provider.</returns>
    public static ServiceProvider BuildAndFinalize(IServiceCollection services)
    {
        var sp = services.BuildServiceProvider();

        // Dispatcher implements ILoggerProvider but also needs ILogger<Dispatcher> —
        // adding it post-construction breaks the circular dependency.
        sp.GetRequiredService<ILoggerFactory>().AddProvider(sp.GetRequiredService<Dispatcher>());

        return sp;
    }
}
