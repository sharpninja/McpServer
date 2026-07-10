using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using McpServer.SessionLog.Transcripts;
using McpServer.Support.Mcp.Models;
using Xunit;

namespace McpServer.Support.Mcp.IntegrationTests.Controllers;

/// <summary>Integration tests for transcript ingestion HTTP endpoints.</summary>
[Trait("Category", "Integration")]
public sealed class SessionLogTranscriptIngestionControllerTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    /// <summary>Initializes a new transcript ingestion integration test instance.</summary>
    /// <param name="factory">Isolated integration test application factory.</param>
    public SessionLogTranscriptIngestionControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    /// <inheritdoc />
    public void Dispose() => _client.Dispose();

    /// <summary>Verifies HTTP path ingestion accepts workspace-relative paths and persists through session-log storage.</summary>
    [Fact]
    public async Task IngestPathAsync_WorkspaceRelativeCodexFixturePersistsAndDeletesFailsafe()
    {
        CopyRealFixtureToWorkspace("codex/session.jsonl", "transcripts/codex/session.jsonl");
        var request = new TranscriptIngestPathRequest
        {
            Path = "transcripts/codex/session.jsonl",
            Agent = "Codex",
            Source = TranscriptSourceKind.Codex,
            Recursive = false,
            Strict = true,
            Persist = true,
            CompatibilityProfile = TranscriptCompatibilityProfile.Codex,
            EmitNormalizedProfile = true,
        };

        var response = await _client.PostAsJsonAsync(
            new Uri("/mcpserver/sessionlog/ingest/path", UriKind.Relative),
            request,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TranscriptIngestRunResponse>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(body);
        Assert.True(body!.Persisted);
        Assert.False(body.Degraded);
        Assert.Equal(1, body.TotalSessions);
        Assert.Empty(body.ImportRecoveryPaths);
        var receipt = Assert.Single(body.Receipts);
        Assert.Equal("Codex", receipt.Source);
        Assert.Equal("codex-real-fixture-session", receipt.SessionId);
        Assert.Equal("persisted", receipt.Status);
        Assert.StartsWith("sessionLogId:", receipt.PersistenceReceipt, StringComparison.Ordinal);
        Assert.True(File.Exists(receipt.YamlArtifactPath), receipt.YamlArtifactPath);
        Assert.True(File.Exists(receipt.CompatibilityArtifactPath), receipt.CompatibilityArtifactPath);
        Assert.False(File.Exists(receipt.ImportRecoveryPath), receipt.ImportRecoveryPath);
        Assert.Contains("codex-real-fixture-session", await File.ReadAllTextAsync(receipt.YamlArtifactPath, TestContext.Current.CancellationToken).ConfigureAwait(true), StringComparison.Ordinal);
    }

    /// <summary>Verifies multipart ZIP upload ingestion persists and removes the per-run staging directory.</summary>
    [Fact]
    public async Task IngestUploadAsync_ZipClaudeFixturePersistsAndDeletesStagingRun()
    {
        var zipBytes = CreateZip(("claude/session.jsonl", File.ReadAllText(ResolveRealFixturePath("claude/session.jsonl"))));
        using var form = new MultipartFormDataContent
        {
            { new StringContent("Claude", Encoding.UTF8), "agent" },
            { new StringContent("Claude", Encoding.UTF8), "source" },
            { new StringContent("true", Encoding.UTF8), "recursive" },
            { new StringContent("true", Encoding.UTF8), "strict" },
            { new StringContent("true", Encoding.UTF8), "persist" },
        };
        using var fileContent = new ByteArrayContent(zipBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
        form.Add(fileContent, "files", "bundle.zip");

        var response = await _client.PostAsync(
            new Uri("/mcpserver/sessionlog/ingest/upload", UriKind.Relative),
            form,
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TranscriptIngestRunResponse>(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(body);
        Assert.True(body!.Persisted);
        Assert.False(body.Degraded);
        var receipt = Assert.Single(body.Receipts);
        Assert.Equal("Claude", receipt.Source);
        Assert.Equal("claude-real-fixture-session", receipt.SessionId);
        Assert.StartsWith("sessionLogId:", receipt.PersistenceReceipt, StringComparison.Ordinal);
        Assert.True(File.Exists(receipt.YamlArtifactPath), receipt.YamlArtifactPath);
        Assert.False(File.Exists(receipt.ImportRecoveryPath), receipt.ImportRecoveryPath);
        Assert.False(Directory.Exists(Path.Combine(_factory.WorkspacePath, ".mcpServer", "Claude", "transcripts", "staging", body.RunId!)));
    }

    /// <summary>Verifies ZIP uploads reject duplicate canonical paths before ingestion starts.</summary>
    [Fact]
    public async Task IngestUploadAsync_RejectsDuplicateZipPaths()
    {
        var zipBytes = CreateZip(("session.jsonl", "{}"), ("session.jsonl", "{}"));
        using var form = new MultipartFormDataContent
        {
            { new StringContent("Codex", Encoding.UTF8), "agent" },
        };
        using var fileContent = new ByteArrayContent(zipBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
        form.Add(fileContent, "files", "duplicate.zip");

        var response = await _client.PostAsync(
            new Uri("/mcpserver/sessionlog/ingest/upload", UriKind.Relative),
            form,
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private void CopyRealFixtureToWorkspace(string sourceRelativePath, string workspaceRelativePath)
    {
        var destination = Path.Combine(_factory.WorkspacePath, workspaceRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Copy(ResolveRealFixturePath(sourceRelativePath), destination, overwrite: true);
    }

    private static string ResolveRealFixturePath(string relativePath)
    {
        var path = Path.Combine(CustomWebApplicationFactory.ResolveSolutionRoot(), "tests", "McpServer.Support.Mcp.Tests", "Fixtures", "Transcripts", "real", relativePath);
        if (!File.Exists(path))
            throw new FileNotFoundException("Missing real transcript fixture.", path);
        return path;
    }

    private static byte[] CreateZip(params (string EntryName, string Content)[] entries)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in entries)
            {
                var zipEntry = archive.CreateEntry(entry.EntryName);
                using var entryStream = zipEntry.Open();
                using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                writer.Write(entry.Content);
            }
        }

        return stream.ToArray();
    }
}
