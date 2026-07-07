using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace McpServer.Common.AgentCli;

/// <summary>
/// Default implementation of <see cref="IProcessEnvironmentService"/>.
/// Resolves user-profile directories and PATH from the Windows registry
/// so that processes launched from a Windows service can find CLIs and
/// authenticate with GitHub. When no explicit <c>runAsUser</c> is provided,
/// automatically detects the interactive desktop user via the WTS API.
/// </summary>
public sealed class ProcessEnvironmentService(
    ILogger<ProcessEnvironmentService> logger) : IProcessEnvironmentService
{
    /// <inheritdoc />
    public void ApplyGitHubToken(ProcessStartInfo psi, string? token)
    {
        var effective = !string.IsNullOrWhiteSpace(token)
            ? token
            : Environment.GetEnvironmentVariable("GH_TOKEN");

        if (!string.IsNullOrWhiteSpace(effective))
            psi.Environment["GH_TOKEN"] = effective;
    }

    /// <inheritdoc />
    public void ApplyRunAsEnvironment(ProcessStartInfo psi, string? runAsUser)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        // Auto-detect interactive desktop user when none is specified.
        var effectiveUser = !string.IsNullOrWhiteSpace(runAsUser)
            ? runAsUser
            : DetectInteractiveUser();

        if (string.IsNullOrWhiteSpace(effectiveUser))
            return;

        var userProfile = ResolveUserProfile(effectiveUser);
        if (!Directory.Exists(userProfile))
        {
            logger.LogWarning("User profile not found: {UserProfile}", userProfile);
            return;
        }

        var appData = Path.Combine(userProfile, "AppData", "Roaming");
        var localAppData = Path.Combine(userProfile, "AppData", "Local");

        psi.Environment["USERPROFILE"] = userProfile;
        psi.Environment["HOME"] = userProfile;
        psi.Environment["APPDATA"] = appData;
        psi.Environment["LOCALAPPDATA"] = localAppData;

        var userPath = ResolveUserPath(effectiveUser, userProfile, localAppData);
        if (!string.IsNullOrWhiteSpace(userPath))
        {
            var currentPath = psi.Environment.TryGetValue("PATH", out var existing)
                ? existing
                : Environment.GetEnvironmentVariable("PATH");
            psi.Environment["PATH"] = $"{userPath};{currentPath}";
        }

        logger.LogDebug("Applied environment for user {User}: USERPROFILE={Profile}", effectiveUser, userProfile);
    }

    /// <inheritdoc />
    public void ApplyAll(ProcessStartInfo psi, string? runAsUser, string? gitHubToken)
    {
        ApplyRunAsEnvironment(psi, runAsUser);
        ApplyGitHubToken(psi, gitHubToken);
    }

    /// <inheritdoc />
    public string ResolveExecutable(ProcessStartInfo psi, string fileName)
    {
        // If already a rooted path or contains a directory separator, use as-is.
        if (Path.IsPathRooted(fileName) || fileName.Contains(Path.DirectorySeparatorChar))
            return fileName;

        var path = psi.Environment.TryGetValue("PATH", out var p) ? p : null;
        if (string.IsNullOrWhiteSpace(path))
            return fileName;

        var extensions = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new[] { ".exe", ".cmd", ".bat", ".com", "" }
            : new[] { "" };

        foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var ext in extensions)
            {
                var candidate = Path.Combine(dir.Trim(), fileName + ext);
                if (File.Exists(candidate))
                {
                    logger.LogDebug("Resolved {FileName} → {FullPath}", fileName, candidate);
                    return candidate;
                }
            }
        }

        logger.LogDebug("Could not resolve {FileName} on injected PATH", fileName);
        return fileName;
    }

    /// <summary>
    /// Detects the interactive desktop user via the Windows Terminal Services API.
    /// Returns null when no interactive session is found (e.g. no user logged in).
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal string? DetectInteractiveUser()
    {
        try
        {
            var sessionId = WTSGetActiveConsoleSessionId();
            if (sessionId == 0xFFFFFFFF)
            {
                logger.LogDebug("No active console session found");
                return null;
            }

            if (WTSQuerySessionInformation(IntPtr.Zero, sessionId, WTS_INFO_CLASS.WTSUserName, out var buffer, out _))
            {
                try
                {
                    var username = Marshal.PtrToStringUni(buffer);
                    if (!string.IsNullOrWhiteSpace(username))
                    {
                        logger.LogDebug("Detected interactive user: {User} (session {SessionId})", username, sessionId);
                        return username;
                    }
                }
                finally
                {
                    WTSFreeMemory(buffer);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to detect interactive user via WTS API");
        }

        return null;
    }

    /// <summary>
    /// Resolves the user profile directory for the given username.
    /// If the current process profile already matches, uses it directly;
    /// otherwise constructs the path under the system Users root.
    /// </summary>
    internal static string ResolveUserProfile(string username)
    {
        var currentProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (currentProfile.Contains(username, StringComparison.OrdinalIgnoreCase))
            return currentProfile;

        return Path.Combine(GetUsersRoot(), username);
    }

    /// <summary>
    /// Resolves the user-specific PATH entries by reading from the registry
    /// (<c>HKEY_USERS\{SID}\Environment\Path</c>) and appending common
    /// tool directories (WinGet, Scoop).
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal string ResolveUserPath(string username, string userProfile, string localAppData)
    {
        var parts = new List<string>();

        try
        {
            using var usersKey = Microsoft.Win32.Registry.Users;
            string? bestPath = null;
            var bestLength = 0;

            foreach (var sid in usersKey.GetSubKeyNames())
            {
                // Skip well-known system SIDs (.DEFAULT, S-1-5-18/19/20, _Classes suffixes).
                if (sid.StartsWith('.') || sid.EndsWith("_Classes", StringComparison.Ordinal))
                    continue;
                if (sid is "S-1-5-18" or "S-1-5-19" or "S-1-5-20")
                    continue;

                try
                {
                    using var envKey = usersKey.OpenSubKey($@"{sid}\Environment");
                    if (envKey is null) continue;

                    var regPath = envKey.GetValue("Path") as string;
                    if (string.IsNullOrWhiteSpace(regPath)) continue;

                    // Match SIDs whose PATH references the user's profile.
                    // Prefer the longest match (real user SID has richer PATH than system SIDs).
                    if (regPath.Contains(username, StringComparison.OrdinalIgnoreCase) && regPath.Length > bestLength)
                    {
                        bestPath = regPath;
                        bestLength = regPath.Length;
                    }
                }
                catch (System.Security.SecurityException)
                {
                    // LocalSystem may not be able to read all registry hives.
                }
            }

            if (bestPath is not null)
                parts.Add(bestPath);
        }
        catch (System.Security.SecurityException ex)
        {
            logger.LogWarning("Cannot read registry for user {User} PATH: {Error}", username, ex.Message);
        }

        // NVM-launched npm shims need the selected Node runtime ahead of older system installs.
        AddIfExists(parts, Path.Combine(userProfile, "scoop", "apps", "nvm", "current", "nodejs", "nodejs"));
        AddIfExists(parts, Path.Combine(userProfile, "scoop", "apps", "nvm", "current", "nodejs"));

        // Common tool directories that host CLIs.
        AddIfExists(parts, Path.Combine(localAppData, "Microsoft", "WinGet", "Links"));
        AddIfExists(parts, Path.Combine(userProfile, "scoop", "shims"));
        AddIfExists(parts, Path.Combine(userProfile, ".cargo", "bin"));
        AddIfExists(parts, Path.Combine(userProfile, "AppData", "Roaming", "npm"));

        return string.Join(";", parts);
    }

    /// <summary>Returns the system Users root directory (typically <c>C:\Users</c>).</summary>
    internal static string GetUsersRoot()
    {
        var profileRoot = Environment.GetEnvironmentVariable("PUBLIC");
        return profileRoot is not null
            ? Path.GetDirectoryName(profileRoot) ?? @"C:\Users"
            : @"C:\Users";
    }

    private static void AddIfExists(List<string> parts, string dir)
    {
        if (Directory.Exists(dir) && !parts.Exists(p => p.Contains(dir, StringComparison.OrdinalIgnoreCase)))
            parts.Add(dir);
    }

    // --- WTS P/Invoke ---

    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll")]
    private static extern uint WTSGetActiveConsoleSessionId();

    [SupportedOSPlatform("windows")]
    [DllImport("wtsapi32.dll", CharSet = CharSet.Unicode)]
    private static extern bool WTSQuerySessionInformation(
        IntPtr hServer, uint sessionId, WTS_INFO_CLASS wtsInfoClass,
        out IntPtr ppBuffer, out uint pBytesReturned);

    [SupportedOSPlatform("windows")]
    [DllImport("wtsapi32.dll")]
    private static extern void WTSFreeMemory(IntPtr pMemory);

    [SupportedOSPlatform("windows")]
    private enum WTS_INFO_CLASS { WTSUserName = 5 }
}
