using McpServer.Repl.Host;

namespace McpServer.Repl.IntegrationTests;

/// <summary>
/// Verifies that the REPL host can bootstrap trusted marker-file client options without cached bearer state.
/// </summary>
public sealed class MarkerFileClientOptionsResolverTests
{
    [Fact]
    public void ResolveWorkspacePath_FindsMarkerByWalkingParents()
    {
        var root = CreateTemporaryWorkspace();
        try
        {
            var nested = Path.Combine(root, "src", "nested");
            Directory.CreateDirectory(nested);
            File.WriteAllText(Path.Combine(root, "AGENTS-README-FIRST.yaml"), BuildMarker(root));

            var markerPath = MarkerFileClientOptionsResolver.FindMarkerFile(nested);

            Assert.Equal(Path.Combine(root, "AGENTS-README-FIRST.yaml"), markerPath);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void TryLoadTrustedMarker_ReturnsMarkerSettingsWhenSignatureMatches()
    {
        var root = CreateTemporaryWorkspace();
        try
        {
            File.WriteAllText(Path.Combine(root, "AGENTS-README-FIRST.yaml"), BuildMarker(root));

            var result = MarkerFileClientOptionsResolver.TryLoadTrustedMarker(root, out var marker);

            Assert.True(result);
            Assert.Equal("http://localhost:7147", marker.BaseUrl);
            Assert.Equal("test-api-key", marker.ApiKey);
            Assert.Equal(root, marker.WorkspacePath);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// FR-MCP-REPL-007: When the marker file is missing, the diagnostic surface must
    /// list every directory that was searched so the user can correct the workspace
    /// path or marker location.
    /// </summary>
    [Fact]
    public void TryResolveWithDiagnostics_WhenMarkerMissing_EnumeratesSearchedPaths()
    {
        var root = CreateTemporaryWorkspace();
        try
        {
            var nested = Path.Combine(root, "src", "nested");
            Directory.CreateDirectory(nested);

            var ok = MarkerFileClientOptionsResolver.TryResolveWithDiagnostics(
                workspacePathOverride: nested,
                markerPathOverride: null,
                out _,
                out var error);

            Assert.False(ok);
            Assert.False(string.IsNullOrWhiteSpace(error));
            Assert.Contains(nested, error, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(root, error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// FR-MCP-REPL-007: An explicit <c>--workspace-path</c> override is honored
    /// instead of walking from <see cref="Environment.CurrentDirectory"/>.
    /// </summary>
    [Fact]
    public void TryResolveWithDiagnostics_AcceptsExplicitWorkspaceArgument()
    {
        var root = CreateTemporaryWorkspace();
        try
        {
            File.WriteAllText(Path.Combine(root, "AGENTS-README-FIRST.yaml"), BuildMarker(root));

            var ok = MarkerFileClientOptionsResolver.TryResolveWithDiagnostics(
                workspacePathOverride: root,
                markerPathOverride: null,
                out var options,
                out _);

            Assert.True(ok);
            Assert.NotNull(options);
            Assert.Equal("test-api-key", options!.ApiKey);
            Assert.Equal(root, options.WorkspacePath);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// FR-MCP-REPL-007 / TR-MCP-REPL-008 regression: the production MCP server
    /// signs marker files with raw LF (<c>\n</c>) line separators via
    /// <c>MarkerFileService.AppendPayloadLine</c>. The resolver must hash the
    /// payload with the same line ending or signatures generated on Linux/macOS
    /// (and on Windows by the .NET server which uses literal <c>'\n'</c>) will
    /// fail to verify when the REPL runs on Windows. This test asserts the
    /// resolver successfully verifies a marker whose signature was computed over
    /// an LF-only payload, independent of the running platform's
    /// <see cref="Environment.NewLine"/>.
    /// </summary>
    [Fact]
    public void TryResolveWithDiagnostics_VerifiesSignatureBuiltWithLfLineEndings()
    {
        var root = CreateTemporaryWorkspace();
        try
        {
            File.WriteAllText(
                Path.Combine(root, "AGENTS-README-FIRST.yaml"),
                BuildServerCompatibleMarker(root));

            var ok = MarkerFileClientOptionsResolver.TryResolveWithDiagnostics(
                workspacePathOverride: root,
                markerPathOverride: null,
                out var options,
                out var error);

            Assert.True(ok, $"Resolver rejected an LF-signed marker: {error}");
            Assert.NotNull(options);
            Assert.Equal("test-api-key", options!.ApiKey);
            Assert.Equal(root, options.WorkspacePath);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// FR-MCP-REPL-007: When a marker exists but signature verification fails, the
    /// diagnostic reports both the marker path AND identifies signature mismatch
    /// (rather than the generic "not found" message).
    /// </summary>
    [Fact]
    public void TryResolveWithDiagnostics_WhenSignatureFails_ReportsReason()
    {
        var root = CreateTemporaryWorkspace();
        try
        {
            var markerPath = Path.Combine(root, "AGENTS-README-FIRST.yaml");
            File.WriteAllText(markerPath, BuildMarker(root).Replace(
                "canonicalization: marker-v1",
                "canonicalization: marker-v0",
                StringComparison.Ordinal));

            var ok = MarkerFileClientOptionsResolver.TryResolveWithDiagnostics(
                workspacePathOverride: root,
                markerPathOverride: null,
                out _,
                out var error);

            Assert.False(ok);
            Assert.Contains(markerPath, error, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("signature", error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTemporaryWorkspace()
    {
        var path = Path.Combine(Path.GetTempPath(), $"repl-marker-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string BuildMarker(string workspacePath)
    {
        const string apiKey = "test-api-key";
        var endpoints = new Dictionary<string, string>
        {
            ["health"] = "/health",
            ["swagger"] = "/swagger/v1/swagger.json",
            ["swaggerUi"] = "/swagger",
            ["mcpTransport"] = "/mcp-transport",
            ["sessionLog"] = "/mcpserver/sessionlog",
            ["sessionLogDialog"] = "/mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/dialog",
            ["contextSearch"] = "/mcpserver/context/search",
            ["contextPack"] = "/mcpserver/context/pack",
            ["contextSources"] = "/mcpserver/context/sources",
            ["todo"] = "/mcpserver/todo",
            ["repo"] = "/mcpserver/repo",
            ["desktop"] = "/mcpserver/desktop",
            ["gitHub"] = "/mcpserver/gh",
            ["tools"] = "/mcpserver/tools",
            ["workspace"] = "/mcpserver/workspace",
            ["serverStartupUtc"] = "/server-startup-utc",
            ["markerFileTimestamp"] = "/marker-file-timestamp?repoPath={workspacePath}",
        };

        var payload = BuildSignaturePayload(apiKey, workspacePath, endpoints);
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(apiKey));
        var signature = Convert.ToHexString(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload)));

        return $$"""
port: 7147
baseUrl: http://localhost:7147
apiKey: {{apiKey}}
endpoints:
  health: /health
  swagger: /swagger/v1/swagger.json
  swaggerUi: /swagger
  mcpTransport: /mcp-transport
  sessionLog: /mcpserver/sessionlog
  sessionLogDialog: /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/dialog
  contextSearch: /mcpserver/context/search
  contextPack: /mcpserver/context/pack
  contextSources: /mcpserver/context/sources
  todo: /mcpserver/todo
  repo: /mcpserver/repo
  desktop: /mcpserver/desktop
  gitHub: /mcpserver/gh
  tools: /mcpserver/tools
  workspace: /mcpserver/workspace
  serverStartupUtc: /server-startup-utc
  markerFileTimestamp: /marker-file-timestamp?repoPath={workspacePath}
workspace: TestWorkspace
workspacePath: {{workspacePath}}
pid: 1234
startedAt: 2026-04-06T20:29:10.3301205+00:00
markerWrittenAtUtc: 2026-04-06T20:29:10.3301205+00:00
serverStartedAtUtc: 2026-04-06T20:29:05.1397259+00:00
signature:
  algorithm: HMAC-SHA256
  canonicalization: marker-v1
  verifier: workspace_api_key
  value: {{signature}}
""";
    }

    /// <summary>
    /// Builds a marker file whose HMAC payload is rendered with literal LF
    /// (<c>\n</c>) separators, matching the production server's
    /// <c>MarkerFileService.AppendPayloadLine</c>. Used by the LF regression
    /// test above so a failing resolver on Windows is easy to spot independent
    /// of how the other helper builds its own marker.
    /// </summary>
    private static string BuildServerCompatibleMarker(string workspacePath)
    {
        const string apiKey = "test-api-key";
        var endpoints = new Dictionary<string, string>
        {
            ["health"] = "/health",
            ["swagger"] = "/swagger/v1/swagger.json",
            ["swaggerUi"] = "/swagger",
            ["mcpTransport"] = "/mcp-transport",
            ["sessionLog"] = "/mcpserver/sessionlog",
            ["sessionLogDialog"] = "/mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/dialog",
            ["contextSearch"] = "/mcpserver/context/search",
            ["contextPack"] = "/mcpserver/context/pack",
            ["contextSources"] = "/mcpserver/context/sources",
            ["todo"] = "/mcpserver/todo",
            ["repo"] = "/mcpserver/repo",
            ["desktop"] = "/mcpserver/desktop",
            ["gitHub"] = "/mcpserver/gh",
            ["tools"] = "/mcpserver/tools",
            ["workspace"] = "/mcpserver/workspace",
            ["serverStartupUtc"] = "/server-startup-utc",
            ["markerFileTimestamp"] = "/marker-file-timestamp?repoPath={workspacePath}",
        };

        var payload = BuildSignaturePayloadLf(apiKey, workspacePath, endpoints);
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(apiKey));
        var signature = Convert.ToHexString(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload)));

        return $$"""
port: 7147
baseUrl: http://localhost:7147
apiKey: {{apiKey}}
endpoints:
  health: /health
  swagger: /swagger/v1/swagger.json
  swaggerUi: /swagger
  mcpTransport: /mcp-transport
  sessionLog: /mcpserver/sessionlog
  sessionLogDialog: /mcpserver/sessionlog/{agent}/{sessionId}/{requestId}/dialog
  contextSearch: /mcpserver/context/search
  contextPack: /mcpserver/context/pack
  contextSources: /mcpserver/context/sources
  todo: /mcpserver/todo
  repo: /mcpserver/repo
  desktop: /mcpserver/desktop
  gitHub: /mcpserver/gh
  tools: /mcpserver/tools
  workspace: /mcpserver/workspace
  serverStartupUtc: /server-startup-utc
  markerFileTimestamp: /marker-file-timestamp?repoPath={workspacePath}
workspace: TestWorkspace
workspacePath: {{workspacePath}}
pid: 1234
startedAt: 2026-04-06T20:29:10.3301205+00:00
markerWrittenAtUtc: 2026-04-06T20:29:10.3301205+00:00
serverStartedAtUtc: 2026-04-06T20:29:05.1397259+00:00
signature:
  algorithm: HMAC-SHA256
  canonicalization: marker-v1
  verifier: workspace_api_key
  value: {{signature}}
""";
    }

    private static string BuildSignaturePayloadLf(
        string apiKey,
        string workspacePath,
        IReadOnlyDictionary<string, string> endpoints)
    {
        var builder = new System.Text.StringBuilder();
        void Line(string key, string value) => builder.Append(key).Append('=').Append(value).Append('\n');

        Line("canonicalization", "marker-v1");
        Line("port", "7147");
        Line("baseUrl", "http://localhost:7147");
        Line("apiKey", apiKey);
        Line("workspace", "TestWorkspace");
        Line("workspacePath", workspacePath);
        Line("pid", "1234");
        Line("startedAt", "2026-04-06T20:29:10.3301205+00:00");
        Line("markerWrittenAtUtc", "2026-04-06T20:29:10.3301205+00:00");
        Line("serverStartedAtUtc", "2026-04-06T20:29:05.1397259+00:00");

        foreach (var endpointName in new[]
        {
            "health", "swagger", "swaggerUi", "mcpTransport", "sessionLog", "sessionLogDialog",
            "contextSearch", "contextPack", "contextSources", "todo", "repo", "desktop",
            "gitHub", "tools", "workspace", "serverStartupUtc", "markerFileTimestamp",
        })
        {
            Line($"endpoints.{endpointName}", endpoints[endpointName]);
        }

        return builder.ToString();
    }

    private static string BuildSignaturePayload(
        string apiKey,
        string workspacePath,
        IReadOnlyDictionary<string, string> endpoints)
    {
        // FR-MCP-REPL-007 fix: match the server's LF-only payload format so
        // unit-test markers and real server markers verify identically.
        var builder = new System.Text.StringBuilder();
        builder.Append("canonicalization=marker-v1\n");
        builder.Append("port=7147\n");
        builder.Append("baseUrl=http://localhost:7147\n");
        builder.Append($"apiKey={apiKey}\n");
        builder.Append("workspace=TestWorkspace\n");
        builder.Append($"workspacePath={workspacePath}\n");
        builder.Append("pid=1234\n");
        builder.Append("startedAt=2026-04-06T20:29:10.3301205+00:00\n");
        builder.Append("markerWrittenAtUtc=2026-04-06T20:29:10.3301205+00:00\n");
        builder.Append("serverStartedAtUtc=2026-04-06T20:29:05.1397259+00:00\n");

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
            builder.Append($"endpoints.{endpointName}={endpoints[endpointName]}\n");
        }

        return builder.ToString();
    }
}
