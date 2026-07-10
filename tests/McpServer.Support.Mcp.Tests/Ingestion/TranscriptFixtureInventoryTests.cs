using System.Text.Json;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Ingestion;

/// <summary>Phase 0 fixture contract tests for MCP-TRANSCRIPT-001 transcript ingestion planning.</summary>
public sealed class TranscriptFixtureInventoryTests
{
    /// <summary>Verifies that sanitized fixtures exist for every source family in the approved transcript plan.</summary>
    [Fact]
    public void FixtureInventoryContainsEveryPlannedSourceFamily()
    {
        var root = ResolveFixtureRoot();
        var expectedFiles = new[]
        {
            "README.md",
            Path.Combine("claude", "basic.jsonl"),
            Path.Combine("codex", "basic.jsonl"),
            Path.Combine("grok", "basic.jsonl"),
            Path.Combine("cline", "session.json"),
            Path.Combine("cline", "messages.json"),
            Path.Combine("cline", "export.jsonl"),
            Path.Combine("copilot", "session-001", "metadata.json"),
            Path.Combine("copilot", "session-001", "events.jsonl"),
            Path.Combine("opencode", "export.jsonl"),
            Path.Combine("opencode", "store-schema.sql")
        };

        foreach (var relativePath in expectedFiles)
        {
            Assert.True(File.Exists(Path.Combine(root, relativePath)), $"Missing transcript fixture: {relativePath}");
        }
    }

    /// <summary>Verifies that JSON and JSONL transcript fixtures are syntactically valid before parser implementation starts.</summary>
    [Fact]
    public void JsonAndJsonlFixturesAreParseable()
    {
        var root = ResolveFixtureRoot();
        foreach (var jsonPath in Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(jsonPath));
            Assert.True(document.RootElement.ValueKind is JsonValueKind.Object or JsonValueKind.Array, jsonPath);
        }

        foreach (var jsonlPath in Directory.EnumerateFiles(root, "*.jsonl", SearchOption.AllDirectories))
        {
            var lines = File.ReadAllLines(jsonlPath).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
            Assert.NotEmpty(lines);
            foreach (var line in lines)
            {
                using var document = JsonDocument.Parse(line);
                Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
            }
        }
    }

    /// <summary>Verifies that the OpenCode SQLite planning fixture documents the read-only tables expected by future tests.</summary>
    [Fact]
    public void OpenCodeSqliteSchemaFixtureDocumentsReadOnlySnapshotShape()
    {
        var schema = File.ReadAllText(Path.Combine(ResolveFixtureRoot(), "opencode", "store-schema.sql"));

        Assert.Contains("CREATE TABLE session", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE TABLE message", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE TABLE tool_event", schema, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("copy the database before reading", schema, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveFixtureRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "tests", "McpServer.Support.Mcp.Tests", "Fixtures", "Transcripts");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate transcript fixture root from test output directory.");
    }
}
