using System.Diagnostics;
using System.Runtime.InteropServices;
using McpServer.Common.Copilot;
using McpServer.Support.Mcp.Native;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// <see cref="IProcessSpawner"/> that launches processes on the interactive
/// desktop session via <see cref="DesktopProcessLauncher"/>.
/// Used when the host runs as a Windows service under LocalSystem (Session 0)
/// so child processes inherit the logged-in user's environment and token.
/// Falls back to <see cref="DefaultProcessSpawner"/> on non-Windows platforms.
/// </summary>
public sealed class DesktopProcessSpawner : IProcessSpawner
{
    private readonly DesktopProcessLauncher? _launcher;
    private readonly DefaultProcessSpawner _fallback = new();
    private readonly ILogger<DesktopProcessSpawner> _logger;

    /// <summary>Creates a new <see cref="DesktopProcessSpawner"/>.</summary>
    public DesktopProcessSpawner(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<DesktopProcessSpawner>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            _launcher = new DesktopProcessLauncher(loggerFactory.CreateLogger<DesktopProcessLauncher>());
    }

    /// <inheritdoc />
    public ISpawnedProcess Spawn(ProcessStartInfo startInfo)
    {
        if (_launcher is null)
        {
            _logger.LogDebug("Desktop launcher unavailable (non-Windows); using default spawner");
            return _fallback.Spawn(startInfo);
        }

        var args = SerializeArguments(startInfo);
        var envVars = ExtractEnvironment(startInfo);

        _logger.LogDebug(
            "Spawning on desktop session: {FileName} in {WorkingDirectory}",
            startInfo.FileName, startInfo.WorkingDirectory);

        var handle = _launcher.LaunchWithStdio(
            startInfo.FileName,
            args,
            string.IsNullOrEmpty(startInfo.WorkingDirectory) ? null : startInfo.WorkingDirectory,
            envVars);

        return new DesktopSpawnedProcess(handle);
    }

    /// <summary>
    /// Serialises <see cref="ProcessStartInfo.ArgumentList"/> into a
    /// single command-line string with proper quoting.
    /// Falls back to <see cref="ProcessStartInfo.Arguments"/> when the list is empty.
    /// </summary>
    private static string SerializeArguments(ProcessStartInfo psi)
    {
        if (psi.ArgumentList.Count == 0)
            return psi.Arguments;

        var parts = new List<string>(psi.ArgumentList.Count);
        foreach (var arg in psi.ArgumentList)
        {
            if (arg.Contains(' ') || arg.Contains('"') || arg.Length == 0)
                parts.Add("\"" + arg.Replace("\"", "\\\"") + "\"");
            else
                parts.Add(arg);
        }

        return string.Join(' ', parts);
    }

    /// <summary>
    /// Extracts environment variables set on the <see cref="ProcessStartInfo"/>
    /// into a dictionary for <see cref="DesktopProcessLauncher"/>.
    /// </summary>
    private static Dictionary<string, string>? ExtractEnvironment(ProcessStartInfo psi)
    {
        if (psi.Environment.Count == 0)
            return null;

        var result = new Dictionary<string, string>(psi.Environment.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in psi.Environment)
        {
            if (value is not null)
                result[key] = value;
        }

        return result.Count > 0 ? result : null;
    }

    /// <summary>
    /// Adapts a <see cref="DesktopProcessHandle"/> to the <see cref="ISpawnedProcess"/> interface.
    /// </summary>
    private sealed class DesktopSpawnedProcess(DesktopProcessHandle handle) : ISpawnedProcess
    {
        private int _exitCode = -1;
        private bool _exited;

        /// <inheritdoc />
        public StreamReader StandardOutput => handle.StandardOutput;

        /// <inheritdoc />
        public StreamReader StandardError => handle.StandardError;

        /// <inheritdoc />
        public StreamWriter? StandardInput => handle.StandardInput;

        /// <inheritdoc />
        public int Id => handle.ProcessId;

        /// <inheritdoc />
        public bool HasExited => _exited;

        /// <inheritdoc />
        public int ExitCode => _exitCode;

        /// <inheritdoc />
        public async Task WaitForExitAsync(CancellationToken cancellationToken = default)
        {
            _exitCode = await handle.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            _exited = true;
        }

        /// <inheritdoc />
        public void Kill()
        {
            try
            {
                using var proc = Process.GetProcessById(handle.ProcessId);
                if (!proc.HasExited)
                    proc.Kill(entireProcessTree: true);
            }
            catch (ArgumentException)
            {
                // Process already exited
            }
            catch (InvalidOperationException)
            {
                // Process already exited
            }
        }

        /// <inheritdoc />
        public void Dispose() => handle.Dispose();
    }
}
