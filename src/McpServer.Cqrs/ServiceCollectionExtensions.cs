using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace McpServer.Cqrs;

/// <summary>
/// TR-MCP-CQRS-001: DI registration extensions for the CQRS framework.
/// Registers the <see cref="Dispatcher"/>, scans assemblies for handlers, and registers pipeline behaviors.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the CQRS <see cref="Dispatcher"/> as a singleton.
    /// Call <see cref="AddCqrsLoggerProvider"/> separately if you want the Dispatcher
    /// to also act as an <see cref="ILoggerProvider"/> (must be registered AFTER logging
    /// infrastructure to avoid a circular dependency).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCqrsDispatcher(this IServiceCollection services)
    {
        services.AddSingleton<Dispatcher>();
        services.AddSingleton<IDispatcher>(sp => sp.GetRequiredService<Dispatcher>());
        return services;
    }

    /// <summary>
    /// Registers the <see cref="Dispatcher"/> as an <see cref="ILoggerProvider"/> for correlation-enriched logging.
    /// Must be called AFTER <see cref="AddCqrsDispatcher"/> and AFTER logging is configured to avoid
    /// a circular dependency (Dispatcher → ILogger → ILoggerFactory → ILoggerProvider → Dispatcher).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCqrsLoggerProvider(this IServiceCollection services)
    {
        services.AddSingleton<ILoggerProvider>(sp => new DeferredDispatcherLoggerProvider(() => sp.GetRequiredService<Dispatcher>()));
        return services;
    }

    /// <summary>
    /// Scans the specified assemblies for <see cref="ICommandHandler{TCommand,TResult}"/> and
    /// <see cref="IQueryHandler{TQuery,TResult}"/> implementations and registers them as transient services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for handlers.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCqrsHandlers(this IServiceCollection services, params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false });

            foreach (var type in types)
            {
                var interfaces = type.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                        (i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
                         i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)));

                foreach (var iface in interfaces)
                {
                    services.AddTransient(iface, type);
                }
            }
        }

        return services;
    }

    /// <summary>
    /// Registers a pipeline behavior. Behaviors are executed in registration order (first registered = outermost).
    /// </summary>
    /// <typeparam name="TBehavior">The behavior type implementing <see cref="IPipelineBehavior"/>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCqrsBehavior<TBehavior>(this IServiceCollection services)
        where TBehavior : class, IPipelineBehavior
    {
        services.AddTransient<IPipelineBehavior, TBehavior>();
        return services;
    }

    /// <summary>
    /// Convenience method: registers the Dispatcher, scans assemblies for handlers, and optionally adds behaviors.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="handlerAssemblies">Assemblies to scan for command/query handlers.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCqrs(this IServiceCollection services, params Assembly[] handlerAssemblies)
    {
        services.AddCqrsDispatcher();
        services.AddCqrsHandlers(handlerAssemblies);
        return services;
    }
}

internal sealed class DeferredDispatcherLoggerProvider : ILoggerProvider
{
    private readonly Func<Dispatcher> _dispatcherFactory;

    public DeferredDispatcherLoggerProvider(Func<Dispatcher> dispatcherFactory)
    {
        _dispatcherFactory = dispatcherFactory;
    }

    public ILogger CreateLogger(string categoryName) => new DeferredDispatcherLogger(_dispatcherFactory, categoryName);

    public void Dispose()
    {
    }
}

internal sealed class DeferredDispatcherLogger : ILogger
{
    private readonly Func<Dispatcher> _dispatcherFactory;
    private readonly string _categoryName;
    private ILogger? _inner;

    public DeferredDispatcherLogger(Func<Dispatcher> dispatcherFactory, string categoryName)
    {
        _dispatcherFactory = dispatcherFactory;
        _categoryName = categoryName;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => GetInnerLogger().BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) => GetInnerLogger().IsEnabled(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
        => GetInnerLogger().Log(logLevel, eventId, state, exception, formatter);

    private ILogger GetInnerLogger()
    {
        _inner ??= _dispatcherFactory().CreateLogger(_categoryName);
        return _inner;
    }
}
