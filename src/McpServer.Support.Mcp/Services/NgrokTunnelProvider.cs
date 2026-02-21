using System.Diagnostics;
using System.Text.Json;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Tunnel provider using <c>ngrok</c> CLI. Starts <c>ngrok http {port}</c>
/// and reads the public URL from the ngrok local API.
/// </summary>
public sealed class NgrokTunnelProvider : ITunnelProvider, IDisposable
{
    /// <inheritdoc />
    public string ProviderName => "ngrok";

    private readonly TunnelOptions _options;
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<NgrokTunnelProvider> _logger;
    private Process? _process;
    private string? _publicUrl;
    private string? _error;

    /// <summary>Initializes a new instance of the <see cref="NgrokTunnelProvider"/> class.</summary>
    public NgrokTunnelProvider(IOptions<TunnelOptions> options, IProcessRunner processRunner, ILogger<NgrokTunnelProvider> logger)
    {
        _options = options.Value;
        _processRunner = processRunner;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Verify ngrok is installed.
        var check = await _processRunner.RunAsync("ngrok", "version", cancellationToken).ConfigureAwait(false);
        if (check.ExitCode != 0)
        {
            _error = "ngrok CLI not found. Install from https://ngrok.com/download";
            _logger.LogError("{Error}", _error);
            return;
        }

        var ngrok = _options.Ngrok;
        var args = $"http {_options.Port} --log stdout --log-format json";
        if (!string.IsNullOrWhiteSpace(ngrok.Subdomain))
            args += $" --subdomain {ngrok.Subdomain}";
        if (!string.IsNullOrWhiteSpace(ngrok.Region))
            args += $" --region {ngrok.Region}";

        var startInfo = new ProcessStartInfo
        {
            FileName = "ngrok",
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        // Pass auth token via environment variable to avoid exposure in process listing.
        if (!string.IsNullOrWhiteSpace(ngrok.AuthToken))
            startInfo.Environment["NGROK_AUTHTOKEN"] = ngrok.AuthToken;

        _process = new Process { StartInfo = startInfo };
        _process.Start();
        _logger.LogInformation("ngrok started (PID {Pid}), waiting for tunnel URL...", _process.Id);

        // Wait briefly then query ngrok API for the public URL.
        await Task.Delay(3000, cancellationToken).ConfigureAwait(false);
        await RefreshPublicUrlAsync(cancellationToken).ConfigureAwait(false);

        if (_publicUrl is not null)
            _logger.LogInformation("ngrok tunnel active: {Url}", _publicUrl);
        else
            _logger.LogWarning("ngrok started but public URL not yet available.");
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
                _logger.LogInformation("ngrok tunnel stopped.");
            }
        }
        catch (InvalidOperationException) { /* process exited between check and kill */ }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<TunnelStatus> GetStatusAsync(CancellationToken ct = default)
    {
        if (_process is null || _process.HasExited)
            return new TunnelStatus(false, Error: _error ?? "Not started.");

        if (_publicUrl is null)
            await RefreshPublicUrlAsync(ct).ConfigureAwait(false);

        return new TunnelStatus(true, _publicUrl, _error);
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
    }

    private async Task RefreshPublicUrlAsync(CancellationToken ct)
    {
        try
        {
            var result = await _processRunner.RunAsync("curl", "-s http://127.0.0.1:4040/api/tunnels", ct).ConfigureAwait(false);
            if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Stdout))
            {
                var doc = JsonDocument.Parse(result.Stdout);
                var tunnels = doc.RootElement.GetProperty("tunnels");
                if (tunnels.GetArrayLength() > 0)
                    _publicUrl = tunnels[0].GetProperty("public_url").GetString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to query ngrok API for public URL.");
        }
    }
}
