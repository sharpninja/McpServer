using McpServer.Repl.Core;
using NSubstitute;

namespace McpServer.Repl.Core.Tests;

public class StubMarkerFileReaderTests
{
    [Fact]
    public async Task ReadAsync_PreCannedTrustBootstrapPayload_ReturnsExpectedData()
    {
        var stub = new StubMarkerFileReader();

        var result = await stub.ReadAsync("/home/user/trusted-workspace", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("/home/user/trusted-workspace", result.WorkspacePath);
        Assert.Equal("http://localhost:5177", result.ServerUrl);
        Assert.Equal("trust-bootstrap-key-123", result.ApiKey);
        Assert.Equal("/home/user/trusted-workspace", result.WorkspaceId);
        Assert.NotNull(result.Metadata);
        Assert.Equal("nonce-challenge-abc", result.Metadata["nonce"]);
        Assert.Equal("sha256-signature-xyz", result.Metadata["signature"]);
    }

    [Fact]
    public async Task ReadAsync_UntrustedWorkspace_ReturnsDataWithoutTrustMetadata()
    {
        var stub = new StubMarkerFileReader();

        var result = await stub.ReadAsync("/home/user/untrusted-workspace", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("/home/user/untrusted-workspace", result.WorkspacePath);
        Assert.Equal("untrusted-key-456", result.ApiKey);
        Assert.True(result.Metadata == null || !result.Metadata.ContainsKey("signature"));
    }

    [Fact]
    public async Task ReadAsync_NonexistentPath_ThrowsFileNotFoundException()
    {
        var stub = new StubMarkerFileReader();

        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await stub.ReadAsync("/nonexistent/path", cancellationToken: TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task TryReadAsync_ValidWorkspace_ReturnsSuccessWithData()
    {
        var stub = new StubMarkerFileReader();

        var (success, data) = await stub.TryReadAsync("/home/user/trusted-workspace", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(success);
        Assert.NotNull(data);
        Assert.Equal("trust-bootstrap-key-123", data.ApiKey);
    }

    [Fact]
    public async Task TryReadAsync_NonexistentPath_ReturnsFailureWithNull()
    {
        var stub = new StubMarkerFileReader();

        var (success, data) = await stub.TryReadAsync("/nonexistent/path", cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(success);
        Assert.Null(data);
    }

    [Fact]
    public async Task VerifyTrustAsync_TrustedWorkspaceWithoutPrompt_ReturnsCachedTrust()
    {
        var stub = new StubMarkerFileReader();

        var result = await stub.VerifyTrustAsync("/home/user/trusted-workspace", requireUserConfirmation: false, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.True(result.IsTrusted);
        Assert.Equal("registry_cached", result.TrustMethod);
    }

    [Fact]
    public async Task VerifyTrustAsync_TrustedWorkspaceWithPrompt_ReturnsUserConfirmed()
    {
        var stub = new StubMarkerFileReader();

        var result = await stub.VerifyTrustAsync("/home/user/trusted-workspace", requireUserConfirmation: true, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.True(result.IsTrusted);
        Assert.Equal("user_confirmed", result.TrustMethod);
    }

    [Fact]
    public async Task VerifyTrustAsync_UntrustedWorkspaceWithoutPrompt_ReturnsNotTrusted()
    {
        var stub = new StubMarkerFileReader();

        var result = await stub.VerifyTrustAsync("/home/user/untrusted-workspace", requireUserConfirmation: false, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.False(result.IsTrusted);
        Assert.Equal("not_trusted", result.TrustMethod);
        Assert.NotNull(result.DenialReason);
    }

    [Fact]
    public async Task VerifyTrustAsync_SignatureVerifiedWorkspace_ReturnsSignatureVerified()
    {
        var stub = new StubMarkerFileReader();

        var result = await stub.VerifyTrustAsync("/home/user/signature-verified", requireUserConfirmation: false, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.True(result.IsTrusted);
        Assert.Equal("signature_verified", result.TrustMethod);
        Assert.NotNull(result.Details);
        Assert.True(result.Details.ContainsKey("signature"));
    }

    [Fact]
    public async Task WatchAsync_SimulatesMarkerFileChange_InvokesCallback()
    {
        var stub = new StubMarkerFileReader();
        var callback = new TaskCompletionSource<IMarkerFileData>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        Func<IMarkerFileData, Task> onChange = data =>
        {
            callback.TrySetResult(data);
            return Task.CompletedTask;
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var watchTask = stub.WatchAsync("/home/user/trusted-workspace", onChange, cts.Token);

        var capturedData = await callback.Task.WaitAsync(TimeSpan.FromSeconds(1), cancellationToken: TestContext.Current.CancellationToken);

        await cts.CancelAsync();

        await Assert.ThrowsAsync<TaskCanceledException>(async () => await watchTask);

        Assert.NotNull(capturedData);
        Assert.Contains("rotated", capturedData.ApiKey);
    }

    [Fact]
    public async Task PreCannedPayloads_CoverMultipleScenarios()
    {
        var stub = new StubMarkerFileReader();

        var trustedData = await stub.ReadAsync("/home/user/trusted-workspace", cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(trustedData.Metadata);
        Assert.True(trustedData.Metadata.ContainsKey("nonce"));

        var untrustedData = await stub.ReadAsync("/home/user/untrusted-workspace", cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(untrustedData.Metadata == null || !untrustedData.Metadata.ContainsKey("nonce"));

        var signatureData = await stub.ReadAsync("/home/user/signature-verified", cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(signatureData.Metadata);
        Assert.True(signatureData.Metadata.ContainsKey("signature"));
    }
}

internal sealed class StubMarkerFileReader : IMarkerFileReader
{
    private readonly Dictionary<string, IMarkerFileData> _preCannedData;

    public StubMarkerFileReader()
    {
        _preCannedData = new Dictionary<string, IMarkerFileData>
        {
            ["/home/user/trusted-workspace"] = CreateMarkerData(
                "/home/user/trusted-workspace",
                "trust-bootstrap-key-123",
                new Dictionary<string, object?>
                {
                    ["nonce"] = "nonce-challenge-abc",
                    ["signature"] = "sha256-signature-xyz",
                    ["trust_level"] = "user_confirmed"
                }),

            ["/home/user/untrusted-workspace"] = CreateMarkerData(
                "/home/user/untrusted-workspace",
                "untrusted-key-456",
                null),

            ["/home/user/signature-verified"] = CreateMarkerData(
                "/home/user/signature-verified",
                "verified-key-789",
                new Dictionary<string, object?>
                {
                    ["signature"] = "sha256-signature-valid",
                    ["public_key"] = "rsa-public-key-pem",
                    ["trust_level"] = "signature_verified"
                })
        };
    }

    public Task<IMarkerFileData> ReadAsync(string workspacePath, CancellationToken cancellationToken = default)
    {
        if (!_preCannedData.TryGetValue(workspacePath, out var data))
        {
            throw new FileNotFoundException($"Marker file not found for workspace: {workspacePath}");
        }

        return Task.FromResult(data);
    }

    public Task<(bool Success, IMarkerFileData? Data)> TryReadAsync(
        string workspacePath,
        CancellationToken cancellationToken = default)
    {
        if (_preCannedData.TryGetValue(workspacePath, out var data))
        {
            return Task.FromResult((true, (IMarkerFileData?)data));
        }

        return Task.FromResult((false, (IMarkerFileData?)null));
    }

    public Task<ITrustVerificationResult> VerifyTrustAsync(
        string workspacePath,
        bool requireUserConfirmation = true,
        CancellationToken cancellationToken = default)
    {
        ITrustVerificationResult result;

        if (workspacePath == "/home/user/trusted-workspace")
        {
            var trustMethod = requireUserConfirmation ? "user_confirmed" : "registry_cached";
            result = CreateTrustResult(true, trustMethod, null, new Dictionary<string, object?>
            {
                ["timestamp"] = DateTimeOffset.UtcNow,
                ["nonce_validated"] = true
            });
        }
        else if (workspacePath == "/home/user/signature-verified")
        {
            result = CreateTrustResult(true, "signature_verified", null, new Dictionary<string, object?>
            {
                ["signature"] = "sha256-signature-valid",
                ["verified_at"] = DateTimeOffset.UtcNow
            });
        }
        else
        {
            result = CreateTrustResult(false, "not_trusted", "Workspace not in trust registry", null);
        }

        return Task.FromResult(result);
    }

    public async Task WatchAsync(
        string workspacePath,
        Func<IMarkerFileData, Task> onChange,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);

        var rotatedData = CreateMarkerData(
            workspacePath,
            "rotated-key-" + Guid.NewGuid().ToString()[..8],
            new Dictionary<string, object?>
            {
                ["rotated_at"] = DateTimeOffset.UtcNow
            });

        await onChange(rotatedData);

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(100, cancellationToken);
        }

        throw new TaskCanceledException();
    }

    private static IMarkerFileData CreateMarkerData(
        string workspacePath,
        string apiKey,
        IReadOnlyDictionary<string, object?>? metadata)
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

    private static ITrustVerificationResult CreateTrustResult(
        bool isTrusted,
        string trustMethod,
        string? denialReason,
        IReadOnlyDictionary<string, object?>? details)
    {
        var result = Substitute.For<ITrustVerificationResult>();
        result.IsTrusted.Returns(isTrusted);
        result.TrustMethod.Returns(trustMethod);
        result.DenialReason.Returns(denialReason);
        result.Details.Returns(details);
        return result;
    }
}
