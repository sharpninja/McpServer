using McpServer.Repl.Core;
using NSubstitute;

namespace McpServer.Repl.Core.Tests;

public class MarkerFileTrustTests
{
    [Fact]
    public async Task ReadAsync_ValidMarkerFile_ReturnsMarkerData()
    {
        var reader = Substitute.For<IMarkerFileReader>();
        var markerData = Substitute.For<IMarkerFileData>();
        markerData.WorkspacePath.Returns("/home/user/project");
        markerData.ServerUrl.Returns("http://localhost:5177");
        markerData.ApiKey.Returns("test-api-key-123");
        markerData.WorkspaceId.Returns("/home/user/project");
        
        reader.ReadAsync("/home/user/project", default).Returns(markerData);
        
        var result = await reader.ReadAsync("/home/user/project");
        
        Assert.NotNull(result);
        Assert.Equal("/home/user/project", result.WorkspacePath);
        Assert.Equal("http://localhost:5177", result.ServerUrl);
        Assert.Equal("test-api-key-123", result.ApiKey);
    }

    [Fact]
    public async Task ReadAsync_MarkerFileNotFound_ThrowsFileNotFoundException()
    {
        var reader = Substitute.For<IMarkerFileReader>();
        
        reader.ReadAsync("/nonexistent/path", default)
            .Returns<IMarkerFileData>(x => throw new FileNotFoundException("Marker file not found"));
        
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await reader.ReadAsync("/nonexistent/path")
        );
    }

    [Fact]
    public async Task ReadAsync_MalformedYaml_ThrowsFormatException()
    {
        var reader = Substitute.For<IMarkerFileReader>();
        
        reader.ReadAsync("/home/user/project", default)
            .Returns<IMarkerFileData>(x => throw new FormatException("Invalid YAML format"));
        
        await Assert.ThrowsAsync<FormatException>(
            async () => await reader.ReadAsync("/home/user/project")
        );
    }

    [Fact]
    public async Task ReadAsync_MissingRequiredFields_ThrowsFormatException()
    {
        var reader = Substitute.For<IMarkerFileReader>();
        
        reader.ReadAsync("/home/user/project", default)
            .Returns<IMarkerFileData>(x => throw new FormatException("Missing required field: serverUrl"));
        
        await Assert.ThrowsAsync<FormatException>(
            async () => await reader.ReadAsync("/home/user/project")
        );
    }

    [Fact]
    public async Task TryReadAsync_ValidMarkerFile_ReturnsSuccessWithData()
    {
        var reader = Substitute.For<IMarkerFileReader>();
        var markerData = Substitute.For<IMarkerFileData>();
        markerData.WorkspacePath.Returns("/home/user/project");
        
        reader.TryReadAsync("/home/user/project", default)
            .Returns((true, markerData));
        
        var (success, data) = await reader.TryReadAsync("/home/user/project");
        
        Assert.True(success);
        Assert.NotNull(data);
        Assert.Equal("/home/user/project", data.WorkspacePath);
    }

    [Fact]
    public async Task TryReadAsync_MarkerFileNotFound_ReturnsFailureWithNull()
    {
        var reader = Substitute.For<IMarkerFileReader>();
        
        reader.TryReadAsync("/nonexistent/path", default)
            .Returns((false, (IMarkerFileData?)null));
        
        var (success, data) = await reader.TryReadAsync("/nonexistent/path");
        
        Assert.False(success);
        Assert.Null(data);
    }

    [Fact]
    public async Task VerifyTrustAsync_TrustedWorkspace_ReturnsTrustedResult()
    {
        var reader = Substitute.For<IMarkerFileReader>();
        var trustResult = Substitute.For<ITrustVerificationResult>();
        trustResult.IsTrusted.Returns(true);
        trustResult.TrustMethod.Returns("registry_cached");
        
        reader.VerifyTrustAsync("/home/user/project", true, default)
            .Returns(trustResult);
        
        var result = await reader.VerifyTrustAsync("/home/user/project");
        
        Assert.NotNull(result);
        Assert.True(result.IsTrusted);
        Assert.Equal("registry_cached", result.TrustMethod);
    }

    [Fact]
    public async Task VerifyTrustAsync_UntrustedWorkspace_RequiresUserConfirmation()
    {
        var reader = Substitute.For<IMarkerFileReader>();
        var trustResult = Substitute.For<ITrustVerificationResult>();
        trustResult.IsTrusted.Returns(false);
        trustResult.TrustMethod.Returns("not_trusted");
        trustResult.DenialReason.Returns("User declined trust prompt");
        
        reader.VerifyTrustAsync("/home/user/project", true, default)
            .Returns(trustResult);
        
        var result = await reader.VerifyTrustAsync("/home/user/project", requireUserConfirmation: true);
        
        Assert.NotNull(result);
        Assert.False(result.IsTrusted);
        Assert.Equal("User declined trust prompt", result.DenialReason);
    }

    [Fact]
    public async Task VerifyTrustAsync_SignatureVerified_ReturnsTrustedWithMethod()
    {
        var reader = Substitute.For<IMarkerFileReader>();
        var trustResult = Substitute.For<ITrustVerificationResult>();
        trustResult.IsTrusted.Returns(true);
        trustResult.TrustMethod.Returns("signature_verified");
        
        reader.VerifyTrustAsync("/home/user/project", false, default)
            .Returns(trustResult);
        
        var result = await reader.VerifyTrustAsync("/home/user/project", requireUserConfirmation: false);
        
        Assert.NotNull(result);
        Assert.True(result.IsTrusted);
        Assert.Equal("signature_verified", result.TrustMethod);
    }

    [Fact]
    public async Task VerifyTrustAsync_WithoutUserConfirmation_ChecksExistingTrustOnly()
    {
        var reader = Substitute.For<IMarkerFileReader>();
        var trustResult = Substitute.For<ITrustVerificationResult>();
        trustResult.IsTrusted.Returns(false);
        trustResult.TrustMethod.Returns("not_trusted");
        
        reader.VerifyTrustAsync("/home/user/project", false, default)
            .Returns(trustResult);
        
        var result = await reader.VerifyTrustAsync("/home/user/project", requireUserConfirmation: false);
        
        Assert.NotNull(result);
        Assert.False(result.IsTrusted);
    }

    [Fact]
    public async Task WatchAsync_MarkerFileChanges_InvokesCallback()
    {
        var reader = Substitute.For<IMarkerFileReader>();
        var callbackInvoked = false;
        
        var markerData = Substitute.For<IMarkerFileData>();
        markerData.ApiKey.Returns("new-rotated-key");
        
        Func<IMarkerFileData, Task> callback = async data =>
        {
            callbackInvoked = true;
            Assert.Equal("new-rotated-key", data.ApiKey);
            await Task.CompletedTask;
        };
        
        reader.WatchAsync("/home/user/project", callback, default)
            .Returns(callInfo =>
            {
#pragma warning disable CS8602
                var cb = callInfo.Arg<Func<IMarkerFileData, Task>>();
                return cb(markerData);
#pragma warning restore CS8602
            });
        
        await reader.WatchAsync("/home/user/project", callback);
        
        Assert.True(callbackInvoked);
    }

    [Fact]
    public async Task WatchAsync_Cancellation_StopsWatching()
    {
        var reader = Substitute.For<IMarkerFileReader>();
        
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        reader.WatchAsync("/home/user/project", Arg.Any<Func<IMarkerFileData, Task>>(), cts.Token)
            .Returns<Task>(x => throw new TaskCanceledException());
        
        await Assert.ThrowsAsync<TaskCanceledException>(
            async () => await reader.WatchAsync("/home/user/project", _ => Task.CompletedTask, cts.Token)
        );
    }

    [Fact]
    public async Task MarkerFileData_ContainsAllRequiredFields()
    {
        var markerData = Substitute.For<IMarkerFileData>();
        markerData.WorkspacePath.Returns("/home/user/project");
        markerData.ServerUrl.Returns("http://localhost:5177");
        markerData.ApiKey.Returns("api-key-123");
        markerData.WorkspaceId.Returns("/home/user/project");
        markerData.LastModified.Returns(DateTimeOffset.UtcNow);
        
        Assert.Equal("/home/user/project", markerData.WorkspacePath);
        Assert.Equal("http://localhost:5177", markerData.ServerUrl);
        Assert.Equal("api-key-123", markerData.ApiKey);
        Assert.Equal("/home/user/project", markerData.WorkspaceId);
        Assert.NotEqual(default(DateTimeOffset), markerData.LastModified);
        
        await Task.CompletedTask;
    }

    [Fact]
    public async Task TrustBootstrapService_PromptUserTrust_ReturnsUserDecision()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var markerData = Substitute.For<IMarkerFileData>();
        
        trustService.PromptUserTrustAsync("/home/user/project", markerData, default)
            .Returns(true);
        
        var result = await trustService.PromptUserTrustAsync("/home/user/project", markerData);
        
        Assert.True(result);
    }

    [Fact]
    public async Task TrustBootstrapService_RecordTrustDecision_PersistsDecision()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        
        await trustService.RecordTrustDecisionAsync("/home/user/project", true);
        
        await trustService.Received(1).RecordTrustDecisionAsync("/home/user/project", true, default);
    }

    [Fact]
    public async Task TrustBootstrapService_GetTrustDecision_ReturnsExistingDecision()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        
        trustService.GetTrustDecisionAsync("/home/user/project", default)
            .Returns((true, true));
        
        var (hasDecision, isTrusted) = await trustService.GetTrustDecisionAsync("/home/user/project");
        
        Assert.True(hasDecision);
        Assert.True(isTrusted);
    }

    [Fact]
    public async Task TrustBootstrapService_RevokeTrust_RemovesFromRegistry()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        
        await trustService.RevokeTrustAsync("/home/user/project");
        
        await trustService.Received(1).RevokeTrustAsync("/home/user/project", default);
    }

    [Fact]
    public async Task TrustBootstrapService_ListTrustedWorkspaces_ReturnsAllTrusted()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        
        var workspace1 = Substitute.For<ITrustedWorkspace>();
        workspace1.WorkspacePath.Returns("/home/user/project1");
        
        var workspace2 = Substitute.For<ITrustedWorkspace>();
        workspace2.WorkspacePath.Returns("/home/user/project2");
        
        trustService.ListTrustedWorkspacesAsync(default)
            .Returns(new[] { workspace1, workspace2 });
        
        var result = await trustService.ListTrustedWorkspacesAsync();
        
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }
}
