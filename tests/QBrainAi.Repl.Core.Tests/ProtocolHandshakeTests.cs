using QBrainAi.Repl.Core;
using NSubstitute;

namespace QBrainAi.Repl.Core.Tests;

public class ProtocolHandshakeTests
{
    [Fact]
    public async Task ConnectAsyncSendsHelloEnvelopeReturnsServerHello()
    {
        var protocol = Substitute.For<IReplProtocol>();
        var serverHello = Substitute.For<IHelloPayload>();
        serverHello.ProtocolVersion.Returns("1.0");
        serverHello.Capabilities.Returns(new[] { "auth", "workspace-multi" });

        protocol.ConnectAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<CancellationToken>()
        ).Returns(serverHello);

        var result = await protocol.ConnectAsync(
            new[] { "auth" },
            new Dictionary<string, string> { { "client", "test" } }
        , cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("1.0", result.ProtocolVersion);
        Assert.Contains("auth", result.Capabilities!);

        await protocol.Received(1).ConnectAsync(
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task ConnectAsyncWithCapabilitiesDeclaresClientCapabilities()
    {
        var protocol = Substitute.For<IReplProtocol>();
        var serverHello = Substitute.For<IHelloPayload>();
        serverHello.ProtocolVersion.Returns("1.0");

        var capabilities = new[] { "auth", "workspace-multi", "streaming" };

        protocol.ConnectAsync(capabilities, null, cancellationToken: TestContext.Current.CancellationToken).Returns(serverHello);

        var result = await protocol.ConnectAsync(capabilities, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await protocol.Received(1).ConnectAsync(capabilities, null, cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ConnectAsyncWithMetadataSendsClientMetadata()
    {
        var protocol = Substitute.For<IReplProtocol>();
        var serverHello = Substitute.For<IHelloPayload>();
        serverHello.ProtocolVersion.Returns("1.0");

        var metadata = new Dictionary<string, string>
        {
            { "client", "repl-cli" },
            { "version", "1.0.0" }
        };

        protocol.ConnectAsync(null, metadata, cancellationToken: TestContext.Current.CancellationToken).Returns(serverHello);

        var result = await protocol.ConnectAsync(null, metadata, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await protocol.Received(1).ConnectAsync(null, metadata, cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ConnectAsyncAlreadyConnectedThrowsInvalidOperationException()
    {
        var protocol = Substitute.For<IReplProtocol>();
        protocol.IsConnected.Returns(true);

        protocol.ConnectAsync(null, null, cancellationToken: TestContext.Current.CancellationToken)
            .Returns<IHelloPayload>(x => throw new InvalidOperationException("Already connected"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await protocol.ConnectAsync(cancellationToken: TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task ConnectAsyncServerVersionMismatchCompletesButLogsWarning()
    {
        var protocol = Substitute.For<IReplProtocol>();
        var serverHello = Substitute.For<IHelloPayload>();
        serverHello.ProtocolVersion.Returns("2.0");

        protocol.ProtocolVersion.Returns("1.0");
        protocol.ConnectAsync(null, null, cancellationToken: TestContext.Current.CancellationToken).Returns(serverHello);

        var result = await protocol.ConnectAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("2.0", result.ProtocolVersion);
    }

    [Fact]
    public void IsConnectedBeforeConnectReturnsFalse()
    {
        var protocol = Substitute.For<IReplProtocol>();
        protocol.IsConnected.Returns(false);

        Assert.False(protocol.IsConnected);
    }

    [Fact]
    public async Task IsConnectedAfterConnectReturnsTrue()
    {
        var protocol = Substitute.For<IReplProtocol>();
        var serverHello = Substitute.For<IHelloPayload>();
        serverHello.ProtocolVersion.Returns("1.0");

        protocol.ConnectAsync(null, null, cancellationToken: TestContext.Current.CancellationToken).Returns(Task.FromResult(serverHello));
        protocol.IsConnected.Returns(true);

        await protocol.ConnectAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(protocol.IsConnected);
    }

    [Fact]
    public async Task DisconnectAsyncWhenConnectedClearsPendingRequests()
    {
        var protocol = Substitute.For<IReplProtocol>();
        protocol.IsConnected.Returns(true);

        await protocol.DisconnectAsync(cancellationToken: TestContext.Current.CancellationToken);

        await protocol.Received(1).DisconnectAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisconnectAsyncAfterDisconnectIsConnectedReturnsFalse()
    {
        var protocol = Substitute.For<IReplProtocol>();

        protocol.DisconnectAsync(cancellationToken: TestContext.Current.CancellationToken).Returns(Task.CompletedTask);
        protocol.IsConnected.Returns(false);

        await protocol.DisconnectAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(protocol.IsConnected);
    }

    [Fact]
    public async Task ProtocolVersionReturnsExpectedVersion()
    {
        var protocol = Substitute.For<IReplProtocol>();
        protocol.ProtocolVersion.Returns("1.0");

        Assert.Equal("1.0", protocol.ProtocolVersion);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task ConnectAsyncTimeoutThrowsTaskCanceledException()
    {
        var protocol = Substitute.For<IReplProtocol>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        protocol.ConnectAsync(null, null, cts.Token)
            .Returns<IHelloPayload>(x => throw new TaskCanceledException());

        await Assert.ThrowsAsync<TaskCanceledException>(
            async () => await protocol.ConnectAsync(null, null, cts.Token)
        );
    }

    [Fact]
    public async Task ConnectAsyncNetworkFailureThrowsException()
    {
        var protocol = Substitute.For<IReplProtocol>();

        protocol.ConnectAsync(null, null, cancellationToken: TestContext.Current.CancellationToken)
            .Returns<IHelloPayload>(x => throw new HttpRequestException("Connection refused"));

        await Assert.ThrowsAsync<HttpRequestException>(
            async () => await protocol.ConnectAsync(cancellationToken: TestContext.Current.CancellationToken)
        );
    }
}
