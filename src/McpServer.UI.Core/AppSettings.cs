using System;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Linq;
using System.Globalization;
using McpServer.UI.Core.Converters;

namespace McpServer.UI.Core;

public sealed class AppSettings
{
    private const string ConfigFileName = "appsettings.config";

    public PathSettings Paths { get; init; } = new();
    public McpSettings Mcp { get; init; } = new();
    public string? WorkingDir { get; init; }

    private static readonly Lazy<AppSettings> CurrentValue = new(Load);

    public static AppSettings Current => CurrentValue.Value;

    public static string ResolveSessionsRootPath()
        => ResolveRequiredPath(Current.Paths.SessionsRootPath, "Paths.SessionsRootPath");

    public static string ResolveHtmlCacheDirectory()
    {
        var dir = Current.Paths.HtmlCacheDirectory;
        if (string.IsNullOrWhiteSpace(dir))
        {
            // Fallback: use a temp directory
            return Path.Combine(Path.GetTempPath(), "McpServerManager_Cache");
        }
        return ResolveRequiredPath(dir, "Paths.HtmlCacheDirectory");
    }

    public static string? ResolveCssFallbackPath()
        => ResolveOptionalPath(Current.Paths.CssFallbackPath);

    public static string? ResolveWorkingDir()
        => ResolveOptionalPath(Current.WorkingDir);

    public static string ResolveMcpBaseUrl()
        => ResolveRequiredUrl(Current.Mcp.BaseUrl, "Mcp.BaseUrl");

    private static AppSettings Load()
    {
        string configPath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
        if (!File.Exists(configPath))
        {
            // On Android there is no appsettings.config; return defaults.
            if (OperatingSystem.IsAndroid())
                return new AppSettings();
            throw new FileNotFoundException("Missing appsettings.config. Ensure it is copied next to the executable.", configPath);
        }

        string json = File.ReadAllText(configPath);
        var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        if (settings == null)
            throw new InvalidOperationException("Failed to parse appsettings.config.");

        return settings;
    }

    private static string ResolveRequiredPath(string? value, string settingName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Setting '{settingName}' is required.");
        return NormalizePath(value);
    }

    private static string? ResolveOptionalPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return NormalizePath(value);
    }

    private static string ResolveRequiredUrl(string? value, string settingName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Setting '{settingName}' is required.");
        var url = value.Trim().TrimEnd('/');
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException($"Setting '{settingName}' must be an absolute http/https URL.");
        }

        // In Debug Linux desktop/WSL runs, route localhost MCP traffic through the
        // Windows host adapter IP (WSL default gateway) for reliable host reachability.
        if (ShouldUseWslHostAdapterIpInLinuxDebug() &&
            (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) || uri.Host == "127.0.0.1"))
        {
            var hostIp = TryGetWslHostAdapterIp() ?? TryGetHostNameserverIp();
            if (!string.IsNullOrEmpty(hostIp) && Uri.CheckHostName(hostIp) != UriHostNameType.Unknown)
            {
                var ub = new UriBuilder(uri) { Host = hostIp };
                return ub.Uri.ToString().TrimEnd('/');
            }
        }

        return uri.ToString().TrimEnd('/');
    }

    private static string? TryGetHostNameserverIp()
    {
        try
        {
            const string resolvConf = "/etc/resolv.conf";
            if (!File.Exists(resolvConf))
                return null;

            var line = File.ReadLines(resolvConf)
                .Select(l => l.Trim())
                .FirstOrDefault(l => l.StartsWith("nameserver ", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(line))
                return null;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
                return null;

            return parts[1];
        }
        catch
        {
            return null;
        }
    }

    private static string? TryGetWslHostAdapterIp()
    {
        try
        {
            const string routeFile = "/proc/net/route";
            if (!File.Exists(routeFile))
                return null;

            foreach (var line in File.ReadLines(routeFile).Skip(1))
            {
                var parts = line.Split(
                    new[] { '\t', ' ' },
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length < 3)
                    continue;

                var destination = parts[1];
                var gateway = parts[2];
                if (!string.Equals(destination, "00000000", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (TryParseRouteHexIpv4(gateway, out var ip))
                    return ip;
            }
        }
        catch
        {
            // best-effort
        }

        return null;
    }

    private static bool TryParseRouteHexIpv4(string hexValue, out string ip)
    {
        ip = string.Empty;
        if (string.IsNullOrWhiteSpace(hexValue) ||
            hexValue.Length != 8 ||
            !uint.TryParse(hexValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var raw))
        {
            return false;
        }

        ip = $"{raw & 0xFF}.{(raw >> 8) & 0xFF}.{(raw >> 16) & 0xFF}.{(raw >> 24) & 0xFF}";
        return true;
    }

    private static bool ShouldUseWslHostAdapterIpInLinuxDebug()
    {
#if DEBUG
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !OperatingSystem.IsAndroid();
#else
        return false;
#endif
    }

    private static string NormalizePath(string value)
    {
        string expanded = Environment.ExpandEnvironmentVariables(value.Trim());
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && PathConverter.IsWindowsPath(expanded))
            return Path.GetFullPath(PathConverter.ToWslPath(expanded));
        if (!Path.IsPathRooted(expanded))
            expanded = Path.Combine(AppContext.BaseDirectory, expanded);
        return Path.GetFullPath(expanded);
    }

    public sealed class PathSettings
    {
        public string? SessionsRootPath { get; init; }
        public string? HtmlCacheDirectory { get; init; }
        public string? CssFallbackPath { get; init; }
    }

    public sealed class McpSettings
    {
        public string? BaseUrl { get; init; }
    }
}

