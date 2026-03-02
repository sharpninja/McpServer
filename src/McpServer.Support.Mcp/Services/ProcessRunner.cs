using System.Diagnostics;
using McpServer.Common.Copilot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Configuration for <see cref="ProcessRunner"/>. Provides the GitHub token
/// so processes launched from a Windows service can authenticate with GitHub.
/// The interactive user's environment is auto-detected via WTS API.
/// </summary>
public sealed class ProcessRunnerOptions
{
    /// <summary>GitHub token passed as <c>GH_TOKEN</c> to spawned processes.</summary>
    public string? GitHubToken { get; set; }
}

/// <summary>
/// TR-PLANNED-013: Default process runner using System.Diagnostics.Process.
/// Applies RunAs environment and GH_TOKEN via <see cref="IProcessEnvironmentService"/>.
/// </summary>
public sealed class ProcessRunner(
    IProcessEnvironmentService processEnvironment,
    IOptions<ProcessRunnerOptions> options,
    ILogger<ProcessRunner> logger) : IProcessRunner
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

            var opts = options.Value;
            processEnvironment.ApplyAll(process.StartInfo, runAsUser: null, opts.GitHubToken);
            process.StartInfo.FileName = processEnvironment.ResolveExecutable(process.StartInfo, fileName);

            logger.LogDebug("Running {FileName} {Arguments}", fileName, arguments);
            process.Start();
            var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
            var stderrTask = process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);
            return new ProcessRunResult(process.ExitCode, stdout, string.IsNullOrWhiteSpace(stderr) ? null : stderr);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            logger.LogWarning(ex, "Process {FileName} not found", fileName);
            return new ProcessRunResult(-1, null, $"{fileName} not found.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Process {FileName} failed", fileName);
            return new ProcessRunResult(-1, null, ex.Message);
        }
    }
}
