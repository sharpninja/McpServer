using McpServer.SessionLog.Transcripts;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>Contract tests for transcript ingestion HTTP endpoints.</summary>
public sealed class TranscriptIngestionControllerTests
{
    /// <summary>Verifies path ingestion delegates to the shared transcript service with the resolved workspace.</summary>
    [Fact]
    public async Task IngestPathAsync_DelegatesWorkspaceBoundRequestAndReturnsRunReceipt()
    {
        var service = Substitute.For<ITranscriptIngestionService>();
        var workspacePath = Path.Combine(Path.GetTempPath(), "mcp-transcript-controller", Guid.NewGuid().ToString("N"));
        var receipt = new TranscriptSessionReceipt(
            TranscriptSourceKind.Codex,
            "root",
            "session-1",
            "hash",
            "pending",
            Path.Combine(workspacePath, ".mcpServer", "Codex", "transcripts", "runs", "run-1", "session-1.hash.sessionlog.yaml"),
            Path.Combine(workspacePath, ".mcpServer", "Codex", "failsafe", "pending", "root.hash.importRecovery.yaml"));
        var ingestionResult = new TranscriptIngestionResult(
            sessions: [],
            diagnostics: [],
            runId: "run-1",
            artifactRootPath: Path.Combine(workspacePath, ".mcpServer", "Codex", "transcripts", "runs", "run-1"),
            importRecoveryPaths: [receipt.ImportRecoveryPath],
            persisted: false,
            degraded: true,
            receipts: [receipt]);
        TranscriptIngestionRequest? captured = null;
        service.IngestPathAsync(Arg.Do<TranscriptIngestionRequest>(request => captured = request), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(ingestionResult));
        var controller = new SessionLogTranscriptIngestionController(
            service,
            new WorkspaceContext { WorkspacePath = workspacePath },
            NullLogger<SessionLogTranscriptIngestionController>.Instance);

        var response = await controller.IngestPathAsync(new TranscriptIngestPathRequest
        {
            Path = "transcripts/session.jsonl",
            Agent = "Codex",
            Source = TranscriptSourceKind.Codex,
            Recursive = false,
            Strict = true,
            Persist = true,
            CompatibilityProfile = TranscriptCompatibilityProfile.Codex,
            EmitNormalizedProfile = true,
        }, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var body = Assert.IsType<TranscriptIngestRunResponse>(ok.Value);
        Assert.NotNull(captured);
        Assert.Equal("transcripts/session.jsonl", captured.Path);
        Assert.Equal("Codex", captured.Agent);
        Assert.Equal(workspacePath, captured.WorkspacePath);
        Assert.Equal(TranscriptSourceKind.Codex, captured.SourceKind);
        Assert.Equal(TranscriptCompatibilityProfile.Codex, captured.CompatibilityProfile);
        Assert.False(captured.Recursive);
        Assert.True(captured.Strict);
        Assert.True(captured.Persist);
        Assert.Equal("run-1", body.RunId);
        Assert.Equal(1, body.TotalSessions);
        Assert.False(body.Persisted);
        Assert.True(body.Degraded);
        Assert.Single(body.Receipts);
        Assert.EndsWith("root.hash.importRecovery.yaml", body.Receipts[0].ImportRecoveryPath, StringComparison.Ordinal);
    }

    /// <summary>Path ingestion returns 207 Multi-Status when a folder run continues after one bundle fails.</summary>
    [Fact]
    public async Task IngestPathAsync_ReturnsMultiStatusForPartialBundleFailure()
    {
        var service = Substitute.For<ITranscriptIngestionService>();
        var workspacePath = Path.Combine(Path.GetTempPath(), "mcp-transcript-controller", Guid.NewGuid().ToString("N"));
        var receipt = new TranscriptSessionReceipt(
            TranscriptSourceKind.Codex,
            "root",
            "session-1",
            "hash",
            "pending",
            Path.Combine(workspacePath, ".mcpServer", "Codex", "transcripts", "runs", "run-partial", "session-1.hash.sessionlog.yaml"),
            Path.Combine(workspacePath, ".mcpServer", "Codex", "failsafe", "pending", "root.hash.importRecovery.yaml"));
        var diagnostic = new TranscriptDiagnostic("normalize_failed", "Malformed transcript bundle.", "warning", "bad/session.jsonl");
        var ingestionResult = new TranscriptIngestionResult(
            sessions: [],
            diagnostics: [diagnostic],
            runId: "run-partial",
            artifactRootPath: Path.Combine(workspacePath, ".mcpServer", "Codex", "transcripts", "runs", "run-partial"),
            importRecoveryPaths: [receipt.ImportRecoveryPath],
            persisted: false,
            degraded: true,
            receipts: [receipt]);
        service.IngestPathAsync(Arg.Any<TranscriptIngestionRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(ingestionResult));
        var controller = new SessionLogTranscriptIngestionController(
            service,
            new WorkspaceContext { WorkspacePath = workspacePath },
            NullLogger<SessionLogTranscriptIngestionController>.Instance);

        var response = await controller.IngestPathAsync(new TranscriptIngestPathRequest
        {
            Path = "transcripts",
            Agent = "Codex",
            Source = TranscriptSourceKind.Auto,
            Recursive = true,
            Strict = false,
            Persist = true,
        }, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var multiStatus = Assert.IsType<ObjectResult>(response.Result);
        Assert.Equal(StatusCodes.Status207MultiStatus, multiStatus.StatusCode);
        var body = Assert.IsType<TranscriptIngestRunResponse>(multiStatus.Value);
        Assert.Equal("run-partial", body.RunId);
        Assert.Single(body.Receipts);
        Assert.Single(body.Diagnostics);
        Assert.Equal("normalize_failed", body.Diagnostics[0].Code);
    }

    /// <summary>Verifies the endpoint requires a resolved workspace context.</summary>
    [Fact]
    public async Task IngestPathAsync_ReturnsNotFoundWhenWorkspaceIsMissing()
    {
        var service = Substitute.For<ITranscriptIngestionService>();
        var controller = new SessionLogTranscriptIngestionController(
            service,
            new WorkspaceContext(),
            NullLogger<SessionLogTranscriptIngestionController>.Instance);

        var response = await controller.IngestPathAsync(new TranscriptIngestPathRequest
        {
            Path = "session.jsonl",
            Agent = "Codex",
        }, TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.IsType<NotFoundObjectResult>(response.Result);
        await service.DidNotReceiveWithAnyArgs().IngestPathAsync(default!, TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    /// <summary>Verifies path authorization failures are mapped to HTTP 403.</summary>
    [Fact]
    public async Task IngestPathAsync_MapsUnauthorizedPathToForbidden()
    {
        var service = Substitute.For<ITranscriptIngestionService>();
        service.IngestPathAsync(Arg.Any<TranscriptIngestionRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<TranscriptIngestionResult>(new UnauthorizedAccessException("outside workspace")));
        var controller = new SessionLogTranscriptIngestionController(
            service,
            new WorkspaceContext { WorkspacePath = "F:/workspace" },
            NullLogger<SessionLogTranscriptIngestionController>.Instance);

        var response = await controller.IngestPathAsync(new TranscriptIngestPathRequest
        {
            Path = "../outside/session.jsonl",
            Agent = "Codex",
        }, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var forbidden = Assert.IsType<ObjectResult>(response.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }
    /// <summary>Verifies multipart upload staging delegates into the shared transcript service and removes staging files.</summary>
    [Fact]
    public async Task IngestUploadAsync_StagesFilesDelegatesAndDeletesStaging()
    {
        var service = Substitute.For<ITranscriptIngestionService>();
        var workspacePath = Path.Combine(Path.GetTempPath(), "mcp-transcript-upload", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspacePath);
        var receipt = new TranscriptSessionReceipt(
            TranscriptSourceKind.Codex,
            "root",
            "session-1",
            "hash",
            "pending",
            Path.Combine(workspacePath, ".mcpServer", "Codex", "transcripts", "runs", "upload-1", "session-1.hash.sessionlog.yaml"),
            Path.Combine(workspacePath, ".mcpServer", "Codex", "failsafe", "pending", "root.hash.importRecovery.yaml"));
        var ingestionResult = new TranscriptIngestionResult(
            sessions: [],
            diagnostics: [],
            runId: "upload-1",
            artifactRootPath: Path.Combine(workspacePath, ".mcpServer", "Codex", "transcripts", "runs", "upload-1"),
            importRecoveryPaths: [receipt.ImportRecoveryPath],
            persisted: false,
            degraded: true,
            receipts: [receipt]);
        TranscriptIngestionRequest? captured = null;
        service.IngestPathAsync(Arg.Do<TranscriptIngestionRequest>(request =>
            {
                captured = request;
                Assert.True(Directory.Exists(request.Path));
                Assert.True(File.Exists(Path.Combine(request.Path, "session.jsonl")));
            }), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(ingestionResult));
        var controller = new SessionLogTranscriptIngestionController(
            service,
            new WorkspaceContext { WorkspacePath = workspacePath },
            NullLogger<SessionLogTranscriptIngestionController>.Instance);
        var file = CreateFormFile("session.jsonl", "{\"session_meta\":{\"id\":\"session-1\"}}");

        var response = await controller.IngestUploadAsync(new TranscriptIngestUploadRequest
        {
            Agent = "Codex",
            Source = TranscriptSourceKind.Codex,
            Recursive = true,
            Strict = true,
            Persist = true,
            CompatibilityProfile = TranscriptCompatibilityProfile.Codex,
            EmitNormalizedProfile = true,
            Files = [file],
        }, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var body = Assert.IsType<TranscriptIngestRunResponse>(ok.Value);
        Assert.NotNull(captured);
        Assert.Equal(workspacePath, captured.WorkspacePath);
        Assert.Equal("Codex", captured.Agent);
        Assert.Equal(TranscriptCompatibilityProfile.Codex, captured.CompatibilityProfile);
        Assert.StartsWith(Path.Combine(workspacePath, ".mcpServer", "Codex", "transcripts", "staging"), captured.Path, StringComparison.OrdinalIgnoreCase);
        Assert.False(Directory.Exists(captured.Path));
        Assert.Equal("upload-1", body.RunId);
        Assert.Single(body.Receipts);
        Directory.Delete(workspacePath, recursive: true);
    }

    /// <summary>Upload ingestion returns 207 Multi-Status when a bundle failure is reported with successful receipts.</summary>
    [Fact]
    public async Task IngestUploadAsync_ReturnsMultiStatusForPartialBundleFailure()
    {
        var service = Substitute.For<ITranscriptIngestionService>();
        var workspacePath = Path.Combine(Path.GetTempPath(), "mcp-transcript-upload", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspacePath);
        var receipt = new TranscriptSessionReceipt(
            TranscriptSourceKind.Codex,
            "root",
            "session-1",
            "hash",
            "pending",
            Path.Combine(workspacePath, ".mcpServer", "Codex", "transcripts", "runs", "upload-partial", "session-1.hash.sessionlog.yaml"),
            Path.Combine(workspacePath, ".mcpServer", "Codex", "failsafe", "pending", "root.hash.importRecovery.yaml"));
        var diagnostic = new TranscriptDiagnostic("normalize_failed", "Malformed transcript bundle.", "warning", "bad/session.jsonl");
        var ingestionResult = new TranscriptIngestionResult(
            sessions: [],
            diagnostics: [diagnostic],
            runId: "upload-partial",
            artifactRootPath: Path.Combine(workspacePath, ".mcpServer", "Codex", "transcripts", "runs", "upload-partial"),
            importRecoveryPaths: [receipt.ImportRecoveryPath],
            persisted: false,
            degraded: true,
            receipts: [receipt]);
        service.IngestPathAsync(Arg.Any<TranscriptIngestionRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(ingestionResult));
        var controller = new SessionLogTranscriptIngestionController(
            service,
            new WorkspaceContext { WorkspacePath = workspacePath },
            NullLogger<SessionLogTranscriptIngestionController>.Instance);
        var file = CreateFormFile("session.jsonl", "{\"type\":\"session_meta\",\"payload\":{\"id\":\"session-1\"}}");

        var response = await controller.IngestUploadAsync(new TranscriptIngestUploadRequest
        {
            Agent = "Codex",
            Source = TranscriptSourceKind.Auto,
            Recursive = true,
            Strict = false,
            Persist = true,
            Files = [file],
        }, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var multiStatus = Assert.IsType<ObjectResult>(response.Result);
        Assert.Equal(StatusCodes.Status207MultiStatus, multiStatus.StatusCode);
        var body = Assert.IsType<TranscriptIngestRunResponse>(multiStatus.Value);
        Assert.Equal("upload-partial", body.RunId);
        Assert.Single(body.Receipts);
        Assert.Single(body.Diagnostics);
        Assert.Equal("normalize_failed", body.Diagnostics[0].Code);
        Directory.Delete(workspacePath, recursive: true);
    }

    /// <summary>Verifies ZIP traversal entries are rejected before service delegation.</summary>
    [Fact]
    public async Task IngestUploadAsync_RejectsZipTraversal()
    {
        var service = Substitute.For<ITranscriptIngestionService>();
        var workspacePath = Path.Combine(Path.GetTempPath(), "mcp-transcript-upload", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspacePath);
        var controller = new SessionLogTranscriptIngestionController(
            service,
            new WorkspaceContext { WorkspacePath = workspacePath },
            NullLogger<SessionLogTranscriptIngestionController>.Instance);
        var zip = CreateZipFormFile("bundle.zip", "../escape.jsonl", "{}");

        var response = await controller.IngestUploadAsync(new TranscriptIngestUploadRequest
        {
            Agent = "Codex",
            Files = [zip],
        }, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var badRequest = Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.NotNull(badRequest.Value);
        await service.DidNotReceiveWithAnyArgs().IngestPathAsync(default!, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Directory.Delete(workspacePath, recursive: true);
    }

    private static IFormFile CreateFormFile(string fileName, string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "files", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/jsonl",
        };
    }

    private static IFormFile CreateZipFormFile(string fileName, string entryName, string content)
    {
        var stream = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry(entryName);
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream);
            writer.Write(content);
        }

        stream.Position = 0;
        return new FormFile(stream, 0, stream.Length, "files", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/zip",
        };
    }
}