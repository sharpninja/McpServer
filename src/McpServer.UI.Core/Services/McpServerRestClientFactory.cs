using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client;

namespace McpServer.UI.Core.Services;

internal static class McpServerRestClientFactory
{
    private const string MarkerFileName = "AGENTS-README-FIRST.yaml";

    public static McpServerClient Create(
        string baseUrl,
        TimeSpan timeout,
        string? apiKey = null,
        string? workspaceRootPath = null,
        string? bearerToken = null)
    {
        var normalizedBaseUrl = NormalizeBaseUrl(baseUrl);
        var resolvedApiKey = string.IsNullOrWhiteSpace(apiKey)
            ? TryResolveApiKey(normalizedBaseUrl, workspaceRootPath)
            : apiKey.Trim();

        var options = new McpServerClientOptions
        {
            BaseUrl = new Uri(normalizedBaseUrl, UriKind.Absolute),
            ApiKey = string.IsNullOrWhiteSpace(resolvedApiKey) ? null : resolvedApiKey,
            BearerToken = string.IsNullOrWhiteSpace(bearerToken) ? null : bearerToken.Trim(),
            Timeout = timeout,
            WorkspacePath = string.IsNullOrWhiteSpace(workspaceRootPath) ? null : workspaceRootPath.Trim(),
            LoggerFactory = AppLogService.Instance
        };

        return McpServerClientFactory.Create(options);
    }

    public static async Task<string?> TryFetchDefaultApiKeyAsync(
        string baseUrl,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedBaseUrl = NormalizeBaseUrl(baseUrl);
            var client = McpServerClientFactory.Create(new McpServerClientOptions
            {
                BaseUrl = new Uri(normalizedBaseUrl, UriKind.Absolute),
                Timeout = timeout ?? TimeSpan.FromSeconds(5)
            });
            var retryDelay = TimeSpan.FromMilliseconds(250);
            var retryDeadlineUtc = DateTime.UtcNow.AddSeconds(10);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return await client.InitializeAsync(cancellationToken);
                }
                catch (McpServerException ex) when (ex.StatusCode == 503 && DateTime.UtcNow < retryDeadlineUtc)
                {
                    await Task.Delay(retryDelay, cancellationToken);
                    var nextDelayMs = Math.Min(retryDelay.TotalMilliseconds * 2d, 1500d);
                    retryDelay = TimeSpan.FromMilliseconds(nextDelayMs);
                }
            }
        }
        catch
        {
            return null;
        }
    }

    public static string? TryResolveApiKey(string baseUrl, string? preferredWorkspaceRootPath = null)
    {
        var normalizedBaseUrl = NormalizeBaseUrl(baseUrl);

        if (TryResolveApiKeyForWorkspaceRoot(preferredWorkspaceRootPath, normalizedBaseUrl) is { Length: > 0 } apiKey)
            return apiKey;

        foreach (var candidateRoot in GetDefaultMarkerCandidateRoots())
        {
            if (TryResolveApiKeyForWorkspaceRoot(candidateRoot, normalizedBaseUrl) is { Length: > 0 } fallbackApiKey)
                return fallbackApiKey;
        }

        return null;
    }

    public static string? TryResolveApiKeyForWorkspaceRoot(string? workspaceRootPath, string? expectedBaseUrl = null)
    {
        if (string.IsNullOrWhiteSpace(workspaceRootPath))
            return null;

        string fullRootPath;
        try
        {
            fullRootPath = Path.GetFullPath(workspaceRootPath.Trim());
        }
        catch
        {
            return null;
        }

        var markerPath = Path.Combine(fullRootPath, MarkerFileName);
        if (!File.Exists(markerPath))
            return null;

        if (!TryReadMarker(markerPath, out var marker))
            return null;

        if (!string.IsNullOrWhiteSpace(expectedBaseUrl) &&
            !string.IsNullOrWhiteSpace(marker.BaseUrl))
        {
            string normalizedExpected;
            string normalizedMarker;
            try
            {
                normalizedExpected = NormalizeBaseUrl(expectedBaseUrl!);
                normalizedMarker = NormalizeBaseUrl(marker.BaseUrl!);
            }
            catch
            {
                return null;
            }

            if (!string.Equals(normalizedExpected, normalizedMarker, StringComparison.OrdinalIgnoreCase))
                return null;
        }

        return string.IsNullOrWhiteSpace(marker.ApiKey) ? null : marker.ApiKey;
    }

    public static string NormalizeBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL is required.", nameof(baseUrl));

        var trimmed = baseUrl.Trim().TrimEnd('/');
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            throw new ArgumentException("Base URL must be an absolute URI.", nameof(baseUrl));

        return uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
    }

    private static IEnumerable<string> GetDefaultMarkerCandidateRoots()
    {
        // Primary path for the current app configuration.
        string? configuredWorkingDir = null;
        try
        {
            configuredWorkingDir = AppSettings.ResolveWorkingDir();
        }
        catch
        {
            configuredWorkingDir = null;
        }

        if (!string.IsNullOrWhiteSpace(configuredWorkingDir))
            yield return configuredWorkingDir;

        // Fallbacks for dev/debug scenarios and ad-hoc launches.
        if (!string.IsNullOrWhiteSpace(Environment.CurrentDirectory))
            yield return Environment.CurrentDirectory;
        if (!string.IsNullOrWhiteSpace(AppContext.BaseDirectory))
            yield return AppContext.BaseDirectory;
    }

    private static bool TryReadMarker(string markerPath, out MarkerInfo marker)
    {
        marker = new MarkerInfo();

        try
        {
            string? baseUrl = null;
            string? apiKey = null;
            foreach (var rawLine in File.ReadLines(markerPath))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;

                if (baseUrl == null && TryParseTopLevelScalar(line, "baseUrl", out var parsedBaseUrl))
                    baseUrl = parsedBaseUrl;
                else if (apiKey == null && TryParseTopLevelScalar(line, "apiKey", out var parsedApiKey))
                    apiKey = parsedApiKey;

                if (baseUrl != null && apiKey != null)
                    break;
            }

            marker = new MarkerInfo { BaseUrl = baseUrl, ApiKey = apiKey };
            return !string.IsNullOrWhiteSpace(marker.ApiKey);
        }
        catch
        {
            return false;
        }
    }

    private static bool TryParseTopLevelScalar(string line, string key, out string? value)
    {
        value = null;
        var prefix = key + ":";
        if (!line.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        var scalar = line.Substring(prefix.Length).Trim();
        if (scalar.Length == 0)
            return false;

        if ((scalar.StartsWith('"') && scalar.EndsWith('"')) ||
            (scalar.StartsWith('\'') && scalar.EndsWith('\'')))
        {
            scalar = scalar.Substring(1, scalar.Length - 2);
        }

        value = scalar.Trim();
        return value.Length > 0;
    }

    private sealed class MarkerInfo
    {
        public string? BaseUrl { get; init; }
        public string? ApiKey { get; init; }
    }
}

