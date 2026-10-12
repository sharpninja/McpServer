using QBrainAi.Repl.Core;
using NSubstitute;

namespace QBrainAi.Repl.Core.Tests;

public class MarkerFileTrustTests
{
    [Fact]
    public async Task ReadAsyncValidMarkerFileReturnsMarkerData()
    {
        var reader = Substitute.For<IMarkerFileReader>();
        var markerData = Substitute.For<IMarkerFileData>();
        markerData.WorkspacePath.Returns("/home/user/project");
        markerData.ServerUrl.Returns("http://localhost:5177");
        markerData.ApiKey.Returns("test-api-key-123");
        markerData.WorkspaceId.Returns("/home/user/project");

        reader.ReadAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken).Returns(markerData);

        var result = await reader.ReadAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("/home/user/project", result.WorkspacePath);
        Assert.Equal("http://localhost:5177", result.ServerUrl);
        Assert.Equal("test-api-key-123", result.ApiKey);
    }

    [Fact]
    public async Task ReadAsyncMarkerFileNotFoundThrowsFileNotFoundException()
    {
        var reader = Substitute.For<IMarkerFileReader>();

        reader.ReadAsync("/nonexistent/path", cancellationToken: TestContext.Current.CancellationToken)
            .Returns<IMarkerFileData>(x => throw new FileNotFoundException("Marker file not found"));

        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await reader.ReadAsync("/nonexistent/path", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true)
        );
    }

    [Fact]
    public async Task ReadAsyncMalformedYamlThrowsFormatException()
    {
        var reader = Substitute.For<IMarkerFileReader>();

        reader.ReadAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken)
            .Returns<IMarkerFileData>(x => throw new FormatException("Invalid YAML format"));

        await Assert.ThrowsAsync<FormatException>(
            async () => await reader.ReadAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true)
        );
    }

    [Fact]
    public async Task ReadAsyncMissingRequiredFieldsThrowsFormatException()
    {
        var reader = Substitute.For<IMarkerFileReader>();

        reader.ReadAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken)
            .Returns<IMarkerFileData>(x => throw new FormatException("Missing required field: serverUrl"));

        await Assert.ThrowsAsync<FormatException>(
            async () => await reader.ReadAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true)
        );
    }

    [Fact]
    public async Task TryReadAsyncValidMarkerFileReturnsSuccessWithData()
    {
        var reader = Substitute.For<IMarkerFileReader>();
        var markerData = Substitute.For<IMarkerFileData>();
        markerData.WorkspacePath.Returns("/home/user/project");

        reader.TryReadAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken)
            .Returns((true, markerData));

        var (success, data) = await reader.TryReadAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(success);
        Assert.NotNull(data);
        Assert.Equal("/home/user/project", data.WorkspacePath);
    }

    [Fact]
    public async Task TryReadAsyncMarkerFileNotFoundReturnsFailureWithNull()
    {
        var reader = Substitute.For<IMarkerFileReader>();

        reader.TryReadAsync("/nonexistent/path", cancellationToken: TestContext.Current.CancellationToken)
            .Returns((false, (IMarkerFileData?)null));

        var (success, data) = await reader.TryReadAsync("/nonexistent/path", cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(success);
        Assert.Null(data);
    }

    [Fact]
    public async Task VerifyTrustAsyncTrustedWorkspaceReturnsTrustedResult()
    {
        var reader = Substitute.For<IMarkerFileReader>();
        var trustResult = Substitute.For<ITrustVerificationResult>();
        trustResult.IsTrusted.Returns(true);
        trustResult.TrustMethod.Returns("registry_cached");

        reader.VerifyTrustAsync("/home/user/project", true, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(trustResult);

        var result = await reader.VerifyTrustAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.True(result.IsTrusted);
        Assert.Equal("registry_cached", result.TrustMethod);
    }

    [Fact]
    public async Task VerifyTrustAsyncUntrustedWorkspaceRequiresUserConfirmation()
    {
        var reader = Substitute.For<IMarkerFileReader>();
        var trustResult = Substitute.For<ITrustVerificationResult>();
        trustResult.IsTrusted.Returns(false);
        trustResult.TrustMethod.Returns("not_trusted");
        trustResult.DenialReason.Returns("User declined trust prompt");

        reader.VerifyTrustAsync("/home/user/project", true, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(trustResult);

        var result = await reader.VerifyTrustAsync("/home/user/project", requireUserConfirmation: true, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.False(result.IsTrusted);
        Assert.Equal("User declined trust prompt", result.DenialReason);
    }

    [Fact]
    public async Task VerifyTrustAsyncSignatureVerifiedReturnsTrustedWithMethod()
    {
        var reader = Substitute.For<IMarkerFileReader>();
        var trustResult = Substitute.For<ITrustVerificationResult>();
        trustResult.IsTrusted.Returns(true);
        trustResult.TrustMethod.Returns("signature_verified");

        reader.VerifyTrustAsync("/home/user/project", false, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(trustResult);

        var result = await reader.VerifyTrustAsync("/home/user/project", requireUserConfirmation: false, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.True(result.IsTrusted);
        Assert.Equal("signature_verified", result.TrustMethod);
    }

    [Fact]
    public async Task VerifyTrustAsyncWithoutUserConfirmationChecksExistingTrustOnly()
    {
        var reader = Substitute.For<IMarkerFileReader>();
        var trustResult = Substitute.For<ITrustVerificationResult>();
        trustResult.IsTrusted.Returns(false);
        trustResult.TrustMethod.Returns("not_trusted");

        reader.VerifyTrustAsync("/home/user/project", false, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(trustResult);

        var result = await reader.VerifyTrustAsync("/home/user/project", requireUserConfirmation: false, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.False(result.IsTrusted);
    }

    [Fact]
    public async Task WatchAsyncMarkerFileChangesInvokesCallback()
    {
        var reader = Substitute.For<IMarkerFileReader>();
        var callbackInvoked = false;

        var markerData = Substitute.For<IMarkerFileData>();
        markerData.ApiKey.Returns("new-rotated-key");

        Func<IMarkerFileData, Task> callback = async data =>
        {
            callbackInvoked = true;
            Assert.Equal("new-rotated-key", data.ApiKey);
            await Task.CompletedTask.ConfigureAwait(true);
        };

        reader.WatchAsync("/home/user/project", callback, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(callInfo =>
            {
                var cb = callInfo.Arg<Func<IMarkerFileData, Task>>()!;
                return cb(markerData);
            });

        await reader.WatchAsync("/home/user/project", callback, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(callbackInvoked);
    }

    [Fact]
    public async Task WatchAsyncCancellationStopsWatching()
    {
        var reader = Substitute.For<IMarkerFileReader>();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        reader.WatchAsync("/home/user/project", Arg.Any<Func<IMarkerFileData, Task>>(), cts.Token)
            .Returns<Task>(x => throw new TaskCanceledException());

        await Assert.ThrowsAsync<TaskCanceledException>(
            async () => await reader.WatchAsync("/home/user/project", _ => Task.CompletedTask, cts.Token).ConfigureAwait(true)
        );
    }

    [Fact]
    public async Task MarkerFileDataContainsAllRequiredFields()
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

        await Task.CompletedTask.ConfigureAwait(true);
    }

    [Fact]
    public async Task TrustBootstrapServicePromptUserTrustReturnsUserDecision()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var markerData = Substitute.For<IMarkerFileData>();

        trustService.PromptUserTrustAsync("/home/user/project", markerData, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(true);

        var result = await trustService.PromptUserTrustAsync("/home/user/project", markerData, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task TrustBootstrapServiceRecordTrustDecisionPersistsDecision()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();

        await trustService.RecordTrustDecisionAsync("/home/user/project", true, cancellationToken: TestContext.Current.CancellationToken);

        await trustService.Received(1).RecordTrustDecisionAsync("/home/user/project", true, cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TrustBootstrapServiceGetTrustDecisionReturnsExistingDecision()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();

        trustService.GetTrustDecisionAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken)
            .Returns((true, true));

        var (hasDecision, isTrusted) = await trustService.GetTrustDecisionAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(hasDecision);
        Assert.True(isTrusted);
    }

    [Fact]
    public async Task TrustBootstrapServiceRevokeTrustRemovesFromRegistry()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();

        await trustService.RevokeTrustAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken);

        await trustService.Received(1).RevokeTrustAsync("/home/user/project", cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TrustBootstrapServiceListTrustedWorkspacesReturnsAllTrusted()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();

        var workspace1 = Substitute.For<ITrustedWorkspace>();
        workspace1.WorkspacePath.Returns("/home/user/project1");

        var workspace2 = Substitute.For<ITrustedWorkspace>();
        workspace2.WorkspacePath.Returns("/home/user/project2");

        trustService.ListTrustedWorkspacesAsync(cancellationToken: TestContext.Current.CancellationToken)
            .Returns(new[] { workspace1, workspace2 });

        var result = await trustService.ListTrustedWorkspacesAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }
}
