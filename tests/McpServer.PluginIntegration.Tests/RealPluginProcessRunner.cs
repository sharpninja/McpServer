using System.Diagnostics;
using System.Text;

namespace McpServer.PluginIntegration.Tests;

/// <summary>
/// TR-MCP-PLUGININT-001 AC3: real process runner for plugin host adapters (stdin/stdout/stderr/timeout).
/// </summary>
public sealed class RealPluginProcessRunner : IPluginProcessRunner
{
    /// <inheritdoc />
    public async Task<PluginProcessLaunchResult> RunAsync(PluginProcessLaunchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = new ProcessStartInfo
        {
            FileName = request.Executable,
            WorkingDirectory = request.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardInputEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        foreach (var pair in request.Environment)
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

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start " + request.Executable);
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.StandardInput.WriteAsync(request.StandardInput.AsMemory(), cancellationToken).ConfigureAwait(false);
        process.StandardInput.Close();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(request.Timeout);
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            try
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        string stdout;
        string stderr;
        try
        {
            stdout = await stdoutTask.ConfigureAwait(false);
            stderr = await stderrTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            stdout = string.Empty;
            stderr = "process-timeout";
        }

        var exit = process.HasExited ? process.ExitCode : -1;
        return new PluginProcessLaunchResult
        {
            ExitCode = exit,
            StandardOutput = stdout,
            StandardError = stderr,
        };
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
        }
    }
}
