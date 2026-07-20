using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client;
using McpServer.Repl.Core;
using Xunit;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// TEST-MCP-QBABSENCE-001: Absence tests proving the REPL generic client passthrough cannot resolve
/// the brain-slot sub-client by name. The passthrough is the agent-facing "client-invoke" back door,
/// so a name mapping there would expose every brain-slot REST method to general agents even after the
/// dedicated agent tools are removed. The tests drive the production
/// <see cref="GenericClientPassthrough"/> against a recording HTTP handler and assert that no
/// brain-slot HTTP request is ever emitted for any casing of the client name.
/// </summary>
public sealed class BrainSlotPassthroughAbsenceTests
{
    /// <summary>
    /// TEST-MCP-QBABSENCE-001: Verifies that resolving the brain-slot client name through the
    /// passthrough does not yield <c>BrainSlotClient</c>. The test uses a recording HTTP handler that
    /// would answer the brain-slot list endpoint, so a surviving mapping is proven by an emitted
    /// request path rather than by an exception type alone.
    /// </summary>
    /// <param name="clientName">The client name casing variant supplied to the passthrough.</param>
    /// <returns>A task that completes when the assertion has run.</returns>
    [Theory]
    [InlineData("brainslots")]
    [InlineData("BrainSlots")]
    [InlineData("BRAINSLOTS")]
    [InlineData("BrAiNsLoTs")]
    public async Task InvokeAsync_BrainSlotsClientName_DoesNotResolveBrainSlotClient(string clientName)
    {
        var handler = new RecordingHttpHandler("[]");
        using var http = new HttpClient(handler);
        var client = CreateClient(http);
        var passthrough = new GenericClientPassthrough(client);

        var exception = await Record.ExceptionAsync(() => passthrough.InvokeAsync(
            clientName,
            "ListAsync",
            new Dictionary<string, object?>(),
            TestContext.Current.CancellationToken));

        Assert.Empty(handler.RequestPaths);
        var invalid = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("Unknown client", invalid.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-QBABSENCE-001: Verifies that the typed <c>BrainSlotClient</c> surface itself is
    /// preserved on <see cref="McpServerClient"/>. Only the agent-reachable passthrough route is
    /// removed; the typed client remains for the workspace-token authorized REST callers.
    /// </summary>
    [Fact]
    public void McpServerClient_BrainSlotsProperty_RemainsAvailableForTypedCallers()
    {
        var handler = new RecordingHttpHandler("[]");
        using var http = new HttpClient(handler);
        var client = CreateClient(http);

        Assert.NotNull(client.BrainSlots);
        Assert.IsType<BrainSlotClient>(client.BrainSlots);
    }

    private static McpServerClient CreateClient(HttpClient http) =>
        new(http, new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
            WorkspacePath = @"F:\GitHub\McpServer",
        });

    /// <summary>
    /// TEST-MCP-QBABSENCE-001: Records every outbound request path so the tests can prove that no
    /// brain-slot endpoint was reached through the passthrough.
    /// </summary>
    private sealed class RecordingHttpHandler : HttpMessageHandler
    {
        private readonly string _responseBody;

        /// <summary>
        /// TEST-MCP-QBABSENCE-001: Initializes the handler with the canned JSON response body.
        /// </summary>
        /// <param name="responseBody">The JSON body returned for every request.</param>
        public RecordingHttpHandler(string responseBody)
        {
            _responseBody = responseBody;
        }

        /// <summary>
        /// TEST-MCP-QBABSENCE-001: Gets the ordered absolute paths of every captured request.
        /// </summary>
        public List<string> RequestPaths { get; } = [];

        /// <summary>
        /// TEST-MCP-QBABSENCE-001: Records the request path and returns the canned JSON response.
        /// </summary>
        /// <param name="request">The outbound request emitted by the client under test.</param>
        /// <param name="cancellationToken">The cancellation token supplied by the caller.</param>
        /// <returns>The canned JSON response.</returns>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestPaths.Add(request.RequestUri!.AbsolutePath);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json"),
            });
        }
    }
}
