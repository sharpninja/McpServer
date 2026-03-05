using McpServer.Cqrs;
using McpServer.UI.Core;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

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

        // Provide a stub health API client so BackendConnectionMonitor resolves cleanly.
        var healthStub = Substitute.For<IHealthApiClient>();
        healthStub.CheckHealthAsync(Arg.Any<CancellationToken>())
            .Returns(new HealthSnapshot(DateTimeOffset.UtcNow, "healthy", "{}", null));
        services.AddSingleton(healthStub);

        services.AddUiCore();

        configureServices?.Invoke(services);
        return services.BuildServiceProvider();
    }
}
