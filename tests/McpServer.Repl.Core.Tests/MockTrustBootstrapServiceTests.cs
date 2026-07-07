using McpServer.Repl.Core;
using NSubstitute;

namespace McpServer.Repl.Core.Tests;

public class MockTrustBootstrapServiceTests
{
    [Fact]
    public async Task PromptUserTrust_ValidNonce_ReturnsTrue()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var workspacePath = "/home/user/project";

        var markerData = CreateMarkerDataWithNonce(workspacePath, "valid-nonce-123");

        trustService.PromptUserTrustAsync(workspacePath, markerData, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(callInfo =>
            {
                var data = callInfo.Arg<IMarkerFileData>();
                if (data?.Metadata != null &&
                    data.Metadata.TryGetValue("nonce", out var nonce) &&
                    IsNonceValid(nonce?.ToString()))
                {
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            });

        var result = await trustService.PromptUserTrustAsync(workspacePath, markerData, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task PromptUserTrust_InvalidNonce_ReturnsFalse()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var workspacePath = "/home/user/project";

        var markerData = CreateMarkerDataWithNonce(workspacePath, "invalid-nonce");

        trustService.PromptUserTrustAsync(workspacePath, markerData, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(callInfo =>
            {
                var data = callInfo.Arg<IMarkerFileData>();
                if (data?.Metadata != null &&
                    data.Metadata.TryGetValue("nonce", out var nonce) &&
                    IsNonceValid(nonce?.ToString()))
                {
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            });

        var result = await trustService.PromptUserTrustAsync(workspacePath, markerData, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task PromptUserTrust_MissingNonce_ReturnsFalse()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var workspacePath = "/home/user/project";

        var markerData = CreateMarkerData(workspacePath, null);

        trustService.PromptUserTrustAsync(workspacePath, markerData, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(false);

        var result = await trustService.PromptUserTrustAsync(workspacePath, markerData, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task RecordTrustDecision_TrustedWorkspace_PersistsDecision()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var workspacePath = "/home/user/project";

        await trustService.RecordTrustDecisionAsync(workspacePath, true, cancellationToken: TestContext.Current.CancellationToken);

        await trustService.Received(1).RecordTrustDecisionAsync(workspacePath, true, cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task RecordTrustDecision_DeniedWorkspace_PersistsNegativeDecision()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var workspacePath = "/home/user/project";

        await trustService.RecordTrustDecisionAsync(workspacePath, false, cancellationToken: TestContext.Current.CancellationToken);

        await trustService.Received(1).RecordTrustDecisionAsync(workspacePath, false, cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetTrustDecision_TrustedWorkspace_ReturnsDecision()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var workspacePath = "/home/user/project";

        trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken)
            .Returns((true, true));

        var (hasDecision, isTrusted) = await trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(hasDecision);
        Assert.True(isTrusted);
    }

    [Fact]
    public async Task GetTrustDecision_DeniedWorkspace_ReturnsNegativeDecision()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var workspacePath = "/home/user/denied-project";

        trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken)
            .Returns((true, false));

        var (hasDecision, isTrusted) = await trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(hasDecision);
        Assert.False(isTrusted);
    }

    [Fact]
    public async Task GetTrustDecision_NewWorkspace_ReturnsNoDecision()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var workspacePath = "/home/user/new-project";

        trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken)
            .Returns((false, false));

        var (hasDecision, isTrusted) = await trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(hasDecision);
        Assert.False(isTrusted);
    }

    [Fact]
    public async Task RevokeTrust_TrustedWorkspace_RemovesFromRegistry()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var workspacePath = "/home/user/project";

        await trustService.RevokeTrustAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken);

        await trustService.Received(1).RevokeTrustAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken);

        trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken)
            .Returns((false, false));

        var (hasDecision, _) = await trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(hasDecision);
    }

    [Fact]
    public async Task ListTrustedWorkspaces_MultipleWorkspaces_ReturnsAll()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();

        var workspace1 = CreateTrustedWorkspace("/home/user/project1", "user_confirmed");
        var workspace2 = CreateTrustedWorkspace("/home/user/project2", "signature_verified");
        var workspace3 = CreateTrustedWorkspace("/home/user/project3", "user_confirmed");

        trustService.ListTrustedWorkspacesAsync(cancellationToken: TestContext.Current.CancellationToken)
            .Returns(new[] { workspace1, workspace2, workspace3 });

        var result = await trustService.ListTrustedWorkspacesAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains(result, w => w.WorkspacePath == "/home/user/project1");
        Assert.Contains(result, w => w.WorkspacePath == "/home/user/project2");
        Assert.Contains(result, w => w.WorkspacePath == "/home/user/project3");
    }

    [Fact]
    public async Task ListTrustedWorkspaces_EmptyRegistry_ReturnsEmpty()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();

        trustService.ListTrustedWorkspacesAsync(cancellationToken: TestContext.Current.CancellationToken)
            .Returns(Array.Empty<ITrustedWorkspace>());

        var result = await trustService.ListTrustedWorkspacesAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ClearAllTrust_RemovesAllDecisions()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();

        await trustService.ClearAllTrustAsync(cancellationToken: TestContext.Current.CancellationToken);

        await trustService.Received(1).ClearAllTrustAsync(cancellationToken: TestContext.Current.CancellationToken);

        trustService.ListTrustedWorkspacesAsync(cancellationToken: TestContext.Current.CancellationToken)
            .Returns(Array.Empty<ITrustedWorkspace>());

        var result = await trustService.ListTrustedWorkspacesAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Empty(result);
    }

    [Fact]
    public async Task NonceValidation_MultipleAttempts_OnlyAcceptsValidNonce()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var workspacePath = "/home/user/project";

        var validNonces = new HashSet<string> { "valid-nonce-123", "valid-nonce-456" };

        trustService.PromptUserTrustAsync(Arg.Any<string>(), Arg.Any<IMarkerFileData>(), cancellationToken: TestContext.Current.CancellationToken)
            .Returns(callInfo =>
            {
                var data = callInfo.Arg<IMarkerFileData>();
                if (data?.Metadata != null &&
                    data.Metadata.TryGetValue("nonce", out var nonce) &&
                    validNonces.Contains(nonce?.ToString() ?? ""))
                {
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            });

        var attempt1 = await trustService.PromptUserTrustAsync(workspacePath,
            CreateMarkerDataWithNonce(workspacePath, "invalid-nonce-1"), cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(attempt1);

        var attempt2 = await trustService.PromptUserTrustAsync(workspacePath,
            CreateMarkerDataWithNonce(workspacePath, "valid-nonce-123"), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(attempt2);

        var attempt3 = await trustService.PromptUserTrustAsync(workspacePath,
            CreateMarkerDataWithNonce(workspacePath, "invalid-nonce-2"), cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(attempt3);

        var attempt4 = await trustService.PromptUserTrustAsync(workspacePath,
            CreateMarkerDataWithNonce(workspacePath, "valid-nonce-456"), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(attempt4);
    }

    [Fact]
    public async Task TrustWorkflow_RecordAfterPrompt_MaintainsState()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var workspacePath = "/home/user/project";
        var markerData = CreateMarkerDataWithNonce(workspacePath, "valid-nonce-123");

        trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken)
            .Returns((false, false));

        trustService.PromptUserTrustAsync(workspacePath, markerData, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(true);

        var (initialHasDecision, _) = await trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(initialHasDecision);

        var prompted = await trustService.PromptUserTrustAsync(workspacePath, markerData, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(prompted);

        await trustService.RecordTrustDecisionAsync(workspacePath, true, cancellationToken: TestContext.Current.CancellationToken);

        trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken)
            .Returns((true, true));

        var (hasDecision, isTrusted) = await trustService.GetTrustDecisionAsync(workspacePath, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(hasDecision);
        Assert.True(isTrusted);
    }

    [Fact]
    public async Task TrustedWorkspace_IncludesMetadata()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();

        var workspace = CreateTrustedWorkspace("/home/user/project", "user_confirmed",
            new Dictionary<string, object?>
            {
                ["user"] = "john.doe",
                ["machine"] = "laptop-01",
                ["timestamp"] = DateTimeOffset.UtcNow
            });

        trustService.ListTrustedWorkspacesAsync(cancellationToken: TestContext.Current.CancellationToken)
            .Returns(new[] { workspace });

        var result = await trustService.ListTrustedWorkspacesAsync(cancellationToken: TestContext.Current.CancellationToken);
        var retrieved = result.First();

        Assert.NotNull(retrieved.Metadata);
        Assert.True(retrieved.Metadata!.ContainsKey("user"));
        Assert.Equal("john.doe", retrieved.Metadata["user"]);
    }

    private static IMarkerFileData CreateMarkerDataWithNonce(string workspacePath, string nonce)
    {
        return CreateMarkerData(workspacePath, new Dictionary<string, object?>
        {
            ["nonce"] = nonce
        });
    }

    private static IMarkerFileData CreateMarkerData(
        string workspacePath,
        IReadOnlyDictionary<string, object?>? metadata)
    {
        var data = Substitute.For<IMarkerFileData>();
        data.WorkspacePath.Returns(workspacePath);
        data.ServerUrl.Returns("http://localhost:5177");
        data.ApiKey.Returns("test-key-123");
        data.WorkspaceId.Returns(workspacePath);
        data.LastModified.Returns(DateTimeOffset.UtcNow);
        data.Metadata.Returns(metadata);
        return data;
    }

    private static ITrustedWorkspace CreateTrustedWorkspace(
        string workspacePath,
        string trustMethod,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        var workspace = Substitute.For<ITrustedWorkspace>();
        workspace.WorkspacePath.Returns(workspacePath);
        workspace.TrustedAt.Returns(DateTimeOffset.UtcNow);
        workspace.TrustMethod.Returns(trustMethod);
        workspace.Metadata.Returns(metadata);
        return workspace;
    }

    private static bool IsNonceValid(string? nonce)
    {
        return !string.IsNullOrWhiteSpace(nonce) && nonce.StartsWith("valid-nonce-");
    }
}
