using System.Diagnostics;
using System.Text;

namespace McpServer.Repl.IntegrationTests;

/// <summary>
/// Helper class to launch and manage mcpserver-repl child processes for integration testing.
/// </summary>
public sealed class ReplChildProcessHelper : IDisposable
{
    private Process? _process;
    private readonly List<string> _stdoutLines = new();
    private readonly List<string> _stderrLines = new();
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Gets all stdout lines received from the child process.
    /// </summary>
    public IReadOnlyList<string> StdoutLines
    {
        get
        {
            lock (_lock)
            {
                return _stdoutLines.ToList();
            }
        }
    }

    /// <summary>
    /// Gets all stderr lines received from the child process.
    /// </summary>
    public IReadOnlyList<string> StderrLines
    {
        get
        {
            lock (_lock)
            {
                return _stderrLines.ToList();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the child process is running.
    /// </summary>
    public bool IsRunning => _process != null && !_process.HasExited;

    /// <summary>
    /// Launches the mcpserver-repl --agent-stdio child process.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the process is started.</returns>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ReplChildProcessHelper));
        }

        if (_process != null)
        {
            throw new InvalidOperationException("Process already started");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --project ../../../../src/McpServer.Repl.Host/McpServer.Repl.Host.csproj -- --agent-stdio",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardInputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        _process = new Process { StartInfo = startInfo };
        _process.OutputDataReceived += OnStdoutDataReceived;
        _process.ErrorDataReceived += OnStderrDataReceived;

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Writes a YAML envelope to the child process stdin.
    /// </summary>
    /// <param name="yamlContent">The YAML content to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    public async Task WriteLineAsync(string yamlContent, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ReplChildProcessHelper));
        }

        if (_process == null || _process.HasExited)
        {
            throw new InvalidOperationException("Process is not running");
        }

        await _process.StandardInput.WriteLineAsync(yamlContent.AsMemory(), cancellationToken);
        await _process.StandardInput.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Waits for a specific number of stdout lines to be received.
    /// </summary>
    /// <param name="count">The number of lines to wait for.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the expected line count was reached; otherwise, false.</returns>
    public async Task<bool> WaitForStdoutLineCountAsync(
        int count,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
        {
            lock (_lock)
            {
                if (_stdoutLines.Count >= count)
                {
                    return true;
                }
            }

            await Task.Delay(50, cancellationToken);
        }

        return false;
    }

    /// <summary>
    /// Waits for stdout to contain specific text.
    /// </summary>
    /// <param name="expectedText">The text to search for.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the text was found; otherwise, false.</returns>
    public async Task<bool> WaitForStdoutContainsAsync(
        string expectedText,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
        {
            lock (_lock)
            {
                if (_stdoutLines.Any(line => line.Contains(expectedText, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }

            await Task.Delay(50, cancellationToken);
        }

        return false;
    }

    /// <summary>
    /// Stops the child process gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous stop operation.</returns>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_process == null || _process.HasExited)
        {
            return;
        }

        try
        {
            _process.StandardInput.Close();
            
            if (!_process.WaitForExit(2000))
            {
                _process.Kill();
            }

            await Task.CompletedTask;
        }
        catch
        {
            // Best effort cleanup
        }
    }

    private void OnStdoutDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data != null)
        {
            lock (_lock)
            {
                _stdoutLines.Add(e.Data);
            }
        }
    }

    private void OnStderrDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data != null)
        {
            lock (_lock)
            {
                _stderrLines.Add(e.Data);
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_process != null)
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill();
                }

                _process.Dispose();
            }
            catch
            {
                // Best effort cleanup
            }
        }

        _disposed = true;
    }
}
