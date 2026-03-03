using System.Reflection;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace McpServer.UI.Core;

/// <summary>
/// DI registration extensions for McpServer.UI.Core.
/// Registers ViewModels, CQRS handlers, and the <see cref="IViewModelRegistry"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all UI.Core ViewModels, CQRS handlers from this assembly,
    /// and the <see cref="IViewModelRegistry"/> scanning this assembly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="additionalViewModelAssemblies">Extra assemblies to scan for ViewModels.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddUiCore(
        this IServiceCollection services,
        params Assembly[] additionalViewModelAssemblies)
    {
        var thisAssembly = typeof(ServiceCollectionExtensions).Assembly;

        // Register CQRS handlers from this assembly
        services.AddCqrsHandlers(thisAssembly);

        // Default permissive auth services (hosts should override with real RBAC implementations)
        services.TryAddSingleton<IRoleContext, AllowAllRoleContext>();
        services.TryAddSingleton<IAuthorizationPolicyService, AllowAllAuthorizationPolicyService>();

        // Register shared workspace context as singleton so all ViewModels observe the same instance
        services.AddSingleton<WorkspaceContextViewModel>();

        // Register ViewModels as transient
        services.AddTransient<WorkspaceListViewModel>();
        services.AddTransient<WorkspaceDetailViewModel>();
        services.AddTransient<WorkspacePolicyViewModel>();
        services.AddTransient<HealthSnapshotsViewModel>();
        services.AddTransient<SessionLogListViewModel>();
        services.AddTransient<SessionLogDetailViewModel>();
        services.AddTransient<DispatcherLogsViewModel>();
        services.AddTransient<RepoListViewModel>();
        services.AddTransient<RepoFileViewModel>();
        services.AddTransient<WriteRepoFileViewModel>();
        services.AddTransient<ContextSearchViewModel>();
        services.AddTransient<ContextPackViewModel>();
        services.AddTransient<ContextSourcesViewModel>();
        services.AddTransient<ContextRebuildIndexViewModel>();
        services.AddTransient<AuthConfigViewModel>();
        services.AddTransient<DiagnosticExecutionPathViewModel>();
        services.AddTransient<DiagnosticAppSettingsPathViewModel>();
        services.AddTransient<TodoListViewModel>();
        services.AddTransient<TodoDetailViewModel>();
        services.AddTransient<CreateTodoViewModel>();
        services.AddTransient<UpdateTodoViewModel>();
        services.AddTransient<DeleteTodoViewModel>();
        services.AddTransient<AnalyzeTodoRequirementsViewModel>();
        services.AddTransient<TodoStatusPromptViewModel>();
        services.AddTransient<TodoImplementPromptViewModel>();
        services.AddTransient<TodoPlanPromptViewModel>();
        services.AddTransient<TunnelListViewModel>();
        services.AddTransient<TemplateListViewModel>();
        services.AddTransient<TemplateDetailViewModel>();
        services.AddTransient<AgentPoolViewModel>();

        // Register the ViewModelRegistry scanning this assembly + any extras
        var allAssemblies = new List<Assembly> { thisAssembly };
        allAssemblies.AddRange(additionalViewModelAssemblies);

        services.AddSingleton<IViewModelRegistry>(sp =>
            new ViewModelRegistry(sp, allAssemblies));

        return services;
    }
}
