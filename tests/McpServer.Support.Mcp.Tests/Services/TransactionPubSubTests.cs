using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using McpServer.Support.Mcp.McpStdio;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Tests for transaction pub-sub delivery seams. TEST-MCP-161.</summary>
public sealed class TransactionPubSubTests
{
    /// <summary>Direct pub-sub commit delivery delegates to the configured subscriber service.</summary>
    [Fact]
    public async Task DirectSubscriberTransactionPubSub_PublishCommitAsync_DelegatesToSubscriberAndPreservesResponse()
    {
        var request = new DiffgramCommitRequest { Manifest = new TransactionManifestDto { TransactionId = "txn-pubsub-commit" } };
        var response = new DiffgramCommitResponse
        {
            TransactionId = "txn-pubsub-commit",
            Status = "committed",
            Reason = TransactionFailureReason.None,
            DiffgramId = "diffgram-txn-pubsub-commit",
        };
        var subscriber = Substitute.For<ISubscriberCommitService>();
        subscriber.CommitDiffgramAsync(request, Arg.Any<CancellationToken>()).Returns(response);
        var pubSub = new DirectSubscriberTransactionPubSub(subscriber);

        var actual = await pubSub.PublishCommitAsync(request, CancellationToken.None).ConfigureAwait(true);

        Assert.Same(response, actual);
        await subscriber.Received(1).CommitDiffgramAsync(request, Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>Direct pub-sub abort delivery delegates to the configured subscriber service.</summary>
    [Fact]
    public async Task DirectSubscriberTransactionPubSub_PublishAbortAsync_DelegatesToSubscriberAndPreservesResponse()
    {
        var request = new TransactionAbortRequest
        {
            Reason = TransactionFailureReason.Aborted,
            Actor = "test",
        };
        var response = new TransactionAbortResponse
        {
            TransactionId = "txn-pubsub-abort",
            Status = "aborted",
            Reason = TransactionFailureReason.Aborted,
        };
        var subscriber = Substitute.For<ISubscriberCommitService>();
        subscriber.AbortTransactionAsync("txn-pubsub-abort", request, Arg.Any<CancellationToken>()).Returns(response);
        var pubSub = new DirectSubscriberTransactionPubSub(subscriber);

        var actual = await pubSub.PublishAbortAsync("txn-pubsub-abort", request, CancellationToken.None).ConfigureAwait(true);

        Assert.Same(response, actual);
        await subscriber.Received(1).AbortTransactionAsync("txn-pubsub-abort", request, Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>In-process transaction security registers the direct pub-sub seam for coordinator delivery.</summary>
    [Fact]
    public void AddInProcessTransactionSecurity_RegistersDirectTransactionPubSub()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddInProcessTransactionSecurity(configuration);

        using var provider = services.BuildServiceProvider();
        var pubSub = provider.GetRequiredService<ITransactionPubSub>();
        var coordinator = provider.GetRequiredService<ITurnTransactionCoordinator>();

        Assert.IsType<DirectSubscriberTransactionPubSub>(pubSub);
        Assert.IsType<TurnTransactionCoordinator>(coordinator);
    }

    /// <summary>Stdio host transaction registration resolves the coordinator required by stdio mutation gates.</summary>
    [Fact]
    public void AddStdioTransactionSecurity_RegistersTurnTransactionCoordinator()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        McpStdioHost.AddStdioTransactionSecurity(services, configuration);

        using var provider = services.BuildServiceProvider();
        var coordinator = provider.GetRequiredService<ITurnTransactionCoordinator>();

        Assert.IsType<TurnTransactionCoordinator>(coordinator);
    }

    /// <summary>HTTP pub-sub commit delivery posts to the subscriber endpoint and preserves acknowledgement bodies.</summary>
    [Fact]
    public async Task HttpSubscriberTransactionPubSub_PublishCommitAsync_PostsCommitToSubscriberEndpointAndPreservesAcknowledgement()
    {
        var request = CreateCommitRequest("txn-http-commit");
        var response = new DiffgramCommitResponse
        {
            TransactionId = "txn-http-commit",
            Status = "committed",
            Reason = TransactionFailureReason.None,
            DiffgramId = "diffgram-txn-http-commit",
        };
        var handler = new CapturingHandler((_, _, _) => Task.FromResult(JsonResponse(HttpStatusCode.OK, response)));
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://subscriber.test/"),
        };
        var pubSub = new HttpSubscriberTransactionPubSub(http);

        var actual = await pubSub.PublishCommitAsync(request, CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("committed", actual.Status);
        Assert.Equal(TransactionFailureReason.None, actual.Reason);
        Assert.Equal("diffgram-txn-http-commit", actual.DiffgramId);
        Assert.Equal(HttpMethod.Post, handler.LastMethod);
        Assert.Equal("http://subscriber.test/mcpserver/subscriber/diffgrams/commit", handler.LastUri?.ToString());
        Assert.Contains("txn-http-commit", handler.LastBody, StringComparison.Ordinal);
        Assert.Contains("encrypted-body", handler.LastBody, StringComparison.Ordinal);
    }

    /// <summary>HTTP pub-sub commit delivery returns subscriber rejection bodies from HTTP 400 responses.</summary>
    [Fact]
    public async Task HttpSubscriberTransactionPubSub_PublishCommitAsync_WhenSubscriberRejects_ReturnsRejectedResponse()
    {
        var request = CreateCommitRequest("txn-http-rejected");
        var response = new DiffgramCommitResponse
        {
            TransactionId = "txn-http-rejected",
            Status = "rejected",
            Reason = TransactionFailureReason.StaleSequence,
        };
        using var http = new HttpClient(new CapturingHandler((_, _, _) => Task.FromResult(JsonResponse(HttpStatusCode.BadRequest, response))))
        {
            BaseAddress = new Uri("http://subscriber.test/"),
        };
        var pubSub = new HttpSubscriberTransactionPubSub(http);

        var actual = await pubSub.PublishCommitAsync(request, CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("rejected", actual.Status);
        Assert.Equal(TransactionFailureReason.StaleSequence, actual.Reason);
        Assert.Equal("txn-http-rejected", actual.TransactionId);
    }

    /// <summary>HTTP pub-sub commit delivery maps transport failures to subscriber-unavailable responses.</summary>
    [Fact]
    public async Task HttpSubscriberTransactionPubSub_PublishCommitAsync_WhenTransportFails_ReturnsSubscriberUnavailable()
    {
        var pubSub = new HttpSubscriberTransactionPubSub(new HttpClient(new ThrowingHandler())
        {
            BaseAddress = new Uri("http://subscriber.test/"),
        });

        var actual = await pubSub.PublishCommitAsync(CreateCommitRequest("txn-http-offline"), CancellationToken.None)
            .ConfigureAwait(true);

        Assert.Equal("txn-http-offline", actual.TransactionId);
        Assert.Equal("rejected", actual.Status);
        Assert.Equal(TransactionFailureReason.SubscriberUnavailable, actual.Reason);
    }

    /// <summary>HTTP pub-sub abort delivery posts to the encoded subscriber transaction route and preserves acknowledgement bodies.</summary>
    [Fact]
    public async Task HttpSubscriberTransactionPubSub_PublishAbortAsync_PostsAbortToEncodedTransactionRouteAndPreservesAcknowledgement()
    {
        var request = new TransactionAbortRequest
        {
            Reason = TransactionFailureReason.Aborted,
            Actor = "test actor",
        };
        var response = new TransactionAbortResponse
        {
            TransactionId = "txn/http abort",
            Status = "aborted",
            Reason = TransactionFailureReason.Aborted,
        };
        var handler = new CapturingHandler((_, _, _) => Task.FromResult(JsonResponse(HttpStatusCode.OK, response)));
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://subscriber.test/"),
        };
        var pubSub = new HttpSubscriberTransactionPubSub(http);

        var actual = await pubSub.PublishAbortAsync("txn/http abort", request, CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("aborted", actual.Status);
        Assert.Equal("http://subscriber.test/mcpserver/subscriber/transactions/txn%2Fhttp%20abort/abort", handler.LastUri?.AbsoluteUri);
        Assert.Contains("test actor", handler.LastBody, StringComparison.Ordinal);
    }

    /// <summary>External broker pub-sub writes a commit envelope to the configured topic and preserves broker acknowledgements.</summary>
    [Fact]
    public async Task ExternalBrokerTransactionPubSub_PublishCommitAsync_WritesCommitEnvelopeToConfiguredTopicAndReturnsBrokerAck()
    {
        var request = CreateCommitRequest("txn-external-broker-commit");
        var broker = new CapturingBroker(envelope => new TransactionPubSubAcknowledgement
        {
            OperationId = envelope.OperationId,
            SubscriberId = envelope.SubscriberId,
            Kind = envelope.Kind,
            Status = "acknowledged",
            Reason = TransactionFailureReason.None,
            ResponseJson = JsonSerializer.Serialize(new DiffgramCommitResponse
            {
                TransactionId = envelope.TransactionId,
                Status = "committed",
                Reason = TransactionFailureReason.None,
                DiffgramId = "diffgram-external-broker",
            }, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            AcknowledgedAtUtc = DateTimeOffset.UtcNow,
        });
        var pubSub = new ExternalBrokerTransactionPubSub(
            broker,
            new TransactionPubSubTopicOptions { CommitTopic = "topic.commit" },
            [new TransactionPubSubSubscriberOptions { SubscriberId = "subscriber-a", Required = true }]);

        var actual = await pubSub.PublishCommitAsync(request, CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("committed", actual.Status);
        Assert.Equal("diffgram-external-broker", actual.DiffgramId);
        var envelope = Assert.Single(broker.Envelopes);
        Assert.Equal("topic.commit", envelope.Topic);
        Assert.Equal("subscriber-a", envelope.SubscriberId);
        Assert.Equal("commit", envelope.Kind);
        Assert.Contains("txn-external-broker-commit", envelope.RequestJson, StringComparison.Ordinal);
    }

    /// <summary>Fan-out pub-sub delivers commits to all required subscribers before returning committed.</summary>
    [Fact]
    public async Task FanOutTransactionPubSub_PublishCommitAsync_DeliversToAllRequiredSubscribersBeforeCommitted()
    {
        var request = CreateCommitRequest("txn-fanout-required");
        var first = Substitute.For<ITransactionPubSub>();
        var second = Substitute.For<ITransactionPubSub>();
        first.PublishCommitAsync(request, Arg.Any<CancellationToken>())
            .Returns(new DiffgramCommitResponse
            {
                TransactionId = "txn-fanout-required",
                Status = "committed",
                Reason = TransactionFailureReason.None,
                DiffgramId = "diffgram-first",
            });
        second.PublishCommitAsync(request, Arg.Any<CancellationToken>())
            .Returns(new DiffgramCommitResponse
            {
                TransactionId = "txn-fanout-required",
                Status = "committed",
                Reason = TransactionFailureReason.None,
                DiffgramId = "diffgram-second",
            });
        var pubSub = new FanOutTransactionPubSub([first, second]);

        var actual = await pubSub.PublishCommitAsync(request, CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("committed", actual.Status);
        await first.Received(1).PublishCommitAsync(request, Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await second.Received(1).PublishCommitAsync(request, Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>In-process transaction security can select the HTTP pub-sub adapter through turn transaction options.</summary>
    [Fact]
    public void AddInProcessTransactionSecurity_WhenHttpPubSubConfigured_RegistersHttpTransactionPubSub()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mcp:TurnTransactions:PubSubTransport"] = nameof(TransactionPubSubTransport.Http),
                ["Mcp:TurnTransactions:SubscriberBaseUrl"] = "http://subscriber.test/",
            })
            .Build();
        services.AddInProcessTransactionSecurity(configuration);

        using var provider = services.BuildServiceProvider();
        var pubSub = provider.GetRequiredService<ITransactionPubSub>();

        Assert.IsType<HttpSubscriberTransactionPubSub>(pubSub);
    }

    /// <summary>In-process transaction security can select the external broker pub-sub adapter through turn transaction options.</summary>
    [Fact]
    public void AddInProcessTransactionSecurity_WhenExternalBrokerConfigured_RegistersExternalBrokerTransactionPubSub()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mcp:TurnTransactions:PubSubTransport"] = nameof(TransactionPubSubTransport.ExternalBroker),
            })
            .Build();
        services.AddInProcessTransactionSecurity(configuration);

        using var provider = services.BuildServiceProvider();
        var pubSub = provider.GetRequiredService<ITransactionPubSub>();

        Assert.IsType<ExternalBrokerTransactionPubSub>(pubSub);
    }

    /// <summary>In-process transaction security can wrap the selected pub-sub adapter with durable replay semantics.</summary>
    [Fact]
    public void AddInProcessTransactionSecurity_WhenDurablePubSubConfigured_RegistersReplayableTransactionPubSub()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mcp:TurnTransactions:DurablePubSubEnabled"] = "true",
            })
            .Build();
        services.AddInProcessTransactionSecurity(configuration);

        using var provider = services.BuildServiceProvider();
        var pubSub = provider.GetRequiredService<ITransactionPubSub>();
        var replay = provider.GetRequiredService<ITransactionPubSubReplayService>();

        Assert.IsAssignableFrom<ITransactionPubSubReplayService>(pubSub);
        Assert.Same(pubSub, replay);
    }

    private static DiffgramCommitRequest CreateCommitRequest(string transactionId)
        => new()
        {
            Manifest = new TransactionManifestDto
            {
                TransactionId = transactionId,
                EncryptedBodySha256 = "encrypted-body-sha",
                DiffgramSha256 = "plain-body-sha",
            },
            EncryptedDiffgramBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("encrypted-body")),
            EncryptedBodySha256 = "encrypted-body-sha",
            DiffgramSha256 = "plain-body-sha",
        };

    private static HttpResponseMessage JsonResponse<TValue>(HttpStatusCode statusCode, TValue value)
        => new(statusCode) { Content = JsonContent.Create(value) };

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, string, CancellationToken, Task<HttpResponseMessage>> _send;

        public CapturingHandler(Func<HttpRequestMessage, string, CancellationToken, Task<HttpResponseMessage>> send)
        {
            _send = send;
        }

        public HttpMethod? LastMethod { get; private set; }

        public Uri? LastUri { get; private set; }

        public string LastBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastMethod = request.Method;
            LastUri = request.RequestUri;
            LastBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return await _send(request, LastBody, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("subscriber offline");
    }

    private sealed class CapturingBroker : ITransactionPubSubBrokerClient
    {
        private readonly Func<TransactionPubSubEnvelope, TransactionPubSubAcknowledgement> _acknowledge;

        public CapturingBroker(Func<TransactionPubSubEnvelope, TransactionPubSubAcknowledgement> acknowledge)
        {
            _acknowledge = acknowledge;
        }

        public List<TransactionPubSubEnvelope> Envelopes { get; } = [];

        public Task<TransactionPubSubAcknowledgement> PublishAsync(
            TransactionPubSubEnvelope envelope,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Envelopes.Add(envelope);
            return Task.FromResult(_acknowledge(envelope));
        }
    }
}
