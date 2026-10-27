using System.Diagnostics;
using System.Text;

namespace QBrainAi.PluginIntegration.Tests;

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
        var timedOut = false;
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            timedOut = true;
            TryKill(process);
            try
            {
                using var exitWait = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var linkedExit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, exitWait.Token);
                await process.WaitForExitAsync(linkedExit.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        string stdout;
        string stderr;
        try
        {
            // After Kill, pipes can stay open if a grandchild ignores SIGKILL semantics on Windows.
            // Bound the drain so a dead child cannot wedge the unit gate with near-zero CPU.
            using var drainCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            drainCts.CancelAfter(TimeSpan.FromSeconds(timedOut ? 3 : 30));
            var drain = Task.WhenAll(stdoutTask, stderrTask);
            await drain.WaitAsync(drainCts.Token).ConfigureAwait(false);
            stdout = stdoutTask.Result;
            stderr = stderrTask.Result;
        }
        catch (OperationCanceledException)
        {
            stdout = string.Empty;
            stderr = timedOut ? "process-timeout" : "process-output-drain-timeout";
            TryKill(process);
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
