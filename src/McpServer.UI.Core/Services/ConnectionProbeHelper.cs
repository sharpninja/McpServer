using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Utility methods extracted from ConnectionViewModel to avoid ViewModel boundary violations
/// (VM006: JsonDocument.Parse, VM007: HttpClient direct usage).
/// </summary>
internal static class ConnectionProbeHelper
{
    /// <summary>
    /// Probes a server URL for health, detecting HTTP→HTTPS redirects.
    /// Returns the (possibly upgraded) base URL.
    /// </summary>
    public static async Task<string> ProbeHealthAndResolveUrlAsync(string url, CancellationToken ct)
    {
        // Use a non-redirecting probe so we can detect HTTP→HTTPS upgrades
        // (e.g. ngrok free tier) and use the final scheme. .NET strips the
        // Authorization header on scheme-change redirects, which breaks Bearer auth.
        using var probeHandler = new HttpClientHandler { AllowAutoRedirect = false };
        using var probe = new HttpClient(probeHandler) { Timeout = TimeSpan.FromSeconds(5) };

        using var healthResponse = await probe.GetAsync($"{url}/health", ct);
        if ((int)healthResponse.StatusCode is >= 301 and <= 308
            && healthResponse.Headers.Location is { } redirectLocation)
        {
            var redirected = redirectLocation.IsAbsoluteUri ? redirectLocation : new Uri(new Uri(url), redirectLocation);
            return $"{redirected.Scheme}://{redirected.Host}:{redirected.Port}";
        }

        return url;
    }

    /// <summary>
    /// Checks whether a JWT token has expired or is near expiry.
    /// </summary>
    public static bool IsJwtExpiredOrNearExpiry(
        string jwtToken,
        TimeSpan skew,
        Func<string, byte[]> base64UrlDecoder,
        out DateTimeOffset? expiresAtUtc)
    {
        expiresAtUtc = null;

        try
        {
            var parts = jwtToken.Split('.');
            if (parts.Length < 2)
                return false;

            var payloadBytes = base64UrlDecoder(parts[1]);
            using var payload = JsonDocument.Parse(payloadBytes);
            if (!payload.RootElement.TryGetProperty("exp", out var expElement))
                return false;

            long expUnixSeconds;
            if (expElement.ValueKind == JsonValueKind.Number)
            {
                if (!expElement.TryGetInt64(out expUnixSeconds))
                    return false;
            }
            else if (expElement.ValueKind == JsonValueKind.String &&
                     long.TryParse(expElement.GetString(), out var parsed))
            {
                expUnixSeconds = parsed;
            }
            else
            {
                return false;
            }

            if (expUnixSeconds <= 0)
                return false;

            expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(expUnixSeconds);
            return expiresAtUtc.Value <= DateTimeOffset.UtcNow.Add(skew);
        }
        catch
        {
            return false;
        }
    }
}

