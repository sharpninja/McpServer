using System.Net.Http;
using QBrainAi.Client;
using QBrainAi.Client.Models;
using QBrainAi.Repl.Core;
using QBrainAi.TestSupport.Handoff;
using Xunit;

namespace QBrainAi.Repl.Core.Tests;

/// <summary>
/// TEST-HANDOFF-006: plugin skill methods must dispatch through the supported REPL workflow
/// and HandoffClient HTTP surface that the server maps onto IHandoffIngestionService. This unit half invokes the
/// workflow the skill documents over an in-memory HTTP handler; the case that reads the skill from the repository
/// checkout is the integration half of this class in QBrainAi.Repl.IntegrationTests (TR-MCP-USECASE-018 AC-006).
/// </summary>
/// <remarks>2026-11-07, TEST-HANDOFF-006; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class; its Category=Integration case SkillDocumentedMethods_DispatchThroughWorkflowToHandoffClient and the helpers only it used (ArgsFor, Request, FindRepoRoot) moved unchanged to the class of the same name in the integration-owned QBrainAi.Repl.IntegrationTests, so this unit-owned project holds only unit cases (slice S3, hostile validation finding R14-F4). The recording HTTP handler both halves use moved once into the linked tests/TestSupport.Shared/HandoffRecordingHandler.cs (DRY). The summary's second sentence now describes this half, which no longer reads the checkout; the unit case is unchanged.</remarks>
public sealed class HandoffSkillDelegationTests
{
    /// <summary>
    /// TEST-HANDOFF-006: invoke the real HandoffWorkflow methods that the plugin skill documents,
    /// not just parse the skill inventory.
    /// </summary>
    [Fact]
    public async Task PluginSkillWorkflow_InvokesTypedClientHandoffEndpoints()
    {
        var handler = new RecordingHandler();
        using var http = new HttpClient(handler);
        var workflow = new HandoffWorkflow(new HandoffClient(http, new QBrainAiClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
        }));

        var ingested = await workflow.IngestAsync(new HandoffIngestionRequest
        {
            SourceKind = HandoffSourceKind.Content,
            Content = "plugin-invoke",
            Mode = HandoffIngestionMode.DraftOnly,
        }, TestContext.Current.CancellationToken);
        var loaded = await workflow.GetAsync("handoff-run-001", TestContext.Current.CancellationToken);
        var approved = await workflow.ApproveAsync("handoff-run-001", new HandoffApprovalRequest { Approved = true }, TestContext.Current.CancellationToken);

        Assert.True(ingested.Success);
        Assert.True(loaded.Success);
        Assert.True(approved.Success);
        Assert.Contains(handler.Requests, item => item.Method == HttpMethod.Post && item.Path.Contains("/qbrainai/handoff/ingest", StringComparison.Ordinal));
        Assert.Contains(handler.Requests, item => item.Method == HttpMethod.Get && item.Path.Contains("/qbrainai/handoff/runs/handoff-run-001", StringComparison.Ordinal));
        Assert.Contains(handler.Requests, item => item.Method == HttpMethod.Post && item.Path.Contains("/qbrainai/handoff/runs/handoff-run-001/approve", StringComparison.Ordinal));
    }
}
