using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using McpServer.Launcher.Models;
using McpServer.Launcher.Native;

namespace McpServer.Launcher.Services;

/// <summary>
/// Launches a process on the interactive desktop using <c>CreateProcessWithTokenW</c>.
/// Duplicates the current process token and targets <c>winsta0\default</c>.
/// </summary>
public sealed class ProcessLauncher : IProcessLauncher
{
    /// <inheritdoc />
    public ProcessLaunchResult Launch(ProcessLaunchRequest request)
    {
        IntPtr duplicatedToken = IntPtr.Zero;
        var pi = new NativeStructs.PROCESS_INFORMATION();

        try
        {
            duplicatedToken = TokenService.GetConsoleSessionUserToken();

            var si = new NativeStructs.STARTUPINFO
            {
                cb = Marshal.SizeOf<NativeStructs.STARTUPINFO>(),
                lpDesktop = "winsta0\\default",
                dwFlags = NativeConstants.STARTF_USESHOWWINDOW,
                wShowWindow = MapWindowStyle(request.WindowStyle, request.CreateNoWindow)
            };

            var creationFlags = NativeConstants.CREATE_UNICODE_ENVIRONMENT;
            if (request.CreateNoWindow)
                creationFlags |= NativeConstants.CREATE_NO_WINDOW;
            else
                creationFlags |= NativeConstants.CREATE_NEW_CONSOLE;

            var envBlock = BuildEnvironmentBlock(request.EnvironmentVariables);

            var commandLine = BuildCommandLine(request.ExecutablePath, request.Arguments);

            var success = NativeMethods.CreateProcessWithTokenW(
                duplicatedToken,
                NativeConstants.LOGON_WITH_PROFILE,
                null,
                commandLine,
                creationFlags,
                envBlock,
                request.WorkingDirectory,
                ref si,
                out pi);

            if (!success)
            {
                var errorCode = Marshal.GetLastWin32Error();
                return new ProcessLaunchResult
                {
                    Success = false,
                    ErrorMessage = new Win32Exception(errorCode).Message,
                    ErrorCode = errorCode
                };
            }

            var result = new ProcessLaunchResult
            {
                Success = true,
                ProcessId = pi.dwProcessId
            };

            if (request.WaitForExit)
            {
                var timeout = request.TimeoutMs ?? NativeConstants.INFINITE;
                var waitResult = NativeMethods.WaitForSingleObject(pi.hProcess, timeout);

                if (waitResult == NativeConstants.WAIT_TIMEOUT)
                {
                    result.ErrorMessage = "Process did not exit within the specified timeout.";
                }
                else if (waitResult == NativeConstants.WAIT_OBJECT_0)
                {
                    if (NativeMethods.GetExitCodeProcess(pi.hProcess, out var exitCode))
                        result.ExitCode = exitCode;
                }
            }

            return result;
        }
        catch (Win32Exception ex)
        {
            return new ProcessLaunchResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorCode = ex.NativeErrorCode
            };
        }
        finally
        {
            if (pi.hThread != IntPtr.Zero)
                NativeMethods.CloseHandle(pi.hThread);
            if (pi.hProcess != IntPtr.Zero)
                NativeMethods.CloseHandle(pi.hProcess);
            if (duplicatedToken != IntPtr.Zero)
                NativeMethods.CloseHandle(duplicatedToken);
        }
    }

    /// <summary>
    /// Maps <see cref="WindowStyleOption"/> to a Win32 SW_* constant.
    /// </summary>
    private static short MapWindowStyle(WindowStyleOption style, bool createNoWindow)
    {
        if (createNoWindow)
            return NativeConstants.SW_HIDE;

        return style switch
        {
            WindowStyleOption.Hidden => NativeConstants.SW_HIDE,
            WindowStyleOption.Minimized => NativeConstants.SW_SHOWMINIMIZED,
            WindowStyleOption.Maximized => NativeConstants.SW_SHOWMAXIMIZED,
            _ => NativeConstants.SW_SHOWNORMAL
        };
    }

    /// <summary>
    /// Builds a command-line string from executable path and arguments.
    /// Quotes the executable path if it contains spaces.
    /// </summary>
    private static string BuildCommandLine(string executablePath, string? arguments)
    {
        var exe = executablePath.Contains(' ') ? $"\"{executablePath}\"" : executablePath;
        return string.IsNullOrEmpty(arguments) ? exe : $"{exe} {arguments}";
    }

    /// <summary>
    /// Builds a Unicode environment block (null-delimited, double-null terminated) for
    /// <c>CreateProcessWithTokenW</c> with <c>CREATE_UNICODE_ENVIRONMENT</c>.
    /// Returns <see cref="IntPtr.Zero"/> if no custom variables are specified.
    /// </summary>
    private static IntPtr BuildEnvironmentBlock(Dictionary<string, string>? environmentVariables)
    {
        if (environmentVariables is not { Count: > 0 })
            return IntPtr.Zero;

        // Merge with current environment
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

        sb.Append('\0'); // Double-null terminator

        var block = Marshal.StringToHGlobalUni(sb.ToString());
        return block;
    }
}
