using McpServer.Client.Models;
using McpServer.Repl.Core;
using NSubstitute;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// TEST-MCP-TRANSCRIPT-008: validates REPL transcript ingestion and normalization routing.
/// </summary>
public sealed class SessionLogTranscriptDispatcherTests
{
    /// <summary>repl.sessionlog.ingestTranscripts delegates to the transcript workflow with HTTP-compatible defaults.</summary>
    [Fact]
    public async Task DispatchAsync_IngestTranscripts_DelegatesToTranscriptWorkflow()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        var workflow = Substitute.For<ITranscriptIngestionWorkflow>();
        var receipt = CreateReceipt("run-ingest");
        workflow.IngestTranscriptsAsync(Arg.Any<TranscriptIngestPathRequest>(), Arg.Any<CancellationToken>())
            .Returns(receipt);
        var sut = new ReplCommandDispatcher(passthrough, transcriptIngestionWorkflow: workflow);
        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-20260710T123000Z-ingest-transcripts",
                Method = SessionLogCommandShapes.IngestTranscriptsMethod,
                Params = new Dictionary<string, object?>
                {
                    ["path"] = "transcripts/session.jsonl",
                    ["agent"] = "Codex",
                    ["source"] = "Codex",
                    ["recursive"] = false,
                    ["strict"] = true,
                }
            }
        };

        var response = await sut.DispatchAsync(envelope, TestContext.Current.CancellationToken);

        Assert.Equal("result", response.Type);
        var payload = Assert.IsType<ResultPayload>(response.Payload);
        Assert.Same(receipt, payload.Result);
        await workflow.Received(1).IngestTranscriptsAsync(
            Arg.Is<TranscriptIngestPathRequest>(request =>
                request != null &&
                request.Path == "transcripts/session.jsonl" &&
                request.Agent == "Codex" &&
                request.Source == TranscriptSourceKind.Codex &&
                request.Recursive == false &&
                request.Strict == true &&
                request.Persist == true &&
                request.EmitNormalizedProfile == false &&
                request.CompatibilityProfile == TranscriptCompatibilityProfile.None),
            Arg.Any<CancellationToken>());
        await passthrough.DidNotReceiveWithAnyArgs().InvokeAsync(default!, default!, default!, TestContext.Current.CancellationToken);
    }

    /// <summary>repl.sessionlog.normalizeTranscripts requires a target profile and does not persist by default.</summary>
    [Fact]
    public async Task DispatchAsync_NormalizeTranscripts_RequiresProfileAndDisablesPersistenceByDefault()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();
        var workflow = Substitute.For<ITranscriptIngestionWorkflow>();
        var receipt = CreateReceipt("run-normalize");
        workflow.NormalizeTranscriptsAsync(Arg.Any<TranscriptIngestPathRequest>(), Arg.Any<CancellationToken>())
            .Returns(receipt);
        var sut = new ReplCommandDispatcher(passthrough, transcriptIngestionWorkflow: workflow);
        var envelope = new YamlEnvelope
        {
            Type = "request",
            Payload = new RequestPayload
            {
                RequestId = "req-20260710T123001Z-normalize-transcripts",
                Method = SessionLogCommandShapes.NormalizeTranscriptsMethod,
                Params = new Dictionary<string, object?>
                {
                    ["path"] = "transcripts/session.jsonl",
                    ["agent"] = "Codex",
                    ["source"] = "Codex",
                    ["targetProfile"] = "Grok",
                }
            }
        };

        var response = await sut.DispatchAsync(envelope, TestContext.Current.CancellationToken);

        Assert.Equal("result", response.Type);
        var payload = Assert.IsType<ResultPayload>(response.Payload);
        Assert.Same(receipt, payload.Result);
        await workflow.Received(1).NormalizeTranscriptsAsync(
            Arg.Is<TranscriptIngestPathRequest>(request =>
                request != null &&
                request.Path == "transcripts/session.jsonl" &&
                request.Agent == "Codex" &&
                request.Source == TranscriptSourceKind.Codex &&
                request.Persist == false &&
                request.EmitNormalizedProfile == true &&
                request.CompatibilityProfile == TranscriptCompatibilityProfile.Grok),
            Arg.Any<CancellationToken>());
    }

    private static TranscriptIngestRunResponse CreateReceipt(string runId) => new()
    {
        RunId = runId,
        TotalSessions = 1,
        Persisted = false,
        Degraded = false,
    };
}
