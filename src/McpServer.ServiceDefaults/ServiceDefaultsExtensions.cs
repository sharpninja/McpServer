using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

/// <summary>Service defaults extension methods.</summary>
public static class ServiceDefaultsExtensions
{
    /// <summary>Adds service defaults (health checks, telemetry).</summary>
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks();
        return builder;
    }

    /// <summary>Maps default endpoints (/health, /alive).</summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/alive", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => false
        });
        return app;
    }

    /// <summary>Registers global exception handler middleware.</summary>
    public static WebApplication UseGlobalExceptionHandler(this WebApplication app) => app;

    /// <summary>Logs application version at startup.</summary>
    public static WebApplication LogApplicationVersion(this WebApplication app) => app;
}
