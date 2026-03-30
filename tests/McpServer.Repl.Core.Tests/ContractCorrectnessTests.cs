using McpServer.Repl.Core;
using NSubstitute;

namespace McpServer.Repl.Core.Tests;

public class ContractCorrectnessTests
{
    [Fact]
    public void IYamlSerializer_HasRequiredMethods()
    {
        var serializer = Substitute.For<IYamlSerializer>();

        var methods = typeof(IYamlSerializer).GetMethods();
        
        Assert.Contains(methods, m => m.Name == nameof(IYamlSerializer.Serialize));
        Assert.Contains(methods, m => m.Name == nameof(IYamlSerializer.Deserialize));
        Assert.Contains(methods, m => m.Name == nameof(IYamlSerializer.TryDeserialize));
        Assert.Contains(methods, m => m.Name == nameof(IYamlSerializer.SerializeStream));
        Assert.Contains(methods, m => m.Name == nameof(IYamlSerializer.DeserializeStream));

        Assert.Equal(5, methods.Length);
    }

    [Fact]
    public void IMarkerFileReader_HasRequiredMethods()
    {
        var reader = Substitute.For<IMarkerFileReader>();

        var methods = typeof(IMarkerFileReader).GetMethods();
        
        Assert.Contains(methods, m => m.Name == nameof(IMarkerFileReader.ReadAsync));
        Assert.Contains(methods, m => m.Name == nameof(IMarkerFileReader.TryReadAsync));
        Assert.Contains(methods, m => m.Name == nameof(IMarkerFileReader.VerifyTrustAsync));
        Assert.Contains(methods, m => m.Name == nameof(IMarkerFileReader.WatchAsync));

        Assert.Equal(4, methods.Length);
    }

    [Fact]
    public void ITrustBootstrapService_HasRequiredMethods()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();

        var methods = typeof(ITrustBootstrapService).GetMethods();
        
        Assert.Contains(methods, m => m.Name == nameof(ITrustBootstrapService.PromptUserTrustAsync));
        Assert.Contains(methods, m => m.Name == nameof(ITrustBootstrapService.RecordTrustDecisionAsync));
        Assert.Contains(methods, m => m.Name == nameof(ITrustBootstrapService.GetTrustDecisionAsync));
        Assert.Contains(methods, m => m.Name == nameof(ITrustBootstrapService.RevokeTrustAsync));
        Assert.Contains(methods, m => m.Name == nameof(ITrustBootstrapService.ListTrustedWorkspacesAsync));
        Assert.Contains(methods, m => m.Name == nameof(ITrustBootstrapService.ClearAllTrustAsync));

        Assert.Equal(6, methods.Length);
    }

    [Fact]
    public void IAuthRotationHandler_HasRequiredMethods()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();

        var methods = typeof(IAuthRotationHandler).GetMethods();
        var properties = typeof(IAuthRotationHandler).GetProperties();
        
        Assert.Contains(properties, p => p.Name == nameof(IAuthRotationHandler.CurrentAuthState));
        Assert.Contains(methods, m => m.Name == nameof(IAuthRotationHandler.UpdateAuthStateAsync));
        Assert.Contains(methods, m => m.Name == nameof(IAuthRotationHandler.RegisterAuthChangeCallback));
        Assert.Contains(methods, m => m.Name == nameof(IAuthRotationHandler.UnregisterAuthChangeCallback));
        Assert.Contains(methods, m => m.Name == nameof(IAuthRotationHandler.RefreshAuthStateAsync));
        Assert.Contains(methods, m => m.Name == nameof(IAuthRotationHandler.ValidateAuthStateAsync));
        Assert.Contains(methods, m => m.Name == nameof(IAuthRotationHandler.ClearAuthState));
    }

    [Fact]
    public void IMarkerFileData_HasRequiredProperties()
    {
        var markerData = Substitute.For<IMarkerFileData>();

        var properties = typeof(IMarkerFileData).GetProperties();
        
        Assert.Contains(properties, p => p.Name == nameof(IMarkerFileData.WorkspacePath));
        Assert.Contains(properties, p => p.Name == nameof(IMarkerFileData.ServerUrl));
        Assert.Contains(properties, p => p.Name == nameof(IMarkerFileData.ApiKey));
        Assert.Contains(properties, p => p.Name == nameof(IMarkerFileData.WorkspaceId));
        Assert.Contains(properties, p => p.Name == nameof(IMarkerFileData.AgentInstructions));
        Assert.Contains(properties, p => p.Name == nameof(IMarkerFileData.Metadata));
        Assert.Contains(properties, p => p.Name == nameof(IMarkerFileData.LastModified));

        Assert.Equal(7, properties.Length);
    }

    [Fact]
    public void ITrustVerificationResult_HasRequiredProperties()
    {
        var trustResult = Substitute.For<ITrustVerificationResult>();

        var properties = typeof(ITrustVerificationResult).GetProperties();
        
        Assert.Contains(properties, p => p.Name == nameof(ITrustVerificationResult.IsTrusted));
        Assert.Contains(properties, p => p.Name == nameof(ITrustVerificationResult.TrustMethod));
        Assert.Contains(properties, p => p.Name == nameof(ITrustVerificationResult.Details));
        Assert.Contains(properties, p => p.Name == nameof(ITrustVerificationResult.DenialReason));

        Assert.Equal(4, properties.Length);
    }

    [Fact]
    public void IAuthState_HasRequiredProperties()
    {
        var authState = Substitute.For<IAuthState>();

        var properties = typeof(IAuthState).GetProperties();
        
        Assert.Contains(properties, p => p.Name == nameof(IAuthState.WorkspacePath));
        Assert.Contains(properties, p => p.Name == nameof(IAuthState.ServerUrl));
        Assert.Contains(properties, p => p.Name == nameof(IAuthState.ApiKey));
        Assert.Contains(properties, p => p.Name == nameof(IAuthState.WorkspaceId));
        Assert.Contains(properties, p => p.Name == nameof(IAuthState.IsValid));
        Assert.Contains(properties, p => p.Name == nameof(IAuthState.LastUpdated));
        Assert.Contains(properties, p => p.Name == nameof(IAuthState.LastValidated));
        Assert.Contains(properties, p => p.Name == nameof(IAuthState.Metadata));

        Assert.Equal(8, properties.Length);
    }

    [Fact]
    public void ITrustedWorkspace_HasRequiredProperties()
    {
        var workspace = Substitute.For<ITrustedWorkspace>();

        var properties = typeof(ITrustedWorkspace).GetProperties();
        
        Assert.Contains(properties, p => p.Name == nameof(ITrustedWorkspace.WorkspacePath));
        Assert.Contains(properties, p => p.Name == nameof(ITrustedWorkspace.TrustedAt));
        Assert.Contains(properties, p => p.Name == nameof(ITrustedWorkspace.TrustMethod));
        Assert.Contains(properties, p => p.Name == nameof(ITrustedWorkspace.Metadata));

        Assert.Equal(4, properties.Length);
    }

    [Fact]
    public void IYamlEnvelope_HasRequiredProperties()
    {
        var envelope = Substitute.For<IYamlEnvelope>();

        var properties = typeof(IYamlEnvelope).GetProperties();
        
        Assert.Contains(properties, p => p.Name == nameof(IYamlEnvelope.Type));
        Assert.Contains(properties, p => p.Name == nameof(IYamlEnvelope.Payload));

        Assert.Equal(2, properties.Length);
    }

    [Fact]
    public void IHelloPayload_HasRequiredProperties()
    {
        var payload = Substitute.For<IHelloPayload>();

        var properties = typeof(IHelloPayload).GetProperties();
        
        Assert.Contains(properties, p => p.Name == nameof(IHelloPayload.ProtocolVersion));
        Assert.Contains(properties, p => p.Name == nameof(IHelloPayload.Capabilities));
        Assert.Contains(properties, p => p.Name == nameof(IHelloPayload.Metadata));

        Assert.Equal(3, properties.Length);
    }

    [Fact]
    public void IRequestPayload_HasRequiredProperties()
    {
        var payload = Substitute.For<IRequestPayload>();

        var properties = typeof(IRequestPayload).GetProperties();
        
        Assert.Contains(properties, p => p.Name == nameof(IRequestPayload.RequestId));
        Assert.Contains(properties, p => p.Name == nameof(IRequestPayload.Method));
        Assert.Contains(properties, p => p.Name == nameof(IRequestPayload.Params));

        Assert.Equal(3, properties.Length);
    }

    [Fact]
    public void IErrorPayload_HasRequiredProperties()
    {
        var payload = Substitute.For<IErrorPayload>();

        var properties = typeof(IErrorPayload).GetProperties();
        
        Assert.Contains(properties, p => p.Name == nameof(IErrorPayload.RequestId));
        Assert.Contains(properties, p => p.Name == nameof(IErrorPayload.Code));
        Assert.Contains(properties, p => p.Name == nameof(IErrorPayload.Message));
        Assert.Contains(properties, p => p.Name == nameof(IErrorPayload.Details));

        Assert.Equal(4, properties.Length);
    }

    [Fact]
    public async Task AllInterfaces_AreReferenceTypes()
    {
        Assert.True(typeof(IYamlSerializer).IsInterface);
        Assert.True(typeof(IMarkerFileReader).IsInterface);
        Assert.True(typeof(ITrustBootstrapService).IsInterface);
        Assert.True(typeof(IAuthRotationHandler).IsInterface);
        Assert.True(typeof(IMarkerFileData).IsInterface);
        Assert.True(typeof(ITrustVerificationResult).IsInterface);
        Assert.True(typeof(IAuthState).IsInterface);
        Assert.True(typeof(ITrustedWorkspace).IsInterface);
        Assert.True(typeof(IYamlEnvelope).IsInterface);
        Assert.True(typeof(IHelloPayload).IsInterface);
        Assert.True(typeof(IRequestPayload).IsInterface);
        Assert.True(typeof(IErrorPayload).IsInterface);

        await Task.CompletedTask;
    }

    [Fact]
    public async Task AllInterfaces_CanBeSubstituted()
    {
        var yamlSerializer = Substitute.For<IYamlSerializer>();
        Assert.NotNull(yamlSerializer);

        var markerReader = Substitute.For<IMarkerFileReader>();
        Assert.NotNull(markerReader);

        var trustService = Substitute.For<ITrustBootstrapService>();
        Assert.NotNull(trustService);

        var authHandler = Substitute.For<IAuthRotationHandler>();
        Assert.NotNull(authHandler);

        var markerData = Substitute.For<IMarkerFileData>();
        Assert.NotNull(markerData);

        var trustResult = Substitute.For<ITrustVerificationResult>();
        Assert.NotNull(trustResult);

        var authState = Substitute.For<IAuthState>();
        Assert.NotNull(authState);

        var trustedWorkspace = Substitute.For<ITrustedWorkspace>();
        Assert.NotNull(trustedWorkspace);

        var envelope = Substitute.For<IYamlEnvelope>();
        Assert.NotNull(envelope);

        await Task.CompletedTask;
    }

    [Fact]
    public async Task AsyncMethods_ReturnTasks()
    {
        var markerReader = Substitute.For<IMarkerFileReader>();

        var readMethod = typeof(IMarkerFileReader).GetMethod(nameof(IMarkerFileReader.ReadAsync));
        Assert.NotNull(readMethod);
        Assert.True(readMethod.ReturnType.IsGenericType);
        Assert.Equal(typeof(Task<>), readMethod.ReturnType.GetGenericTypeDefinition());

        var trustService = Substitute.For<ITrustBootstrapService>();

        var promptMethod = typeof(ITrustBootstrapService).GetMethod(nameof(ITrustBootstrapService.PromptUserTrustAsync));
        Assert.NotNull(promptMethod);
        Assert.True(promptMethod.ReturnType.IsGenericType);
        Assert.Equal(typeof(Task<>), promptMethod.ReturnType.GetGenericTypeDefinition());

        await Task.CompletedTask;
    }

    [Fact]
    public async Task CancellationTokenParameters_AreOptional()
    {
        var markerReader = Substitute.For<IMarkerFileReader>();

        var readMethod = typeof(IMarkerFileReader).GetMethod(nameof(IMarkerFileReader.ReadAsync));
        Assert.NotNull(readMethod);
        
        var parameters = readMethod.GetParameters();
        var cancellationTokenParam = parameters.FirstOrDefault(p => p.ParameterType == typeof(CancellationToken));
        
        Assert.NotNull(cancellationTokenParam);
        Assert.True(cancellationTokenParam.IsOptional || cancellationTokenParam.HasDefaultValue);

        await Task.CompletedTask;
    }

    [Fact]
    public void NullableProperties_AreCorrectlyMarked()
    {
        var markerDataProps = typeof(IMarkerFileData).GetProperties();
        var metadataProperty = markerDataProps.First(p => p.Name == nameof(IMarkerFileData.Metadata));
        
        var nullabilityContext = new System.Reflection.NullabilityInfoContext();
        var nullabilityInfo = nullabilityContext.Create(metadataProperty);
        
        Assert.Equal(System.Reflection.NullabilityState.Nullable, nullabilityInfo.ReadState);

        var trustResultProps = typeof(ITrustVerificationResult).GetProperties();
        var denialReasonProperty = trustResultProps.First(p => p.Name == nameof(ITrustVerificationResult.DenialReason));
        
        var denialReasonNullability = nullabilityContext.Create(denialReasonProperty);
        Assert.Equal(System.Reflection.NullabilityState.Nullable, denialReasonNullability.ReadState);
    }

    [Fact]
    public async Task MarkerFileData_RequiredPropertiesNotNullable()
    {
        var properties = typeof(IMarkerFileData).GetProperties();
        var nullabilityContext = new System.Reflection.NullabilityInfoContext();

        var workspacePathProp = properties.First(p => p.Name == nameof(IMarkerFileData.WorkspacePath));
        var workspacePathNullability = nullabilityContext.Create(workspacePathProp);
        Assert.Equal(System.Reflection.NullabilityState.NotNull, workspacePathNullability.ReadState);

        var apiKeyProp = properties.First(p => p.Name == nameof(IMarkerFileData.ApiKey));
        var apiKeyNullability = nullabilityContext.Create(apiKeyProp);
        Assert.Equal(System.Reflection.NullabilityState.NotNull, apiKeyNullability.ReadState);

        await Task.CompletedTask;
    }
}
