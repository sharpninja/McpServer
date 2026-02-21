using System.Diagnostics;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Tunnel provider using <c>cloudflared</c> CLI. Starts a quick tunnel or
/// named tunnel and parses the public URL from stdout.
/// </summary>
public sealed class CloudflareTunnelProvider : ITunnelProvider, IDisposable
{
    /// <inheritdoc />
    public string ProviderName => "cloudflare";

    private readonly TunnelOptions _options;
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<CloudflareTunnelProvider> _logger;
    private Process? _process;
    private string? _publicUrl;
    private string? _error;

    /// <summary>Initializes a new instance of the <see cref="CloudflareTunnelProvider"/> class.</summary>
    public CloudflareTunnelProvider(IOptions<TunnelOptions> options, IProcessRunner processRunner, ILogger<CloudflareTunnelProvider> logger)
    {
        _options = options.Value;
        _processRunner = processRunner;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var check = await _processRunner.RunAsync("cloudflared", "version", cancellationToken).ConfigureAwait(false);
        if (check.ExitCode != 0)
        {
            _error = "cloudflared CLI not found. Install from https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/downloads/";
            _logger.LogError("{Error}", _error);
            return;
        }

        var cf = _options.Cloudflare;
        string args;

        if (!string.IsNullOrWhiteSpace(cf.TunnelName))
        {
            // Named tunnel (requires prior `cloudflared tunnel create`).
            args = $"tunnel run {cf.TunnelName}";
        }
        else
        {
            // Quick tunnel — cloudflared assigns a random *.trycloudflare.com URL.
            args = $"tunnel --url http://localhost:{_options.Port}";
            if (!string.IsNullOrWhiteSpace(cf.Hostname))
                args += $" --hostname {cf.Hostname}";
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "cloudflared",
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        _process = new Process { StartInfo = startInfo };
        _process.Start();
        _logger.LogInformation("cloudflared started (PID {Pid}), waiting for tunnel URL...", _process.Id);

        // cloudflared prints the URL to stderr for quick tunnels.
        _ = Task.Run(() => ReadPublicUrlFromStderr(cancellationToken), cancellationToken);

        // Give it time to connect.
        await Task.Delay(5000, cancellationToken).ConfigureAwait(false);

        if (_publicUrl is not null)
            _logger.LogInformation("Cloudflare tunnel active: {Url}", _publicUrl);
        else
            _logger.LogWarning("cloudflared started but public URL not yet captured.");
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
                _logger.LogInformation("Cloudflare tunnel stopped.");
            }
        }
        catch (InvalidOperationException) { /* process exited between check and kill */ }
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
    }

    private async Task ReadPublicUrlFromStderr(CancellationToken ct)
    {
        if (_process?.StandardError is null) return;

        try
        {
            while (!ct.IsCancellationRequested && !_process.HasExited)
            {
                var line = await _process.StandardError.ReadLineAsync(ct).ConfigureAwait(false);
                if (line is null) break;

                // cloudflared logs: "... https://xxxx.trycloudflare.com ..."
                var idx = line.IndexOf("https://", StringComparison.Ordinal);
                if (idx >= 0)
                {
                    var end = line.IndexOf(' ', idx);
                    _publicUrl = end > idx ? line[idx..end] : line[idx..];
                    _logger.LogDebug("Captured Cloudflare tunnel URL: {Url}", _publicUrl);
                    break;
                }
            }
        }
        catch (OperationCanceledException) { /* shutting down */ }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error reading cloudflared stderr.");
        }
    }
}
