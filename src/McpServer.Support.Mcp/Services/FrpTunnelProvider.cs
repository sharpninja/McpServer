using System.Diagnostics;
using System.Globalization;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Tunnel provider using <c>frpc</c> (FRP client). Generates a temporary
/// <c>frpc.toml</c> config and starts the FRP client process.
/// Requires a running <c>frps</c> server (see <c>docker-compose.frps.yml</c>).
/// </summary>
public sealed class FrpTunnelProvider : ITunnelProvider, IDisposable
{
    /// <inheritdoc />
    public string ProviderName => "frp";

    private readonly TunnelOptions _options;
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<FrpTunnelProvider> _logger;
    private Process? _process;
    private string? _configPath;
    private string? _publicUrl;
    private string? _error;

    /// <summary>Initializes a new instance of the <see cref="FrpTunnelProvider"/> class.</summary>
    public FrpTunnelProvider(IOptions<TunnelOptions> options, IProcessRunner processRunner, ILogger<FrpTunnelProvider> logger)
    {
        _options = options.Value;
        _processRunner = processRunner;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var check = await _processRunner.RunAsync("frpc", "verify --strict", cancellationToken).ConfigureAwait(false);
        // frpc verify will fail without a config, but we just need it to exist (exit code is fine).
        var whichCheck = await _processRunner.RunAsync("frpc", "--version", cancellationToken).ConfigureAwait(false);
        if (whichCheck.ExitCode != 0)
        {
            _error = "frpc CLI not found. Install from https://github.com/fatedier/frp/releases";
            _logger.LogError("{Error}", _error);
            return;
        }

        var frp = _options.Frp;
        _configPath = Path.Combine(Path.GetTempPath(), $"frpc_{Guid.NewGuid():N}.toml");
        var config = GenerateConfig(frp);
        await File.WriteAllTextAsync(_configPath, config, cancellationToken).ConfigureAwait(false);

        var startInfo = new ProcessStartInfo
        {
            FileName = "frpc",
            Arguments = $"-c \"{_configPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        _process = new Process { StartInfo = startInfo };
        _process.Start();
        _logger.LogInformation("frpc started (PID {Pid}) with config {Config}", _process.Id, _configPath);

        // Construct the expected public URL.
        _publicUrl = BuildPublicUrl(frp);
        if (_publicUrl is not null)
            _logger.LogInformation("FRP tunnel expected URL: {Url}", _publicUrl);

        // Give frpc time to connect to frps.
        await Task.Delay(3000, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_process is not null && !_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(5000);
                _logger.LogInformation("FRP tunnel stopped.");
            }
        }
        catch (InvalidOperationException) { /* process exited between check and kill */ }

        CleanupConfig();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<TunnelStatus> GetStatusAsync(CancellationToken ct = default)
    {
        if (_process is null || _process.HasExited)
            return Task.FromResult(new TunnelStatus(false, Error: _error ?? "Not started."));

        return Task.FromResult(new TunnelStatus(true, _publicUrl, _error));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            if (_process is not null && !_process.HasExited)
                _process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException) { /* process already exited */ }
        _process?.Dispose();
        CleanupConfig();
    }

    private string GenerateConfig(FrpTunnelOptions frp)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[common]");
        sb.AppendLine(CultureInfo.InvariantCulture, $"serverAddr = \"{frp.ServerAddress}\"");
        sb.AppendLine(CultureInfo.InvariantCulture, $"serverPort = {frp.ServerPort}");

        if (!string.IsNullOrWhiteSpace(frp.Token))
            sb.AppendLine(CultureInfo.InvariantCulture, $"auth.token = \"{frp.Token}\"");

        sb.AppendLine();
        sb.AppendLine("[[proxies]]");
        sb.AppendLine("name = \"mcp-http\"");
        sb.AppendLine("type = \"http\"");
        sb.AppendLine(CultureInfo.InvariantCulture, $"localPort = {_options.Port}");

        if (!string.IsNullOrWhiteSpace(frp.Subdomain))
            sb.AppendLine(CultureInfo.InvariantCulture, $"subdomain = \"{frp.Subdomain}\"");
        else if (!string.IsNullOrWhiteSpace(frp.CustomDomain))
            sb.AppendLine(CultureInfo.InvariantCulture, $"customDomains = [\"{frp.CustomDomain}\"]");

        return sb.ToString();
    }

    private static string? BuildPublicUrl(FrpTunnelOptions frp)
    {
        if (!string.IsNullOrWhiteSpace(frp.CustomDomain))
            return $"http://{frp.CustomDomain}";
        if (!string.IsNullOrWhiteSpace(frp.Subdomain))
            return $"http://{frp.Subdomain}.{frp.ServerAddress}";
        return null;
    }

    private void CleanupConfig()
    {
        if (_configPath is not null && File.Exists(_configPath))
        {
            try { File.Delete(_configPath); }
            catch { /* best-effort */ }
            _configPath = null;
        }
    }
}
