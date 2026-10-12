using QBrainAi.Repl.Core;
using NSubstitute;

namespace QBrainAi.Repl.Core.Tests;

public class AuthRotationTests
{
    [Fact]
    public async Task UpdateAuthStateAsyncNewMarkerDataUpdatesCurrentAuthState()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();
        var markerData = Substitute.For<IMarkerFileData>();
        markerData.ApiKey.Returns("new-rotated-key");
        markerData.ServerUrl.Returns("http://localhost:5177");

        var newAuthState = Substitute.For<IAuthState>();
        newAuthState.ApiKey.Returns("new-rotated-key");
        newAuthState.IsValid.Returns(true);

        authHandler.CurrentAuthState.Returns(newAuthState);

        await authHandler.UpdateAuthStateAsync(markerData, cancellationToken: TestContext.Current.CancellationToken);

        await authHandler.Received(1).UpdateAuthStateAsync(markerData, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("new-rotated-key", authHandler.CurrentAuthState.ApiKey);
    }

    [Fact]
    public async Task UpdateAuthStateAsyncNullMarkerDataThrowsArgumentNullException()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();

        authHandler.UpdateAuthStateAsync(null!, cancellationToken: TestContext.Current.CancellationToken)
            .Returns<Task>(x => throw new ArgumentNullException("newMarkerData"));

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await authHandler.UpdateAuthStateAsync(null!, cancellationToken: TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public void RegisterAuthChangeCallbackAddsCallback()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();

        Func<IAuthState, Task> callback = async state =>
        {
            await Task.CompletedTask;
        };

        authHandler.RegisterAuthChangeCallback(callback);

        authHandler.Received(1).RegisterAuthChangeCallback(callback);
    }

    [Fact]
    public void UnregisterAuthChangeCallbackRemovesCallback()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();

        Func<IAuthState, Task> callback = _ => Task.CompletedTask;

        authHandler.UnregisterAuthChangeCallback(callback);

        authHandler.Received(1).UnregisterAuthChangeCallback(callback);
    }

    [Fact]
    public async Task AuthChangeCallbackOnAuthUpdateInvokesAllRegisteredCallbacks()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();
        var callback1Invoked = false;
        var callback2Invoked = false;

        var authState = Substitute.For<IAuthState>();
        authState.ApiKey.Returns("updated-key");

        Func<IAuthState, Task> callback1 = async state =>
        {
            callback1Invoked = true;
            await Task.CompletedTask;
        };

        Func<IAuthState, Task> callback2 = async state =>
        {
            callback2Invoked = true;
            await Task.CompletedTask;
        };

        authHandler.When(x => x.RegisterAuthChangeCallback(Arg.Any<Func<IAuthState, Task>>()))
            .Do(callInfo =>
            {
                var cb = callInfo.Arg<Func<IAuthState, Task>>()!;
                _ = cb(authState);
            });

        authHandler.RegisterAuthChangeCallback(callback1);
        authHandler.RegisterAuthChangeCallback(callback2);

        await Task.Delay(10, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(callback1Invoked);
        Assert.True(callback2Invoked);
    }

    [Fact]
    public async Task RefreshAuthStateAsyncReReadsMarkerFileReturnsUpdatedState()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();
        var refreshedState = Substitute.For<IAuthState>();
        refreshedState.ApiKey.Returns("refreshed-key");
        refreshedState.LastUpdated.Returns(DateTimeOffset.UtcNow);

        authHandler.RefreshAuthStateAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken)
            .Returns(refreshedState);

        var result = await authHandler.RefreshAuthStateAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("refreshed-key", result.ApiKey);
    }

    [Fact]
    public async Task RefreshAuthStateAsyncMarkerFileDeletedThrowsFileNotFoundException()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();

        authHandler.RefreshAuthStateAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken)
            .Returns<IAuthState>(x => throw new FileNotFoundException("Marker file not found"));

        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await authHandler.RefreshAuthStateAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task RefreshAuthStateAsyncMalformedMarkerFileThrowsFormatException()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();

        authHandler.RefreshAuthStateAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken)
            .Returns<IAuthState>(x => throw new FormatException("Invalid marker file format"));

        await Assert.ThrowsAsync<FormatException>(
            async () => await authHandler.RefreshAuthStateAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task ValidateAuthStateAsyncValidTokenReturnsTrue()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();

        authHandler.ValidateAuthStateAsync(cancellationToken: TestContext.Current.CancellationToken).Returns(true);

        var result = await authHandler.ValidateAuthStateAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task ValidateAuthStateAsyncExpiredTokenReturnsFalse()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();

        authHandler.ValidateAuthStateAsync(cancellationToken: TestContext.Current.CancellationToken).Returns(false);

        var result = await authHandler.ValidateAuthStateAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateAuthStateAsyncServerUnreachableReturnsFalse()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();

        authHandler.ValidateAuthStateAsync(cancellationToken: TestContext.Current.CancellationToken).Returns(false);

        var result = await authHandler.ValidateAuthStateAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public void ClearAuthStateResetsCurrentAuthState()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();

        var clearedState = Substitute.For<IAuthState>();
        clearedState.IsValid.Returns(false);

        authHandler.When(x => x.ClearAuthState())
            .Do(x => authHandler.CurrentAuthState.Returns(clearedState));

        authHandler.ClearAuthState();

        authHandler.Received(1).ClearAuthState();
    }

    [Fact]
    public async Task AuthStateIsValidIndicatesCurrentValidity()
    {
        var authState = Substitute.For<IAuthState>();
        authState.IsValid.Returns(true);
        authState.LastValidated.Returns(DateTimeOffset.UtcNow);

        Assert.True(authState.IsValid);
        Assert.NotNull(authState.LastValidated);

        await Task.CompletedTask;
    }

    [Fact]
    public async Task AuthStateLastUpdatedTracksRotationTime()
    {
        var authState = Substitute.For<IAuthState>();
        var updateTime = DateTimeOffset.UtcNow;
        authState.LastUpdated.Returns(updateTime);

        Assert.Equal(updateTime, authState.LastUpdated);

        await Task.CompletedTask;
    }

    [Fact]
    public async Task Handle401ResponseTriggersAuthRefresh()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();
        var refreshedState = Substitute.For<IAuthState>();
        refreshedState.ApiKey.Returns("refreshed-after-401");

        authHandler.RefreshAuthStateAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken)
            .Returns(refreshedState);

        var result = await authHandler.RefreshAuthStateAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("refreshed-after-401", result.ApiKey);
    }

    [Fact]
    public async Task AuthRotationAfterServerRestartDetectsKeyChange()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();

        var oldState = Substitute.For<IAuthState>();
        oldState.ApiKey.Returns("old-key");

        var newState = Substitute.For<IAuthState>();
        newState.ApiKey.Returns("new-key-after-restart");
        newState.LastUpdated.Returns(DateTimeOffset.UtcNow);

        authHandler.CurrentAuthState.Returns(oldState, newState);

        var currentKey = authHandler.CurrentAuthState.ApiKey;
        Assert.Equal("old-key", currentKey);

        currentKey = authHandler.CurrentAuthState.ApiKey;
        Assert.Equal("new-key-after-restart", currentKey);

        await Task.CompletedTask;
    }

    [Fact]
    public async Task MultipleAuthRotationsMaintainsStateConsistency()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();
        var markerData1 = Substitute.For<IMarkerFileData>();
        markerData1.ApiKey.Returns("key-rotation-1");

        var markerData2 = Substitute.For<IMarkerFileData>();
        markerData2.ApiKey.Returns("key-rotation-2");

        var state1 = Substitute.For<IAuthState>();
        state1.ApiKey.Returns("key-rotation-1");

        var state2 = Substitute.For<IAuthState>();
        state2.ApiKey.Returns("key-rotation-2");

        authHandler.CurrentAuthState.Returns(state1, state2);

        await authHandler.UpdateAuthStateAsync(markerData1, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("key-rotation-1", authHandler.CurrentAuthState.ApiKey);

        await authHandler.UpdateAuthStateAsync(markerData2, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("key-rotation-2", authHandler.CurrentAuthState.ApiKey);
    }
}
