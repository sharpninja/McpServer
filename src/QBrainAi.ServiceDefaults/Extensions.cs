using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

// ReSharper disable once CheckNamespace — Aspire service defaults live in Microsoft.Extensions.Hosting by convention.

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// TR-INFRA-001: Adds common .NET Aspire services — service discovery, resilience, health checks, and OpenTelemetry —
/// to every service project in the solution. Reference this project from each service.
/// </summary>
/// <seealso href="https://aka.ms/dotnet/aspire/service-defaults"/>
public static class ServiceDefaultsExtensions
{
    /// <summary>
    /// TR-INFRA-001: Registers Aspire service defaults including OpenTelemetry, health checks,
    /// service discovery, and HTTP client resilience.
    /// </summary>
    /// <param name="builder">The host application builder to configure.</param>
    /// <returns>The same builder instance for chaining.</returns>
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        {
            options.AllowedSchemes = ["https"];
        });

        return builder;
    }

    /// <summary>
    /// TR-INFRA-001: Configures OpenTelemetry logging, metrics (ASP.NET Core, HTTP, runtime),
    /// and distributed tracing (ASP.NET Core, HTTP) for the application.
    /// </summary>
    /// <param name="builder">The host application builder to configure.</param>
    /// <returns>The same builder instance for chaining.</returns>
    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation()
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;
    }

    /// <summary>
    /// TR-INFRA-001: Adds a default liveness health check ("self") tagged with "live"
    /// so that <c>/health</c> and <c>/alive</c> endpoints respond without requiring database connectivity.
    /// </summary>
    /// <param name="builder">The host application builder to configure.</param>
    /// <returns>The same builder instance for chaining.</returns>
    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// Registers the global exception handler middleware at the start of the pipeline.
    /// This should be called before any other middleware so that unhandled exceptions
    /// from every middleware and controller are caught, logged, and returned as a
    /// generic 500 JSON response.
    /// </summary>
    public static WebApplication UseGlobalExceptionHandler(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.Use(next =>
        {
            var logger = app.Services.GetRequiredService<ILogger<GlobalExceptionHandlerMiddleware>>();
            var middleware = new GlobalExceptionHandlerMiddleware(next, logger);
            return middleware.InvokeAsync;
        });

        return app;
    }

    /// <summary>
    /// Logs the application name and assembly version at startup.
    /// The informational version includes the full semantic version from GitVersion
    /// (e.g. <c>0.1.0-alpha.42+Branch.develop.Sha.abc1234</c>).
    /// </summary>
    public static WebApplication LogApplicationVersion(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var version = GetApplicationVersion();
        app.Logger.LogInformation("Starting {ApplicationName} v{Version}", app.Environment.ApplicationName, version);
        return app;
    }

    /// <summary>
    /// Returns the informational version of the entry assembly, or "unknown" if unavailable.
    /// This includes the full GitVersion semantic version with branch and SHA metadata.
    /// </summary>
    public static string GetApplicationVersion()
    {
        return Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "unknown";
    }

    /// <summary>
    /// TR-INFRA-001: Maps standard health-check endpoints used by Docker, Railway, Aspire, and load balancers:
    /// <c>/health</c> (liveness), <c>/alive</c> (liveness), and <c>/ready</c> (readiness, includes DB checks).
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The same application instance for chaining.</returns>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        
        // Map /health (liveness-only) so Docker, Railway, load balancers, and the mobile app see Healthy when the process is up.
        // Only checks tagged "live" run here; Aspire Npgsql adds a DB check without "live", so /health stays Healthy even if DB is temporarily unreachable.
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live"),
            ResponseWriter = CreateHealthCheckResponseWriter(includeException: false),
        });

        // Map /alive (same as /health) and /ready (all checks, including DB) for orchestrators that distinguish liveness vs readiness.
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live"),
            ResponseWriter = CreateHealthCheckResponseWriter(includeException: false),
        });
        
        app.MapHealthChecks("/ready", new HealthCheckOptions
        {
            ResponseWriter = CreateHealthCheckResponseWriter(includeException: true),
        });

        return app;
    }

    /// <summary>Response writer that logs the health result via ILogger (e.g. Serilog) then writes JSON.</summary>
    internal static Func<HttpContext, HealthReport, Task> CreateHealthCheckResponseWriter(bool includeException)
    {
        var version = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "unknown";

        return async (context, report) =>
        {
            var path = context.Request.Path.Value ?? "/health";
            var logger = context.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("HealthChecks");
            if (logger is not null)
            {
                logger.LogTrace(
                    "Health check {Path} completed with status {Status} in {TotalDurationMs}ms",
                    path, report.Status, report.TotalDuration.TotalMilliseconds);
            }

            context.Response.ContentType = "application/json";
            var checks = report.Entries
                .Select(e => new HealthCheckEntryResponse(
                    e.Key,
                    e.Value.Status.ToString(),
                    includeException ? e.Value.Description ?? string.Empty : e.Value.Description,
                    e.Value.Duration.TotalMilliseconds,
                    includeException ? e.Value.Exception?.Message : null))
                .ToArray();
            var nonce = context.Request.Query.TryGetValue("nonce", out var nonceValues)
                ? nonceValues.ToString()
                : null;
            var storage = await ResolveStorageFieldAsync(context, report).ConfigureAwait(false);
            var payload = new HealthCheckResponse(report.Status.ToString(), version, checks, nonce, storage);
            var result = System.Text.Json.JsonSerializer.Serialize(payload, ServiceDefaultsJsonContext.Default.HealthCheckResponse);
            await context.Response.WriteAsync(result).ConfigureAwait(false);
        };
    }

    /// <summary>
    /// TR-MCP-HEALTH-003: resolves the explicit <c>storage</c> reachability field for the health
    /// payload from the "storage"-tagged health check, WITHOUT changing the top-level status
    /// semantics: <c>/health</c> stays liveness-only (Healthy during a storage-only outage) and
    /// the nonce echo is untouched. Returns <see langword="null"/> (field omitted) when the
    /// hosting service registers no "storage"-tagged check.
    /// </summary>
    private static async Task<string?> ResolveStorageFieldAsync(HttpContext context, HealthReport report)
    {
        // Prefer an entry already computed for this request (the /ready report runs all checks).
        foreach (var entry in report.Entries)
        {
            if (entry.Value.Tags.Contains(StorageCheckTag))
                return entry.Value.Status == HealthStatus.Healthy ? "reachable" : "unreachable";
        }

        var healthCheckService = context.RequestServices.GetService<HealthCheckService>();
        if (healthCheckService is null)
            return null;

        var storageReport = await healthCheckService
            .CheckHealthAsync(r => r.Tags.Contains(StorageCheckTag), context.RequestAborted)
            .ConfigureAwait(false);
        if (storageReport.Entries.Count == 0)
            return null;

        return storageReport.Status == HealthStatus.Healthy ? "reachable" : "unreachable";
    }

    /// <summary>TR-MCP-HEALTH-003: tag that marks the storage-connectivity health check.</summary>
    internal const string StorageCheckTag = "storage";
}
