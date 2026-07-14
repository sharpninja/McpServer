using System.Diagnostics;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using System.Net.Http.Json;
using System.Text.Json;
using McpServer.TransactionSecurity;

namespace McpServer.TransactionSecurity.Services;

/// <summary>Broker-neutral transaction pub-sub seam for subscriber commit and abort delivery. FR-MCP-121.</summary>
public interface ITransactionPubSub
{
    /// <summary>Publishes a signed diffgram commit request and returns the subscriber acknowledgement.</summary>
    /// <param name="request">Diffgram commit request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Subscriber commit response.</returns>
    Task<DiffgramCommitResponse> PublishCommitAsync(
        DiffgramCommitRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Publishes a transaction abort request and returns the subscriber acknowledgement.</summary>
    /// <param name="transactionId">Transaction identifier.</param>
    /// <param name="request">Abort request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Subscriber abort response.</returns>
    Task<TransactionAbortResponse> PublishAbortAsync(
        string transactionId,
        TransactionAbortRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>Optional compensation surface for transaction pub-sub implementations that can cancel replayable handoffs. FR-MCP-121.</summary>
public interface ITransactionPubSubCompensation
{
    /// <summary>Cancels a pending commit handoff after local rollback compensation succeeds.</summary>
    /// <param name="transactionId">Transaction identifier.</param>
    /// <param name="reason">Structured cancellation reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the cancellation operation.</returns>
    Task CancelPendingCommitAsync(
        string transactionId,
        TransactionFailureReason reason,
        CancellationToken cancellationToken = default);
}

/// <summary>Replay surface for durable transaction pub-sub messages. FR-MCP-121.</summary>
public interface ITransactionPubSubReplayService
{
    /// <summary>Replays pending durable commit and abort messages through the configured delivery adapter.</summary>
    /// <param name="maxMessages">Maximum pending messages to attempt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Replay result counts.</returns>
    Task<TransactionPubSubReplayResult> ReplayPendingAsync(
        int maxMessages = 100,
        CancellationToken cancellationToken = default);

    /// <summary>Gets pending durable pub-sub message status records.</summary>
    /// <param name="maxMessages">Maximum records to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Pending message status records.</returns>
    Task<IReadOnlyList<TransactionPubSubMessageStatus>> GetPendingMessagesAsync(
        int maxMessages = 100,
        CancellationToken cancellationToken = default);

    /// <summary>Purges completed durable pub-sub messages older than the supplied cutoff.</summary>
    /// <param name="completedBeforeUtc">Cutoff for acknowledged or canceled messages.</param>
    /// <param name="maxMessages">Maximum records to purge.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Retention purge result counts.</returns>
    Task<TransactionPubSubRetentionResult> PurgeCompletedAsync(
        DateTimeOffset completedBeforeUtc,
        int maxMessages = 100,
        CancellationToken cancellationToken = default);
}

/// <summary>Replay result counts for durable transaction pub-sub delivery.</summary>
public sealed class TransactionPubSubReplayResult
{
    /// <summary>Number of pending messages attempted.</summary>
    public int AttemptedCount { get; set; }

    /// <summary>Number of attempted messages that reached terminal subscriber acknowledgement.</summary>
    public int AcknowledgedCount { get; set; }

    /// <summary>Number of attempted messages left pending for a future replay.</summary>
    public int PendingCount { get; set; }
}

/// <summary>Retention purge result counts for durable transaction pub-sub delivery.</summary>
public sealed class TransactionPubSubRetentionResult
{
    /// <summary>UTC cutoff used for acknowledged or canceled message retention.</summary>
    public DateTimeOffset CompletedBeforeUtc { get; set; }

    /// <summary>Maximum records eligible for this purge cycle.</summary>
    public int MaxMessages { get; set; }

    /// <summary>Number of completed durable pub-sub messages purged.</summary>
    public int PurgedCount { get; set; }

    /// <summary>Number of replayable records intentionally retained.</summary>
    public int RetainedPendingCount { get; set; }
}

/// <summary>Public status projection for one durable transaction pub-sub message.</summary>
public sealed class TransactionPubSubMessageStatus
{
    /// <summary>Deterministic operation identifier for the commit or abort message.</summary>
    public string OperationId { get; set; } = string.Empty;

    /// <summary>Transaction identifier associated with the message.</summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>Message kind, such as commit or abort.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Logical broker topic name associated with the message.</summary>
    public string TopicName { get; set; } = string.Empty;

    /// <summary>Subscriber identifier associated with the message.</summary>
    public string SubscriberId { get; set; } = string.Empty;

    /// <summary>Durable delivery status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Number of delivery attempts.</summary>
    public int AttemptCount { get; set; }

    /// <summary>Last structured failure reason when available.</summary>
    public TransactionFailureReason Reason { get; set; }

    /// <summary>UTC timestamp when the message was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>UTC timestamp when the message was last updated.</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

/// <summary>Direct pub-sub adapter that delivers transaction messages to the configured subscriber service.</summary>
public sealed class DirectSubscriberTransactionPubSub : ITransactionPubSub
{
    private readonly ISubscriberCommitService _subscriber;

    /// <summary>Initializes a new instance of the <see cref="DirectSubscriberTransactionPubSub"/> class.</summary>
    /// <param name="subscriber">Subscriber commit service.</param>
    public DirectSubscriberTransactionPubSub(ISubscriberCommitService subscriber)
    {
        _subscriber = subscriber;
    }

    /// <inheritdoc />
    public Task<DiffgramCommitResponse> PublishCommitAsync(
        DiffgramCommitRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _subscriber.CommitDiffgramAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TransactionAbortResponse> PublishAbortAsync(
        string transactionId,
        TransactionAbortRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);
        ArgumentNullException.ThrowIfNull(request);
        return _subscriber.AbortTransactionAsync(transactionId, request, cancellationToken);
    }
}

/// <summary>HTTP pub-sub adapter that delivers transaction messages to an external subscriber host.</summary>
public sealed class HttpSubscriberTransactionPubSub : ITransactionPubSub
{
    private readonly HttpClient _http;

    /// <summary>Initializes a new instance of the <see cref="HttpSubscriberTransactionPubSub"/> class.</summary>
    /// <param name="http">HTTP client configured with the subscriber base address.</param>
    public HttpSubscriberTransactionPubSub(HttpClient http)
    {
        _http = http;
    }

    /// <inheritdoc />
    public async Task<DiffgramCommitResponse> PublishCommitAsync(
        DiffgramCommitRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            using var response = await _http.PostAsJsonAsync(
                    "mcpserver/subscriber/diffgrams/commit",
                    request,
                    TransactionSecurityJsonContext.Default.DiffgramCommitRequest,
                    cancellationToken)
                .ConfigureAwait(false);
            var body = await response.Content.ReadFromJsonAsync(TransactionSecurityJsonContext.Default.DiffgramCommitResponse, cancellationToken)
                .ConfigureAwait(false);
            return body ?? SubscriberUnavailable(request);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return SubscriberUnavailable(request);
        }
    }

    /// <inheritdoc />
    public async Task<TransactionAbortResponse> PublishAbortAsync(
        string transactionId,
        TransactionAbortRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            using var response = await _http.PostAsJsonAsync(
                    $"mcpserver/subscriber/transactions/{Uri.EscapeDataString(transactionId)}/abort",
                    request,
                    TransactionSecurityJsonContext.Default.TransactionAbortRequest,
                    cancellationToken)
                .ConfigureAwait(false);
            var body = await response.Content.ReadFromJsonAsync(TransactionSecurityJsonContext.Default.TransactionAbortResponse, cancellationToken)
                .ConfigureAwait(false);
            return body ?? SubscriberUnavailable(transactionId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return SubscriberUnavailable(transactionId);
        }
    }

    private static DiffgramCommitResponse SubscriberUnavailable(DiffgramCommitRequest request)
        => new()
        {
            TransactionId = request.Manifest.TransactionId,
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
        };

    private static TransactionAbortResponse SubscriberUnavailable(string transactionId)
        => new()
        {
            TransactionId = transactionId.Trim(),
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
        };
}

/// <summary>Transaction pub-sub adapter that fans out each handoff to multiple subscriber adapters. FR-MCP-121.</summary>
public sealed class FanOutTransactionPubSub : ITransactionPubSub
{
    private readonly IReadOnlyList<FanOutSubscriber> _subscribers;

    /// <summary>Initializes a new instance of the <see cref="FanOutTransactionPubSub"/> class.</summary>
    /// <param name="subscribers">Subscriber adapters that must all receive each handoff.</param>
    public FanOutTransactionPubSub(IReadOnlyList<ITransactionPubSub> subscribers)
        : this(subscribers.Select((subscriber, index) => new FanOutSubscriber($"subscriber-{index + 1}", true, subscriber)).ToArray())
    {
    }

    internal FanOutTransactionPubSub(IReadOnlyList<FanOutSubscriber> subscribers)
    {
        ArgumentNullException.ThrowIfNull(subscribers);
        if (subscribers.Count == 0)
            throw new ArgumentException("At least one subscriber is required.", nameof(subscribers));
        _subscribers = subscribers;
    }

    /// <inheritdoc />
    public async Task<DiffgramCommitResponse> PublishCommitAsync(
        DiffgramCommitRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var responses = new List<(FanOutSubscriber Subscriber, DiffgramCommitResponse Response)>(_subscribers.Count);
        foreach (var subscriber in _subscribers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                responses.Add((subscriber, await subscriber.PubSub.PublishCommitAsync(request, cancellationToken).ConfigureAwait(false)));
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                responses.Add((subscriber, CommitUnavailable(request.Manifest.TransactionId)));
            }
        }

        return SelectCommitResponse(request.Manifest.TransactionId, responses);
    }

    /// <inheritdoc />
    public async Task<TransactionAbortResponse> PublishAbortAsync(
        string transactionId,
        TransactionAbortRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);
        ArgumentNullException.ThrowIfNull(request);
        var responses = new List<(FanOutSubscriber Subscriber, TransactionAbortResponse Response)>(_subscribers.Count);
        foreach (var subscriber in _subscribers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                responses.Add((subscriber, await subscriber.PubSub.PublishAbortAsync(transactionId, request, cancellationToken).ConfigureAwait(false)));
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                responses.Add((subscriber, AbortUnavailable(transactionId)));
            }
        }

        return SelectAbortResponse(transactionId, responses);
    }

    private static DiffgramCommitResponse SelectCommitResponse(
        string transactionId,
        IReadOnlyList<(FanOutSubscriber Subscriber, DiffgramCommitResponse Response)> responses)
    {
        foreach (var (subscriber, response) in responses)
        {
            if (subscriber.Required &&
                response.Reason is not TransactionFailureReason.None and not TransactionFailureReason.DuplicateConflict)
                return response;
        }

        return responses.FirstOrDefault().Response ?? CommitUnavailable(transactionId);
    }

    private static TransactionAbortResponse SelectAbortResponse(
        string transactionId,
        IReadOnlyList<(FanOutSubscriber Subscriber, TransactionAbortResponse Response)> responses)
    {
        foreach (var (subscriber, response) in responses)
        {
            if (subscriber.Required &&
                response.Reason is not TransactionFailureReason.Aborted and not TransactionFailureReason.None)
                return response;
        }

        return responses.FirstOrDefault().Response ?? AbortUnavailable(transactionId);
    }

    internal sealed record FanOutSubscriber(string SubscriberId, bool Required, ITransactionPubSub PubSub);

    private static DiffgramCommitResponse CommitUnavailable(string transactionId)
        => new()
        {
            TransactionId = transactionId,
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
        };

    private static TransactionAbortResponse AbortUnavailable(string transactionId)
        => new()
        {
            TransactionId = transactionId,
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
        };
}

/// <summary>Client abstraction for publishing transaction pub-sub envelopes to an external broker. FR-MCP-121.</summary>
public interface ITransactionPubSubBrokerClient
{
    /// <summary>Publishes one broker envelope and returns its acknowledgement.</summary>
    /// <param name="envelope">Broker envelope.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Broker acknowledgement.</returns>
    Task<TransactionPubSubAcknowledgement> PublishAsync(
        TransactionPubSubEnvelope envelope,
        CancellationToken cancellationToken = default);
}

/// <summary>External process/topic transaction pub-sub adapter. FR-MCP-121.</summary>
public sealed class ExternalBrokerTransactionPubSub : ITransactionPubSub
{
    private const string KindCommit = "commit";
    private const string KindAbort = "abort";
    private static readonly JsonSerializerOptions SerializerOptions = TransactionSecurityJsonContext.Default.Options;
    private readonly ITransactionPubSubBrokerClient _brokerClient;
    private readonly TransactionPubSubTopicOptions _topics;
    private readonly IReadOnlyList<TransactionPubSubSubscriberOptions> _subscribers;

    /// <summary>Initializes a new instance of the <see cref="ExternalBrokerTransactionPubSub"/> class.</summary>
    /// <param name="brokerClient">External broker client.</param>
    /// <param name="topics">Topic configuration.</param>
    /// <param name="subscribers">Subscriber configuration.</param>
    public ExternalBrokerTransactionPubSub(
        ITransactionPubSubBrokerClient brokerClient,
        TransactionPubSubTopicOptions? topics = null,
        IReadOnlyList<TransactionPubSubSubscriberOptions>? subscribers = null)
    {
        _brokerClient = brokerClient;
        _topics = topics ?? new TransactionPubSubTopicOptions();
        _subscribers = NormalizeSubscribers(subscribers);
    }

    /// <inheritdoc />
    public async Task<DiffgramCommitResponse> PublishCommitAsync(
        DiffgramCommitRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var transactionId = NormalizeTransactionId(request.Manifest.TransactionId);
        var acknowledgements = new List<(TransactionPubSubSubscriberOptions Subscriber, TransactionPubSubAcknowledgement Ack)>(_subscribers.Count);
        foreach (var subscriber in _subscribers)
        {
            var envelope = CreateEnvelope(
                transactionId,
                KindCommit,
                ResolveTopic(subscriber.CommitTopic, _topics.CommitTopic),
                subscriber.SubscriberId,
                request);
            acknowledgements.Add((subscriber, await PublishEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false)));
        }

        return SelectCommitResponse(transactionId, acknowledgements);
    }

    /// <inheritdoc />
    public async Task<TransactionAbortResponse> PublishAbortAsync(
        string transactionId,
        TransactionAbortRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);
        ArgumentNullException.ThrowIfNull(request);
        var normalizedTransactionId = NormalizeTransactionId(transactionId);
        var acknowledgements = new List<(TransactionPubSubSubscriberOptions Subscriber, TransactionPubSubAcknowledgement Ack)>(_subscribers.Count);
        foreach (var subscriber in _subscribers)
        {
            var envelope = CreateEnvelope(
                normalizedTransactionId,
                KindAbort,
                ResolveTopic(subscriber.AbortTopic, _topics.AbortTopic),
                subscriber.SubscriberId,
                request);
            acknowledgements.Add((subscriber, await PublishEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false)));
        }

        return SelectAbortResponse(normalizedTransactionId, acknowledgements);
    }

    private async Task<TransactionPubSubAcknowledgement> PublishEnvelopeAsync(
        TransactionPubSubEnvelope envelope,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _brokerClient.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return UnavailableAcknowledgement(envelope);
        }
    }

    private static TransactionPubSubEnvelope CreateEnvelope(
        string transactionId,
        string kind,
        string topic,
        string subscriberId,
        DiffgramCommitRequest request)
        => CreateEnvelopeCore(transactionId, kind, topic, subscriberId,
            JsonSerializer.Serialize(request, TransactionSecurityJsonContext.Default.DiffgramCommitRequest));

    private static TransactionPubSubEnvelope CreateEnvelope(
        string transactionId,
        string kind,
        string topic,
        string subscriberId,
        TransactionAbortRequest request)
        => CreateEnvelopeCore(transactionId, kind, topic, subscriberId,
            JsonSerializer.Serialize(request, TransactionSecurityJsonContext.Default.TransactionAbortRequest));

    private static TransactionPubSubEnvelope CreateEnvelopeCore(
        string transactionId,
        string kind,
        string topic,
        string subscriberId,
        string requestJson)
        => new()
        {
            OperationId = $"{topic}:{subscriberId}:{kind}:{transactionId}",
            TransactionId = transactionId,
            Kind = kind,
            Topic = topic,
            SubscriberId = subscriberId,
            RequestJson = requestJson,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

    private static DiffgramCommitResponse SelectCommitResponse(
        string transactionId,
        IReadOnlyList<(TransactionPubSubSubscriberOptions Subscriber, TransactionPubSubAcknowledgement Ack)> acknowledgements)
    {
        foreach (var (subscriber, ack) in acknowledgements)
        {
            if (subscriber.Required &&
                ack.Reason is not TransactionFailureReason.None and not TransactionFailureReason.DuplicateConflict)
                return CommitResponse(transactionId, ack);
        }

        return CommitResponse(transactionId, acknowledgements.FirstOrDefault().Ack);
    }

    private static TransactionAbortResponse SelectAbortResponse(
        string transactionId,
        IReadOnlyList<(TransactionPubSubSubscriberOptions Subscriber, TransactionPubSubAcknowledgement Ack)> acknowledgements)
    {
        foreach (var (subscriber, ack) in acknowledgements)
        {
            if (subscriber.Required &&
                ack.Reason is not TransactionFailureReason.Aborted and not TransactionFailureReason.None)
                return AbortResponse(transactionId, ack);
        }

        return AbortResponse(transactionId, acknowledgements.FirstOrDefault().Ack);
    }

    private static DiffgramCommitResponse CommitResponse(string transactionId, TransactionPubSubAcknowledgement? ack)
    {
        if (!string.IsNullOrWhiteSpace(ack?.ResponseJson) &&
            JsonSerializer.Deserialize(ack.ResponseJson, TransactionSecurityJsonContext.Default.DiffgramCommitResponse) is { } response)
            return response;

        return new DiffgramCommitResponse
        {
            TransactionId = transactionId,
            Status = ack?.Reason == TransactionFailureReason.None ? "committed" : "rejected",
            Reason = ack?.Reason ?? TransactionFailureReason.SubscriberUnavailable,
            CommittedAtUtc = ack?.Reason == TransactionFailureReason.None ? ack.AcknowledgedAtUtc : null,
        };
    }

    private static TransactionAbortResponse AbortResponse(string transactionId, TransactionPubSubAcknowledgement? ack)
    {
        if (!string.IsNullOrWhiteSpace(ack?.ResponseJson) &&
            JsonSerializer.Deserialize(ack.ResponseJson, TransactionSecurityJsonContext.Default.TransactionAbortResponse) is { } response)
            return response;

        return new TransactionAbortResponse
        {
            TransactionId = transactionId,
            Status = ack?.Reason is TransactionFailureReason.Aborted or TransactionFailureReason.None ? "aborted" : "rejected",
            Reason = ack?.Reason == TransactionFailureReason.None ? TransactionFailureReason.Aborted : ack?.Reason ?? TransactionFailureReason.SubscriberUnavailable,
            AbortedAtUtc = ack?.AcknowledgedAtUtc ?? DateTimeOffset.UtcNow,
        };
    }

    private static IReadOnlyList<TransactionPubSubSubscriberOptions> NormalizeSubscribers(
        IReadOnlyList<TransactionPubSubSubscriberOptions>? subscribers)
        => subscribers is { Count: > 0 }
            ? subscribers.Select((subscriber, index) => new TransactionPubSubSubscriberOptions
            {
                SubscriberId = string.IsNullOrWhiteSpace(subscriber.SubscriberId) ? $"subscriber-{index + 1}" : subscriber.SubscriberId.Trim(),
                PartyId = string.IsNullOrWhiteSpace(subscriber.PartyId) ? null : subscriber.PartyId.Trim(),
                BaseUrl = string.IsNullOrWhiteSpace(subscriber.BaseUrl) ? null : subscriber.BaseUrl.Trim(),
                CommitTopic = string.IsNullOrWhiteSpace(subscriber.CommitTopic) ? null : subscriber.CommitTopic.Trim(),
                AbortTopic = string.IsNullOrWhiteSpace(subscriber.AbortTopic) ? null : subscriber.AbortTopic.Trim(),
                Required = subscriber.Required,
            }).ToArray()
            : [new TransactionPubSubSubscriberOptions { SubscriberId = "broker", Required = true }];

    private static string ResolveTopic(string? overrideTopic, string defaultTopic)
        => string.IsNullOrWhiteSpace(overrideTopic) ? defaultTopic : overrideTopic.Trim();

    private static string NormalizeTransactionId(string value)
        => string.IsNullOrWhiteSpace(value) ? $"txn-{Guid.NewGuid():N}" : value.Trim();

    private static TransactionPubSubAcknowledgement UnavailableAcknowledgement(TransactionPubSubEnvelope envelope)
        => new()
        {
            OperationId = envelope.OperationId,
            SubscriberId = envelope.SubscriberId,
            Kind = envelope.Kind,
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
            AcknowledgedAtUtc = DateTimeOffset.UtcNow,
        };
}

/// <summary>Process-backed external broker client that exchanges JSON envelopes over stdin/stdout. FR-MCP-121.</summary>
public sealed class ProcessTopicTransactionPubSubBrokerClient : ITransactionPubSubBrokerClient
{
    private static readonly JsonSerializerOptions SerializerOptions = TransactionSecurityJsonContext.Default.Options;
    private readonly Microsoft.Extensions.Options.IOptionsMonitor<TurnTransactionOptions> _options;

    /// <summary>Initializes a new instance of the <see cref="ProcessTopicTransactionPubSubBrokerClient"/> class.</summary>
    /// <param name="options">Turn transaction options.</param>
    public ProcessTopicTransactionPubSubBrokerClient(
        Microsoft.Extensions.Options.IOptionsMonitor<TurnTransactionOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public async Task<TransactionPubSubAcknowledgement> PublishAsync(
        TransactionPubSubEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        var processOptions = _options.CurrentValue.PubSubBrokerProcess;
        if (string.IsNullOrWhiteSpace(processOptions.ExecutablePath))
            return Unavailable(envelope);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, processOptions.PublishTimeoutSeconds)));
        using var process = new Process
        {
            StartInfo = CreateStartInfo(processOptions),
            EnableRaisingEvents = false,
        };

        try
        {
            if (!process.Start())
                return Unavailable(envelope);

            await process.StandardInput
                .WriteLineAsync(JsonSerializer.Serialize(envelope, TransactionSecurityJsonContext.Default.TransactionPubSubEnvelope))
                .ConfigureAwait(false);
            process.StandardInput.Close();

            var outputTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            var output = await outputTask.ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(output))
                return Unavailable(envelope);

            return JsonSerializer.Deserialize(output, TransactionSecurityJsonContext.Default.TransactionPubSubAcknowledgement)
                ?? Unavailable(envelope);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            return Unavailable(envelope);
        }
    }

    private static ProcessStartInfo CreateStartInfo(TransactionPubSubBrokerProcessOptions options)
    {
        var startInfo = new ProcessStartInfo(options.ExecutablePath!)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        if (!string.IsNullOrWhiteSpace(options.Arguments))
            startInfo.Arguments = options.Arguments;
        if (!string.IsNullOrWhiteSpace(options.WorkingDirectory))
            startInfo.WorkingDirectory = options.WorkingDirectory;
        return startInfo;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static TransactionPubSubAcknowledgement Unavailable(TransactionPubSubEnvelope envelope)
        => new()
        {
            OperationId = envelope.OperationId,
            SubscriberId = envelope.SubscriberId,
            Kind = envelope.Kind,
            Status = "rejected",
            Reason = TransactionFailureReason.SubscriberUnavailable,
            AcknowledgedAtUtc = DateTimeOffset.UtcNow,
        };
}

internal interface ITransactionPubSubBrokerStore : IDisposable
{
    Task<TransactionPubSubMessageState> SavePendingAsync(
        TransactionPubSubMessageState message,
        CancellationToken cancellationToken);

    Task MarkAttemptAsync(
        string operationId,
        TransactionFailureReason reason,
        CancellationToken cancellationToken);

    Task MarkAcknowledgedAsync(
        string operationId,
        string responseJson,
        TransactionFailureReason reason,
        CancellationToken cancellationToken);

    Task MarkCanceledAsync(
        string operationId,
        string responseJson,
        TransactionFailureReason reason,
        CancellationToken cancellationToken);

    Task<TransactionPubSubMessageState?> TryClaimPendingAsync(
        string operationId,
        DateTimeOffset staleInProgressBeforeUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TransactionPubSubMessageState>> GetPendingAsync(
        int maxMessages,
        DateTimeOffset staleInProgressBeforeUtc,
        CancellationToken cancellationToken);

    Task<int> PurgeCompletedAsync(
        DateTimeOffset completedBeforeUtc,
        int maxMessages,
        CancellationToken cancellationToken);
}

internal sealed record TransactionPubSubMessageState(
    string OperationId,
    string TransactionId,
    string Kind,
    string TopicName,
    string SubscriberId,
    string Status,
    string RequestJson,
    string? ResponseJson,
    int AttemptCount,
    TransactionFailureReason Reason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

internal sealed class DurableTransactionPubSub : ITransactionPubSub, ITransactionPubSubReplayService, ITransactionPubSubCompensation, IDisposable
{
    private const string KindCommit = "commit";
    private const string KindAbort = "abort";
    private const string StatusAcknowledged = "acknowledged";
    private const string StatusCanceled = "canceled";
    private const string StatusInProgress = "in_progress";
    private const string StatusPending = "pending";
    private const string DefaultTopicName = "mcp.turntransactions";
    private const string DefaultSubscriberId = "all-required";
    private static readonly JsonSerializerOptions SerializerOptions = TransactionSecurityJsonContext.Default.Options;
    private readonly ITransactionPubSub _inner;
    private readonly ITransactionPubSubBrokerStore _store;
    private readonly TimeSpan _inProgressClaimLease;
    private readonly string _topicName;
    private readonly string _subscriberId;

    public DurableTransactionPubSub(
        ITransactionPubSub inner,
        ITransactionPubSubBrokerStore store,
        TimeSpan? inProgressClaimLease = null,
        string? topicName = null,
        string? subscriberId = null)
    {
        _inner = inner;
        _store = store;
        _inProgressClaimLease = NormalizeClaimLease(inProgressClaimLease);
        _topicName = NormalizeTopicName(topicName);
        _subscriberId = NormalizeSubscriberId(subscriberId);
    }

    public async Task<DiffgramCommitResponse> PublishCommitAsync(
        DiffgramCommitRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var transactionId = NormalizeTransactionId(request.Manifest.TransactionId);
        var message = CreateMessage(OperationId(KindCommit, transactionId), transactionId, KindCommit, request);
        var saved = await _store.SavePendingAsync(
                message,
                cancellationToken)
            .ConfigureAwait(false);
        if (HasConflictingPayload(saved, message))
            return DuplicateCommitConflict(transactionId);

        if (TryReadAcknowledgedCommit(saved, out var acknowledged))
            return acknowledged;

        if (TryReadCanceledCommit(saved, out var canceled))
            return canceled;

        return await PublishCommitCoreAsync(saved.OperationId, request, transactionId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TransactionAbortResponse> PublishAbortAsync(
        string transactionId,
        TransactionAbortRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);
        ArgumentNullException.ThrowIfNull(request);
        var normalizedTransactionId = NormalizeTransactionId(transactionId);
        var message = CreateMessage(OperationId(KindAbort, normalizedTransactionId), normalizedTransactionId, KindAbort, request);
        var saved = await _store.SavePendingAsync(
                message,
                cancellationToken)
            .ConfigureAwait(false);
        if (HasConflictingPayload(saved, message))
            return DuplicateAbortConflict(normalizedTransactionId);

        if (TryReadAcknowledgedAbort(saved, out var acknowledged))
            return acknowledged;

        return await PublishAbortCoreAsync(saved.OperationId, normalizedTransactionId, request, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TransactionPubSubReplayResult> ReplayPendingAsync(
        int maxMessages = 100,
        CancellationToken cancellationToken = default)
    {
        var pending = await _store.GetPendingAsync(
                Math.Max(1, maxMessages),
                StaleInProgressBeforeUtc(),
                cancellationToken)
            .ConfigureAwait(false);
        var result = new TransactionPubSubReplayResult();
        foreach (var message in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var claimed = await _store.TryClaimPendingAsync(
                    message.OperationId,
                    StaleInProgressBeforeUtc(),
                    cancellationToken)
                .ConfigureAwait(false);
            if (claimed is null)
                continue;

            result.AttemptedCount++;
            var acknowledged = claimed.Kind switch
            {
                KindCommit => await ReplayCommitAsync(claimed, cancellationToken).ConfigureAwait(false),
                KindAbort => await ReplayAbortAsync(claimed, cancellationToken).ConfigureAwait(false),
                _ => false,
            };
            if (acknowledged)
                result.AcknowledgedCount++;
            else
                result.PendingCount++;
        }

        return result;
    }

    public async Task<IReadOnlyList<TransactionPubSubMessageStatus>> GetPendingMessagesAsync(
        int maxMessages = 100,
        CancellationToken cancellationToken = default)
    {
        var pending = await _store.GetPendingAsync(
                Math.Max(1, maxMessages),
                StaleInProgressBeforeUtc(),
                cancellationToken)
            .ConfigureAwait(false);
        return pending.Select(ToStatus).ToArray();
    }

    public async Task<TransactionPubSubRetentionResult> PurgeCompletedAsync(
        DateTimeOffset completedBeforeUtc,
        int maxMessages = 100,
        CancellationToken cancellationToken = default)
    {
        var limit = Math.Max(1, maxMessages);
        var retainedPending = await _store.GetPendingAsync(
                limit,
                StaleInProgressBeforeUtc(),
                cancellationToken)
            .ConfigureAwait(false);
        var purged = await _store.PurgeCompletedAsync(
                completedBeforeUtc,
                limit,
                cancellationToken)
            .ConfigureAwait(false);
        return new TransactionPubSubRetentionResult
        {
            CompletedBeforeUtc = completedBeforeUtc,
            MaxMessages = limit,
            PurgedCount = purged,
            RetainedPendingCount = retainedPending.Count,
        };
    }

    public async Task CancelPendingCommitAsync(
        string transactionId,
        TransactionFailureReason reason,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);
        var normalizedTransactionId = NormalizeTransactionId(transactionId);
        var response = new DiffgramCommitResponse
        {
            TransactionId = normalizedTransactionId,
            Status = "rejected",
            Reason = reason,
        };
        await _store.MarkCanceledAsync(
                OperationId(KindCommit, normalizedTransactionId),
                JsonSerializer.Serialize(response, TransactionSecurityJsonContext.Default.DiffgramCommitResponse),
                reason,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public void Dispose()
        => _store.Dispose();

    private async Task<bool> ReplayCommitAsync(TransactionPubSubMessageState message, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize(message.RequestJson, TransactionSecurityJsonContext.Default.DiffgramCommitRequest);
        if (request is null)
        {
            await _store.MarkAttemptAsync(message.OperationId, TransactionFailureReason.Unknown, cancellationToken)
                .ConfigureAwait(false);
            return false;
        }

        var response = await PublishCommitCoreAsync(message.OperationId, request, message.TransactionId, cancellationToken)
            .ConfigureAwait(false);
        return IsAcknowledged(response);
    }

    private async Task<bool> ReplayAbortAsync(TransactionPubSubMessageState message, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize(message.RequestJson, TransactionSecurityJsonContext.Default.TransactionAbortRequest);
        if (request is null)
        {
            await _store.MarkAttemptAsync(message.OperationId, TransactionFailureReason.Unknown, cancellationToken)
                .ConfigureAwait(false);
            return false;
        }

        var response = await PublishAbortCoreAsync(message.OperationId, message.TransactionId, request, cancellationToken)
            .ConfigureAwait(false);
        return IsAcknowledged(response);
    }

    private async Task<DiffgramCommitResponse> PublishCommitCoreAsync(
        string operationId,
        DiffgramCommitRequest request,
        string transactionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _inner.PublishCommitAsync(request, cancellationToken).ConfigureAwait(false);
            if (IsAcknowledged(response))
            {
                await _store.MarkAcknowledgedAsync(
                        operationId,
                        JsonSerializer.Serialize(response, TransactionSecurityJsonContext.Default.DiffgramCommitResponse),
                        response.Reason,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await _store.MarkAttemptAsync(operationId, response.Reason, cancellationToken).ConfigureAwait(false);
            }

            return response;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            await _store.MarkAttemptAsync(operationId, TransactionFailureReason.SubscriberUnavailable, cancellationToken)
                .ConfigureAwait(false);
            return new DiffgramCommitResponse
            {
                TransactionId = transactionId,
                Status = "rejected",
                Reason = TransactionFailureReason.SubscriberUnavailable,
            };
        }
    }

    private async Task<TransactionAbortResponse> PublishAbortCoreAsync(
        string operationId,
        string transactionId,
        TransactionAbortRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _inner.PublishAbortAsync(transactionId, request, cancellationToken).ConfigureAwait(false);
            if (IsAcknowledged(response))
            {
                await _store.MarkAcknowledgedAsync(
                        operationId,
                        JsonSerializer.Serialize(response, TransactionSecurityJsonContext.Default.TransactionAbortResponse),
                        response.Reason,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await _store.MarkAttemptAsync(operationId, response.Reason, cancellationToken).ConfigureAwait(false);
            }

            return response;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            await _store.MarkAttemptAsync(operationId, TransactionFailureReason.SubscriberUnavailable, cancellationToken)
                .ConfigureAwait(false);
            return new TransactionAbortResponse
            {
                TransactionId = transactionId,
                Status = "rejected",
                Reason = TransactionFailureReason.SubscriberUnavailable,
            };
        }
    }

    private TransactionPubSubMessageState CreateMessage(
        string operationId,
        string transactionId,
        string kind,
        DiffgramCommitRequest request)
        => CreateMessageCore(operationId, transactionId, kind,
            JsonSerializer.Serialize(request, TransactionSecurityJsonContext.Default.DiffgramCommitRequest));

    private TransactionPubSubMessageState CreateMessage(
        string operationId,
        string transactionId,
        string kind,
        TransactionAbortRequest request)
        => CreateMessageCore(operationId, transactionId, kind,
            JsonSerializer.Serialize(request, TransactionSecurityJsonContext.Default.TransactionAbortRequest));

    private TransactionPubSubMessageState CreateMessageCore(
        string operationId,
        string transactionId,
        string kind,
        string requestJson)
        => new(
            operationId,
            transactionId,
            kind,
            _topicName,
            _subscriberId,
            StatusPending,
            requestJson,
            null,
            0,
            TransactionFailureReason.None,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

    private DateTimeOffset StaleInProgressBeforeUtc()
        => DateTimeOffset.UtcNow.Subtract(_inProgressClaimLease);

    private static TimeSpan NormalizeClaimLease(TimeSpan? lease)
    {
        if (lease is null)
            return TimeSpan.FromMinutes(5);
        return lease.Value <= TimeSpan.Zero
            ? TimeSpan.Zero
            : lease.Value;
    }

    private static bool TryReadAcknowledgedCommit(
        TransactionPubSubMessageState message,
        out DiffgramCommitResponse response)
    {
        if (string.Equals(message.Status, StatusAcknowledged, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(message.ResponseJson) &&
            JsonSerializer.Deserialize(message.ResponseJson, TransactionSecurityJsonContext.Default.DiffgramCommitResponse) is { } acknowledged)
        {
            response = acknowledged;
            return true;
        }

        response = new DiffgramCommitResponse();
        return false;
    }

    private static bool TryReadCanceledCommit(
        TransactionPubSubMessageState message,
        out DiffgramCommitResponse response)
    {
        if (string.Equals(message.Status, StatusCanceled, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(message.ResponseJson) &&
            JsonSerializer.Deserialize(message.ResponseJson, TransactionSecurityJsonContext.Default.DiffgramCommitResponse) is { } canceled)
        {
            response = canceled;
            return true;
        }

        response = new DiffgramCommitResponse();
        return false;
    }

    private static bool TryReadAcknowledgedAbort(
        TransactionPubSubMessageState message,
        out TransactionAbortResponse response)
    {
        if (string.Equals(message.Status, StatusAcknowledged, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(message.ResponseJson) &&
            JsonSerializer.Deserialize(message.ResponseJson, TransactionSecurityJsonContext.Default.TransactionAbortResponse) is { } acknowledged)
        {
            response = acknowledged;
            return true;
        }

        response = new TransactionAbortResponse();
        return false;
    }

    private static bool IsAcknowledged(DiffgramCommitResponse response)
        => response.Reason != TransactionFailureReason.SubscriberUnavailable;

    private static bool IsAcknowledged(TransactionAbortResponse response)
        => response.Reason != TransactionFailureReason.SubscriberUnavailable;

    private static bool HasConflictingPayload(
        TransactionPubSubMessageState saved,
        TransactionPubSubMessageState message)
        => string.Equals(saved.OperationId, message.OperationId, StringComparison.OrdinalIgnoreCase) &&
           !string.Equals(saved.RequestJson, message.RequestJson, StringComparison.Ordinal);

    private static DiffgramCommitResponse DuplicateCommitConflict(string transactionId)
        => new()
        {
            TransactionId = transactionId,
            Status = "rejected",
            Reason = TransactionFailureReason.DuplicateConflict,
        };

    private static TransactionAbortResponse DuplicateAbortConflict(string transactionId)
        => new()
        {
            TransactionId = transactionId,
            Status = "rejected",
            Reason = TransactionFailureReason.DuplicateConflict,
        };

    private string OperationId(string kind, string transactionId)
        => string.Equals(_topicName, DefaultTopicName, StringComparison.OrdinalIgnoreCase) &&
           string.Equals(_subscriberId, DefaultSubscriberId, StringComparison.OrdinalIgnoreCase)
            ? $"{kind}:{transactionId}"
            : $"{_topicName}:{_subscriberId}:{kind}:{transactionId}";

    private static string NormalizeTransactionId(string value)
        => string.IsNullOrWhiteSpace(value) ? $"txn-{Guid.NewGuid():N}" : value.Trim();

    private static string NormalizeTopicName(string? value)
        => string.IsNullOrWhiteSpace(value) ? DefaultTopicName : value.Trim();

    private static string NormalizeSubscriberId(string? value)
        => string.IsNullOrWhiteSpace(value) ? DefaultSubscriberId : value.Trim();

    private static TransactionPubSubMessageStatus ToStatus(TransactionPubSubMessageState state)
        => new()
        {
            OperationId = state.OperationId,
            TransactionId = state.TransactionId,
            Kind = state.Kind,
            TopicName = state.TopicName,
            SubscriberId = state.SubscriberId,
            Status = state.Status,
            AttemptCount = state.AttemptCount,
            Reason = state.Reason,
            CreatedAtUtc = state.CreatedAtUtc,
            UpdatedAtUtc = state.UpdatedAtUtc,
        };
}

internal sealed class NoopTransactionPubSubReplayService : ITransactionPubSubReplayService
{
    public Task<TransactionPubSubReplayResult> ReplayPendingAsync(
        int maxMessages = 100,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new TransactionPubSubReplayResult());

    public Task<IReadOnlyList<TransactionPubSubMessageStatus>> GetPendingMessagesAsync(
        int maxMessages = 100,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TransactionPubSubMessageStatus>>([]);

    public Task<TransactionPubSubRetentionResult> PurgeCompletedAsync(
        DateTimeOffset completedBeforeUtc,
        int maxMessages = 100,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new TransactionPubSubRetentionResult
        {
            CompletedBeforeUtc = completedBeforeUtc,
            MaxMessages = Math.Max(1, maxMessages),
        });
}
