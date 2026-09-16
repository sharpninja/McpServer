using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using McpServer.Support.Mcp.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>TR-MCP-HOSTILEREVIEW-006: Registers the hostile-review queue-and-record pipeline.</summary>
public static class HostileReviewServiceCollectionExtensions
{
    /// <summary>Adds hostile-review services.</summary>
    public static IServiceCollection AddHostileReviewServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOptions<HostileReviewWorkerOptions>()
            .BindConfiguration(HostileReviewWorkerOptions.SectionName)
            .Validate(
                options =>
                {
                    try
                    {
                        options.Validate();
                        return true;
                    }
                    catch (InvalidOperationException)
                    {
                        return false;
                    }
                },
                "HostileReviewWorker options failed startup validation.")
            .ValidateOnStart();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<HostileReviewService>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<HostileReviewWorkerOptions>>().Value;
            var db = sp.GetRequiredService<McpServer.Support.Mcp.Storage.McpDbContext>();
            return new HostileReviewService(db, options, sp.GetService<TimeProvider>());
        });
        services.AddScoped<IHostileReviewService>(sp => sp.GetRequiredService<HostileReviewService>());
        services.AddScoped<IHostileReviewWorker>(sp => sp.GetRequiredService<HostileReviewService>());
        services.AddScoped<McpServer.Cqrs.Mvvm.IHostileReviewDirectorExecutor, HostileReviewDirectorExecutor>();
        return services;
    }
}
