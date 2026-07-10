using System.Net;
using System.Text;
using McpServer.Client;
using McpServer.Client.Models;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// TEST-MCP-TRANSCRIPT-008: validates REPL transcript workflows can upload local files and folders.
/// </summary>
public sealed class TranscriptIngestionWorkflowTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key",
    };

    /// <summary>Local normalization uses multipart upload so the server does not need to read the local path.</summary>
    [Fact]
    public async Task NormalizeTranscriptsAsync_LocalFile_UsesMultipartUpload()
    {
        var tempRoot = CreateTempRoot();
        try
        {
            var transcriptPath = Path.Combine(tempRoot, "session.jsonl");
            await File.WriteAllTextAsync(transcriptPath, "{\"session_meta\":{\"id\":\"local-file\"}}", TestContext.Current.CancellationToken);
            var handler = new CapturingTranscriptHandler("""{"runId":"upload-local-file","totalSessions":1,"persisted":false,"degraded":false,"receipts":[]}""");
            using var http = new HttpClient(handler);
            var workflow = new TranscriptIngestionWorkflow(new SessionLogClient(http, DefaultOptions));

            var result = await workflow.NormalizeTranscriptsAsync(new TranscriptIngestPathRequest
            {
                Path = transcriptPath,
                Agent = "Codex",
                Source = TranscriptSourceKind.Codex,
                Recursive = false,
                Strict = true,
                Persist = false,
                CompatibilityProfile = TranscriptCompatibilityProfile.Grok,
                EmitNormalizedProfile = true,
            }, TestContext.Current.CancellationToken);

            Assert.Equal("upload-local-file", result.RunId);
            Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
            Assert.Equal("/mcpserver/sessionlog/ingest/upload", handler.LastRequest.RequestUri!.AbsolutePath);
            Assert.StartsWith("multipart/form-data", handler.LastRequest.Content!.Headers.ContentType!.MediaType, StringComparison.Ordinal);
            Assert.Contains("name=agent", handler.LastRequestBody, StringComparison.Ordinal);
            Assert.Contains("Codex", handler.LastRequestBody, StringComparison.Ordinal);
            Assert.Contains("name=compatibilityProfile", handler.LastRequestBody, StringComparison.Ordinal);
            Assert.Contains("Grok", handler.LastRequestBody, StringComparison.Ordinal);
            Assert.Contains("name=emitNormalizedProfile", handler.LastRequestBody, StringComparison.Ordinal);
            Assert.Contains("true", handler.LastRequestBody, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("filename=session.jsonl", handler.LastRequestBody, StringComparison.Ordinal);
            Assert.Contains("local-file", handler.LastRequestBody, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    /// <summary>Local folder ingestion uploads recursive files with stable relative names.</summary>
    [Fact]
    public async Task IngestTranscriptsAsync_LocalFolder_UploadsRecursiveFilesWithRelativeNames()
    {
        var tempRoot = CreateTempRoot();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempRoot, "root.jsonl"), "{\"session_meta\":{\"id\":\"root\"}}", TestContext.Current.CancellationToken);
            Directory.CreateDirectory(Path.Combine(tempRoot, "nested"));
            await File.WriteAllTextAsync(Path.Combine(tempRoot, "nested", "child.jsonl"), "{\"session_meta\":{\"id\":\"child\"}}", TestContext.Current.CancellationToken);
            var handler = new CapturingTranscriptHandler("""{"runId":"upload-local-folder","totalSessions":2,"persisted":false,"degraded":false,"receipts":[]}""");
            using var http = new HttpClient(handler);
            var workflow = new TranscriptIngestionWorkflow(new SessionLogClient(http, DefaultOptions));

            var result = await workflow.IngestTranscriptsAsync(new TranscriptIngestPathRequest
            {
                Path = tempRoot,
                Agent = "Codex",
                Source = TranscriptSourceKind.Codex,
                Recursive = true,
                Strict = true,
                Persist = false,
            }, TestContext.Current.CancellationToken);

            Assert.Equal("upload-local-folder", result.RunId);
            Assert.Equal("/mcpserver/sessionlog/ingest/upload", handler.LastRequest!.RequestUri!.AbsolutePath);
            Assert.Contains("filename=root.jsonl", handler.LastRequestBody, StringComparison.Ordinal);
            Assert.Contains("root", handler.LastRequestBody, StringComparison.Ordinal);
            Assert.Contains("nested", handler.LastRequestBody, StringComparison.Ordinal);
            Assert.Contains("child.jsonl", handler.LastRequestBody, StringComparison.Ordinal);
            Assert.Contains("child", handler.LastRequestBody, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static string CreateTempRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "mcp-repl-transcript", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class CapturingTranscriptHandler : HttpMessageHandler
    {
        private readonly string _responseBody;

        public CapturingTranscriptHandler(string responseBody)
        {
            _responseBody = responseBody;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        public string LastRequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json"),
            };
        }
    }
}
