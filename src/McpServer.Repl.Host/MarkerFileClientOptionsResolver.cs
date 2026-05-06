using System.Security.Cryptography;
using System.Text;
using McpServer.Client;

namespace McpServer.Repl.Host;

/// <summary>
/// Resolves REPL client options from environment variables and the workspace marker file.
/// </summary>
public static class MarkerFileClientOptionsResolver
{
    private const string MarkerFileName = "AGENTS-README-FIRST.yaml";

    /// <summary>
    /// Builds REPL client options, preferring trusted marker-file settings when available.
    /// </summary>
    /// <returns>The resolved client options.</returns>
    public static McpServerClientOptions Resolve()
    {
        var configuredServerUrl = Environment.GetEnvironmentVariable("MCP_SERVER_URL");
        var workspacePath = ResolveWorkspacePath();
        if (TryLoadTrustedMarker(workspacePath, out var marker))
        {
            return new McpServerClientOptions
            {
                BaseUrl = new Uri(marker.BaseUrl),
                ApiKey = marker.ApiKey,
                WorkspacePath = marker.WorkspacePath,
            };
        }

        return new McpServerClientOptions
        {
            BaseUrl = new Uri(configuredServerUrl ?? "http://localhost:7147"),
            WorkspacePath = workspacePath,
        };
    }

    /// <summary>
    /// Resolves the workspace root used for marker-file discovery.
    /// </summary>
    /// <returns>The resolved workspace path.</returns>
    public static string ResolveWorkspacePath()
    {
        var explicitWorkspacePath = Environment.GetEnvironmentVariable("MCP_WORKSPACE_PATH");
        if (!string.IsNullOrWhiteSpace(explicitWorkspacePath))
        {
            return explicitWorkspacePath;
        }

        explicitWorkspacePath = Environment.GetEnvironmentVariable("MCP_WORKSPACE");
        if (!string.IsNullOrWhiteSpace(explicitWorkspacePath))
        {
            return explicitWorkspacePath;
        }

        var markerPath = FindMarkerFile(Environment.CurrentDirectory);
        if (!string.IsNullOrWhiteSpace(markerPath))
        {
            return Path.GetDirectoryName(markerPath) ?? Environment.CurrentDirectory;
        }

        return Environment.CurrentDirectory;
    }

    /// <summary>
    /// Attempts to load and verify the trusted marker file for a workspace.
    /// </summary>
    /// <param name="workspacePath">The workspace path used as the marker discovery root.</param>
    /// <param name="marker">The verified marker settings when discovery succeeds.</param>
    /// <returns><see langword="true"/> when a valid trusted marker file is found; otherwise, <see langword="false"/>.</returns>
    public static bool TryLoadTrustedMarker(string workspacePath, out MarkerSettings marker)
    {
        marker = default;
        var markerPath = FindMarkerFile(workspacePath);
        if (string.IsNullOrWhiteSpace(markerPath) || !File.Exists(markerPath))
        {
            return false;
        }

        var parsed = ParseMarker(File.ReadAllLines(markerPath));
        if (parsed is null)
        {
            return false;
        }

        if (!VerifyMarkerSignature(parsed.Value))
        {
            return false;
        }

        marker = parsed.Value;
        return true;
    }

    /// <summary>
    /// Walks upward from a starting path to locate the workspace marker file.
    /// </summary>
    /// <param name="startPath">The path to begin searching from.</param>
    /// <returns>The marker file path when found; otherwise, <see langword="null"/>.</returns>
    public static string? FindMarkerFile(string startPath)
    {
        var current = new DirectoryInfo(startPath);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, MarkerFileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }

    internal static MarkerSettings? ParseMarker(IEnumerable<string> lines)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var endpoints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var signature = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var agentPlugins = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? currentSection = null;

        foreach (var rawLine in lines)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            var line = rawLine.TrimEnd();
            if (!char.IsWhiteSpace(rawLine[0]))
            {
                currentSection = null;
            }

            if (rawLine.StartsWith("endpoints:", StringComparison.Ordinal))
            {
                currentSection = "endpoints";
                continue;
            }

            if (rawLine.StartsWith("signature:", StringComparison.Ordinal))
            {
                currentSection = "signature";
                continue;
            }

            if (rawLine.StartsWith("agent_plugins:", StringComparison.Ordinal))
            {
                currentSection = "agent_plugins";
                continue;
            }

            if (currentSection is not null && rawLine.StartsWith("  ", StringComparison.Ordinal))
            {
                var sectionParts = line.Trim().Split(':', 2);
                if (sectionParts.Length != 2)
                {
                    continue;
                }

                var key = sectionParts[0].Trim();
                var value = sectionParts[1].Trim().Trim('"');
                if (currentSection == "endpoints")
                {
                    endpoints[key] = value;
                }
                else if (currentSection == "signature")
                {
                    signature[key] = value;
                }
                else if (currentSection == "agent_plugins"
                         && (string.Equals(key, "policy", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(key, "contract_digest", StringComparison.OrdinalIgnoreCase)))
                {
                    agentPlugins[key] = value;
                }

                continue;
            }

            var parts = line.Split(':', 2);
            if (parts.Length != 2)
            {
                continue;
            }

            values[parts[0].Trim()] = parts[1].Trim().Trim('"');
        }

        if (!values.TryGetValue("baseUrl", out var baseUrl)
            || !values.TryGetValue("apiKey", out var apiKey)
            || !values.TryGetValue("workspacePath", out var workspacePath)
            || !values.TryGetValue("port", out var port)
            || !values.TryGetValue("workspace", out var workspace)
            || !values.TryGetValue("pid", out var pid)
            || !values.TryGetValue("startedAt", out var startedAt)
            || !values.TryGetValue("markerWrittenAtUtc", out var markerWrittenAtUtc)
            || !values.TryGetValue("serverStartedAtUtc", out var serverStartedAtUtc)
            || !signature.TryGetValue("canonicalization", out var canonicalization)
            || !signature.TryGetValue("value", out var signatureValue))
        {
            return null;
        }

        return new MarkerSettings(
            Port: port,
            BaseUrl: baseUrl,
            ApiKey: apiKey,
            Workspace: workspace,
            WorkspacePath: workspacePath,
            Pid: pid,
            StartedAt: startedAt,
            MarkerWrittenAtUtc: markerWrittenAtUtc,
            ServerStartedAtUtc: serverStartedAtUtc,
            SignatureCanonicalization: canonicalization,
            SignatureValue: signatureValue,
            Endpoints: endpoints,
            AgentPlugins: agentPlugins);
    }

    internal static bool VerifyMarkerSignature(MarkerSettings marker)
    {
        if (!string.Equals(marker.SignatureCanonicalization, "marker-v1", StringComparison.Ordinal))
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(marker.ApiKey));
        var payload = BuildSignaturePayload(marker);
        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        return string.Equals(hash, marker.SignatureValue, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildSignaturePayload(MarkerSettings marker)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"canonicalization={marker.SignatureCanonicalization}");
        builder.AppendLine($"port={marker.Port}");
        builder.AppendLine($"baseUrl={marker.BaseUrl}");
        builder.AppendLine($"apiKey={marker.ApiKey}");
        builder.AppendLine($"workspace={marker.Workspace}");
        builder.AppendLine($"workspacePath={marker.WorkspacePath}");
        builder.AppendLine($"pid={marker.Pid}");
        builder.AppendLine($"startedAt={marker.StartedAt}");
        builder.AppendLine($"markerWrittenAtUtc={marker.MarkerWrittenAtUtc}");
        builder.AppendLine($"serverStartedAtUtc={marker.ServerStartedAtUtc}");

        foreach (var endpointName in new[]
        {
            "health",
            "swagger",
            "swaggerUi",
            "mcpTransport",
            "sessionLog",
            "sessionLogDialog",
            "contextSearch",
            "contextPack",
            "contextSources",
            "todo",
            "repo",
            "desktop",
            "gitHub",
            "tools",
            "workspace",
            "serverStartupUtc",
            "markerFileTimestamp",
        })
        {
            marker.Endpoints.TryGetValue(endpointName, out var endpointValue);
            builder.AppendLine($"endpoints.{endpointName}={endpointValue ?? string.Empty}");
        }

        marker.AgentPlugins.TryGetValue("policy", out var policy);
        marker.AgentPlugins.TryGetValue("contract_digest", out var contractDigest);
        if (policy is not null || contractDigest is not null)
        {
            builder.AppendLine($"agentPlugins.policy={policy ?? string.Empty}");
            builder.AppendLine($"agentPlugins.contractDigest={contractDigest ?? string.Empty}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Represents the trusted marker values needed to bootstrap a REPL client.
    /// </summary>
    /// <param name="Port">The server port recorded in the marker file.</param>
    /// <param name="BaseUrl">The base server URL recorded in the marker file.</param>
    /// <param name="ApiKey">The workspace API key recorded in the marker file.</param>
    /// <param name="Workspace">The workspace name recorded in the marker file.</param>
    /// <param name="WorkspacePath">The workspace path recorded in the marker file.</param>
    /// <param name="Pid">The server process identifier recorded in the marker file.</param>
    /// <param name="StartedAt">The server start timestamp recorded in the marker file.</param>
    /// <param name="MarkerWrittenAtUtc">The marker write timestamp recorded in the marker file.</param>
    /// <param name="ServerStartedAtUtc">The server start timestamp in UTC recorded in the marker file.</param>
    /// <param name="SignatureCanonicalization">The signature canonicalization format.</param>
    /// <param name="SignatureValue">The marker signature value.</param>
    /// <param name="Endpoints">The endpoint map recorded in the marker file.</param>
    /// <param name="AgentPlugins">The signed agent plugin policy/digest values recorded in the marker file.</param>
    public readonly record struct MarkerSettings(
        string Port,
        string BaseUrl,
        string ApiKey,
        string Workspace,
        string WorkspacePath,
        string Pid,
        string StartedAt,
        string MarkerWrittenAtUtc,
        string ServerStartedAtUtc,
        string SignatureCanonicalization,
        string SignatureValue,
        IReadOnlyDictionary<string, string> Endpoints,
        IReadOnlyDictionary<string, string> AgentPlugins);
}
