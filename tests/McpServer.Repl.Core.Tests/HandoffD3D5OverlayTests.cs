using System.Net;
using System.Text;
using McpServer.Client;
using McpServer.Repl.Core;
using Xunit;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// Overlay G3 D3: shipped <see cref="RequirementsWorkflow.GenerateDocumentAsync"/> must call
/// <see cref="RequirementsClient.GenerateAsync"/> on /mcpserver/requirements/generate.
/// TEST-HANDOFF-006 / TEST-MCP-REPL-009.
/// </summary>
public sealed class HandoffD3D5OverlayTests
{
    /// <summary>
    /// D3: markdown FR generateDocument hits the shipped generate endpoint with doc=functional.
    /// </summary>
    [Fact]
    public async Task D3_GenerateDocumentAsync_ShippedRequirementsWorkflow_GetsFunctionalMarkdown()
    {
        var handler = new CapturingHandler();
        var workflow = BuildWorkflow(handler);

        var result = await workflow.GenerateDocumentAsync(
            "markdown",
            "fr",
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.NotNull(handler.LastRequest);
        Assert.NotNull(handler.LastRequest!.RequestUri);
        Assert.Contains("/mcpserver/requirements/generate", handler.LastRequest.RequestUri.AbsolutePath, StringComparison.Ordinal);
        var query = handler.LastRequest.RequestUri.Query;
        Assert.Contains("doc=functional", query, StringComparison.Ordinal);
        Assert.Contains("format=markdown", query, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(result.Content));
    }

    /// <summary>
    /// D3: wiki generateDocument with docType=all hits format=wiki and doc=all.
    /// </summary>
    [Fact]
    public async Task D3_GenerateDocumentAsync_ShippedRequirementsWorkflow_GetsWikiAll()
    {
        var handler = new CapturingHandler
        {
            ContentType = "application/zip",
            Body = [0x50, 0x4B, 0x03, 0x04],
        };
        var workflow = BuildWorkflow(handler);

        var result = await workflow.GenerateDocumentAsync(
            "wiki",
            "all",
            @"F:\GitHub\McpServer",
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.NotNull(handler.LastRequest);
        Assert.NotNull(handler.LastRequest!.RequestUri);
        Assert.Contains("/mcpserver/requirements/generate", handler.LastRequest.RequestUri.AbsolutePath, StringComparison.Ordinal);
        var query = handler.LastRequest.RequestUri.Query;
        Assert.Contains("doc=all", query, StringComparison.Ordinal);
        Assert.Contains("format=wiki", query, StringComparison.Ordinal);
        Assert.Equal(@"F:\GitHub\McpServer", Assert.Single(handler.LastRequest.Headers.GetValues("X-Workspace-Path")));
    }

    private static RequirementsWorkflow BuildWorkflow(HttpMessageHandler handler)
    {
        var client = new RequirementsClient(new HttpClient(handler), new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
            WorkspacePath = @"F:\GitHub\McpServer",
        });
        return new RequirementsWorkflow(client);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        public string ContentType { get; set; } = "text/markdown";

        public byte[] Body { get; set; } = Encoding.UTF8.GetBytes("# Functional Requirements\n\n## FR-HANDOFF-001 Ingest workspace-scoped handoff documents\n");

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            var content = new ByteArrayContent(Body);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(ContentType);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }
    }
}
