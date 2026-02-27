using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace McpServer.Director;

/// <summary>
/// TR-MCP-LOG-001: Configures Serilog file-based logging for the Director CLI.
/// Terminal.Gui owns stdout/stderr, so all log output goes to a rolling file.
/// </summary>
internal static class DirectorLogging
{
    private static readonly string s_logDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "McpServer", "Director", "logs");

    /// <summary>Registers Serilog file logging with the service collection.</summary>
    public static IServiceCollection AddDirectorLogging(this IServiceCollection services)
    {
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(s_logDir, "director-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddSerilog(serilogLogger, dispose: true);
        });

        return services;
    }
}
