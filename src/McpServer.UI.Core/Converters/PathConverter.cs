using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia.Data.Converters;

namespace McpServer.UI.Core.Converters;

/// <summary>
/// Converts between Windows paths and WSL-style paths under /mnt (e.g. E:\github\foo → /mnt/e/github/foo).
/// </summary>
public static class PathConverter
{
    /// <summary>
    /// Converts a Windows path to a path under /mnt (WSL convention).
    /// On Windows runtime, returns the requested path as-is. On Linux, e.g. "E:\github\FunWasHad\docs" → "/mnt/e/github/FunWasHad/docs".
    /// </summary>
    /// <param name="windowsPath">Windows path (e.g. E:\foo\bar or E:/foo/bar).</param>
    /// <returns>On Windows: the path unchanged. On Linux: path under /mnt with forward slashes, or normalized.</returns>
    public static string ToWslPath(string? windowsPath)
    {
        if (string.IsNullOrWhiteSpace(windowsPath))
            return windowsPath ?? "";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return windowsPath;

        var trimmed = windowsPath.Trim();
        if (trimmed.Length < 2)
            return windowsPath;

        // Already a Unix-style absolute path
        if (trimmed.StartsWith("/") && !trimmed.StartsWith("//"))
            return NormalizeForwardSlashes(trimmed);

        // Windows drive letter: "C:" or "c:"
        char drive = trimmed[0];
        if (char.IsLetter(drive) && trimmed[1] == ':')
        {
            string rest = trimmed.Length > 2 ? trimmed.Substring(2) : "";
            rest = rest.TrimStart('\\', '/');
            string driveLower = drive.ToString().ToLowerInvariant();
            string combined = "/mnt/" + driveLower + (string.IsNullOrEmpty(rest) ? "" : "/" + rest);
            return NormalizeForwardSlashes(combined);
        }

        // UNC or other: return normalized
        return NormalizeForwardSlashes(trimmed.Replace('\\', '/'));
    }

    /// <summary>
    /// Converts a WSL path under /mnt back to a Windows-style path.
    /// E.g. "/mnt/e/github/foo" → "E:\github\foo".
    /// </summary>
    /// <param name="wslPath">Path under /mnt (e.g. /mnt/e/foo/bar).</param>
    /// <returns>Windows path with backslashes, or the original path if conversion does not apply.</returns>
    public static string ToWindowsPath(string? wslPath)
    {
        if (string.IsNullOrWhiteSpace(wslPath))
            return wslPath ?? "";

        var trimmed = wslPath.Trim().Replace('\\', '/');
        if (trimmed.Length < 5)
            return wslPath;

        // /mnt/x/ or /mnt/x
        if (trimmed.StartsWith("/mnt/", StringComparison.OrdinalIgnoreCase))
        {
            string afterMnt = trimmed.Substring(5); // after "/mnt/"
            int firstSlash = afterMnt.IndexOf('/');
            string drivePart = firstSlash < 0 ? afterMnt : afterMnt.Substring(0, firstSlash);
            string rest = firstSlash < 0 ? "" : afterMnt.Substring(firstSlash + 1);

            if (drivePart.Length == 1 && char.IsLetter(drivePart[0]))
            {
                string driveUpper = drivePart.ToUpperInvariant();
                string win = driveUpper + ":" + (string.IsNullOrEmpty(rest) ? "" : "\\" + rest.Replace('/', '\\'));
                return win;
            }
        }

        return wslPath;
    }

    /// <summary>
    /// Returns a path suitable for the current platform: WSL-style on Linux, Windows-style on Windows.
    /// </summary>
    public static string ToDisplayPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return path ?? "";

        if (IsWindowsPath(path))
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                ? ToWslPath(path)
                : path;

        return path;
    }

    /// <summary>
    /// True if the path looks like a Windows path (has a drive letter or backslashes).
    /// </summary>
    public static bool IsWindowsPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length < 2) return false;
        return (char.IsLetter(path[0]) && path[1] == ':') || path.IndexOf('\\') >= 0;
    }

    private static string NormalizeForwardSlashes(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        return path.Replace('\\', '/').TrimEnd('/');
    }
}

/// <summary>
/// Avalonia value converter for bindings: converts Windows paths to /mnt paths for display.
/// </summary>
public class PathToWslConverter : IValueConverter
{
    public static readonly PathToWslConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string path)
            return PathConverter.ToWslPath(path);
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string path)
            return PathConverter.ToWindowsPath(path);
        return value;
    }
}

