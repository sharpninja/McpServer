using System.ComponentModel;
using System.Runtime.InteropServices;
using McpServer.Launcher.Native;

namespace McpServer.Launcher.Services;

/// <summary>
/// Handles token acquisition for launching processes on the interactive desktop.
/// </summary>
internal static class TokenService
{
    /// <summary>
    /// Gets the logged-in user's token from the active console session and duplicates it
    /// as a primary token suitable for <c>CreateProcessWithTokenW</c>.
    /// Falls back to duplicating the current process token if WTS APIs fail (e.g. when
    /// running interactively rather than as a service).
    /// </summary>
    /// <returns>A duplicated primary token handle. The caller is responsible for closing this handle.</returns>
    /// <exception cref="Win32Exception">Thrown when token operations fail.</exception>
    internal static IntPtr GetConsoleSessionUserToken()
    {
        var sessionId = NativeMethods.WTSGetActiveConsoleSessionId();
        if (sessionId != -1 && NativeMethods.WTSQueryUserToken(sessionId, out var userToken))
        {
            try
            {
                return DuplicateToken(userToken);
            }
            finally
            {
                NativeMethods.CloseHandle(userToken);
            }
        }

        // Fallback: duplicate the current process token (works when running interactively)
        return DuplicateCurrentProcessToken();
    }

    /// <summary>
    /// Duplicates the current process token as a primary token suitable for <c>CreateProcessWithTokenW</c>.
    /// </summary>
    /// <returns>A duplicated primary token handle. The caller is responsible for closing this handle.</returns>
    /// <exception cref="Win32Exception">Thrown when token operations fail.</exception>
    internal static IntPtr DuplicateCurrentProcessToken()
    {
        if (!NativeMethods.OpenProcessToken(
                NativeMethods.GetCurrentProcess(),
                NativeConstants.TOKEN_DUPLICATE | NativeConstants.TOKEN_QUERY | NativeConstants.TOKEN_ASSIGN_PRIMARY,
                out var existingToken))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to open current process token.");
        }

        try
        {
            return DuplicateToken(existingToken);
        }
        finally
        {
            NativeMethods.CloseHandle(existingToken);
        }
    }

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
}
