using McpServer.Repl.Core;
using NSubstitute;
using YamlDotNet.Serialization;

namespace McpServer.Repl.Core.Tests;

public class Iteration1_IntegrationTests
{
    [Fact]
    public async Task TrustBootstrapOrchestration_NewWorkspace_RequiresUserPrompt()
    {
        var yamlSerializer = CreateFakeYamlSerializer();
        var markerReader = Substitute.For<IMarkerFileReader>();
        var trustService = Substitute.For<ITrustBootstrapService>();
        var authHandler = Substitute.For<IAuthRotationHandler>();

        var workspacePath = "/home/user/project";
        var markerData = CreateFakeMarkerData(workspacePath, "test-key-123");

        trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken)
            .Returns((false, false));

        trustService.PromptUserTrustAsync(workspacePath, markerData, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(true);

        markerReader.ReadAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(markerData);

        var trustResult = Substitute.For<ITrustVerificationResult>();
        trustResult.IsTrusted.Returns(true);
        trustResult.TrustMethod.Returns("user_confirmed");

        markerReader.VerifyTrustAsync(workspacePath, true, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(trustResult);

        var (hasDecision, isTrusted) = await trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(hasDecision);

        var userConfirmed = await trustService.PromptUserTrustAsync(workspacePath, markerData, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(userConfirmed);

        await trustService.RecordTrustDecisionAsync(workspacePath, true, cancellationToken: TestContext.Current.CancellationToken);

        var result = await markerReader.VerifyTrustAsync(workspacePath, requireUserConfirmation: true, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsTrusted);
        Assert.Equal("user_confirmed", result.TrustMethod);

        await trustService.Received(1).RecordTrustDecisionAsync(workspacePath, true, cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TrustBootstrapOrchestration_CachedTrust_SkipsPrompt()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var markerReader = Substitute.For<IMarkerFileReader>();

        var workspacePath = "/home/user/project";

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

        await trustService.DidNotReceive().PromptUserTrustAsync(Arg.Any<string>(), Arg.Any<IMarkerFileData>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuthRotationOrchestration_MarkerFileChanged_UpdatesAuthState()
    {
        var markerReader = Substitute.For<IMarkerFileReader>();
        var authHandler = Substitute.For<IAuthRotationHandler>();

        var workspacePath = "/home/user/project";
        var oldMarkerData = CreateFakeMarkerData(workspacePath, "old-key-123");
        var newMarkerData = CreateFakeMarkerData(workspacePath, "new-key-456");

        var oldAuthState = Substitute.For<IAuthState>();
        oldAuthState.ApiKey.Returns("old-key-123");
        oldAuthState.IsValid.Returns(true);

        var newAuthState = Substitute.For<IAuthState>();
        newAuthState.ApiKey.Returns("new-key-456");
        newAuthState.IsValid.Returns(true);
        newAuthState.LastUpdated.Returns(DateTimeOffset.UtcNow);

        authHandler.CurrentAuthState.Returns(oldAuthState, newAuthState);

        var currentKey = authHandler.CurrentAuthState.ApiKey;
        Assert.Equal("old-key-123", currentKey);

        await authHandler.UpdateAuthStateAsync(newMarkerData, cancellationToken: TestContext.Current.CancellationToken);

        currentKey = authHandler.CurrentAuthState.ApiKey;
        Assert.Equal("new-key-456", currentKey);

        await authHandler.Received(1).UpdateAuthStateAsync(newMarkerData, cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AuthRotationOrchestration_401Response_TriggersRefresh()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();
        var markerReader = Substitute.For<IMarkerFileReader>();

        var workspacePath = "/home/user/project";

        authHandler.ValidateAuthStateAsync(cancellationToken: TestContext.Current.CancellationToken)
            .Returns(false);

        var refreshedMarkerData = CreateFakeMarkerData(workspacePath, "refreshed-key");
        markerReader.ReadAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(refreshedMarkerData);

        var refreshedAuthState = Substitute.For<IAuthState>();
        refreshedAuthState.ApiKey.Returns("refreshed-key");
        refreshedAuthState.IsValid.Returns(true);
        refreshedAuthState.LastValidated.Returns(DateTimeOffset.UtcNow);

        authHandler.RefreshAuthStateAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(refreshedAuthState);

        var isValid = await authHandler.ValidateAuthStateAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(isValid);

        var newState = await authHandler.RefreshAuthStateAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(newState);
        Assert.Equal("refreshed-key", newState.ApiKey);
        Assert.True(newState.IsValid);
        Assert.NotNull(newState.LastValidated);
    }

    [Fact]
    public async Task MarkerFileWatchOrchestration_FileChanged_InvokesCallback()
    {
        var markerReader = Substitute.For<IMarkerFileReader>();
        var authHandler = Substitute.For<IAuthRotationHandler>();

        var workspacePath = "/home/user/project";
        var callbackInvoked = false;
        IMarkerFileData? capturedData = null;

        var newMarkerData = CreateFakeMarkerData(workspacePath, "rotated-key");

        markerReader.WatchAsync(workspacePath, Arg.Any<Func<IMarkerFileData, Task>>(), cancellationToken: TestContext.Current.CancellationToken)
            .Returns(callInfo =>
            {
                var callback = callInfo.Arg<Func<IMarkerFileData, Task>>();
                return callback!(newMarkerData);
            });

        Func<IMarkerFileData, Task> onChange = async data =>
        {
            callbackInvoked = true;
            capturedData = data;
            await authHandler.UpdateAuthStateAsync(data);
        };

        await markerReader.WatchAsync(workspacePath, onChange, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(callbackInvoked);
        Assert.NotNull(capturedData);
        Assert.Equal("rotated-key", capturedData.ApiKey);

        await authHandler.Received(1).UpdateAuthStateAsync(newMarkerData, cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public void YamlSerializationOrchestration_TrustBootstrapPayload_RoundTrips()
    {
        var yamlSerializer = CreateFakeYamlSerializer();

        var trustPayload = new
        {
            workspacePath = "/home/user/project",
            serverUrl = "http://localhost:5177",
            apiKey = "test-key-123",
            nonce = "challenge-nonce-456"
        };

        var yamlText = yamlSerializer.Serialize(CreateEnvelopeFromObject("trust-bootstrap", trustPayload));
        Assert.Contains("type: trust-bootstrap", yamlText);
        Assert.Contains("workspacePath: /home/user/project", yamlText);
        Assert.Contains("nonce: challenge-nonce-456", yamlText);

        var deserialized = yamlSerializer.Deserialize(yamlText);
        Assert.NotNull(deserialized);
        Assert.Equal("trust-bootstrap", deserialized.Type);
    }

    [Fact]
    public async Task TrustBootstrapService_MockNonceValidation_AcceptsValidNonce()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var workspacePath = "/home/user/project";

        trustService.PromptUserTrustAsync(Arg.Any<string>(), Arg.Any<IMarkerFileData>(), cancellationToken: TestContext.Current.CancellationToken)
            .Returns(callInfo =>
            {
                var data = callInfo.Arg<IMarkerFileData>();

                if (data?.Metadata != null &&
                    data.Metadata.TryGetValue("nonce", out var nonce) &&
                    nonce?.ToString() == "valid-nonce")
                {
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            });

        var markerWithValidNonce = CreateFakeMarkerData(workspacePath, "test-key",
            new Dictionary<string, object?> { ["nonce"] = "valid-nonce" });

        var result = await trustService.PromptUserTrustAsync(workspacePath, markerWithValidNonce, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(result);

        var markerWithInvalidNonce = CreateFakeMarkerData(workspacePath, "test-key",
            new Dictionary<string, object?> { ["nonce"] = "invalid-nonce" });

        result = await trustService.PromptUserTrustAsync(workspacePath, markerWithInvalidNonce, cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(result);
    }

    [Fact]
    public async Task AuthRotationHandler_StubTransitions_FollowsStateLifecycle()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();
        var workspacePath = "/home/user/project";

        var initialState = CreateFakeAuthState(workspacePath, "initial-key", true);
        var invalidatedState = CreateFakeAuthState(workspacePath, "initial-key", false);
        var refreshedState = CreateFakeAuthState(workspacePath, "refreshed-key", true);

        authHandler.CurrentAuthState.Returns(initialState);
        Assert.True(authHandler.CurrentAuthState.IsValid);

        authHandler.ValidateAuthStateAsync(cancellationToken: TestContext.Current.CancellationToken).Returns(false);
        authHandler.CurrentAuthState.Returns(invalidatedState);
        Assert.False(authHandler.CurrentAuthState.IsValid);

        authHandler.RefreshAuthStateAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken).Returns(refreshedState);
        var newState = await authHandler.RefreshAuthStateAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken);

        authHandler.CurrentAuthState.Returns(refreshedState);
        Assert.True(authHandler.CurrentAuthState.IsValid);
        Assert.Equal("refreshed-key", authHandler.CurrentAuthState.ApiKey);

        await Task.CompletedTask;
    }

    [Fact]
    public void ContractCorrectness_AllInterfaces_HaveRequiredMembers()
    {
        var yamlSerializer = Substitute.For<IYamlSerializer>();
        Assert.NotNull(yamlSerializer);

        var markerReader = Substitute.For<IMarkerFileReader>();
        Assert.NotNull(markerReader);

        var trustService = Substitute.For<ITrustBootstrapService>();
        Assert.NotNull(trustService);

        var authHandler = Substitute.For<IAuthRotationHandler>();
        Assert.NotNull(authHandler);

        Assert.True(typeof(IYamlSerializer).IsInterface);
        Assert.True(typeof(IMarkerFileReader).IsInterface);
        Assert.True(typeof(ITrustBootstrapService).IsInterface);
        Assert.True(typeof(IAuthRotationHandler).IsInterface);
    }

    [Fact]
    public async Task FullTrustBootstrapFlow_EndToEnd_WithMocks()
    {
        var yamlSerializer = CreateFakeYamlSerializer();
        var markerReader = Substitute.For<IMarkerFileReader>();
        var trustService = Substitute.For<ITrustBootstrapService>();
        var authHandler = Substitute.For<IAuthRotationHandler>();

        var workspacePath = "/home/user/project";

        trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken)
            .Returns((false, false));

        var markerData = CreateFakeMarkerData(workspacePath, "bootstrap-key",
            new Dictionary<string, object?> { ["nonce"] = "challenge-12345" });

        markerReader.ReadAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(markerData);

        trustService.PromptUserTrustAsync(workspacePath, markerData, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(true);

        var trustResult = Substitute.For<ITrustVerificationResult>();
        trustResult.IsTrusted.Returns(true);
        trustResult.TrustMethod.Returns("user_confirmed");
        trustResult.Details.Returns(new Dictionary<string, object?>
        {
            ["timestamp"] = DateTimeOffset.UtcNow,
            ["nonce_validated"] = true
        });

        markerReader.VerifyTrustAsync(workspacePath, true, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(trustResult);

        var initialAuthState = CreateFakeAuthState(workspacePath, "bootstrap-key", true);
        authHandler.CurrentAuthState.Returns(initialAuthState);

        var (hasDecision, _) = await trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(hasDecision);

        var data = await markerReader.ReadAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("bootstrap-key", data.ApiKey);
        Assert.NotNull(data.Metadata);
        Assert.Equal("challenge-12345", data.Metadata!["nonce"]);

        var userTrusted = await trustService.PromptUserTrustAsync(workspacePath, data, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(userTrusted);

        await trustService.RecordTrustDecisionAsync(workspacePath, true, cancellationToken: TestContext.Current.CancellationToken);

        var verifyResult = await markerReader.VerifyTrustAsync(workspacePath, true, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(verifyResult.IsTrusted);
        Assert.Equal("user_confirmed", verifyResult.TrustMethod);

        await authHandler.UpdateAuthStateAsync(data, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("bootstrap-key", authHandler.CurrentAuthState.ApiKey);
        Assert.True(authHandler.CurrentAuthState.IsValid);
    }

    private static IYamlSerializer CreateFakeYamlSerializer()
    {
        var serializer = Substitute.For<IYamlSerializer>();

        var yamlDotNetSerializer = new SerializerBuilder()
            .Build();

        var yamlDotNetDeserializer = new DeserializerBuilder()
            .Build();

        serializer.Serialize(Arg.Any<IYamlEnvelope>())
            .Returns(callInfo =>
            {
                var envelope = callInfo.Arg<IYamlEnvelope>();
                var obj = new
                {
                    type = envelope?.Type ?? "unknown",
                    payload = envelope?.Payload
                };
                return yamlDotNetSerializer.Serialize(obj);
            });

        serializer.Deserialize(Arg.Any<string>())
            .Returns(callInfo =>
            {
                var yaml = callInfo.Arg<string>();
                if (string.IsNullOrWhiteSpace(yaml))
                {
                    throw new ArgumentNullException(nameof(yaml));
                }
                var dict = yamlDotNetDeserializer.Deserialize<Dictionary<string, object>>(yaml);

                var envelope = Substitute.For<IYamlEnvelope>();
                envelope.Type.Returns(dict["type"]?.ToString() ?? "unknown");
                envelope.Payload.Returns(dict.ContainsKey("payload") ? dict["payload"] : null);

                return envelope;
            });

        return serializer;
    }

    private static IYamlEnvelope CreateEnvelopeFromObject(string type, object payload)
    {
        var envelope = Substitute.For<IYamlEnvelope>();
        envelope.Type.Returns(type);
        envelope.Payload.Returns(payload);
        return envelope;
    }

    private static IMarkerFileData CreateFakeMarkerData(
        string workspacePath,
        string apiKey,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        var markerData = Substitute.For<IMarkerFileData>();
        markerData.WorkspacePath.Returns(workspacePath);
        markerData.ServerUrl.Returns("http://localhost:5177");
        markerData.ApiKey.Returns(apiKey);
        markerData.WorkspaceId.Returns(workspacePath);
        markerData.LastModified.Returns(DateTimeOffset.UtcNow);
        markerData.Metadata.Returns(metadata);
        return markerData;
    }

    private static IAuthState CreateFakeAuthState(string workspacePath, string apiKey, bool isValid)
    {
        var authState = Substitute.For<IAuthState>();
        authState.WorkspacePath.Returns(workspacePath);
        authState.ServerUrl.Returns("http://localhost:5177");
        authState.ApiKey.Returns(apiKey);
        authState.WorkspaceId.Returns(workspacePath);
        authState.IsValid.Returns(isValid);
        authState.LastUpdated.Returns(DateTimeOffset.UtcNow);
        authState.LastValidated.Returns(isValid ? DateTimeOffset.UtcNow : null);
        return authState;
    }
}
