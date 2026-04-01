using McpServer.Repl.Core;
using NSubstitute;

namespace McpServer.Repl.Core.Tests;

public class StubAuthRotationHandlerTests
{
    [Fact]
    public async Task UpdateAuthState_NewMarkerData_TransitionsToNewState()
    {
        var stub = new StubAuthRotationHandler("/home/user/project", "initial-key");

        Assert.Equal("initial-key", stub.CurrentAuthState.ApiKey);
        Assert.True(stub.CurrentAuthState.IsValid);

        var newMarkerData = CreateMarkerData("/home/user/project", "updated-key");

        await stub.UpdateAuthStateAsync(newMarkerData);

        Assert.Equal("updated-key", stub.CurrentAuthState.ApiKey);
        Assert.True(stub.CurrentAuthState.IsValid);
        Assert.True(stub.CurrentAuthState.LastUpdated > DateTimeOffset.UtcNow.AddSeconds(-2));
    }

    [Fact]
    public async Task UpdateAuthState_NullMarkerData_ThrowsArgumentNullException()
    {
        var stub = new StubAuthRotationHandler("/home/user/project", "initial-key");

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await stub.UpdateAuthStateAsync(null!)
        );
    }

    [Fact]
    public async Task RegisterAuthChangeCallback_InvokesOnUpdate()
    {
        var stub = new StubAuthRotationHandler("/home/user/project", "initial-key");

        var callbackInvoked = false;
        IAuthState? capturedState = null;

        Func<IAuthState, Task> callback = async state =>
        {
            callbackInvoked = true;
            capturedState = state;
            await Task.CompletedTask;
        };

        stub.RegisterAuthChangeCallback(callback);

        var newMarkerData = CreateMarkerData("/home/user/project", "callback-key");
        await stub.UpdateAuthStateAsync(newMarkerData);

        Assert.True(callbackInvoked);
        Assert.NotNull(capturedState);
        Assert.Equal("callback-key", capturedState.ApiKey);
    }

    [Fact]
    public async Task RegisterAuthChangeCallback_MultipleCallbacks_AllInvoked()
    {
        var stub = new StubAuthRotationHandler("/home/user/project", "initial-key");

        var callback1Invoked = false;
        var callback2Invoked = false;
        var callback3Invoked = false;

        stub.RegisterAuthChangeCallback(async _ => { callback1Invoked = true; await Task.CompletedTask; });
        stub.RegisterAuthChangeCallback(async _ => { callback2Invoked = true; await Task.CompletedTask; });
        stub.RegisterAuthChangeCallback(async _ => { callback3Invoked = true; await Task.CompletedTask; });

        var newMarkerData = CreateMarkerData("/home/user/project", "multi-callback-key");
        await stub.UpdateAuthStateAsync(newMarkerData);

        Assert.True(callback1Invoked);
        Assert.True(callback2Invoked);
        Assert.True(callback3Invoked);
    }

    [Fact]
    public async Task UnregisterAuthChangeCallback_RemovesCallback()
    {
        var stub = new StubAuthRotationHandler("/home/user/project", "initial-key");

        var callback1Invoked = false;
        var callback2Invoked = false;

        Func<IAuthState, Task> callback1 = async _ => { callback1Invoked = true; await Task.CompletedTask; };
        Func<IAuthState, Task> callback2 = async _ => { callback2Invoked = true; await Task.CompletedTask; };

        stub.RegisterAuthChangeCallback(callback1);
        stub.RegisterAuthChangeCallback(callback2);

        stub.UnregisterAuthChangeCallback(callback1);

        var newMarkerData = CreateMarkerData("/home/user/project", "unregister-test-key");
        await stub.UpdateAuthStateAsync(newMarkerData);

        Assert.False(callback1Invoked);
        Assert.True(callback2Invoked);
    }

    [Fact]
    public async Task RefreshAuthState_ReReadsMarkerFile_ReturnsUpdatedState()
    {
        var stub = new StubAuthRotationHandler("/home/user/project", "initial-key");

        var refreshedState = await stub.RefreshAuthStateAsync("/home/user/project");

        Assert.NotNull(refreshedState);
        Assert.Contains("refreshed", refreshedState.ApiKey);
        Assert.True(refreshedState.IsValid);
        Assert.NotNull(refreshedState.LastValidated);
    }

    [Fact]
    public async Task RefreshAuthState_NonexistentWorkspace_ThrowsFileNotFoundException()
    {
        var stub = new StubAuthRotationHandler("/home/user/project", "initial-key");

        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await stub.RefreshAuthStateAsync("/nonexistent/workspace")
        );
    }

    [Fact]
    public async Task ValidateAuthState_ValidToken_ReturnsTrue()
    {
        var stub = new StubAuthRotationHandler("/home/user/project", "valid-key");

        var isValid = await stub.ValidateAuthStateAsync();

        Assert.True(isValid);
        Assert.NotNull(stub.CurrentAuthState.LastValidated);
    }

    [Fact]
    public async Task ValidateAuthState_ExpiredToken_ReturnsFalse()
    {
        var stub = new StubAuthRotationHandler("/home/user/project", "expired-key");

        var isValid = await stub.ValidateAuthStateAsync();

        Assert.False(isValid);
    }

    [Fact]
    public void ClearAuthState_InvalidatesCurrentState()
    {
        var stub = new StubAuthRotationHandler("/home/user/project", "initial-key");

        Assert.True(stub.CurrentAuthState.IsValid);

        stub.ClearAuthState();

        Assert.False(stub.CurrentAuthState.IsValid);
        Assert.Null(stub.CurrentAuthState.ApiKey);
    }

    [Fact]
    public async Task AuthStateTransitions_FollowLifecycle()
    {
        var stub = new StubAuthRotationHandler("/home/user/project", "key-v1");

        Assert.True(stub.CurrentAuthState.IsValid);
        Assert.Equal("key-v1", stub.CurrentAuthState.ApiKey);

        var markerV2 = CreateMarkerData("/home/user/project", "key-v2");
        await stub.UpdateAuthStateAsync(markerV2);

        Assert.True(stub.CurrentAuthState.IsValid);
        Assert.Equal("key-v2", stub.CurrentAuthState.ApiKey);

        var markerV3 = CreateMarkerData("/home/user/project", "key-v3");
        await stub.UpdateAuthStateAsync(markerV3);

        Assert.True(stub.CurrentAuthState.IsValid);
        Assert.Equal("key-v3", stub.CurrentAuthState.ApiKey);

        stub.ClearAuthState();
        Assert.False(stub.CurrentAuthState.IsValid);

        var refreshed = await stub.RefreshAuthStateAsync("/home/user/project");
        Assert.True(refreshed.IsValid);
        Assert.Contains("refreshed", refreshed.ApiKey);
    }

    [Fact]
    public async Task SimulateServerRestart_DetectsKeyChange()
    {
        var stub = new StubAuthRotationHandler("/home/user/project", "pre-restart-key");

        var isValid = await stub.ValidateAuthStateAsync();
        Assert.True(isValid);

        var markerAfterRestart = CreateMarkerData("/home/user/project", "post-restart-key");
        await stub.UpdateAuthStateAsync(markerAfterRestart);

        Assert.Equal("post-restart-key", stub.CurrentAuthState.ApiKey);
        Assert.True(stub.CurrentAuthState.IsValid);
    }

    private static IMarkerFileData CreateMarkerData(string workspacePath, string apiKey)
    {
        var data = Substitute.For<IMarkerFileData>();
        data.WorkspacePath.Returns(workspacePath);
        data.ServerUrl.Returns("http://localhost:5177");
        data.ApiKey.Returns(apiKey);
        data.WorkspaceId.Returns(workspacePath);
        data.LastModified.Returns(DateTimeOffset.UtcNow);
        return data;
    }
}

internal sealed class StubAuthRotationHandler : IAuthRotationHandler
{
    private readonly List<Func<IAuthState, Task>> _callbacks = new();
    private IAuthState _currentAuthState;

    public StubAuthRotationHandler(string workspacePath, string initialApiKey)
    {
        _currentAuthState = CreateAuthState(workspacePath, initialApiKey, true, DateTimeOffset.UtcNow);
    }

    public IAuthState CurrentAuthState => _currentAuthState;

    public async Task UpdateAuthStateAsync(IMarkerFileData newMarkerData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(newMarkerData);

        _currentAuthState = CreateAuthState(
            newMarkerData.WorkspacePath,
            newMarkerData.ApiKey,
            true,
            DateTimeOffset.UtcNow);

        foreach (var callback in _callbacks)
        {
            await callback(_currentAuthState);
        }
    }

    public void RegisterAuthChangeCallback(Func<IAuthState, Task> onAuthChanged)
    {
        ArgumentNullException.ThrowIfNull(onAuthChanged);
        _callbacks.Add(onAuthChanged);
    }

    public void UnregisterAuthChangeCallback(Func<IAuthState, Task> onAuthChanged)
    {
        ArgumentNullException.ThrowIfNull(onAuthChanged);
        _callbacks.Remove(onAuthChanged);
    }

    public Task<IAuthState> RefreshAuthStateAsync(string workspacePath, CancellationToken cancellationToken = default)
    {
        if (!workspacePath.StartsWith("/home/user/"))
        {
            throw new FileNotFoundException($"Marker file not found for workspace: {workspacePath}");
        }

        var refreshedState = CreateAuthState(
            workspacePath,
            $"refreshed-key-{Guid.NewGuid().ToString()[..8]}",
            true,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        _currentAuthState = refreshedState;

        return Task.FromResult(refreshedState);
    }

    public Task<bool> ValidateAuthStateAsync(CancellationToken cancellationToken = default)
    {
        var isValid = _currentAuthState.IsValid &&
                      !string.IsNullOrEmpty(_currentAuthState.ApiKey) &&
                      !_currentAuthState.ApiKey.Contains("expired");

        if (isValid)
        {
            var validated = CreateAuthState(
                _currentAuthState.WorkspacePath,
                _currentAuthState.ApiKey,
                true,
                _currentAuthState.LastUpdated,
                DateTimeOffset.UtcNow);
            _currentAuthState = validated;
        }

        return Task.FromResult(isValid);
    }

    public void ClearAuthState()
    {
        _currentAuthState = CreateAuthState(
            _currentAuthState.WorkspacePath,
            null!,
            false,
            DateTimeOffset.UtcNow,
            null);
    }

    private static IAuthState CreateAuthState(
        string workspacePath,
        string apiKey,
        bool isValid,
        DateTimeOffset lastUpdated,
        DateTimeOffset? lastValidated = null)
    {
        var state = Substitute.For<IAuthState>();
        state.WorkspacePath.Returns(workspacePath);
        state.ServerUrl.Returns("http://localhost:5177");
        state.ApiKey.Returns(apiKey);
        state.WorkspaceId.Returns(workspacePath);
        state.IsValid.Returns(isValid);
        state.LastUpdated.Returns(lastUpdated);
        state.LastValidated.Returns(lastValidated);
        return state;
    }
}
