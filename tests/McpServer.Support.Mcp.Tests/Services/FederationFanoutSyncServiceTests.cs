using System.Net;
using System.Text.Json;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Tests for LocalProxy fanout sync polling and acknowledgement.</summary>
public sealed class FederationFanoutSyncServiceTests
{
    /// <summary>Valid hub envelopes are verified, applied, and acknowledged by recipient sequence.</summary>
    [Fact]
    public async Task SyncOnceAsync_VerifiesEnvelopeAppliesAndAcknowledgesRecipientSequence()
    {
        var options = CreateOptions();
        var signer = new FederationEnvelopeSigner(options);
        var operation = new FederationOperationRequest
        {
            OperationId = "op-1",
            ProxyId = "PAYTON-DESKTOP",
            Domain = "todo",
            ResourceId = "PLAN-FEDERATION-001",
            BodyBase64 = Convert.ToBase64String("{\"done\":true}"u8.ToArray()),
        };
        var item = new FederationSyncItem
        {
            Sequence = 7,
            OperationId = "op-1",
            ProxyId = "PAYTON-DESKTOP",
            Domain = "todo",
            ResourceId = "PLAN-FEDERATION-001",
            Envelope = signer.Sign(operation, "hub", "PAYTON-LEGION2"),
        };
        var apply = Substitute.For<IFederationOperationApplyService>();
        apply.ApplyAsync(Arg.Any<FederationOperationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new FederationApplyResult { Applied = true, Version = "v2" });
        var handler = new FanoutHandler([item]);
        var sut = CreateSut(apply, handler, options, signer);

        await sut.SyncOnceAsync(CancellationToken.None).ConfigureAwait(true);

        _ = apply.Received(1).ApplyAsync(
            Arg.Is<FederationOperationRequest>(o => o != null && o.OperationId == "op-1"),
            Arg.Any<CancellationToken>());
        Assert.All(handler.ApiKeys, value => Assert.Equal("hub-secret", value));
        Assert.Contains("/mcpserver/federation/sync/7/ack", handler.AckUris.Single(), StringComparison.Ordinal);
        Assert.Contains("\"status\":\"applied\"", handler.AckBodies.Single(), StringComparison.Ordinal);
    }

    /// <summary>Invalid envelopes are rejected and not applied locally.</summary>
    [Fact]
    public async Task SyncOnceAsync_InvalidEnvelopeAcksRejectedAndDoesNotApply()
    {
        var options = CreateOptions();
        var signer = new FederationEnvelopeSigner(options);
        var operation = new FederationOperationRequest
        {
            OperationId = "op-1",
            ProxyId = "PAYTON-DESKTOP",
            Domain = "todo",
            BodyBase64 = Convert.ToBase64String("{}"u8.ToArray()),
        };
        var envelope = signer.Sign(operation, "hub", "PAYTON-DESKTOP");
        var item = new FederationSyncItem
        {
            Sequence = 8,
            OperationId = "op-1",
            ProxyId = "PAYTON-DESKTOP",
            Domain = "todo",
            Envelope = envelope,
        };
        var apply = Substitute.For<IFederationOperationApplyService>();
        var handler = new FanoutHandler([item]);
        var sut = CreateSut(apply, handler, options, signer);

        await sut.SyncOnceAsync(CancellationToken.None).ConfigureAwait(true);

        _ = apply.DidNotReceiveWithAnyArgs().ApplyAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains("/mcpserver/federation/sync/8/ack", handler.AckUris.Single(), StringComparison.Ordinal);
        Assert.Contains("\"status\":\"rejected\"", handler.AckBodies.Single(), StringComparison.Ordinal);
    }

    /// <summary>When signing is enabled, bare sync rows without envelopes are rejected before apply.</summary>
    [Fact]
    public async Task SyncOnceAsync_SigningEnabledRejectsUnsignedSyncItem()
    {
        var options = CreateOptions();
        var signer = new FederationEnvelopeSigner(options);
        var item = new FederationSyncItem
        {
            Sequence = 11,
            OperationId = "op-unsigned-1",
            ProxyId = "PAYTON-DESKTOP",
            Domain = "todo",
            ResourceId = "PLAN-FEDERATION-001",
        };
        var apply = Substitute.For<IFederationOperationApplyService>();
        var handler = new FanoutHandler([item]);
        var sut = CreateSut(apply, handler, options, signer);

        await sut.SyncOnceAsync(CancellationToken.None).ConfigureAwait(true);

        _ = apply.DidNotReceiveWithAnyArgs().ApplyAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains("/mcpserver/federation/sync/11/ack", handler.AckUris.Single(), StringComparison.Ordinal);
        Assert.Contains("\"status\":\"rejected\"", handler.AckBodies.Single(), StringComparison.Ordinal);
        Assert.Contains("Signed federation envelope is required", handler.AckBodies.Single(), StringComparison.Ordinal);
    }

    /// <summary>Signed local execution envelopes run through the local executor after signature verification.</summary>
    [Fact]
    public async Task SyncOnceAsync_LocalExecutionEnvelopeExecutesAndAcknowledges()
    {
        var options = CreateOptions(localExecutionEnabled: true);
        var signer = new FederationEnvelopeSigner(options);
        var request = new FederationLocalExecutionRequest
        {
            Method = "desktop_launch",
            WorkspacePath = @"F:\GitHub\McpServer",
            ExecutablePath = @"C:\Windows\System32\notepad.exe",
        };
        var operation = new FederationOperationRequest
        {
            OperationId = "op-local-1",
            ProxyId = "PAYTON-DESKTOP",
            Domain = "local_execution",
            BodyBase64 = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(request, new JsonSerializerOptions(JsonSerializerDefaults.Web))),
        };
        var item = new FederationSyncItem
        {
            Sequence = 9,
            OperationId = "op-local-1",
            ProxyId = "PAYTON-DESKTOP",
            Domain = "local_execution",
            Envelope = signer.Sign(operation, "hub", "PAYTON-LEGION2", "local_execution"),
        };
        var apply = Substitute.For<IFederationOperationApplyService>();
        var localExecution = Substitute.For<IFederationLocalExecutionService>();
        localExecution.ExecuteAsync(Arg.Any<FederationLocalExecutionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new FederationLocalExecutionResult { Success = true, Message = "desktop_launch completed." });
        var handler = new FanoutHandler([item]);
        var sut = CreateSut(apply, handler, options, signer, localExecution);

        await sut.SyncOnceAsync(CancellationToken.None).ConfigureAwait(true);

        _ = apply.DidNotReceiveWithAnyArgs().ApplyAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
        _ = localExecution.Received(1).ExecuteAsync(
            Arg.Is<FederationLocalExecutionRequest>(r =>
                r != null &&
                r.Method == "desktop_launch" &&
                r.WorkspacePath == @"F:\GitHub\McpServer"),
            Arg.Any<CancellationToken>());
        Assert.Contains("\"status\":\"applied\"", handler.AckBodies.Single(), StringComparison.Ordinal);
    }

    /// <summary>Local execution envelopes are rejected when proxy policy disables local execution.</summary>
    [Fact]
    public async Task SyncOnceAsync_LocalExecutionDisabledRejectsEnvelope()
    {
        var options = CreateOptions(localExecutionEnabled: false);
        var signer = new FederationEnvelopeSigner(options);
        var operation = new FederationOperationRequest
        {
            OperationId = "op-local-1",
            ProxyId = "PAYTON-DESKTOP",
            Domain = "local_execution",
            Method = "desktop_launch",
            BodyBase64 = Convert.ToBase64String("{}"u8.ToArray()),
        };
        var item = new FederationSyncItem
        {
            Sequence = 10,
            OperationId = "op-local-1",
            ProxyId = "PAYTON-DESKTOP",
            Domain = "local_execution",
            Method = "desktop_launch",
            Envelope = signer.Sign(operation, "hub", "PAYTON-LEGION2", "local_execution"),
        };
        var apply = Substitute.For<IFederationOperationApplyService>();
        var localExecution = Substitute.For<IFederationLocalExecutionService>();
        var handler = new FanoutHandler([item]);
        var sut = CreateSut(apply, handler, options, signer, localExecution);

        await sut.SyncOnceAsync(CancellationToken.None).ConfigureAwait(true);

        _ = localExecution.DidNotReceiveWithAnyArgs().ExecuteAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains("\"status\":\"conflict\"", handler.AckBodies.Single(), StringComparison.Ordinal);
        Assert.Contains("local execution is disabled", handler.AckBodies.Single(), StringComparison.Ordinal);
    }

    private static FederationFanoutSyncService CreateSut(
        IFederationOperationApplyService apply,
        HttpMessageHandler handler,
        IOptionsMonitor<FederationOptions> options,
        IFederationEnvelopeSigner signer,
        IFederationLocalExecutionService? localExecution = null)
    {
        var registry = new FederationRegistry(Microsoft.Extensions.Options.Options.Create(options.CurrentValue));
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(FederationProxyService.HttpClientName).Returns(new HttpClient(handler));
        return new FederationFanoutSyncService(
            registry,
            apply,
            factory,
            options,
            signer,
            localExecution,
            NullLogger<FederationFanoutSyncService>.Instance);
    }

    private static IOptionsMonitor<FederationOptions> CreateOptions(bool localExecutionEnabled = false)
    {
        var monitor = Substitute.For<IOptionsMonitor<FederationOptions>>();
        monitor.CurrentValue.Returns(new FederationOptions
        {
            Enabled = true,
            Role = FederationRole.LocalProxy,
            HubBaseUrl = "http://hub.example:7147",
            HubAccessToken = "hub-secret",
            ProxyId = "PAYTON-LEGION2",
            EnrollmentToken = "test-secret",
            Sync = new FederationSyncOptions { FanoutIntervalSeconds = 1 },
            LocalExecution = new FederationLocalExecutionOptions
            {
                Enabled = localExecutionEnabled,
                AllowedMethods = ["desktop_launch"],
            },
        });
        return monitor;
    }

    private sealed class FanoutHandler : HttpMessageHandler
    {
        private readonly IReadOnlyList<FederationSyncItem> _items;

        public FanoutHandler(IReadOnlyList<FederationSyncItem> items)
        {
            _items = items;
        }

        public List<string> AckUris { get; } = [];

        public List<string> AckBodies { get; } = [];

        public List<string?> ApiKeys { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ApiKeys.Add(request.Headers.TryGetValues("X-Api-Key", out var values) ? values.SingleOrDefault() : null);

            if (request.Method == HttpMethod.Get)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent(_items),
                };
            }

            AckUris.Add(request.RequestUri!.AbsolutePath);
            AckBodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent(new FederationOperationResponse { OperationId = "op-1", Status = "acknowledged" }),
            };
        }

        private static StringContent JsonContent<T>(T value)
            => new(JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web)), System.Text.Encoding.UTF8, "application/json");
    }
}
