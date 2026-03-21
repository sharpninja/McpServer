using System.Text.Json;
using System.Text.Json.Serialization;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    private readonly DesktopLaunchOptions _desktopLaunchOptions;
    private readonly ILogger<DesktopLaunchService> _logger;
    private readonly IProcessRunner _processRunner;

    /// <summary>
    /// FR-MCP-047/TR-MCP-DESKTOP-001: Initializes the desktop-launch service with the
    /// configured launcher location, structured process runner, and logger.
    /// </summary>
    /// <param name="configuration">Application configuration used to resolve launcher paths.</param>
    /// <param name="desktopLaunchOptions">Privileged desktop-launch feature-gate and allowlist configuration.</param>
    /// <param name="processRunner">Process runner used to invoke <c>McpServer.Launcher.exe</c>.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public DesktopLaunchService(
        IConfiguration configuration,
        IOptions<DesktopLaunchOptions> desktopLaunchOptions,
        IProcessRunner processRunner,
        ILogger<DesktopLaunchService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _desktopLaunchOptions = desktopLaunchOptions?.Value ?? throw new ArgumentNullException(nameof(desktopLaunchOptions));
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

        if (!_desktopLaunchOptions.Enabled)
        {
            _logger.LogWarning(
                "Rejected desktop launch for workspace {WorkspacePath} because desktop launch is disabled.",
                workspacePath);
            return CreateFailureResult("Desktop launch is disabled. Enable Mcp:DesktopLaunch:Enabled to allow local process launch.");
        }

        if (_desktopLaunchOptions.AllowedExecutables.Count == 0)
        {
            _logger.LogWarning(
                "Rejected desktop launch for workspace {WorkspacePath} because no desktop executables are allowlisted.",
                workspacePath);
            return CreateFailureResult("No desktop executables are allowlisted. Configure Mcp:DesktopLaunch:AllowedExecutables.");
        }

        var normalizedExecutablePath = NormalizeExecutablePath(request.ExecutablePath);
        if (normalizedExecutablePath is null)
            return CreateFailureResult("executablePath must be a non-empty absolute path.");

        if (!PathGlobMatcher.MatchesAny(normalizedExecutablePath, _desktopLaunchOptions.AllowedExecutables))
        {
            _logger.LogWarning(
                "Rejected desktop launch for workspace {WorkspacePath} because executable {ExecutablePath} does not match the configured allowlist.",
                workspacePath,
                normalizedExecutablePath);
            return CreateFailureResult("Executable path is not in the configured desktop allowlist.");
        }

        var launcherPath = ResolveLauncherPath(workspacePath);
        if (launcherPath is null)
            return CreateFailureResult("McpServer.Launcher.exe not found. Check Mcp:LauncherPath configuration.");

        try
        {
            var payload = new DesktopLaunchRequest
            {
                ExecutablePath = normalizedExecutablePath,
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

    private static string? NormalizeExecutablePath(string? executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath) || !Path.IsPathRooted(executablePath))
            return null;

        try
        {
            return Path.GetFullPath(executablePath);
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
        catch (PathTooLongException)
        {
            return null;
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
