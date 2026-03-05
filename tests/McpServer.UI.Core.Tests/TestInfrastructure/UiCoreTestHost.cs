using McpServer.Cqrs;
using McpServer.UI.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.UI.Core.Tests.TestInfrastructure;

/// <summary>
/// Shared test host factory for UI.Core dispatcher, handlers, and ViewModels.
/// </summary>
internal static class UiCoreTestHost
{
    public static ServiceProvider Create(Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        // Register dispatcher directly to avoid logger-provider circular setup in tests.
        services.AddSingleton<Dispatcher>();
        services.AddUiCore();

        configureServices?.Invoke(services);
        return services.BuildServiceProvider();
    }
}
