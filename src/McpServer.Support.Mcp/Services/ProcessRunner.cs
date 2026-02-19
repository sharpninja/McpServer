using System.Diagnostics;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-013: Default process runner using System.Diagnostics.Process.
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
    /// <inheritdoc />
    public async Task<ProcessRunResult> RunAsync(string fileName, string arguments, CancellationToken ct = default)
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
            var stderrTask = process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);
            return new ProcessRunResult(process.ExitCode, stdout, string.IsNullOrWhiteSpace(stderr) ? null : stderr);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return new ProcessRunResult(-1, null, $"{fileName} not found.");
        }
        catch (Exception ex)
        {
            return new ProcessRunResult(-1, null, ex.Message);
        }
    }
}
