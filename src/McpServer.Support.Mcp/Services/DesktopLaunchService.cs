using System.Text.Json;
using System.Text.Json.Serialization;
using McpServer.Support.Mcp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-047/TR-MCP-DESKTOP-001: Shared desktop-launch service used by both the HTTP
/// controller and the STDIO MCP tool surface.
/// </summary>
public sealed class DesktopLaunchService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IConfiguration _configuration;
    private readonly ILogger<DesktopLaunchService> _logger;
    private readonly IProcessRunner _processRunner;

    /// <summary>
    /// FR-MCP-047/TR-MCP-DESKTOP-001: Initializes the desktop-launch service with the
    /// configured launcher location, structured process runner, and logger.
    /// </summary>
    /// <param name="configuration">Application configuration used to resolve launcher paths.</param>
    /// <param name="processRunner">Process runner used to invoke <c>McpServer.Launcher.exe</c>.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public DesktopLaunchService(
        IConfiguration configuration,
        IProcessRunner processRunner,
        ILogger<DesktopLaunchService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// FR-MCP-047/TR-MCP-DESKTOP-001: Launches a local desktop process for the specified
    /// workspace by invoking <c>McpServer.Launcher.exe</c> and normalizing its JSON result.
    /// </summary>
    /// <param name="workspacePath">Absolute workspace path used for launcher resolution.</param>
    /// <param name="request">Structured launch request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A normalized launch result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
    public async Task<DesktopLaunchResult> LaunchAsync(
        string workspacePath,
        DesktopLaunchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(workspacePath))
            return CreateFailureResult("workspacePath is required.");

        var launcherPath = ResolveLauncherPath(workspacePath);
        if (launcherPath is null)
            return CreateFailureResult("McpServer.Launcher.exe not found. Check Mcp:LauncherPath configuration.");

        try
        {
            var payload = new DesktopLaunchRequest
            {
                ExecutablePath = request.ExecutablePath,
                Arguments = request.Arguments,
                WorkingDirectory = request.WorkingDirectory,
                EnvironmentVariables = request.EnvironmentVariables,
                CreateNoWindow = request.CreateNoWindow,
                WindowStyle = string.IsNullOrWhiteSpace(request.WindowStyle) ? "Normal" : request.WindowStyle,
                WaitForExit = request.WaitForExit,
                TimeoutMs = request.TimeoutMs
            };

            var json = JsonSerializer.Serialize(payload, s_jsonOptions);
            var escapedJson = json.Replace("\"", "\\\"", StringComparison.Ordinal);
            var result = await _processRunner.RunAsync(launcherPath, $"\"{escapedJson}\"", cancellationToken).ConfigureAwait(false);

            if (TryParseResult(result.Stdout, out var parsedResult))
                return parsedResult;
            if (TryParseResult(result.Stderr, out parsedResult))
                return parsedResult;

            if (result.ExitCode != 0)
            {
                var errorBody = string.IsNullOrWhiteSpace(result.Stderr) ? result.Stdout : result.Stderr;
                return CreateFailureResult(
                    string.IsNullOrWhiteSpace(errorBody)
                        ? $"Launcher exited with code {result.ExitCode}."
                        : $"Launcher exited with code {result.ExitCode}: {errorBody}");
            }

            return CreateFailureResult("No output from launcher.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Desktop launch failed for workspace {WorkspacePath}", workspacePath);
            return CreateFailureResult(ex.Message);
        }
    }

    private string? ResolveLauncherPath(string workspacePath)
    {
        var configPath = _configuration["Mcp:LauncherPath"];
        if (!string.IsNullOrWhiteSpace(configPath) && File.Exists(configPath))
            return configPath;

        var assemblyDir = AppContext.BaseDirectory;
        var sideBySide = Path.Combine(assemblyDir, "McpServer.Launcher.exe");
        if (File.Exists(sideBySide))
            return sideBySide;

        var publishPath = Path.Combine(workspacePath, "_publish", "McpServer.Launcher", "McpServer.Launcher.exe");
        if (File.Exists(publishPath))
            return publishPath;

        return null;
    }

    private static bool TryParseResult(string? payload, out DesktopLaunchResult result)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            result = null!;
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<DesktopLaunchResult>(payload.Trim(), s_jsonOptions);
            if (parsed is null)
            {
                result = null!;
                return false;
            }

            result = parsed;
            return true;
        }
        catch (JsonException)
        {
            result = null!;
            return false;
        }
    }

    private static DesktopLaunchResult CreateFailureResult(string message) => new()
    {
        Success = false,
        ErrorMessage = message
    };
}
