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
        // Resolve symlinks — CreateProcessAsUser may not follow them (e.g. WinGet shims)
        executablePath = ResolveSymlinks(executablePath);

        // Create pipes for stdin, stdout, stderr
        CreatePipeWithInheritance(out var stdinRead, out var stdinWrite, inheritRead: true);
        CreatePipeWithInheritance(out var stdoutRead, out var stdoutWrite, inheritRead: false);
        CreatePipeWithInheritance(out var stderrRead, out var stderrWrite, inheritRead: false);

        IntPtr token = IntPtr.Zero;
        var useCreateProcessAsUser = false;

        try
        {
            // Try WTSQueryUserToken first — gives a token bound to the console session.
            // CreateProcessAsUser with this token launches directly in that session
            // without creating a new logon (avoids STATUS_DLL_INIT_FAILED).
            var consoleSessionId = NativeMethods.WTSGetActiveConsoleSessionId();
            if (consoleSessionId != -1 &&
                NativeMethods.WTSQueryUserToken(consoleSessionId, out var userToken))
            {
                _logger.LogDebug("Acquired console session {SessionId} user token via WTSQueryUserToken", consoleSessionId);
                token = userToken;
                useCreateProcessAsUser = true;
            }
            else
            {
                // Fallback: duplicate current process token
                token = GetConsoleSessionUserToken();
            }

            var si = new NativeStructs.STARTUPINFO
            {
                cb = Marshal.SizeOf<NativeStructs.STARTUPINFO>(),
                lpDesktop = "winsta0\\default",
                dwFlags = NativeConstants.STARTF_USESTDHANDLES,
                hStdInput = stdinRead,
                hStdOutput = stdoutWrite,
                hStdError = stderrWrite
            };

            var creationFlags = NativeConstants.CREATE_UNICODE_ENVIRONMENT | NativeConstants.CREATE_NO_WINDOW;
            var envBlock = BuildEnvironmentBlock(environmentVariables);

            var commandLine = BuildCommandLine(executablePath, arguments);

            _logger.LogDebug(
                "Launching desktop process ({Method}): {CommandLine} in {WorkingDirectory}",
                useCreateProcessAsUser ? "CreateProcessAsUser" : "CreateProcessWithTokenW",
                commandLine, workingDirectory ?? "(default)");

            bool success;
            NativeStructs.PROCESS_INFORMATION pi;

            if (useCreateProcessAsUser)
            {
                success = NativeMethods.CreateProcessAsUser(
                    token,
                    null,
                    commandLine,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    true, // bInheritHandles — required for stdio pipes
                    creationFlags,
                    envBlock,
                    workingDirectory,
                    ref si,
                    out pi);
            }
            else
            {
                success = NativeMethods.CreateProcessWithTokenW(
                    token,
                    NativeConstants.LOGON_WITH_PROFILE,
                    null,
                    commandLine,
                    creationFlags,
                    envBlock,
                    workingDirectory,
                    ref si,
                    out pi);
            }

            if (!success)
            {
                var errorCode = Marshal.GetLastWin32Error();
                var method = useCreateProcessAsUser ? "CreateProcessAsUser" : "CreateProcessWithTokenW";
                throw new Win32Exception(errorCode, $"{method} failed for '{executablePath}'.");
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
            CloseIfValid(token);
            throw;
        }
    }

    /// <summary>
    /// Launches a process on the interactive desktop with a visible console window.
    /// No stdio pipes are created — the process runs interactively.
    /// Returns the process ID.
    /// </summary>
    /// <param name="executablePath">Path to the executable.</param>
    /// <param name="arguments">Command-line arguments.</param>
    /// <param name="workingDirectory">Working directory (null for current).</param>
    /// <param name="environmentVariables">Additional environment variables to set.</param>
    /// <returns>The process ID of the launched process.</returns>
    /// <exception cref="Win32Exception">Thrown when native API calls fail.</exception>
    internal int LaunchVisible(
        string executablePath,
        string arguments,
        string? workingDirectory = null,
        Dictionary<string, string>? environmentVariables = null)
    {
        executablePath = ResolveSymlinks(executablePath);

        IntPtr token = IntPtr.Zero;
        var useCreateProcessAsUser = false;

        try
        {
            // Try WTSQueryUserToken first — gives a token bound to the console session.
            // CreateProcessAsUser with this token launches directly in that session.
            var consoleSessionId = NativeMethods.WTSGetActiveConsoleSessionId();
            if (consoleSessionId != -1 &&
                NativeMethods.WTSQueryUserToken(consoleSessionId, out var userToken))
            {
                _logger.LogDebug("Acquired console session {SessionId} user token via WTSQueryUserToken", consoleSessionId);
                token = userToken;
                useCreateProcessAsUser = true;
            }
            else
            {
                // Fallback: duplicate current process token
                token = DuplicateCurrentProcessToken();
            }

            var si = new NativeStructs.STARTUPINFO
            {
                cb = Marshal.SizeOf<NativeStructs.STARTUPINFO>(),
                lpDesktop = "winsta0\\default",
                dwFlags = NativeConstants.STARTF_USESHOWWINDOW,
                wShowWindow = NativeConstants.SW_SHOWNORMAL
            };

            var creationFlags = NativeConstants.CREATE_UNICODE_ENVIRONMENT | NativeConstants.CREATE_NEW_CONSOLE;
            var envBlock = BuildEnvironmentBlock(environmentVariables);

            var commandLine = BuildCommandLine(executablePath, arguments);

            _logger.LogDebug(
                "Launching visible desktop process ({Method}): {CommandLine} in {WorkingDirectory}",
                useCreateProcessAsUser ? "CreateProcessAsUser" : "CreateProcessWithTokenW",
                commandLine, workingDirectory ?? "(default)");

            bool success;
            NativeStructs.PROCESS_INFORMATION pi;

            if (useCreateProcessAsUser)
            {
                success = NativeMethods.CreateProcessAsUser(
                    token,
                    null,
                    commandLine,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    creationFlags,
                    envBlock,
                    workingDirectory,
                    ref si,
                    out pi);
            }
            else
            {
                success = NativeMethods.CreateProcessWithTokenW(
                    token,
                    NativeConstants.LOGON_WITH_PROFILE,
                    null,
                    commandLine,
                    creationFlags,
                    envBlock,
                    workingDirectory,
                    ref si,
                    out pi);
            }

            if (!success)
            {
                var errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode, $"Desktop process launch failed for '{executablePath}'.");
            }

            NativeMethods.CloseHandle(pi.hProcess);
            NativeMethods.CloseHandle(pi.hThread);

            _logger.LogInformation("Visible desktop process launched: PID={ProcessId}", pi.dwProcessId);
            return pi.dwProcessId;
        }
        finally
        {
            CloseIfValid(token);
        }
    }

    /// <summary>
    /// Gets a token for launching processes on the interactive desktop.
    /// Tries WTSQueryUserToken first (requires SE_TCB_NAME, works for LocalSystem services),
    /// then falls back to duplicating the current process token (works when the service runs
    /// as the same user who is logged in at the console).
    /// </summary>
    private IntPtr GetConsoleSessionUserToken()
    {
        var sessionId = NativeMethods.WTSGetActiveConsoleSessionId();
        if (sessionId != -1)
        {
            if (NativeMethods.WTSQueryUserToken(sessionId, out var userToken))
            {
                _logger.LogDebug("Acquired console session {SessionId} user token via WTSQueryUserToken", sessionId);
                try
                {
                    return DuplicateToken(userToken);
                }
                finally
                {
                    NativeMethods.CloseHandle(userToken);
                }
            }

            var wtsError = Marshal.GetLastWin32Error();
            _logger.LogDebug(
                "WTSQueryUserToken failed for session {SessionId} (error {ErrorCode}), falling back to current process token",
                sessionId, wtsError);
        }

        // Fallback: duplicate the current process token and reassign to the console session
        var token = DuplicateCurrentProcessToken();
        if (sessionId != -1)
        {
            if (!NativeMethods.SetTokenInformation(
                    token,
                    NativeConstants.TOKEN_SESSION_ID,
                    ref sessionId,
                    sizeof(int)))
            {
                var setErr = Marshal.GetLastWin32Error();
                _logger.LogWarning(
                    "SetTokenInformation(TokenSessionId={SessionId}) failed (error {ErrorCode}); process may launch in Session 0",
                    sessionId, setErr);
            }
            else
            {
                _logger.LogDebug("Set duplicated token session to {SessionId}", sessionId);
            }
        }

        return token;
    }

    /// <summary>
    /// Duplicates the current process token as a primary token.
    /// </summary>
    private IntPtr DuplicateCurrentProcessToken()
    {
        if (!NativeMethods.OpenProcessToken(
                NativeMethods.GetCurrentProcess(),
                NativeConstants.TOKEN_DUPLICATE | NativeConstants.TOKEN_QUERY
                    | NativeConstants.TOKEN_ASSIGN_PRIMARY | NativeConstants.TOKEN_ADJUST_SESSIONID,
                out var existingToken))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to open current process token.");
        }

        try
        {
            _logger.LogDebug("Duplicating current process token for desktop launch");
            return DuplicateToken(existingToken);
        }
        finally
        {
            NativeMethods.CloseHandle(existingToken);
        }
    }

    /// <summary>
    /// Duplicates a token as a primary token with all access.
    /// </summary>
    private static IntPtr DuplicateToken(IntPtr sourceToken)
    {
        if (!NativeMethods.DuplicateTokenEx(
                sourceToken,
                NativeConstants.TOKEN_ALL_ACCESS,
                IntPtr.Zero,
                NativeConstants.SECURITY_IMPERSONATION,
                NativeConstants.TOKEN_PRIMARY,
                out var duplicatedToken))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to duplicate token.");
        }

        return duplicatedToken;
    }

    /// <summary>Locates <c>pwsh.exe</c> under Program Files (PowerShell 7 stable or preview).</summary>
    private static string? TryGetPwshExecutablePath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (string.IsNullOrEmpty(root))
            return null;

        foreach (var relative in new[] { Path.Combine("PowerShell", "7", "pwsh.exe"), Path.Combine("PowerShell", "7-preview", "pwsh.exe") })
        {
            var full = Path.Combine(root, relative);
            if (File.Exists(full))
                return full;
        }

        return null;
    }

    /// <summary>
    /// Resolves a command name to its full executable path by running
    /// <c>Get-Command</c> in a <c>pwsh.exe</c> session on the interactive desktop.
    /// Returns null if <c>pwsh.exe</c> is not installed or the command cannot be resolved.
    /// </summary>
    /// <param name="commandName">The command name to resolve (e.g. <c>copilot</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The full path to the executable, or null if not found.</returns>
    internal async Task<string?> ResolveCommandPathAsync(string commandName, CancellationToken cancellationToken = default)
    {
        var pwshPath = TryGetPwshExecutablePath();
        if (string.IsNullOrEmpty(pwshPath))
        {
            _logger.LogWarning("pwsh.exe not found under Program Files; cannot resolve command");
            return null;
        }

        var escapedName = commandName.Replace("'", "''");
        var arguments = $"-NoProfile -NonInteractive -Command \"(Get-Command '{escapedName}' -ErrorAction SilentlyContinue).Source\"";

        _logger.LogDebug("Resolving command path for '{Command}' via desktop pwsh.exe", commandName);

        using var handle = LaunchWithStdio(pwshPath, arguments);
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

    /// <summary>
    /// Resolves symbolic links to their final target path.
    /// <c>CreateProcessWithTokenW</c> may not follow symlinks (e.g. WinGet shims).
    /// </summary>
    private string ResolveSymlinks(string path)
    {
        try
        {
            var fi = new FileInfo(path);
            if (fi.LinkTarget is { } target)
            {
                // Resolve relative link targets
                var resolved = Path.IsPathRooted(target)
                    ? target
                    : Path.GetFullPath(target, Path.GetDirectoryName(path)!);

                if (File.Exists(resolved))
                {
                    _logger.LogDebug("Resolved symlink '{Original}' → '{Target}'", path, resolved);
                    return resolved;
                }
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _logger.LogWarning("Failed to resolve symlink for '{Path}': {Error}", path, ex.Message);
        }

        return path;
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
