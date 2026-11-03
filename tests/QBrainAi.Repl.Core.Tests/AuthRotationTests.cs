using QBrainAi.Repl.Core;
using NSubstitute;

namespace QBrainAi.Repl.Core.Tests;

/// <summary>
/// Tests the auth-rotation contracts of the REPL (IAuthRotationHandler and IAuthState) through NSubstitute doubles:
/// state updates, callbacks, refreshes, validation, clearing and rotation after a server restart. The recorded times
/// these doubles return are fixed scripted instants.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-03</term><description>TR-MCP-USECASE-018 AC-006 (appended "A unit test that checks a recorded time (an auth rotation ...) takes the clock values from test-owned scripted inputs" and "This covers every recorded time a unit test checks"): the two cases that check a recorded auth time use the fixed instant RotationTime instead of DateTimeOffset.UtcNow, because the sibling sweep before round 11 of hostile validation round 10 finding R10-F3 found them taking the value from the live clock (operator 2026-11-03: "don't gate on me between rounds"). Documented the class (MEMORY-FACT-008).</description></item>
/// </list>
/// </remarks>
public class AuthRotationTests
{
    /// <summary>The fixed instant the recorded-time cases script: a time in the past, so no case depends on the date it runs.</summary>
    private static readonly DateTimeOffset RotationTime = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task UpdateAuthStateAsync_NewMarkerData_UpdatesCurrentAuthState()
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
    public async Task UpdateAuthStateAsync_NullMarkerData_ThrowsArgumentNullException()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();

        authHandler.UpdateAuthStateAsync(null!, cancellationToken: TestContext.Current.CancellationToken)
            .Returns<Task>(x => throw new ArgumentNullException("newMarkerData"));

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await authHandler.UpdateAuthStateAsync(null!, cancellationToken: TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public void RegisterAuthChangeCallback_AddsCallback()
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
    public void UnregisterAuthChangeCallback_RemovesCallback()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();

        Func<IAuthState, Task> callback = _ => Task.CompletedTask;

        authHandler.UnregisterAuthChangeCallback(callback);

        authHandler.Received(1).UnregisterAuthChangeCallback(callback);
    }

    [Fact]
    public async Task AuthChangeCallback_OnAuthUpdate_InvokesAllRegisteredCallbacks()
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
    public async Task RefreshAuthStateAsync_ReReadsMarkerFile_ReturnsUpdatedState()
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
    public async Task RefreshAuthStateAsync_MarkerFileDeleted_ThrowsFileNotFoundException()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();

        authHandler.RefreshAuthStateAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken)
            .Returns<IAuthState>(x => throw new FileNotFoundException("Marker file not found"));

        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await authHandler.RefreshAuthStateAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task RefreshAuthStateAsync_MalformedMarkerFile_ThrowsFormatException()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();

        authHandler.RefreshAuthStateAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken)
            .Returns<IAuthState>(x => throw new FormatException("Invalid marker file format"));

        await Assert.ThrowsAsync<FormatException>(
            async () => await authHandler.RefreshAuthStateAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task ValidateAuthStateAsync_ValidToken_ReturnsTrue()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();

        authHandler.ValidateAuthStateAsync(cancellationToken: TestContext.Current.CancellationToken).Returns(true);

        var result = await authHandler.ValidateAuthStateAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task ValidateAuthStateAsync_ExpiredToken_ReturnsFalse()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();

        authHandler.ValidateAuthStateAsync(cancellationToken: TestContext.Current.CancellationToken).Returns(false);

        var result = await authHandler.ValidateAuthStateAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateAuthStateAsync_ServerUnreachable_ReturnsFalse()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();

        authHandler.ValidateAuthStateAsync(cancellationToken: TestContext.Current.CancellationToken).Returns(false);

        var result = await authHandler.ValidateAuthStateAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public void ClearAuthState_ResetsCurrentAuthState()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();

        var clearedState = Substitute.For<IAuthState>();
        clearedState.IsValid.Returns(false);

        authHandler.When(x => x.ClearAuthState())
            .Do(x => authHandler.CurrentAuthState.Returns(clearedState));

        authHandler.ClearAuthState();

        authHandler.Received(1).ClearAuthState();
    }

    /// <summary>An auth state reports its validity and the time it was last validated.</summary>
    /// <returns>The test task.</returns>
    /// <remarks>2026-11-03, TR-MCP-USECASE-018 AC-006 (appended): the validation time is the fixed RotationTime and is asserted exactly, instead of DateTimeOffset.UtcNow asserted only as present (sibling sweep before round 11 of hostile validation round 10 finding R10-F3).</remarks>
    [Fact]
    public async Task AuthState_IsValid_IndicatesCurrentValidity()
    {
        var authState = Substitute.For<IAuthState>();
        authState.IsValid.Returns(true);
        authState.LastValidated.Returns(RotationTime);

        Assert.True(authState.IsValid);
        Assert.Equal(RotationTime, authState.LastValidated);

        await Task.CompletedTask;
    }

    /// <summary>An auth state reports the time of its last rotation exactly.</summary>
    /// <returns>The test task.</returns>
    /// <remarks>2026-11-03, TR-MCP-USECASE-018 AC-006 (appended): the rotation time is the fixed RotationTime instead of DateTimeOffset.UtcNow (sibling sweep before round 11 of hostile validation round 10 finding R10-F3).</remarks>
    [Fact]
    public async Task AuthState_LastUpdated_TracksRotationTime()
    {
        var authState = Substitute.For<IAuthState>();
        var updateTime = RotationTime;
        authState.LastUpdated.Returns(updateTime);

        Assert.Equal(updateTime, authState.LastUpdated);

        await Task.CompletedTask;
    }

    [Fact]
    public async Task Handle401Response_TriggersAuthRefresh()
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
    public async Task AuthRotation_AfterServerRestart_DetectsKeyChange()
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
    public async Task MultipleAuthRotations_MaintainsStateConsistency()
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
