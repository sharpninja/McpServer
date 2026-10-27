// SPDX-License-Identifier: Apache-2.0
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QBrainAi.Client;
using ClientAuditRequest = QBrainAi.Client.Models.SessionAuditEventRequest;

namespace QBrainAi.Support.Mcp.Tests.Services;

public sealed partial class ServerAuditSqliteFixture
{
    /// <summary>
    /// FR-QB-003/005, TEST-QB-069: Executes actual typed-client serialization and
    /// response binding against the existing real SQLite service fixture. Only
    /// the HTTP handler is injected; it forwards bytes to the actual service.
    /// No network server, native REPL, authentication or process acceptance is claimed.
    /// </summary>
    /// <param name="operation">Submit or independent Read through SessionLogClient.</param>
    /// <param name="inputJson">The immutable captured audit envelope.</param>
    /// <returns>Actual reply, transmitted payload and independently stored payload.</returns>
    public string InvokeTypedClient(string operation, string inputJson)
        => InvokeTypedClientAsync(operation, inputJson).GetAwaiter().GetResult();

    private async Task<string> InvokeTypedClientAsync(string operation, string inputJson)
    {
        var envelope = JsonSerializer.Deserialize<ClientAuditRequest>(inputJson, JsonOptions)
            ?? throw new InvalidOperationException("Typed audit envelope is missing.");
        using var handler = new AuditServiceHandler(this, envelope, inputJson);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://s6-fixture.invalid/") };
        var client = new SessionLogClient(http, new QBrainAiClientOptions
        {
            BaseUrl = new Uri("http://s6-fixture.invalid/"),
            WorkspacePath = envelope.WorkspacePath,
            ApiKey = "synthetic-task-owned-fixture-key",
        });
        object? reply = operation switch
        {
            "Submit" => await client.AppendAuditEventAsync(envelope).ConfigureAwait(false),
            "Read" => await client.ReadAuditEventAsync(envelope.AgentIdentity, envelope.McpSessionId,
                envelope.RequestId, envelope.EventId).ConfigureAwait(false),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown typed-client fixture operation."),
        };
        await using var db = OpenDb(workspacePath: envelope.WorkspacePath);
        var stored = await db.SessionAuditEvents.AsNoTracking()
            .SingleOrDefaultAsync(row => row.EventId == envelope.EventId).ConfigureAwait(false);
        _trace.Enqueue(new { operation = "TypedClientTransport", typedOperation = operation, handler.RequestUri, handler.RequestBody });
        return JsonSerializer.Serialize(new
        {
            reply, handler.RequestUri, handler.RequestBody,
            wirePayloadJson = handler.WirePayloadJson,
            storedPayloadJson = stored?.PayloadJson,
            databasePath = DatabasePath,
        }, JsonOptions);
    }

    private sealed class AuditServiceHandler(ServerAuditSqliteFixture fixture, ClientAuditRequest envelope, string inputJson) : HttpMessageHandler
    {
        internal string RequestUri { get; private set; } = string.Empty;
        internal string RequestBody { get; private set; } = string.Empty;
        internal string? WirePayloadJson { get; private set; }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri?.AbsoluteUri ?? throw new InvalidOperationException("Actual client omitted URI.");
            if (request.RequestUri!.Host != "s6-fixture.invalid")
                throw new InvalidOperationException("Task-owned injected handler refuses another host.");
            var expectedReadPath = $"/qbrainai/sessionlog/{Uri.EscapeDataString(envelope.AgentIdentity)}/{Uri.EscapeDataString(envelope.McpSessionId)}/{Uri.EscapeDataString(envelope.RequestId)}/event/{Uri.EscapeDataString(envelope.EventId)}";
            object? result;
            if (request.Method == HttpMethod.Post && request.RequestUri.AbsolutePath == "/qbrainai/sessionlog/event")
            {
                RequestBody = await (request.Content ?? throw new InvalidOperationException("Actual client omitted body."))
                    .ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                using var wire = JsonDocument.Parse(RequestBody);
                WirePayloadJson = wire.RootElement.GetProperty("payloadJson").GetString();
                result = await fixture.InvokeAsync("Submit", RequestBody).ConfigureAwait(false);
            }
            else if (request.Method == HttpMethod.Get && request.RequestUri.AbsolutePath == expectedReadPath)
            {
                result = await fixture.InvokeAsync("Read", inputJson).ConfigureAwait(false);
            }
            else
            {
                throw new InvalidOperationException("Unexpected actual typed-client route or method.");
            }
            return new HttpResponseMessage(result is null ? HttpStatusCode.NotFound : HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StringContent(JsonSerializer.Serialize(result, JsonOptions), Encoding.UTF8, "application/json"),
            };
        }
    }
}
