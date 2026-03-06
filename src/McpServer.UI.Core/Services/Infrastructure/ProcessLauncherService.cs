using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Services.Infrastructure;

/// <summary>
/// Host implementation of <see cref="IProcessLauncherService"/> backed by <see cref="Process"/>.
/// </summary>
public sealed class ProcessLauncherService : IProcessLauncherService
{
    public void OpenWithDefaultApp(string pathOrUrl)
    {
        Process.Start(new ProcessStartInfo(pathOrUrl) { UseShellExecute = true });
    }

    public async Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        CancellationToken ct = default)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory ?? string.Empty,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        var stderr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        return new ProcessResult(process.ExitCode, stdout, stderr);
    }

    public void ShellExecute(string command, string arguments)
    {
        Process.Start(new ProcessStartInfo(command, arguments) { UseShellExecute = true });
    }
}

