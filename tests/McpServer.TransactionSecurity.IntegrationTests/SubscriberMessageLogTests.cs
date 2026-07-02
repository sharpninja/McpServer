using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Options;

namespace McpServer.TransactionSecurity.IntegrationTests;

/// <summary>
/// TEST-MCP-SUBLOG-001: High-performance subscriber message logging (FR-MCP-SUBLOG-001). Verifies the Parseable
/// sink request shape, error-swallowing, and that the subscriber emits one message-log entry per received message.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SubscriberMessageLogTests
{
    private const string PublisherPartyId = "publisher-1";
    private const string SubscriberPartyId = "subscriber-1";

    /// <summary>The Parseable sink POSTs a flat JSON batch to /api/v1/ingest with X-P-Stream and basic auth.</summary>
    [Fact]
    public async Task ParseableSink_PostsBatchWithStreamAndAuth()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK);
        using var http = new HttpClient(handler);
        var sink = new ParseableSubscriberMessageLog(http, new SubscriberParseableOptions
        {
            Enabled = true,
            Url = "http://parseable.test",
            StreamName = "mcp-subscriber",
            Username = "user",
            Password = "pass",
        });

        await sink.LogAsync(new SubscriberMessageLogEntry(
            "subscriber.transaction.committed", "txn-1", "None", "diffgram-txn-1", DateTimeOffset.UnixEpoch)).ConfigureAwait(true);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("http://parseable.test/api/v1/ingest", handler.LastRequestUri);
        Assert.Equal("mcp-subscriber", handler.LastStreamHeader);
        Assert.Equal("Basic", handler.LastAuthScheme);
        Assert.Equal(Convert.ToBase64String(Encoding.UTF8.GetBytes("user:pass")), handler.LastAuthParameter);
        Assert.Contains("subscriber.transaction.committed", handler.LastBody, StringComparison.Ordinal);
        Assert.Contains("txn-1", handler.LastBody, StringComparison.Ordinal);
    }

    /// <summary>A failing Parseable endpoint never throws out of the sink (best-effort).</summary>
    [Fact]
    public async Task ParseableSink_SwallowsTransportErrors()
    {
        var handler = new CapturingHandler(throws: true);
        using var http = new HttpClient(handler);
        var sink = new ParseableSubscriberMessageLog(http, new SubscriberParseableOptions
        {
            Enabled = true,
            Url = "http://parseable.test",
        });

        var ex = await Record.ExceptionAsync(() => sink.LogAsync(new SubscriberMessageLogEntry(
            "subscriber.transaction.rejected", "txn-2", "ManifestSignatureMismatch", null, DateTimeOffset.UnixEpoch))).ConfigureAwait(true);

        Assert.Null(ex);
    }

    /// <summary>The subscriber emits a message-log entry for each received message (committed and rejected).</summary>
    [Fact]
    public async Task Subscriber_EmitsMessageLog_PerReceivedMessage()
    {
        var canonicalizer = new TransactionManifestCanonicalizer();
        using var keyServer = new InMemoryKeyServerService(
            new FixedOptionsMonitor<KeyServerOptions>(new KeyServerOptions()), canonicalizer);
        await keyServer.RegisterPartyAsync(new PartyRegistrationRequest { PartyId = PublisherPartyId, Role = "publisher" }).ConfigureAwait(true);
        await keyServer.RegisterPartyAsync(new PartyRegistrationRequest { PartyId = SubscriberPartyId, Role = "subscriber" }).ConfigureAwait(true);

        var capturing = new CapturingMessageLog();
        using var subscriber = new InMemorySubscriberCommitService(
            keyServer,
            canonicalizer,
            new FixedOptionsMonitor<SubscriberOptions>(new SubscriberOptions { PartyId = SubscriberPartyId }),
            new TransactionDiffgramProtector(),
            capturing);

        var ok = await SignManifestAsync(keyServer, "txn-ok", sequence: 1, nonce: "n-ok").ConfigureAwait(true);
        await subscriber.CommitDiffgramAsync(CreateCommitRequest(ok)).ConfigureAwait(true);

        var tampered = await SignManifestAsync(keyServer, "txn-bad", sequence: 2, nonce: "n-bad").ConfigureAwait(true);
        tampered.DiffgramSha256 = Sha256Hex("tampered");
        await subscriber.CommitDiffgramAsync(CreateCommitRequest(tampered)).ConfigureAwait(true);

        Assert.Contains(capturing.Entries, e => e.EventName == "subscriber.transaction.committed" && e.TransactionId == "txn-ok");
        Assert.Contains(capturing.Entries, e => e.EventName == "subscriber.transaction.rejected" && e.TransactionId == "txn-bad" && e.Reason == nameof(TransactionFailureReason.ManifestSignatureMismatch));
    }

    private static async Task<TransactionManifestDto> SignManifestAsync(
        InMemoryKeyServerService keyServer, string transactionId, long sequence, string nonce)
    {
        var response = await keyServer.SignManifestAsync(new TransactionManifestSignRequest
        {
            TransactionId = transactionId,
            TurnId = "turn",
            PublisherPartyId = PublisherPartyId,
            SubscriberPartyId = SubscriberPartyId,
            Sequence = sequence,
            Nonce = nonce,
            DiffgramSha256 = Sha256Hex("plain-diffgram"),
            EncryptedBodySha256 = Sha256Hex("encrypted-diffgram"),
        }).ConfigureAwait(true);
        Assert.True(response.Success);
        return response.Manifest!;
    }

    private static DiffgramCommitRequest CreateCommitRequest(TransactionManifestDto manifest)
        => new()
        {
            Manifest = manifest,
            EncryptedDiffgramBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("encrypted-diffgram")),
            EncryptedBodySha256 = manifest.EncryptedBodySha256,
            DiffgramSha256 = manifest.DiffgramSha256,
        };

    private static string Sha256Hex(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class CapturingMessageLog : ISubscriberMessageLog
    {
        public ConcurrentBag<SubscriberMessageLogEntry> Entries { get; } = [];

        public Task LogAsync(SubscriberMessageLogEntry entry, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingHandler(HttpStatusCode statusCode = HttpStatusCode.OK, bool throws = false) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        public string? LastRequestUri { get; private set; }

        public string? LastStreamHeader { get; private set; }

        public string? LastAuthScheme { get; private set; }

        public string? LastAuthParameter { get; private set; }

        public string LastBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (throws)
                throw new HttpRequestException("simulated parseable outage");

            LastRequest = request;
            LastRequestUri = request.RequestUri?.ToString();
            LastStreamHeader = request.Headers.TryGetValues("X-P-Stream", out var values) ? values.FirstOrDefault() : null;
            LastAuthScheme = request.Headers.Authorization?.Scheme;
            LastAuthParameter = request.Headers.Authorization?.Parameter;
            LastBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return new HttpResponseMessage(statusCode);
        }
    }

    private sealed class FixedOptionsMonitor<TOptions>(TOptions currentValue) : IOptionsMonitor<TOptions>
        where TOptions : class
    {
        public TOptions CurrentValue { get; } = currentValue;

        public TOptions Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<TOptions, string?> listener) => null;
    }
}
