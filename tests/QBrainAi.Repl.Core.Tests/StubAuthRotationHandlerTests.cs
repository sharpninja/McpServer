using QBrainAi.Repl.Core;
using QBrainAi.Repl.Core.Tests.TestSupport;
using NSubstitute;

namespace QBrainAi.Repl.Core.Tests;

public class StubAuthRotationHandlerTests
{
    /// <summary>
    /// TR-MCP-USECASE-018 AC-006 (appended 2026-11-03): updating the auth state from new marker data takes the new key,
    /// stays valid and records the scripted update time exactly, from a test-owned clock (hostile validation round 10
    /// finding R10-F3: the case formerly compared LastUpdated with the live clock).
    /// </summary>
    [Fact]
    public async Task UpdateAuthState_NewMarkerData_TransitionsToNewState()
    {
        var created = new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);
        var updated = created.AddMinutes(5);
        var stub = new StubAuthRotationHandler("/home/user/project", "initial-key", new ScriptedClock(created, updated).Next);

        Assert.Equal("initial-key", stub.CurrentAuthState.ApiKey);
        Assert.True(stub.CurrentAuthState.IsValid);
        RecordedTimeContractTests.AssertRecordedTimes([created], [stub.CurrentAuthState.LastUpdated], "initial auth state");

        var newMarkerData = CreateMarkerData("/home/user/project", "updated-key");

        await stub.UpdateAuthStateAsync(newMarkerData, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("updated-key", stub.CurrentAuthState.ApiKey);
        Assert.True(stub.CurrentAuthState.IsValid);
        RecordedTimeContractTests.AssertRecordedTimes([updated], [stub.CurrentAuthState.LastUpdated], "updated auth state");
    }

    [Fact]
    public async Task UpdateAuthState_NullMarkerData_ThrowsArgumentNullException()
    {
        var stub = new StubAuthRotationHandler("/home/user/project", "initial-key");

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await stub.UpdateAuthStateAsync(null!, cancellationToken: TestContext.Current.CancellationToken)
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
        await stub.UpdateAuthStateAsync(newMarkerData, cancellationToken: TestContext.Current.CancellationToken);

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
        await stub.UpdateAuthStateAsync(newMarkerData, cancellationToken: TestContext.Current.CancellationToken);

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
        await stub.UpdateAuthStateAsync(newMarkerData, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(callback1Invoked);
        Assert.True(callback2Invoked);
    }

    [Fact]
    public async Task RefreshAuthState_ReReadsMarkerFile_ReturnsUpdatedState()
    {
        var stub = new StubAuthRotationHandler("/home/user/project", "initial-key");

        var refreshedState = await stub.RefreshAuthStateAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken);

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
            async () => await stub.RefreshAuthStateAsync("/nonexistent/workspace", cancellationToken: TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task ValidateAuthState_ValidToken_ReturnsTrue()
    {
        var stub = new StubAuthRotationHandler("/home/user/project", "valid-key");

        var isValid = await stub.ValidateAuthStateAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(isValid);
        Assert.NotNull(stub.CurrentAuthState.LastValidated);
    }

    [Fact]
    public async Task ValidateAuthState_ExpiredToken_ReturnsFalse()
    {
        var stub = new StubAuthRotationHandler("/home/user/project", "expired-key");

        var isValid = await stub.ValidateAuthStateAsync(cancellationToken: TestContext.Current.CancellationToken);

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
        await stub.UpdateAuthStateAsync(markerV2, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(stub.CurrentAuthState.IsValid);
        Assert.Equal("key-v2", stub.CurrentAuthState.ApiKey);

        var markerV3 = CreateMarkerData("/home/user/project", "key-v3");
        await stub.UpdateAuthStateAsync(markerV3, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(stub.CurrentAuthState.IsValid);
        Assert.Equal("key-v3", stub.CurrentAuthState.ApiKey);

        stub.ClearAuthState();
        Assert.False(stub.CurrentAuthState.IsValid);

        var refreshed = await stub.RefreshAuthStateAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(refreshed.IsValid);
        Assert.Contains("refreshed", refreshed.ApiKey);
    }

    [Fact]
    public async Task SimulateServerRestart_DetectsKeyChange()
    {
        var stub = new StubAuthRotationHandler("/home/user/project", "pre-restart-key");

        var isValid = await stub.ValidateAuthStateAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(isValid);

        var markerAfterRestart = CreateMarkerData("/home/user/project", "post-restart-key");
        await stub.UpdateAuthStateAsync(markerAfterRestart, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("post-restart-key", stub.CurrentAuthState.ApiKey);
        Assert.True(stub.CurrentAuthState.IsValid);
    }

    /// <summary>TR-MCP-USECASE-018 AC-006: scripted marker data for one workspace and key, with the fixed modification time <see cref="MarkerModified"/>.</summary>
    /// <param name="workspacePath">The workspace path, also used as the workspace id.</param>
    /// <param name="apiKey">The key the marker holds.</param>
    /// <returns>A substitute marker with a local server address.</returns>
    /// <remarks>2026-11-05, TR-MCP-USECASE-018 AC-006: this entry documents the helper, changed on 2026-11-03 to return the fixed modification time instead of the live clock (hostile validation round 10 finding R10-F3), because the tenth holistic read before hostile validation round 13 (finding d1; MEMORY-FACT-008) found it without XML documentation.</remarks>
    private static IMarkerFileData CreateMarkerData(string workspacePath, string apiKey)
    {
        var data = Substitute.For<IMarkerFileData>();
        data.WorkspacePath.Returns(workspacePath);
        data.ServerUrl.Returns("http://localhost:5177");
        data.ApiKey.Returns(apiKey);
        data.WorkspaceId.Returns(workspacePath);
        data.LastModified.Returns(MarkerModified);
        return data;
    }

    /// <summary>The fixed modification time of the scripted marker data (2026-11-03: no live clock in this unit class).</summary>
    private static readonly DateTimeOffset MarkerModified = new(2026, 1, 15, 8, 0, 0, TimeSpan.Zero);
}

/// <summary>
/// Test stub of the auth-rotation handler: keeps the auth state in memory and stamps each change with a test-owned clock.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-03</term><description>TR-MCP-USECASE-018 AC-006 (appended "A unit test that checks a recorded time ... takes the clock values from test-owned scripted inputs and asserts them exactly"): every recorded time comes from an injected clock (by default <see cref="ScriptedClock.Stepping"/> from a fixed instant) instead of DateTimeOffset.UtcNow, because hostile validation round 10 finding R10-F3 showed the update case compared LastUpdated with the live clock (operator 2026-11-03: "don't gate on me between rounds").</description></item>
/// </list>
/// </remarks>
internal sealed class StubAuthRotationHandler : IAuthRotationHandler
{
    private readonly List<Func<IAuthState, Task>> _callbacks = new();

    /// <summary>TR-MCP-USECASE-018 AC-006: the test-owned clock each auth state change records its time from.</summary>
    /// <remarks>2026-11-05, TR-MCP-USECASE-018 AC-006: this entry documents the field, added on 2026-11-03 with the injected clock (hostile validation round 10 finding R10-F3), because the ninth holistic read before hostile validation round 13 (finding e1; MEMORY-FACT-008) found it without XML documentation.</remarks>
    private readonly Func<DateTimeOffset> _clock;
    private IAuthState _currentAuthState;

    /// <summary>Creates the stub with an initial key and a test-owned clock.</summary>
    /// <param name="workspacePath">The workspace path.</param>
    /// <param name="initialApiKey">The initial key.</param>
    /// <param name="clock">Returns the time each state change records; by default a stepping clock from a fixed instant.</param>
    public StubAuthRotationHandler(string workspacePath, string initialApiKey, Func<DateTimeOffset>? clock = null)
    {
        _clock = clock ?? ScriptedClock.Stepping(new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero), TimeSpan.FromSeconds(1));
        _currentAuthState = CreateAuthState(workspacePath, initialApiKey, true, _clock());
    }

    public IAuthState CurrentAuthState => _currentAuthState;

    /// <inheritdoc />
    /// <remarks>Takes the marker's workspace and key as a valid state stamped from the test-owned clock, then runs every registered callback. 2026-11-05, TR-MCP-USECASE-018 AC-006: this entry documents the method, changed on 2026-11-03 to read the injected clock instead of DateTimeOffset.UtcNow (hostile validation round 10 finding R10-F3), because the tenth holistic read before hostile validation round 13 (finding d1; MEMORY-FACT-008) found it without XML documentation.</remarks>
    public async Task UpdateAuthStateAsync(IMarkerFileData newMarkerData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(newMarkerData);

        _currentAuthState = CreateAuthState(
            newMarkerData.WorkspacePath,
            newMarkerData.ApiKey,
            true,
            _clock());

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

    /// <inheritdoc />
    /// <remarks>Throws for a workspace outside /home/user/; otherwise takes a new random key, stamping its update and validation times from the test-owned clock. 2026-11-05, TR-MCP-USECASE-018 AC-006: this entry documents the method, changed on 2026-11-03 to read the injected clock instead of DateTimeOffset.UtcNow (hostile validation round 10 finding R10-F3), because the tenth holistic read before hostile validation round 13 (finding d1; MEMORY-FACT-008) found it without XML documentation.</remarks>
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
            _clock(),
            _clock());

        _currentAuthState = refreshedState;

        return Task.FromResult(refreshedState);
    }

    /// <inheritdoc />
    /// <remarks>Valid when the state is valid and its key is present and does not contain "expired"; a valid state keeps its update time and takes its validation time from the test-owned clock. 2026-11-05, TR-MCP-USECASE-018 AC-006: this entry documents the method, changed on 2026-11-03 to read the injected clock instead of DateTimeOffset.UtcNow (hostile validation round 10 finding R10-F3), because the tenth holistic read before hostile validation round 13 (finding d1; MEMORY-FACT-008) found it without XML documentation.</remarks>
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
                _clock());
            _currentAuthState = validated;
        }

        return Task.FromResult(isValid);
    }

    /// <inheritdoc />
    /// <remarks>Replaces the state with an invalid one with no key and no validation time, its update time from the test-owned clock. 2026-11-05, TR-MCP-USECASE-018 AC-006: this entry documents the method, changed on 2026-11-03 to read the injected clock instead of DateTimeOffset.UtcNow (hostile validation round 10 finding R10-F3), because the tenth holistic read before hostile validation round 13 (finding d1; MEMORY-FACT-008) found it without XML documentation.</remarks>
    public void ClearAuthState()
    {
        _currentAuthState = CreateAuthState(
            _currentAuthState.WorkspacePath,
            null!,
            false,
            _clock(),
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
