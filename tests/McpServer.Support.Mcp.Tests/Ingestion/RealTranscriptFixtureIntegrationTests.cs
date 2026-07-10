using System.Text.Json;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Ingestion;

/// <summary>Integration fixture tests for sanitized real transcript samples used by MCP-TRANSCRIPT-001.</summary>
public sealed class RealTranscriptFixtureIntegrationTests
{
    /// <summary>Verifies that the real-derived fixture manifest covers every approved source agent.</summary>
    [Fact]
    public void RealTranscriptManifestCoversEveryAgent()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(ResolveRealFixtureRoot(), "manifest.json")));
        var fixtures = manifest.RootElement.GetProperty("fixtures").EnumerateArray().ToArray();
        var sourceKinds = fixtures.Select(item => item.GetProperty("sourceKind").GetString()).ToArray();

        Assert.Contains("Claude", sourceKinds);
        Assert.Contains("Codex", sourceKinds);
        Assert.Contains("Grok", sourceKinds);
        Assert.Contains("Cline", sourceKinds);
        Assert.Contains("Copilot", sourceKinds);
        Assert.Contains("OpenCode", sourceKinds);

        foreach (var fixture in fixtures)
        {
            var relativePath = fixture.GetProperty("path").GetString();
            Assert.False(string.IsNullOrWhiteSpace(relativePath));
            Assert.True(File.Exists(Path.Combine(ResolveRealFixtureRoot(), relativePath)), $"Missing real transcript fixture: {relativePath}");
        }
    }

    /// <summary>Verifies that JSONL real-derived fixtures retain native provider event names.</summary>
    [Theory]
    [InlineData("codex/session.jsonl", "session_meta", "response_item")]
    [InlineData("claude/session.jsonl", "last-prompt", "assistant")]
    [InlineData("grok/chat_history.jsonl", "system", "assistant")]
    [InlineData("grok/events.jsonl", "mcp_config_resolved", "mcp_server_starting")]
    [InlineData("copilot/events.jsonl", "session.mcp_servers_loaded", "assistant.message")]
    [InlineData("opencode/events.jsonl", "step_start", "text")]
    public void RealJsonlFixturesContainProviderSpecificEvents(string relativePath, string firstExpectedType, string secondExpectedType)
    {
        var records = ReadJsonl(Path.Combine(ResolveRealFixtureRoot(), relativePath));
        var types = records.Select(GetRequiredType).ToArray();

        Assert.Contains(firstExpectedType, types);
        Assert.Contains(secondExpectedType, types);
    }

    /// <summary>Verifies that paired and exported JSON real-derived fixtures retain native provider shapes.</summary>
    [Fact]
    public void RealJsonFixturesContainProviderSpecificNativeShapes()
    {
        var root = ResolveRealFixtureRoot();

        using var clineSession = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "cline", "session.json")));
        Assert.Equal("cli", clineSession.RootElement.GetProperty("source").GetString());
        Assert.Equal("cline", clineSession.RootElement.GetProperty("provider").GetString());
        Assert.Equal("error", clineSession.RootElement.GetProperty("status").GetString());
        Assert.Equal("fresh-smoke-session-provider-rejected", clineSession.RootElement.GetProperty("metadata").GetProperty("capture").GetString());

        using var clineMessages = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "cline", "messages.json")));
        Assert.Equal("cline", clineMessages.RootElement.GetProperty("agent").GetString());
        Assert.NotEmpty(clineMessages.RootElement.GetProperty("messages").EnumerateArray());

        using var openCodeExport = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "opencode", "export.json")));
        Assert.Equal("opencode", openCodeExport.RootElement.GetProperty("info").GetProperty("model").GetProperty("providerID").GetString());
        Assert.Equal("assistant", openCodeExport.RootElement.GetProperty("messages")[1].GetProperty("info").GetProperty("role").GetString());
        Assert.Equal("step-finish", openCodeExport.RootElement.GetProperty("messages")[1].GetProperty("parts")[2].GetProperty("type").GetString());
    }

    /// <summary>Verifies that real-derived fixtures do not contain known secret or user-home markers.</summary>
    [Fact]
    public void RealFixturesAreSanitizedBeforeCommit()
    {
        var root = ResolveRealFixtureRoot();
        var forbiddenMarkers = new[]
        {
            "accessToken",
            "refreshToken",
            "workos:",
            "eyJhbGci",
            "gho_",
            "github_pat_",
            "ninja@",
            "C:/Users/kingd",
            "C:\\Users\\kingd"
        };

        foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(path);
            foreach (var marker in forbiddenMarkers)
            {
                Assert.DoesNotContain(marker, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private static string ResolveRealFixtureRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "tests", "McpServer.Support.Mcp.Tests", "Fixtures", "Transcripts", "real");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate real transcript fixture root from test output directory.");
    }

    private static JsonElement[] ReadJsonl(string path)
    {
        var records = new List<JsonElement>();
        foreach (var line in File.ReadAllLines(path).Where(line => !string.IsNullOrWhiteSpace(line)))
        {
            using var document = JsonDocument.Parse(line);
            records.Add(document.RootElement.Clone());
        }

        Assert.NotEmpty(records);
        return records.ToArray();
    }

    private static string GetRequiredType(JsonElement record)
    {
        Assert.True(record.TryGetProperty("type", out var type), "JSONL record is missing required type property.");
        return type.GetString() ?? string.Empty;
    }
}