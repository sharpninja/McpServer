using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using McpServer.Client.Models;
using Xunit;

namespace McpServer.Client.Tests;

/// <summary>
/// TEST-HANDOFF-001: consumer tests for typed-client handoff contracts and HTTP shapes.
/// Uses mock HTTP responses so the client contract can fail independently of the live server.
/// </summary>
public sealed class HandoffClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key",
    };

    /// <summary>
    /// TEST-HANDOFF-001: IngestHandoffAsync posts the shared ingest contract to /mcpserver/handoff/ingest.
    /// </summary>
    [Fact]
    public async Task IngestHandoffAsync_PostsIngestContract()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"success":true,"created":false,"replayed":false,"requiresReview":false,"diagnostics":[]}""");
        using var http = new HttpClient(handler);
        var client = new HandoffClient(http, DefaultOptions);

        var result = await client.IngestHandoffAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "# handoff",
            Mode = HandoffIngestionMode.DraftOnly,
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/handoff/ingest", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains("\"sourceKind\":\"Content\"", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.Contains("\"mode\":\"DraftOnly\"", handler.LastRequestBody!, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-HANDOFF-001: GetHandoffRunAsync reads /mcpserver/handoff/runs/{runId}.
    /// </summary>
    [Fact]
    public async Task GetHandoffRunAsync_SendsCorrectUrl()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"success":true,"provenance":{"runId":"handoff-run-001","sourceKind":"Path","sourceLocator":"docs/handoff.md","contentSha256":"abc","extractedAtUtc":"2026-08-16T18:00:00Z","promptVersion":"handoff-todo-draft/v1","mode":"DraftOnly","reviewState":"None"}}""");
        using var http = new HttpClient(handler);
        var client = new HandoffClient(http, DefaultOptions);

        var result = await client.GetHandoffRunAsync("handoff-run-001", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("handoff-run-001", result.Provenance!.RunId);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/handoff/runs/handoff-run-001", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-HANDOFF-001: ApproveHandoffAsync posts the approval contract.
    /// </summary>
    [Fact]
    public async Task ApproveHandoffAsync_PostsApprovalContract()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"success":true,"created":true,"createdTodoId":"MCP-HANDOFFDEMO-001","diagnostics":[]}""");
        using var http = new HttpClient(handler);
        var client = new HandoffClient(http, DefaultOptions);

        var result = await client.ApproveHandoffAsync(
            "handoff-run-001",
            new HandoffApprovalRequest { Approved = true, Reviewer = "operator" },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Created);
        Assert.Equal("MCP-HANDOFFDEMO-001", result.CreatedTodoId);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/handoff/runs/handoff-run-001/approve", handler.LastRequest.RequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains("\"approved\":true", handler.LastRequestBody!, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-HANDOFF-001: public contract types round-trip through the typed client JSON context.
    /// </summary>
    [Fact]
    public void HandoffContracts_RoundTripThroughClientJsonContext()
    {
        var original = new HandoffIngestionResult
        {
            Success = true,
            Created = false,
            Replayed = false,
            RequiresReview = true,
            CreatedTodoId = null,
            Draft = new HandoffTodoDraft
            {
                Id = "MCP-HANDOFFDEMO-001",
                Title = "Demo",
                Section = "MCP Server",
                Priority = "high",
                Estimate = "2h",
                Description = ["Line"],
                TechnicalDetails = ["Detail"],
                ImplementationTasks = [new HandoffTodoDraftTask { Task = "Write tests", Done = false }],
                DependsOn = ["MCP-HANDOFF-001"],
                FunctionalRequirements = ["FR-HANDOFF-001"],
                TechnicalRequirements = ["TR-HANDOFF-CONTRACT-001"],
                Confidence = 0.8,
                UnknownSourceNotes = ["source author missing"],
            },
            Provenance = new HandoffProvenance
            {
                RunId = "handoff-run-001",
                SourceKind = HandoffSourceKind.Artifact,
                SourceLocator = "artifact:doc-1",
                ContentSha256 = "aabb",
                ExtractedAtUtc = new DateTimeOffset(2026, 8, 16, 18, 0, 0, TimeSpan.Zero),
                PromptVersion = "handoff-todo-draft/v1",
                TemplateVersion = "handoff-todo-draft",
                Agent = "plan-agent",
                Model = "test-model",
                Confidence = 0.8,
                Mode = HandoffIngestionMode.RequireReview,
                ReviewState = HandoffReviewState.PendingReview,
            },
            Diagnostics =
            [
                new HandoffDiagnostic
                {
                    Code = "draft_invalid_priority",
                    Severity = HandoffDiagnosticSeverity.Error,
                    Field = "priority",
                    Message = "Priority is not a known value.",
                },
            ],
        };

        var json = JsonSerializer.Serialize(original, McpClientJsonContext.Default.HandoffIngestionResult);
        var restored = JsonSerializer.Deserialize(json, McpClientJsonContext.Default.HandoffIngestionResult);

        Assert.NotNull(restored);
        Assert.Equal(original.RequiresReview, restored!.RequiresReview);
        Assert.Equal(original.Draft!.Id, restored.Draft!.Id);
        Assert.Equal(original.Draft.UnknownSourceNotes[0], restored.Draft.UnknownSourceNotes[0]);
        Assert.Equal(original.Provenance!.SourceKind, restored.Provenance!.SourceKind);
        Assert.Equal(original.Provenance.PromptVersion, restored.Provenance.PromptVersion);
        Assert.Equal(original.Diagnostics[0].Field, restored.Diagnostics[0].Field);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("# handoff body", json, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-HANDOFF-001: documented enum values exist on the public contract.
    /// </summary>
    [Fact]
    public void HandoffEnums_ExposeDocumentedValues()
    {
        Assert.Equal(new[] { HandoffSourceKind.Path, HandoffSourceKind.Content, HandoffSourceKind.Artifact }, Enum.GetValues<HandoffSourceKind>());
        Assert.Equal(new[] { HandoffIngestionMode.DraftOnly, HandoffIngestionMode.RequireReview, HandoffIngestionMode.CreateWhenConfident }, Enum.GetValues<HandoffIngestionMode>());
    }
}
