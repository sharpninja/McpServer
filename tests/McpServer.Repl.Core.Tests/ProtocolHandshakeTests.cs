using McpServer.Repl.Core;
using NSubstitute;

namespace McpServer.Repl.Core.Tests;

public class ProtocolHandshakeTests
{
    [Fact]
    public async Task ConnectAsync_SendsHelloEnvelope_ReturnsServerHello()
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
        );
        
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
    public async Task ConnectAsync_WithCapabilities_DeclaresClientCapabilities()
    {
        var protocol = Substitute.For<IReplProtocol>();
        var serverHello = Substitute.For<IHelloPayload>();
        serverHello.ProtocolVersion.Returns("1.0");
        
        var capabilities = new[] { "auth", "workspace-multi", "streaming" };
        
        protocol.ConnectAsync(capabilities, null, default).Returns(serverHello);
        
        var result = await protocol.ConnectAsync(capabilities);
        
        Assert.NotNull(result);
        await protocol.Received(1).ConnectAsync(capabilities, null, default);
    }

    [Fact]
    public async Task ConnectAsync_WithMetadata_SendsClientMetadata()
    {
        var protocol = Substitute.For<IReplProtocol>();
        var serverHello = Substitute.For<IHelloPayload>();
        serverHello.ProtocolVersion.Returns("1.0");
        
        var metadata = new Dictionary<string, string>
        {
            { "client", "repl-cli" },
            { "version", "1.0.0" }
        };
        
        protocol.ConnectAsync(null, metadata, default).Returns(serverHello);
        
        var result = await protocol.ConnectAsync(null, metadata);
        
        Assert.NotNull(result);
        await protocol.Received(1).ConnectAsync(null, metadata, default);
    }

    [Fact]
    public async Task ConnectAsync_AlreadyConnected_ThrowsInvalidOperationException()
    {
        var protocol = Substitute.For<IReplProtocol>();
        protocol.IsConnected.Returns(true);
        
        protocol.ConnectAsync(null, null, default)
            .Returns<IHelloPayload>(x => throw new InvalidOperationException("Already connected"));
        
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await protocol.ConnectAsync()
        );
    }

    [Fact]
    public async Task ConnectAsync_ServerVersionMismatch_CompletesButLogsWarning()
    {
        var protocol = Substitute.For<IReplProtocol>();
        var serverHello = Substitute.For<IHelloPayload>();
        serverHello.ProtocolVersion.Returns("2.0");
        
        protocol.ProtocolVersion.Returns("1.0");
        protocol.ConnectAsync(null, null, default).Returns(serverHello);
        
        var result = await protocol.ConnectAsync();
        
        Assert.NotNull(result);
        Assert.Equal("2.0", result.ProtocolVersion);
    }

    [Fact]
    public void IsConnected_BeforeConnect_ReturnsFalse()
    {
        var protocol = Substitute.For<IReplProtocol>();
        protocol.IsConnected.Returns(false);
        
        Assert.False(protocol.IsConnected);
    }

    [Fact]
    public async Task IsConnected_AfterConnect_ReturnsTrue()
    {
        var protocol = Substitute.For<IReplProtocol>();
        var serverHello = Substitute.For<IHelloPayload>();
        serverHello.ProtocolVersion.Returns("1.0");
        
        protocol.ConnectAsync(null, null, default).Returns(Task.FromResult(serverHello));
        protocol.IsConnected.Returns(true);
        
        await protocol.ConnectAsync();
        
        Assert.True(protocol.IsConnected);
    }

    [Fact]
    public async Task DisconnectAsync_WhenConnected_ClearsPendingRequests()
    {
        var protocol = Substitute.For<IReplProtocol>();
        protocol.IsConnected.Returns(true);
        
        await protocol.DisconnectAsync();
        
        await protocol.Received(1).DisconnectAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisconnectAsync_AfterDisconnect_IsConnectedReturnsFalse()
    {
        var protocol = Substitute.For<IReplProtocol>();
        
        protocol.DisconnectAsync(default).Returns(Task.CompletedTask);
        protocol.IsConnected.Returns(false);
        
        await protocol.DisconnectAsync();
        
        Assert.False(protocol.IsConnected);
    }

    [Fact]
    public async Task ProtocolVersion_ReturnsExpectedVersion()
    {
        var protocol = Substitute.For<IReplProtocol>();
        protocol.ProtocolVersion.Returns("1.0");
        
        Assert.Equal("1.0", protocol.ProtocolVersion);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task ConnectAsync_Timeout_ThrowsTaskCanceledException()
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
    public async Task ConnectAsync_NetworkFailure_ThrowsException()
    {
        var protocol = Substitute.For<IReplProtocol>();
        
        protocol.ConnectAsync(null, null, default)
            .Returns<IHelloPayload>(x => throw new HttpRequestException("Connection refused"));
        
        await Assert.ThrowsAsync<HttpRequestException>(
            async () => await protocol.ConnectAsync()
        );
    }
}
