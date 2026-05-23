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
        return FindMarkerFile(startPath, out _);
    }

    /// <summary>
    /// FR-MCP-REPL-007: Walks upward from a starting path to locate the workspace
    /// marker file and reports every directory that was searched.
    /// </summary>
    /// <param name="startPath">The path to begin searching from.</param>
    /// <param name="searchedPaths">Every directory walked in search order.</param>
    /// <returns>The marker file path when found; otherwise, <see langword="null"/>.</returns>
    public static string? FindMarkerFile(string startPath, out IReadOnlyList<string> searchedPaths)
    {
        var searched = new List<string>();
        var current = new DirectoryInfo(startPath);
        while (current is not null)
        {
            searched.Add(current.FullName);
            var candidate = Path.Combine(current.FullName, MarkerFileName);
            if (File.Exists(candidate))
            {
                searchedPaths = searched;
                return candidate;
            }

            current = current.Parent;
        }

        searchedPaths = searched;
        return null;
    }

    /// <summary>
    /// FR-MCP-REPL-007: Resolves REPL client options with diagnostic surface. Honors
    /// explicit workspace-path or marker-file overrides. On failure, the
    /// <paramref name="error"/> string enumerates every directory searched and
    /// reports whether the marker file was missing or its signature failed to verify.
    /// </summary>
    /// <param name="workspacePathOverride">Optional explicit workspace path (CLI <c>--workspace-path</c>).</param>
    /// <param name="markerPathOverride">Optional explicit marker file path (CLI <c>--marker-file</c>).</param>
    /// <param name="options">The resolved options on success.</param>
    /// <param name="error">Diagnostic message on failure.</param>
    /// <returns><see langword="true"/> when resolution succeeds; otherwise <see langword="false"/>.</returns>
    public static bool TryResolveWithDiagnostics(
        string? workspacePathOverride,
        string? markerPathOverride,
        out McpServerClientOptions? options,
        out string error)
    {
        options = null;
        error = string.Empty;

        string? markerPath;
        IReadOnlyList<string> searchedPaths;

        if (!string.IsNullOrWhiteSpace(markerPathOverride))
        {
            markerPath = markerPathOverride;
            searchedPaths = new[] { markerPathOverride };
            if (!File.Exists(markerPath))
            {
                error = $"Marker file not found at explicit path '{markerPath}'. Pass --workspace-path <dir> or place AGENTS-README-FIRST.yaml at that path.";
                return false;
            }
        }
        else
        {
            var searchRoot = !string.IsNullOrWhiteSpace(workspacePathOverride)
                ? workspacePathOverride
                : Environment.GetEnvironmentVariable("MCP_WORKSPACE_PATH")
                    ?? Environment.GetEnvironmentVariable("MCP_WORKSPACE")
                    ?? Environment.CurrentDirectory;

            markerPath = FindMarkerFile(searchRoot, out searchedPaths);
            if (markerPath is null)
            {
                var pathList = string.Join("; ", searchedPaths);
                error = $"Marker file '{MarkerFileName}' not found. Searched: {pathList}. To override, pass --workspace-path <dir> or --marker-file <path>.";
                return false;
            }
        }

        var parsed = ParseMarker(File.ReadAllLines(markerPath));
        if (parsed is null)
        {
            error = $"Marker file at '{markerPath}' is malformed: missing required fields (baseUrl/apiKey/workspacePath/signature).";
            return false;
        }

        if (!VerifyMarkerSignature(parsed.Value))
        {
            error = $"Marker file at '{markerPath}' failed HMAC-SHA256 signature verification. Either the marker is stale (server restarted) or has been tampered with. Re-read the file or restart the server.";
            return false;
        }

        options = new McpServerClientOptions
        {
            BaseUrl = new Uri(parsed.Value.BaseUrl),
            ApiKey = parsed.Value.ApiKey,
            WorkspacePath = parsed.Value.WorkspacePath,
        };
        return true;
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
        // FR-MCP-REPL-007 fix: the server's MarkerFileService.AppendPayloadLine
        // always terminates with a literal LF ('\n'). StringBuilder.AppendLine
        // honours Environment.NewLine which is CRLF on Windows, so we cannot use
        // it here - the HMAC payload must be byte-identical to what the server
        // hashed regardless of which OS the REPL is running on.
        var builder = new StringBuilder();
        AppendLfLine(builder, "canonicalization", marker.SignatureCanonicalization);
        AppendLfLine(builder, "port", marker.Port);
        AppendLfLine(builder, "baseUrl", marker.BaseUrl);
        AppendLfLine(builder, "apiKey", marker.ApiKey);
        AppendLfLine(builder, "workspace", marker.Workspace);
        AppendLfLine(builder, "workspacePath", marker.WorkspacePath);
        AppendLfLine(builder, "pid", marker.Pid);
        AppendLfLine(builder, "startedAt", marker.StartedAt);
        AppendLfLine(builder, "markerWrittenAtUtc", marker.MarkerWrittenAtUtc);
        AppendLfLine(builder, "serverStartedAtUtc", marker.ServerStartedAtUtc);

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
            AppendLfLine(builder, $"endpoints.{endpointName}", endpointValue ?? string.Empty);
        }

        marker.AgentPlugins.TryGetValue("policy", out var policy);
        marker.AgentPlugins.TryGetValue("contract_digest", out var contractDigest);
        if (policy is not null || contractDigest is not null)
        {
            AppendLfLine(builder, "agentPlugins.policy", policy ?? string.Empty);
            AppendLfLine(builder, "agentPlugins.contractDigest", contractDigest ?? string.Empty);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Mirrors the server's <c>MarkerFileService.AppendPayloadLine</c>: writes
    /// <c>{key}={value}\n</c> with a literal LF, normalising any embedded CRLF
    /// in <paramref name="value"/> to LF so the HMAC payload matches across
    /// Linux / macOS / Windows.
    /// </summary>
    private static void AppendLfLine(StringBuilder builder, string key, object? value)
    {
        builder.Append(key)
            .Append('=')
            .Append((value?.ToString() ?? string.Empty).ReplaceLineEndings("\n"))
            .Append('\n');
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
