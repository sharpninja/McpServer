using System.ComponentModel;
using System.Runtime.InteropServices;
using McpServer.Launcher.Native;

namespace McpServer.Launcher.Services;

/// <summary>
/// Handles token duplication for the current process.
/// </summary>
internal static class TokenService
{
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
            if (!NativeMethods.DuplicateTokenEx(
                    existingToken,
                    NativeConstants.TOKEN_ALL_ACCESS,
                    IntPtr.Zero,
                    NativeConstants.SECURITY_IMPERSONATION,
                    NativeConstants.TOKEN_PRIMARY,
                    out var duplicatedToken))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to duplicate process token.");
            }

            return duplicatedToken;
        }
        finally
        {
            NativeMethods.CloseHandle(existingToken);
        }
    }
}
