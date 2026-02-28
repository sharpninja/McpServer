using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace McpServer.Support.Mcp.Native;

/// <summary>
/// Handle to a process launched on the interactive desktop with redirected stdio streams.
/// Dispose to close all native handles.
/// </summary>
internal sealed class DesktopProcessHandle : IDisposable
{
    private readonly IntPtr _processHandle;
    private readonly IntPtr _threadHandle;
    private bool _disposed;

    /// <summary>
    /// Creates a new <see cref="DesktopProcessHandle"/>.
    /// </summary>
    internal DesktopProcessHandle(
        int processId,
        IntPtr processHandle,
        IntPtr threadHandle,
        StreamWriter standardInput,
        StreamReader standardOutput,
        StreamReader standardError)
    {
        ProcessId = processId;
        _processHandle = processHandle;
        _threadHandle = threadHandle;
        StandardInput = standardInput;
        StandardOutput = standardOutput;
        StandardError = standardError;
    }

    /// <summary>Process ID of the launched process.</summary>
    internal int ProcessId { get; }

    /// <summary>Writer connected to the process's standard input.</summary>
    internal StreamWriter StandardInput { get; }

    /// <summary>Reader connected to the process's standard output.</summary>
    internal StreamReader StandardOutput { get; }

    /// <summary>Reader connected to the process's standard error.</summary>
    internal StreamReader StandardError { get; }

    /// <summary>
    /// Waits for the process to exit and returns the exit code.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The process exit code.</returns>
    internal async Task<int> WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        // Poll-based async wait to avoid blocking a thread pool thread.
        while (!cancellationToken.IsCancellationRequested)
        {
            var waitResult = NativeMethods.WaitForSingleObject(_processHandle, 100);
            if (waitResult == NativeConstants.WAIT_OBJECT_0)
            {
                if (NativeMethods.GetExitCodeProcess(_processHandle, out var exitCode))
                    return exitCode;
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to get exit code.");
            }

            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return -1; // Unreachable
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        StandardInput.Dispose();
        StandardOutput.Dispose();
        StandardError.Dispose();

        if (_threadHandle != IntPtr.Zero)
            NativeMethods.CloseHandle(_threadHandle);
        if (_processHandle != IntPtr.Zero)
            NativeMethods.CloseHandle(_processHandle);
    }
}

/// <summary>
/// Launches a process on the interactive desktop using <c>CreateProcessWithTokenW</c>
/// with full stdio pipe access for reading/writing the process streams.
/// </summary>
internal sealed class DesktopProcessLauncher
{
    private readonly ILogger<DesktopProcessLauncher> _logger;

    /// <summary>
    /// Creates a new <see cref="DesktopProcessLauncher"/>.
    /// </summary>
    internal DesktopProcessLauncher(ILogger<DesktopProcessLauncher> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Launches a process on the interactive desktop with redirected stdio.
    /// </summary>
    /// <param name="executablePath">Path to the executable.</param>
    /// <param name="arguments">Command-line arguments.</param>
    /// <param name="workingDirectory">Working directory (null for current).</param>
    /// <param name="environmentVariables">Additional environment variables to set.</param>
    /// <returns>A <see cref="DesktopProcessHandle"/> with PID and stdio streams.</returns>
    /// <exception cref="Win32Exception">Thrown when native API calls fail.</exception>
    internal DesktopProcessHandle LaunchWithStdio(
        string executablePath,
        string arguments,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null)
    {
        // Create pipes for stdin, stdout, stderr
        CreatePipeWithInheritance(out var stdinRead, out var stdinWrite, inheritRead: true);
        CreatePipeWithInheritance(out var stdoutRead, out var stdoutWrite, inheritRead: false);
        CreatePipeWithInheritance(out var stderrRead, out var stderrWrite, inheritRead: false);

        IntPtr duplicatedToken = IntPtr.Zero;

        try
        {
            duplicatedToken = GetConsoleSessionUserToken();

            var si = new NativeStructs.STARTUPINFO
            {
                cb = Marshal.SizeOf<NativeStructs.STARTUPINFO>(),
                lpDesktop = "winsta0\\default",
                dwFlags = NativeConstants.STARTF_USESTDHANDLES,
                hStdInput = stdinRead,
                hStdOutput = stdoutWrite,
                hStdError = stderrWrite
            };

            var creationFlags = NativeConstants.CREATE_UNICODE_ENVIRONMENT | NativeConstants.CREATE_NEW_CONSOLE;
            var envBlock = BuildEnvironmentBlock(environmentVariables);

            var commandLine = BuildCommandLine(executablePath, arguments);

            _logger.LogDebug(
                "Launching desktop process: {CommandLine} in {WorkingDirectory}",
                commandLine, workingDirectory ?? "(default)");

            var success = NativeMethods.CreateProcessWithTokenW(
                duplicatedToken,
                NativeConstants.LOGON_WITH_PROFILE,
                null,
                commandLine,
                creationFlags,
                envBlock,
                workingDirectory,
                ref si,
                out var pi);

            if (!success)
            {
                var errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode, $"CreateProcessWithTokenW failed for '{executablePath}'.");
            }

            // Close the child-side pipe ends (they're inherited by the child process)
            NativeMethods.CloseHandle(stdinRead);
            stdinRead = IntPtr.Zero;
            NativeMethods.CloseHandle(stdoutWrite);
            stdoutWrite = IntPtr.Zero;
            NativeMethods.CloseHandle(stderrWrite);
            stderrWrite = IntPtr.Zero;

            // Wrap parent-side pipe handles in managed streams
            var stdinStream = new FileStream(new SafeFileHandle(stdinWrite, ownsHandle: true), FileAccess.Write);
            stdinWrite = IntPtr.Zero; // SafeFileHandle owns it now
            var stdoutStream = new FileStream(new SafeFileHandle(stdoutRead, ownsHandle: true), FileAccess.Read);
            stdoutRead = IntPtr.Zero;
            var stderrStream = new FileStream(new SafeFileHandle(stderrRead, ownsHandle: true), FileAccess.Read);
            stderrRead = IntPtr.Zero;

            var stdinWriter = new StreamWriter(stdinStream, Encoding.UTF8) { AutoFlush = true };
            var stdoutReader = new StreamReader(stdoutStream, Encoding.UTF8);
            var stderrReader = new StreamReader(stderrStream, Encoding.UTF8);

            _logger.LogInformation("Desktop process launched: PID={ProcessId}", pi.dwProcessId);

            return new DesktopProcessHandle(
                pi.dwProcessId,
                pi.hProcess,
                pi.hThread,
                stdinWriter,
                stdoutReader,
                stderrReader);
        }
        catch
        {
            // Clean up any handles that weren't transferred
            CloseIfValid(stdinRead);
            CloseIfValid(stdinWrite);
            CloseIfValid(stdoutRead);
            CloseIfValid(stdoutWrite);
            CloseIfValid(stderrRead);
            CloseIfValid(stderrWrite);
            CloseIfValid(duplicatedToken);
            throw;
        }
    }

    /// <summary>
    /// Gets the logged-in user's token from the active console session via WTSQueryUserToken,
    /// then duplicates it as a primary token for CreateProcessWithTokenW.
    /// </summary>
    private static IntPtr GetConsoleSessionUserToken()
    {
        var sessionId = NativeMethods.WTSGetActiveConsoleSessionId();
        if (sessionId == -1)
            throw new InvalidOperationException("No active console session found. Is a user logged in?");

        if (!NativeMethods.WTSQueryUserToken(sessionId, out var userToken))
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"WTSQueryUserToken failed for session {sessionId}. Service may need SE_TCB_NAME privilege.");

        try
        {
            if (!NativeMethods.DuplicateTokenEx(
                    userToken,
                    NativeConstants.TOKEN_ALL_ACCESS,
                    IntPtr.Zero,
                    NativeConstants.SECURITY_IMPERSONATION,
                    NativeConstants.TOKEN_PRIMARY,
                    out var duplicatedToken))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to duplicate console session user token.");
            }

            return duplicatedToken;
        }
        finally
        {
            NativeMethods.CloseHandle(userToken);
        }
    }

    /// <summary>
    /// Resolves a command name to its full executable path by running
    /// <c>Get-Command</c> in a PowerShell session on the interactive desktop.
    /// Returns null if the command cannot be resolved.
    /// </summary>
    /// <param name="commandName">The command name to resolve (e.g. <c>copilot</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The full path to the executable, or null if not found.</returns>
    internal async Task<string?> ResolveCommandPathAsync(string commandName, CancellationToken cancellationToken = default)
    {
        var psPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell", "v1.0", "powershell.exe");

        if (!File.Exists(psPath))
        {
            _logger.LogWarning("PowerShell not found at {Path}, cannot resolve command", psPath);
            return null;
        }

        var escapedName = commandName.Replace("'", "''");
        var arguments = $"-NoProfile -NonInteractive -Command \"(Get-Command '{escapedName}' -ErrorAction SilentlyContinue).Source\"";

        _logger.LogDebug("Resolving command path for '{Command}' via desktop PowerShell", commandName);

        using var handle = LaunchWithStdio(psPath, arguments);
        var stdout = await handle.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var exitCode = await handle.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var resolvedPath = stdout.Trim();
        if (exitCode == 0 && !string.IsNullOrEmpty(resolvedPath) && File.Exists(resolvedPath))
        {
            _logger.LogInformation("Resolved '{Command}' → '{Path}'", commandName, resolvedPath);
            return resolvedPath;
        }

        _logger.LogWarning("Could not resolve command '{Command}' on desktop (exit={ExitCode}, output='{Output}')",
            commandName, exitCode, resolvedPath);
        return null;
    }

    private static void CreatePipeWithInheritance(out IntPtr readHandle, out IntPtr writeHandle, bool inheritRead)
    {
        var sa = new NativeStructs.SECURITY_ATTRIBUTES
        {
            nLength = Marshal.SizeOf<NativeStructs.SECURITY_ATTRIBUTES>(),
            bInheritHandle = true,
            lpSecurityDescriptor = IntPtr.Zero
        };

        if (!NativeMethods.CreatePipe(out readHandle, out writeHandle, ref sa, 0))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create pipe.");

        // Remove inheritance from the parent-side handle
        var parentHandle = inheritRead ? writeHandle : readHandle;
        if (!NativeMethods.SetHandleInformation(parentHandle, NativeConstants.HANDLE_FLAG_INHERIT, 0))
        {
            var error = Marshal.GetLastWin32Error();
            NativeMethods.CloseHandle(readHandle);
            NativeMethods.CloseHandle(writeHandle);
            throw new Win32Exception(error, "Failed to set handle information.");
        }
    }

    private static string BuildCommandLine(string executablePath, string arguments)
    {
        var exe = executablePath.Contains(' ') ? $"\"{executablePath}\"" : executablePath;
        return string.IsNullOrEmpty(arguments) ? exe : $"{exe} {arguments}";
    }

    private static IntPtr BuildEnvironmentBlock(Dictionary<string, string>? environmentVariables)
    {
        if (environmentVariables is not { Count: > 0 })
            return IntPtr.Zero;

        var env = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in Environment.GetEnvironmentVariables())
        {
            if (entry is System.Collections.DictionaryEntry de &&
                de.Key is string key && de.Value is string value)
            {
                env[key] = value;
            }
        }

        foreach (var (key, value) in environmentVariables)
            env[key] = value;

        var sb = new StringBuilder();
        foreach (var (key, value) in env)
        {
            sb.Append(key);
            sb.Append('=');
            sb.Append(value);
            sb.Append('\0');
        }

        sb.Append('\0');
        return Marshal.StringToHGlobalUni(sb.ToString());
    }

    private static void CloseIfValid(IntPtr handle)
    {
        if (handle != IntPtr.Zero)
            NativeMethods.CloseHandle(handle);
    }
}
