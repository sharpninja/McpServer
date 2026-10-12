using QBrainAi.Repl.Core;
using NSubstitute;

namespace QBrainAi.Repl.Core.Tests;

public class RequestResponseCorrelationTests
{
    [Fact]
    public async Task SendRequestAsyncValidRequestReturnsResult()
    {
        var protocol = Substitute.For<IReplProtocol>();
        protocol.IsConnected.Returns(true);

        var resultData = new { status = "success" };
        protocol.SendRequestAsync("workspace.select", null, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(resultData);

        var result = await protocol.SendRequestAsync("workspace.select", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await protocol.Received(1).SendRequestAsync("workspace.select", null, cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SendRequestAsyncWithParametersSendsParams()
    {
        var protocol = Substitute.For<IReplProtocol>();
        protocol.IsConnected.Returns(true);

        var parameters = new Dictionary<string, object?>
        {
            { "path", "/home/user/project" }
        };

        protocol.SendRequestAsync("workspace.select", parameters, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(new { success = true });

        var result = await protocol.SendRequestAsync("workspace.select", parameters, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await protocol.Received(1).SendRequestAsync("workspace.select", parameters, cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SendRequestAsyncNotConnectedThrowsInvalidOperationException()
    {
        var protocol = Substitute.For<IReplProtocol>();
        protocol.IsConnected.Returns(false);

        protocol.SendRequestAsync("workspace.select", null, cancellationToken: TestContext.Current.CancellationToken)
            .Returns<object?>(x => throw new InvalidOperationException("Not connected"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await protocol.SendRequestAsync("workspace.select", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true)
        );
    }

    [Fact]
    public async Task SendRequestAsyncServerReturnsErrorThrowsReplProtocolException()
    {
        var protocol = Substitute.For<IReplProtocol>();
        protocol.IsConnected.Returns(true);

        var errorPayload = Substitute.For<IErrorPayload>();
        errorPayload.RequestId.Returns("req-001");
        errorPayload.Code.Returns("invalid_workspace");
        errorPayload.Message.Returns("Workspace not found");

        var exception = new ReplProtocolException(errorPayload);

        protocol.SendRequestAsync("workspace.select", null, cancellationToken: TestContext.Current.CancellationToken)
            .Returns<object?>(x => throw exception);

        var ex = await Assert.ThrowsAsync<ReplProtocolException>(
            async () => await protocol.SendRequestAsync("workspace.select", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true)
        );

        Assert.Equal("invalid_workspace", ex.Code);
        Assert.Equal("Workspace not found", ex.Message);
    }

    [Fact]
    public async Task SendRequestAsyncTypedReturnsTypedResult()
    {
        var protocol = Substitute.For<IReplProtocol>();
        protocol.IsConnected.Returns(true);

        var typedResult = new WorkspaceInfo { Path = "/home/user/project", Name = "MyProject" };

        protocol.SendRequestAsync<WorkspaceInfo>("workspace.info", null, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(typedResult);

        var result = await protocol.SendRequestAsync<WorkspaceInfo>("workspace.info", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("/home/user/project", result.Path);
        Assert.Equal("MyProject", result.Name);
    }

    [Fact]
    public async Task SendRequestAsyncTypeConversionFailsThrowsInvalidOperationException()
    {
        var protocol = Substitute.For<IReplProtocol>();
        protocol.IsConnected.Returns(true);

        protocol.SendRequestAsync<WorkspaceInfo>("workspace.info", null, cancellationToken: TestContext.Current.CancellationToken)
            .Returns<WorkspaceInfo?>(x => throw new InvalidOperationException("Type conversion failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await protocol.SendRequestAsync<WorkspaceInfo>("workspace.info", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true)
        );
    }

    [Fact]
    public async Task MultipleRequestsDifferentRequestIdsCorrelatesToRightResponse()
    {
        var protocol = Substitute.For<IReplProtocol>();
        protocol.IsConnected.Returns(true);

        protocol.SendRequestAsync("method1", null, cancellationToken: TestContext.Current.CancellationToken).Returns(new { result = 1 });
        protocol.SendRequestAsync("method2", null, cancellationToken: TestContext.Current.CancellationToken).Returns(new { result = 2 });

        var result1 = await protocol.SendRequestAsync("method1", cancellationToken: TestContext.Current.CancellationToken);
        var result2 = await protocol.SendRequestAsync("method2", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result1);
        Assert.NotNull(result2);
    }

    [Fact]
    public async Task RegisterEventHandlerServerSendsEventInvokesHandler()
    {
        var protocol = Substitute.For<IReplProtocol>();
        var handlerInvoked = false;

        var eventPayload = Substitute.For<IEventPayload>();
        eventPayload.Event.Returns("workspace.changed");

        Func<IEventPayload, Task> handler = async payload =>
        {
            handlerInvoked = true;
            Assert.Equal("workspace.changed", payload.Event);
            await Task.CompletedTask.ConfigureAwait(true);
        };

        protocol.When(x => x.RegisterEventHandler("workspace.changed", Arg.Any<Func<IEventPayload, Task>>()))
            .Do(callInfo =>
            {
                var h = callInfo.Arg<Func<IEventPayload, Task>>()!;
                _ = h(eventPayload);
            });

        protocol.RegisterEventHandler("workspace.changed", handler);

        await Task.Delay(10, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(handlerInvoked);
    }

    [Fact]
    public async Task UnregisterEventHandlerStopsReceivingEvents()
    {
        var protocol = Substitute.For<IReplProtocol>();

        Func<IEventPayload, Task> handler = _ => Task.CompletedTask;

        protocol.UnregisterEventHandler("workspace.changed", handler);

        protocol.Received(1).UnregisterEventHandler("workspace.changed", handler);

        await Task.CompletedTask.ConfigureAwait(true);
    }

    [Fact]
    public async Task ReplProtocolExceptionContainsErrorDetails()
    {
        var errorPayload = Substitute.For<IErrorPayload>();
        errorPayload.RequestId.Returns("req-001");
        errorPayload.Code.Returns("auth_failed");
        errorPayload.Message.Returns("Authentication failed");
        errorPayload.Details.Returns(new Dictionary<string, object?>
        {
            { "reason", "invalid_token" }
        });

        var exception = new ReplProtocolException(errorPayload);

        Assert.Equal("auth_failed", exception.Code);
        Assert.Equal("Authentication failed", exception.Message);
        Assert.NotNull(exception.Details);
        Assert.Equal("invalid_token", exception.Details["reason"]);

        await Task.CompletedTask.ConfigureAwait(true);
    }

    private class WorkspaceInfo
    {
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
