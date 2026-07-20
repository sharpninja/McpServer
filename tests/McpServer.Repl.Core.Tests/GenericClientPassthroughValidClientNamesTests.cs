using System;
using System.Collections.Generic;
using System.Linq;
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
/// TEST-MCP-QBABSENCE-001: Honesty tests for the "Unknown client" diagnostic emitted by
/// <see cref="GenericClientPassthrough"/>. The advertised valid-client list must be derived from the
/// same source the resolver uses, so the error text can never name a client the passthrough then
/// refuses to resolve. The immediate regression guarded here is the brain-slot sub-client: its typed
/// property survives on <see cref="McpServerClient"/>, but the passthrough mapping was removed, so the
/// diagnostic must not advertise it. Fixtures: a recording HTTP handler that would answer any request,
/// proving no network call is made while the names are probed.
/// </summary>
public sealed class GenericClientPassthroughValidClientNamesTests
{
    /// <summary>
    /// TEST-MCP-QBABSENCE-001: Verifies that the "Unknown client" diagnostic does not advertise the
    /// brain-slot sub-client under any spelling. Uses an unknown client name to force the diagnostic and
    /// asserts the rendered valid-client list is free of brain-slot references.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [Fact]
    public async Task InvokeAsync_UnknownClient_ErrorTextDoesNotAdvertiseBrainSlots()
    {
        var message = await GetUnknownClientMessageAsync("NoSuchClientName");

        Assert.DoesNotContain("BrainSlot", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("QuadBrain", message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// TEST-MCP-QBABSENCE-001: Verifies that the diagnostic still advertises sub-clients that really do
    /// resolve, so scrubbing the brain-slot entry does not gut the diagnostic. Asserts a representative
    /// set of live sub-client names is present in the rendered list.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [Fact]
    public async Task InvokeAsync_UnknownClient_ErrorTextStillListsResolvableClients()
    {
        var names = await GetAdvertisedClientNamesAsync();

        Assert.Contains("Todo", names);
        Assert.Contains("Context", names);
        Assert.Contains("SessionLog", names);
        Assert.Contains("Requirements", names);
        Assert.Contains("Triage", names);
    }

    /// <summary>
    /// TEST-MCP-QBABSENCE-001: Verifies that every name the diagnostic advertises actually resolves
    /// through the passthrough. Each advertised name is invoked with a method that cannot exist; a
    /// resolvable client fails at method resolution ("Unknown method"), whereas an advertised-but-dead
    /// name fails at client resolution ("Unknown client") and is reported as a liar.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [Fact]
    public async Task InvokeAsync_EveryAdvertisedClientName_ResolvesThroughThePassthrough()
    {
        var names = await GetAdvertisedClientNamesAsync();
        Assert.NotEmpty(names);

        var handler = new NoNetworkHttpHandler();
        using var http = new HttpClient(handler);
        var passthrough = new GenericClientPassthrough(CreateClient(http));

        var liars = new List<string>();
        foreach (var name in names)
        {
            var exception = await Record.ExceptionAsync(() => passthrough.InvokeAsync(
                name,
                "NoSuchMethodOnAnyClient",
                new Dictionary<string, object?>(),
                TestContext.Current.CancellationToken));

            var message = exception?.Message ?? string.Empty;
            if (!message.Contains("Unknown method", StringComparison.Ordinal))
            {
                liars.Add($"{name} => {message}");
            }
        }

        Assert.Empty(liars);
        Assert.Empty(handler.RequestPaths);
    }

    /// <summary>
    /// TEST-MCP-QBABSENCE-001: Verifies that the brain-slot client name is rejected at client resolution
    /// and that the rejection message does not echo the advertised list back with a brain-slot entry.
    /// </summary>
    /// <param name="clientName">The brain-slot client name casing variant under test.</param>
    /// <returns>A task that completes when the assertion has run.</returns>
    [Theory]
    [InlineData("brainslots")]
    [InlineData("BrainSlots")]
    [InlineData("BRAINSLOTS")]
    public async Task InvokeAsync_BrainSlotsClientName_ReportsUnknownClientWithoutNamingIt(string clientName)
    {
        var message = await GetUnknownClientMessageAsync(clientName);

        var listStart = message.IndexOf("Valid clients:", StringComparison.Ordinal);
        Assert.True(listStart >= 0, $"Diagnostic did not render a valid-client list: {message}");
        var advertised = message[listStart..];
        Assert.DoesNotContain("BrainSlot", advertised, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> GetUnknownClientMessageAsync(string clientName)
    {
        var handler = new NoNetworkHttpHandler();
        using var http = new HttpClient(handler);
        var passthrough = new GenericClientPassthrough(CreateClient(http));

        var exception = await Record.ExceptionAsync(() => passthrough.InvokeAsync(
            clientName,
            "ListAsync",
            new Dictionary<string, object?>(),
            TestContext.Current.CancellationToken));

        var invalid = Assert.IsType<InvalidOperationException>(exception);
        Assert.Empty(handler.RequestPaths);
        return invalid.Message;
    }

    private static async Task<IReadOnlyList<string>> GetAdvertisedClientNamesAsync()
    {
        var message = await GetUnknownClientMessageAsync("NoSuchClientName");
        var listStart = message.IndexOf("Valid clients:", StringComparison.Ordinal);
        Assert.True(listStart >= 0, $"Diagnostic did not render a valid-client list: {message}");

        return message[(listStart + "Valid clients:".Length)..]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static McpServerClient CreateClient(HttpClient http) =>
        new(http, new McpServerClientOptions
        {
            BaseUrl = new Uri("http://localhost:7147"),
            ApiKey = "test-key",
            WorkspacePath = @"F:\GitHub\McpServer",
        });

    /// <summary>
    /// TEST-MCP-QBABSENCE-001: Records every outbound request path so the tests can prove that name
    /// probing never reaches the network.
    /// </summary>
    private sealed class NoNetworkHttpHandler : HttpMessageHandler
    {
        /// <summary>
        /// TEST-MCP-QBABSENCE-001: Gets the ordered absolute paths of every captured request.
        /// </summary>
        public List<string> RequestPaths { get; } = [];

        /// <summary>
        /// TEST-MCP-QBABSENCE-001: Records the request path and returns an empty JSON payload.
        /// </summary>
        /// <param name="request">The outbound request emitted by the client under test.</param>
        /// <param name="cancellationToken">The cancellation token supplied by the caller.</param>
        /// <returns>An empty JSON response.</returns>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestPaths.Add(request.RequestUri!.AbsolutePath);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            });
        }
    }
}
