using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace McpServer.VsExtension.McpTodo;

/// <summary>
/// Lightweight helper to invoke the Copilot CLI agent from net472 code.
/// Mirrors the approach in McpServer.Common.Copilot but without external dependencies.
/// </summary>
internal static class CopilotCliHelper
{
    private static readonly Regex s_ansiEscapePattern = new(@"\x1B\[[0-9;]*[A-Za-z]", RegexOptions.Compiled);

    /// <summary>
    /// Working directory for the Copilot CLI process. Set to the solution directory
    /// so the CLI picks up project instructions and MCP config.
    /// </summary>
    internal static string? WorkingDirectory { get; set; }

    /// <summary>
    /// Invoke the Copilot CLI agent with the given prompt and return stdout.
    /// </summary>
    internal static Task<CopilotCliResult> InvokeAsync(string prompt, int timeoutMs = 120_000) =>
        InvokeAsync(prompt, null, CancellationToken.None, timeoutMs);

    /// <summary>
    /// Invoke the Copilot CLI agent with the given prompt, streaming stdout
    /// line-by-line via <paramref name="onStdoutLine"/>.
    /// </summary>
    internal static Task<CopilotCliResult> InvokeAsync(string prompt, Action<string>? onStdoutLine, int timeoutMs = 120_000) =>
        InvokeAsync(prompt, onStdoutLine, CancellationToken.None, timeoutMs);

    /// <summary>
    /// Invoke the Copilot CLI agent with the given prompt, streaming stdout
    /// line-by-line via <paramref name="onStdoutLine"/>. Supports cancellation:
    /// when <paramref name="cancellationToken"/> is triggered, Ctrl+C is sent
    /// three times (100 ms apart), then remaining output is drained until idle
    /// for 30 seconds before the process is killed.
    /// </summary>
    internal static async Task<CopilotCliResult> InvokeAsync(
        string prompt,
        Action<string>? onStdoutLine,
        CancellationToken cancellationToken,
        int timeoutMs = 120_000)
    {
        string? tmpFile = null;
        try
        {
            CopilotOutputPane.Log($">>> Prompt:\n{prompt}");

            // Write prompt to a temp file to avoid shell escaping issues
            tmpFile = Path.GetTempFileName();
            File.WriteAllText(tmpFile, prompt);

            var shell = "pwsh";
            var agentCmd = $"copilot -p \"$(Get-Content -Raw '{tmpFile.Replace("'", "''")}')\" --model gpt-5.3-codex --allow-all";
            var shellArgs = $"-NoProfile -Command \"{agentCmd}\"";

            CopilotOutputPane.Log($">>> Command: {shell} {shellArgs}");

            var psi = new ProcessStartInfo
            {
                FileName = shell,
                Arguments = shellArgs,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                WorkingDirectory = WorkingDirectory ?? "",
            };

            using (var process = Process.Start(psi))
            {
                if (process == null)
                {
                    CopilotOutputPane.Log("<<< Error: Process.Start returned null");
                    return new CopilotCliResult { State = "spawnError", Body = "Process.Start returned null" };
                }

                var stdoutBuf = new StringBuilder();
                var stderrTask = process.StandardError.ReadToEndAsync();
                var wasCancelled = false;

                // Register cancellation: kill the process immediately
                using (cancellationToken.Register(() =>
                {
                    wasCancelled = true;
                    CopilotOutputPane.Log(">>> Cancellation requested — killing Copilot CLI process");
                    TryKillProcess(process);
                }))
                {
                    // Stream stdout line-by-line
                    string? line;
                    while ((line = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false)) != null)
                    {
                        stdoutBuf.AppendLine(line);
                        onStdoutLine?.Invoke(s_ansiEscapePattern.Replace(line, ""));
                    }
                }

                var exited = process.WaitForExit(timeoutMs);
                var stderr = await stderrTask.ConfigureAwait(false);
                var stdout = stdoutBuf.ToString();

                if (!exited)
                {
                    TryKillProcess(process);
                    CopilotOutputPane.Log($"<<< Timeout after {timeoutMs}ms\nStdout: {stdout}\nStderr: {stderr}");
                    return new CopilotCliResult { State = "timeout", Body = stdout, Stderr = stderr };
                }

                string state;
                if (wasCancelled)
                    state = "cancelled";
                else
                    state = process.ExitCode == 0 ? "success" : "error";

                CopilotOutputPane.Log($"<<< {state} (exit code {process.ExitCode})\n{stdout}");
                if (!string.IsNullOrWhiteSpace(stderr))
                    CopilotOutputPane.Log($"<<< Stderr:\n{stderr}");

                return new CopilotCliResult
                {
                    State = state,
                    Body = stdout,
                    Stderr = stderr,
                    ExitCode = process.ExitCode,
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(ex.ToString());
            CopilotOutputPane.Log($"<<< SpawnError: {ex.Message}");
            return new CopilotCliResult { State = "spawnError", Body = ex.Message };
        }
        finally
        {
            if (tmpFile != null)
                TryDeleteFile(tmpFile);
        }
    }

    private static void TryKillProcess(Process process)
    {
        try { if (!process.HasExited) process.Kill(); }
        catch (InvalidOperationException ex)
        {
            System.Diagnostics.Trace.TraceWarning(ex.ToString());
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning(ex.ToString());
        }
    }

    private static void TryDeleteFile(string path)
    {
        try { File.Delete(path); }
        catch (IOException ex)
        {
            System.Diagnostics.Trace.TraceWarning(ex.ToString());
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Diagnostics.Trace.TraceWarning(ex.ToString());
        }
    }
}

/// <summary>
/// Writes to the "McpServer Copilot" output window pane in Visual Studio.
/// Safe to call from any thread.
/// </summary>
#pragma warning disable VSTHRD010, VSTHRD110, VSSDK007 // Threading handled internally via RunAsync + SwitchToMainThreadAsync
internal static class CopilotOutputPane
{
    private const string PaneTitle = "McpServer Copilot";

    internal static void Log(string message)
    {
        try
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                GetOrCreatePane()?.OutputString($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            });
        }
        catch
        {
            // Swallow — logging should never break functionality
        }
    }

    internal static void Show()
    {
        try
        {
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                GetOrCreatePane()?.Activate();
            });
        }
        catch
        {
            // Swallow
        }
    }

    private static EnvDTE.OutputWindowPane GetOrCreatePane()
    {
        var dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
        if (dte == null) return null!;
        var outputWindow = (EnvDTE.OutputWindow)dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput)?.Object!;
        if (outputWindow == null) return null!;

        try { return outputWindow.OutputWindowPanes.Item(PaneTitle); }
        catch { return outputWindow.OutputWindowPanes.Add(PaneTitle); }
    }
}
#pragma warning restore VSTHRD010, VSTHRD110, VSSDK007

internal sealed class CopilotCliResult
{
    public string State { get; set; } = "";
    public string Body { get; set; } = "";
    public string Stderr { get; set; } = "";
    public int? ExitCode { get; set; }
}
