using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// TEST-MCP-PLUGININT-001: one long-lived Node MCP stdio session against a catalog
/// <c>dist/index.js</c> entrypoint. Used by Cline v1 so production session tools persist.
/// </summary>
public sealed class NodeMcpStdioSession : IAsyncDisposable
{
    private Process? _process;
    private readonly StringBuilder _stderr = new();
    private int _nextId = 1;

    /// <summary>
    /// Starts <paramref name="entrypointPath"/> with Node and completes MCP initialize.
    /// </summary>
    /// <param name="entrypointPath">Absolute path to dist/index.js.</param>
    /// <param name="pluginRoot">Plugin repository root used as process cwd.</param>
    /// <param name="workspacePath">Isolated fixture workspace (marker + MCP_WORKSPACE_PATH).</param>
    /// <param name="environment">Extra environment including plugin-root variables.</param>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <returns>A task that completes when initialize has been acknowledged.</returns>
    public async Task StartAsync(
        string entrypointPath,
        string pluginRoot,
        string workspacePath,
        IReadOnlyDictionary<string, string> environment,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entrypointPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(workspacePath);
        ArgumentNullException.ThrowIfNull(environment);
        cancellationToken.ThrowIfCancellationRequested();
        if (_process is not null)
        {
            throw new InvalidOperationException("Node MCP stdio session is already started.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "node",
            WorkingDirectory = pluginRoot,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardInputEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        startInfo.ArgumentList.Add(entrypointPath);
        foreach (var pair in environment)
        {
            if (string.IsNullOrEmpty(pair.Value))
            {
                startInfo.Environment.Remove(pair.Key);
            }
            else
            {
                startInfo.Environment[pair.Key] = pair.Value;
            }
        }

        startInfo.Environment["MCP_WORKSPACE_PATH"] = workspacePath;
        startInfo.Environment["MCPSERVER_WORKSPACE_PATH"] = workspacePath;

        _process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start node.");
        _ = DrainStderrAsync(_process, cancellationToken);
        await WaitForReadyAsync(cancellationToken).ConfigureAwait(false);

        var initializeId = _nextId++;
        await WriteMessageAsync(
            JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = initializeId,
                method = "initialize",
                @params = new
                {
                    protocolVersion = "2024-11-05",
                    capabilities = new { },
                    clientInfo = new { name = "pluginint", version = "1.0" },
                },
            }),
            cancellationToken).ConfigureAwait(false);
        try
        {
            var initialize = await ReadMessageAsync(cancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(20), cancellationToken)
                .ConfigureAwait(false);
            if (initialize.Contains("\"error\"", StringComparison.Ordinal)
                && initialize.Contains("\"code\"", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Node MCP initialize returned an error. body=" + initialize + " stderr=" + _stderr);
            }
        }
        catch (TimeoutException ex)
        {
            throw new TimeoutException("Node MCP initialize timed out. stderr=" + _stderr, ex);
        }

        await WriteMessageAsync(
            """{"jsonrpc":"2.0","method":"notifications/initialized"}""",
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Invokes an MCP tool on the live stdio session and returns the raw JSON-RPC body.
    /// </summary>
    /// <param name="toolName">Production tool name such as session_begin_turn.</param>
    /// <param name="argumentsJson">JSON object for the tool arguments.</param>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <returns>JSON-RPC response body.</returns>
    public async Task<string> CallToolAsync(string toolName, string argumentsJson, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentException.ThrowIfNullOrWhiteSpace(argumentsJson);
        cancellationToken.ThrowIfCancellationRequested();
        if (_process is null)
        {
            throw new InvalidOperationException("Node MCP stdio session has not started.");
        }

        var id = _nextId++;
        var payload = "{\"jsonrpc\":\"2.0\",\"id\":" + id.ToString(CultureInfo.InvariantCulture)
            + ",\"method\":\"tools/call\",\"params\":{\"name\":" + JsonSerializer.Serialize(toolName)
            + ",\"arguments\":" + argumentsJson + "}}";
        await WriteMessageAsync(payload, cancellationToken).ConfigureAwait(false);
        var response = await ReadMessageAsync(cancellationToken).WaitAsync(TimeSpan.FromSeconds(20), cancellationToken).ConfigureAwait(false);
        if (response.Contains("\"error\"", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(toolName + " MCP error: " + response + " stderr=" + _stderr);
        }

        return response;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_process is null)
        {
            return;
        }

        try
        {
            if (!_process.HasExited)
            {
                _process.StandardInput.Close();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                try
                {
                    await _process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
        }
        catch
        {
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    private async Task WaitForReadyAsync(CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (_stderr)
            {
                if (_stderr.ToString().Contains("MCP server ready", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            if (_process?.HasExited == true)
            {
                throw new InvalidOperationException("Node MCP process exited during startup. stderr=" + _stderr);
            }

            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException("Node MCP process did not write MCP server ready. stderr=" + _stderr);
    }

    private async Task WriteMessageAsync(string json, CancellationToken cancellationToken)
    {
        if (_process is null)
        {
            throw new InvalidOperationException("Node MCP stdio session has not started.");
        }

        // MCP SDK StdioServerTransport is newline-delimited JSON, not LSP Content-Length.
        await _process.StandardInput.WriteLineAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);
        await _process.StandardInput.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> ReadMessageAsync(CancellationToken cancellationToken)
    {
        if (_process is null)
        {
            throw new InvalidOperationException("Node MCP stdio session has not started.");
        }

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await _process.StandardOutput.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                throw new EndOfStreamException("Node MCP stdout closed before a complete JSON-RPC line. stderr=" + _stderr);
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            return line;
        }
    }

    private async Task DrainStderrAsync(Process process, CancellationToken cancellationToken)
    {
        try
        {
            var buffer = new char[1024];
            while (!cancellationToken.IsCancellationRequested && !process.HasExited)
            {
                var read = await process.StandardError.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                if (read <= 0)
                {
                    break;
                }

                lock (_stderr)
                {
                    _stderr.Append(buffer, 0, read);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }
}
