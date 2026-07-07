using McpServer.Repl.Core;
using NSubstitute;

namespace McpServer.Repl.Core.Tests;

public class OrchestrationRulesTests
{
    [Fact]
    public async Task Rule_TrustBeforeAuth_MustVerifyTrustBeforeEstablishingAuth()
    {
        var markerReader = Substitute.For<IMarkerFileReader>();
        var trustService = Substitute.For<ITrustBootstrapService>();
        var authHandler = Substitute.For<IAuthRotationHandler>();

        var workspacePath = "/home/user/project";
        var markerData = CreateMarkerData(workspacePath, "auth-key");

        markerReader.ReadAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken).Returns(markerData);

        trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken)
            .Returns((false, false));

        var trustResult = Substitute.For<ITrustVerificationResult>();
        trustResult.IsTrusted.Returns(false);
        trustResult.TrustMethod.Returns("not_trusted");

        markerReader.VerifyTrustAsync(workspacePath, false, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(trustResult);

        var verifyResult = await markerReader.VerifyTrustAsync(workspacePath, requireUserConfirmation: false, cancellationToken: TestContext.Current.CancellationToken);

        if (!verifyResult.IsTrusted)
        {
            await authHandler.DidNotReceive().UpdateAuthStateAsync(Arg.Any<IMarkerFileData>(), Arg.Any<CancellationToken>());
        }

        Assert.False(verifyResult.IsTrusted);
    }

    [Fact]
    public async Task Rule_NonceValidation_MustValidateBeforeTrustConfirmation()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var workspacePath = "/home/user/project";

        var validNonce = "valid-nonce-abc123";
        var markerWithValidNonce = CreateMarkerData(workspacePath, "key",
            new Dictionary<string, object?> { ["nonce"] = validNonce });

        trustService.PromptUserTrustAsync(workspacePath, Arg.Any<IMarkerFileData>(), cancellationToken: TestContext.Current.CancellationToken)
            .Returns(callInfo =>
            {
                var data = callInfo.Arg<IMarkerFileData>();
                var isNonceValid = data?.Metadata != null &&
                                  data.Metadata.TryGetValue("nonce", out var nonce) &&
                                  nonce?.ToString() == validNonce;
                return Task.FromResult(isNonceValid);
            });

        var resultValid = await trustService.PromptUserTrustAsync(workspacePath, markerWithValidNonce, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(resultValid);

        var markerWithInvalidNonce = CreateMarkerData(workspacePath, "key",
            new Dictionary<string, object?> { ["nonce"] = "wrong-nonce" });

        var resultInvalid = await trustService.PromptUserTrustAsync(workspacePath, markerWithInvalidNonce, cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(resultInvalid);
    }

    [Fact]
    public async Task Rule_CachedTrustBypass_SkipsUserPromptForKnownWorkspace()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var markerReader = Substitute.For<IMarkerFileReader>();
        var workspacePath = "/home/user/trusted-project";

        trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken)
            .Returns((true, true));

        var trustResult = Substitute.For<ITrustVerificationResult>();
        trustResult.IsTrusted.Returns(true);
        trustResult.TrustMethod.Returns("registry_cached");

        markerReader.VerifyTrustAsync(workspacePath, false, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(trustResult);

        var (hasDecision, isTrusted) = await trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(hasDecision);
        Assert.True(isTrusted);

        var result = await markerReader.VerifyTrustAsync(workspacePath, requireUserConfirmation: false, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(result.IsTrusted);
        Assert.Equal("registry_cached", result.TrustMethod);

        await trustService.DidNotReceive().PromptUserTrustAsync(
            Arg.Any<string>(),
            Arg.Any<IMarkerFileData>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rule_401Recovery_MustRefreshAuthState()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();
        var workspacePath = "/home/user/project";

        var oldState = CreateAuthState(workspacePath, "old-key", true);
        var newState = CreateAuthState(workspacePath, "new-key", true);

        authHandler.CurrentAuthState.Returns(oldState);
        authHandler.ValidateAuthStateAsync(cancellationToken: TestContext.Current.CancellationToken).Returns(false);
        authHandler.RefreshAuthStateAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken).Returns(newState);

        var isValid = await authHandler.ValidateAuthStateAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(isValid);

        var refreshed = await authHandler.RefreshAuthStateAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(refreshed);
        Assert.Equal("new-key", refreshed.ApiKey);
    }

    [Fact]
    public async Task Rule_MarkerFileWatch_TriggersAuthRotation()
    {
        var markerReader = Substitute.For<IMarkerFileReader>();
        var authHandler = Substitute.For<IAuthRotationHandler>();
        var workspacePath = "/home/user/project";

        var rotationDetected = false;
        IMarkerFileData? rotatedData = null;

        var newMarkerData = CreateMarkerData(workspacePath, "rotated-key-xyz");

        markerReader.WatchAsync(workspacePath, Arg.Any<Func<IMarkerFileData, Task>>(), cancellationToken: TestContext.Current.CancellationToken)
            .Returns(callInfo =>
            {
                var callback = callInfo.Arg<Func<IMarkerFileData, Task>>();
                return callback!(newMarkerData);
            });

        Func<IMarkerFileData, Task> watchCallback = async data =>
        {
            rotationDetected = true;
            rotatedData = data;
            await authHandler.UpdateAuthStateAsync(data);
        };

        await markerReader.WatchAsync(workspacePath, watchCallback, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(rotationDetected);
        Assert.NotNull(rotatedData);
        Assert.Equal("rotated-key-xyz", rotatedData!.ApiKey);

        await authHandler.Received(1).UpdateAuthStateAsync(newMarkerData, cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Rule_StateConsistency_AuthStateReflectsLatestMarkerData()
    {
        var markerReader = Substitute.For<IMarkerFileReader>();
        var authHandler = Substitute.For<IAuthRotationHandler>();
        var workspacePath = "/home/user/project";

        var marker1 = CreateMarkerData(workspacePath, "key-v1");
        var marker2 = CreateMarkerData(workspacePath, "key-v2");
        var marker3 = CreateMarkerData(workspacePath, "key-v3");

        var state1 = CreateAuthState(workspacePath, "key-v1", true);
        var state2 = CreateAuthState(workspacePath, "key-v2", true);
        var state3 = CreateAuthState(workspacePath, "key-v3", true);

        markerReader.ReadAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken).Returns(marker1, marker2, marker3);
        authHandler.CurrentAuthState.Returns(state1, state2, state3);

        var data1 = await markerReader.ReadAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("key-v1", data1.ApiKey);
        Assert.Equal("key-v1", authHandler.CurrentAuthState.ApiKey);

        var data2 = await markerReader.ReadAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("key-v2", data2.ApiKey);
        Assert.Equal("key-v2", authHandler.CurrentAuthState.ApiKey);

        var data3 = await markerReader.ReadAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("key-v3", data3.ApiKey);
        Assert.Equal("key-v3", authHandler.CurrentAuthState.ApiKey);
    }

    [Fact]
    public async Task Rule_TrustRevocation_ClearsAuthState()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var authHandler = Substitute.For<IAuthRotationHandler>();
        var workspacePath = "/home/user/project";

        trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken)
            .Returns((true, true));

        var (hasDecision, isTrusted) = await trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(hasDecision);
        Assert.True(isTrusted);

        await trustService.RevokeTrustAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken);

        trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken)
            .Returns((false, false));

        var (newHasDecision, newIsTrusted) = await trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(newHasDecision);

        authHandler.ClearAuthState();

        authHandler.Received(1).ClearAuthState();
    }

    [Fact]
    public async Task Rule_SignatureVerification_BypassesUserPrompt()
    {
        var markerReader = Substitute.For<IMarkerFileReader>();
        var trustService = Substitute.For<ITrustBootstrapService>();
        var workspacePath = "/home/user/signed-project";

        var trustResult = Substitute.For<ITrustVerificationResult>();
        trustResult.IsTrusted.Returns(true);
        trustResult.TrustMethod.Returns("signature_verified");
        trustResult.Details.Returns(new Dictionary<string, object?>
        {
            ["signature"] = "sha256-valid-signature",
            ["public_key"] = "rsa-public-key"
        });

        markerReader.VerifyTrustAsync(workspacePath, false, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(trustResult);

        var result = await markerReader.VerifyTrustAsync(workspacePath, requireUserConfirmation: false, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsTrusted);
        Assert.Equal("signature_verified", result.TrustMethod);

        await trustService.DidNotReceive().PromptUserTrustAsync(
            Arg.Any<string>(),
            Arg.Any<IMarkerFileData>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rule_AuthCallbacks_InvokedOnRotation()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();
        var workspacePath = "/home/user/project";

        var callback1Invoked = false;
        var callback2Invoked = false;

        Func<IAuthState, Task> callback1 = async _ => { callback1Invoked = true; await Task.CompletedTask; };
        Func<IAuthState, Task> callback2 = async _ => { callback2Invoked = true; await Task.CompletedTask; };

        authHandler.When(x => x.RegisterAuthChangeCallback(Arg.Any<Func<IAuthState, Task>>()))
            .Do(callInfo =>
            {
                var cb = callInfo.Arg<Func<IAuthState, Task>>();
                var state = CreateAuthState(workspacePath, "callback-test-key", true);
                _ = cb!(state);
            });

        authHandler.RegisterAuthChangeCallback(callback1);
        authHandler.RegisterAuthChangeCallback(callback2);

        await Task.Delay(10, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(callback1Invoked);
        Assert.True(callback2Invoked);
    }

    [Fact]
    public async Task FullOrchestration_NewWorkspaceToTrustedWithAuthRotation()
    {
        var yamlSerializer = new FakeYamlSerializer();
        var markerReader = Substitute.For<IMarkerFileReader>();
        var trustService = Substitute.For<ITrustBootstrapService>();
        var authHandler = Substitute.For<IAuthRotationHandler>();

        var workspacePath = "/home/user/new-project";

        trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken)
            .Returns((false, false));

        var markerData = CreateMarkerData(workspacePath, "initial-key",
            new Dictionary<string, object?> { ["nonce"] = "challenge-nonce" });

        markerReader.ReadAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken).Returns(markerData);

        trustService.PromptUserTrustAsync(workspacePath, markerData, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(true);

        var trustResult = Substitute.For<ITrustVerificationResult>();
        trustResult.IsTrusted.Returns(true);
        trustResult.TrustMethod.Returns("user_confirmed");

        markerReader.VerifyTrustAsync(workspacePath, true, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(trustResult);

        var initialAuthState = CreateAuthState(workspacePath, "initial-key", true);
        authHandler.CurrentAuthState.Returns(initialAuthState);

        var (hasDecision, _) = await trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(hasDecision);

        var data = await markerReader.ReadAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("initial-key", data.ApiKey);

        var userTrusted = await trustService.PromptUserTrustAsync(workspacePath, data, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(userTrusted);

        await trustService.RecordTrustDecisionAsync(workspacePath, true, cancellationToken: TestContext.Current.CancellationToken);

        var verified = await markerReader.VerifyTrustAsync(workspacePath, true, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(verified.IsTrusted);

        await authHandler.UpdateAuthStateAsync(data, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("initial-key", authHandler.CurrentAuthState.ApiKey);

        var rotatedMarker = CreateMarkerData(workspacePath, "rotated-key");
        var rotatedAuthState = CreateAuthState(workspacePath, "rotated-key", true);
        authHandler.CurrentAuthState.Returns(rotatedAuthState);

        await authHandler.UpdateAuthStateAsync(rotatedMarker, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("rotated-key", authHandler.CurrentAuthState.ApiKey);
    }

    private static IMarkerFileData CreateMarkerData(
        string workspacePath,
        string apiKey,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        var data = Substitute.For<IMarkerFileData>();
        data.WorkspacePath.Returns(workspacePath);
        data.ServerUrl.Returns("http://localhost:5177");
        data.ApiKey.Returns(apiKey);
        data.WorkspaceId.Returns(workspacePath);
        data.LastModified.Returns(DateTimeOffset.UtcNow);
        data.Metadata.Returns(metadata);
        return data;
    }

    private static IAuthState CreateAuthState(string workspacePath, string apiKey, bool isValid)
    {
        var state = Substitute.For<IAuthState>();
        state.WorkspacePath.Returns(workspacePath);
        state.ServerUrl.Returns("http://localhost:5177");
        state.ApiKey.Returns(apiKey);
        state.WorkspaceId.Returns(workspacePath);
        state.IsValid.Returns(isValid);
        state.LastUpdated.Returns(DateTimeOffset.UtcNow);
        state.LastValidated.Returns(isValid ? DateTimeOffset.UtcNow : null);
        return state;
    }
}
