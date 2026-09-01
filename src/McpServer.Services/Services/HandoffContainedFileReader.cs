using System.Runtime.InteropServices;
using System.Text;
using McpServer.Support.Mcp.Requirements;
using Microsoft.Win32.SafeHandles;

namespace McpServer.Support.Mcp.Services;

/// <summary>TR-HANDOFF-SECURITY-001: Handle-safe bounded reader for workspace-contained handoff files.</summary>
internal static class HandoffContainedFileReader
{
    /// <summary>Opens the path, verifies the opened handle stays inside the workspace, then reads at most 8 MiB.</summary>
    public static async Task<(bool Success, string? Text, string? Code, string? Message)> ReadAsync(
        string workspacePath,
        string fullPath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        FileStream stream;
        try
        {
            stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
        }
        catch (FileNotFoundException)
        {
            return (false, null, "source_missing", "The handoff file was not found.");
        }
        catch (DirectoryNotFoundException)
        {
            return (false, null, "source_missing", "The handoff file was not found.");
        }
        catch (UnauthorizedAccessException)
        {
            return (false, null, "source_external", "The path could not be opened inside the workspace.");
        }

        await using (stream.ConfigureAwait(false))
        {
            if (!TryGetFinalPath(stream.SafeFileHandle, out var resolvedPath))
                return (false, null, "source_reparse", "The opened file handle could not be resolved inside the workspace.");

            var finalPath = string.IsNullOrWhiteSpace(resolvedPath) ? fullPath : resolvedPath;

            if (!RequirementsWikiPathSecurity.IsContainedByRoot(workspacePath, finalPath) ||
                RequirementsWikiPathSecurity.EscapesWorkspaceThroughReparsePoint(workspacePath, finalPath))
            {
                return (false, null, "source_reparse", "The path escapes the workspace through a reparse point.");
            }

            var text = await ReadBoundedAsync(stream, cancellationToken).ConfigureAwait(false);
            if (text is null)
                return (false, null, HandoffErrorCodes.SourceOversized, "Decoded content exceeds the 8 MiB limit.");

            return (true, text, null, null);
        }
    }

    /// <summary>Reads UTF-8 text with a hard decoded-size stop. Returns null when the bound is exceeded.</summary>
    internal static async Task<string?> ReadBoundedAsync(Stream stream, CancellationToken cancellationToken)
    {
        var bytes = new MemoryStream();
        var buffer = new byte[4096];
        var total = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
            if (read == 0)
                break;
            total += read;
            if (total > HandoffPromptDefaults.MaxDecodedBytes)
                return null;
            await bytes.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }

        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    internal static bool TryGetFinalPath(SafeFileHandle handle, out string? path)
    {
        path = null;
        if (!OperatingSystem.IsWindows())
        {
            path = null;
            return true;
        }

        var buffer = new char[1024];
        while (true)
        {
            var length = GetFinalPathNameByHandle(handle, buffer, (uint)buffer.Length, 0);
            if (length == 0)
                return false;

            if (length >= buffer.Length)
            {
                buffer = new char[length + 1];
                continue;
            }

            path = NormalizeFinalPath(new string(buffer, 0, (int)length));
            return !string.IsNullOrWhiteSpace(path);
        }
    }

    internal static string NormalizeFinalPath(string path)
    {
        if (path.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
            return @"\\" + path[@"\\?\UNC\".Length..];
        if (path.StartsWith(@"\\?\", StringComparison.Ordinal))
            return path[4..];
        return path;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint GetFinalPathNameByHandle(
        SafeFileHandle hFile,
        [Out] char[] lpszFilePath,
        uint cchFilePath,
        uint dwFlags);
}
