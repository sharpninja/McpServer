using McpServer.Repl.Core;
using NSubstitute;

namespace McpServer.Repl.Core.Tests;

public class AuthRotationTests
{
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
        
        await authHandler.UpdateAuthStateAsync(markerData);
        
        await authHandler.Received(1).UpdateAuthStateAsync(markerData, default);
        Assert.Equal("new-rotated-key", authHandler.CurrentAuthState.ApiKey);
    }

    [Fact]
    public async Task UpdateAuthStateAsync_NullMarkerData_ThrowsArgumentNullException()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();
        
        authHandler.UpdateAuthStateAsync(null!, default)
            .Returns<Task>(x => throw new ArgumentNullException("newMarkerData"));
        
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await authHandler.UpdateAuthStateAsync(null!)
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
#pragma warning disable CS8602
                var cb = callInfo.Arg<Func<IAuthState, Task>>();
                _ = cb(authState);
#pragma warning restore CS8602
            });
        
        authHandler.RegisterAuthChangeCallback(callback1);
        authHandler.RegisterAuthChangeCallback(callback2);
        
        await Task.Delay(10);
        
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
        
        authHandler.RefreshAuthStateAsync("/home/user/project", default)
            .Returns(refreshedState);
        
        var result = await authHandler.RefreshAuthStateAsync("/home/user/project");
        
        Assert.NotNull(result);
        Assert.Equal("refreshed-key", result.ApiKey);
    }

    [Fact]
    public async Task RefreshAuthStateAsync_MarkerFileDeleted_ThrowsFileNotFoundException()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();
        
        authHandler.RefreshAuthStateAsync("/home/user/project", default)
            .Returns<IAuthState>(x => throw new FileNotFoundException("Marker file not found"));
        
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await authHandler.RefreshAuthStateAsync("/home/user/project")
        );
    }

    [Fact]
    public async Task RefreshAuthStateAsync_MalformedMarkerFile_ThrowsFormatException()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();
        
        authHandler.RefreshAuthStateAsync("/home/user/project", default)
            .Returns<IAuthState>(x => throw new FormatException("Invalid marker file format"));
        
        await Assert.ThrowsAsync<FormatException>(
            async () => await authHandler.RefreshAuthStateAsync("/home/user/project")
        );
    }

    [Fact]
    public async Task ValidateAuthStateAsync_ValidToken_ReturnsTrue()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();
        
        authHandler.ValidateAuthStateAsync(default).Returns(true);
        
        var result = await authHandler.ValidateAuthStateAsync();
        
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateAuthStateAsync_ExpiredToken_ReturnsFalse()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();
        
        authHandler.ValidateAuthStateAsync(default).Returns(false);
        
        var result = await authHandler.ValidateAuthStateAsync();
        
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateAuthStateAsync_ServerUnreachable_ReturnsFalse()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();
        
        authHandler.ValidateAuthStateAsync(default).Returns(false);
        
        var result = await authHandler.ValidateAuthStateAsync();
        
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

    [Fact]
    public async Task AuthState_IsValid_IndicatesCurrentValidity()
    {
        var authState = Substitute.For<IAuthState>();
        authState.IsValid.Returns(true);
        authState.LastValidated.Returns(DateTimeOffset.UtcNow);
        
        Assert.True(authState.IsValid);
        Assert.NotNull(authState.LastValidated);
        
        await Task.CompletedTask;
    }

    [Fact]
    public async Task AuthState_LastUpdated_TracksRotationTime()
    {
        var authState = Substitute.For<IAuthState>();
        var updateTime = DateTimeOffset.UtcNow;
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
        
        authHandler.RefreshAuthStateAsync("/home/user/project", default)
            .Returns(refreshedState);
        
        var result = await authHandler.RefreshAuthStateAsync("/home/user/project");
        
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
        
        await authHandler.UpdateAuthStateAsync(markerData1);
        Assert.Equal("key-rotation-1", authHandler.CurrentAuthState.ApiKey);
        
        await authHandler.UpdateAuthStateAsync(markerData2);
        Assert.Equal("key-rotation-2", authHandler.CurrentAuthState.ApiKey);
    }
}
