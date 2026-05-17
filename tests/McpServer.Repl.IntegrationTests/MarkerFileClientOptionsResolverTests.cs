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

    private static string BuildSignaturePayload(
        string apiKey,
        string workspacePath,
        IReadOnlyDictionary<string, string> endpoints)
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("canonicalization=marker-v1");
        builder.AppendLine("port=7147");
        builder.AppendLine("baseUrl=http://localhost:7147");
        builder.AppendLine($"apiKey={apiKey}");
        builder.AppendLine("workspace=TestWorkspace");
        builder.AppendLine($"workspacePath={workspacePath}");
        builder.AppendLine("pid=1234");
        builder.AppendLine("startedAt=2026-04-06T20:29:10.3301205+00:00");
        builder.AppendLine("markerWrittenAtUtc=2026-04-06T20:29:10.3301205+00:00");
        builder.AppendLine("serverStartedAtUtc=2026-04-06T20:29:05.1397259+00:00");

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
            builder.AppendLine($"endpoints.{endpointName}={endpoints[endpointName]}");
        }

        return builder.ToString();
    }
}
