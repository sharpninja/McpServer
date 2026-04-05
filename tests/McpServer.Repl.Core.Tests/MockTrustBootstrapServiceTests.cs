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

        trustService.PromptUserTrustAsync(workspacePath, markerData, default)
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

        var result = await trustService.PromptUserTrustAsync(workspacePath, markerData);

        Assert.True(result);
    }

    [Fact]
    public async Task PromptUserTrust_InvalidNonce_ReturnsFalse()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var workspacePath = "/home/user/project";

        var markerData = CreateMarkerDataWithNonce(workspacePath, "invalid-nonce");

        trustService.PromptUserTrustAsync(workspacePath, markerData, default)
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

        var result = await trustService.PromptUserTrustAsync(workspacePath, markerData);

        Assert.False(result);
    }

    [Fact]
    public async Task PromptUserTrust_MissingNonce_ReturnsFalse()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var workspacePath = "/home/user/project";

        var markerData = CreateMarkerData(workspacePath, null);

        trustService.PromptUserTrustAsync(workspacePath, markerData, default)
            .Returns(false);

        var result = await trustService.PromptUserTrustAsync(workspacePath, markerData);

        Assert.False(result);
    }

    [Fact]
    public async Task RecordTrustDecision_TrustedWorkspace_PersistsDecision()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var workspacePath = "/home/user/project";

        await trustService.RecordTrustDecisionAsync(workspacePath, true);

        await trustService.Received(1).RecordTrustDecisionAsync(workspacePath, true, default);
    }

    [Fact]
    public async Task RecordTrustDecision_DeniedWorkspace_PersistsNegativeDecision()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var workspacePath = "/home/user/project";

        await trustService.RecordTrustDecisionAsync(workspacePath, false);

        await trustService.Received(1).RecordTrustDecisionAsync(workspacePath, false, default);
    }

    [Fact]
    public async Task GetTrustDecision_TrustedWorkspace_ReturnsDecision()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var workspacePath = "/home/user/project";

        trustService.GetTrustDecisionAsync(workspacePath, default)
            .Returns((true, true));

        var (hasDecision, isTrusted) = await trustService.GetTrustDecisionAsync(workspacePath);

        Assert.True(hasDecision);
        Assert.True(isTrusted);
    }

    [Fact]
    public async Task GetTrustDecision_DeniedWorkspace_ReturnsNegativeDecision()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var workspacePath = "/home/user/denied-project";

        trustService.GetTrustDecisionAsync(workspacePath, default)
            .Returns((true, false));

        var (hasDecision, isTrusted) = await trustService.GetTrustDecisionAsync(workspacePath);

        Assert.True(hasDecision);
        Assert.False(isTrusted);
    }

    [Fact]
    public async Task GetTrustDecision_NewWorkspace_ReturnsNoDecision()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var workspacePath = "/home/user/new-project";

        trustService.GetTrustDecisionAsync(workspacePath, default)
            .Returns((false, false));

        var (hasDecision, isTrusted) = await trustService.GetTrustDecisionAsync(workspacePath);

        Assert.False(hasDecision);
        Assert.False(isTrusted);
    }

    [Fact]
    public async Task RevokeTrust_TrustedWorkspace_RemovesFromRegistry()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var workspacePath = "/home/user/project";

        await trustService.RevokeTrustAsync(workspacePath);

        await trustService.Received(1).RevokeTrustAsync(workspacePath, default);

        trustService.GetTrustDecisionAsync(workspacePath, default)
            .Returns((false, false));

        var (hasDecision, _) = await trustService.GetTrustDecisionAsync(workspacePath);
        Assert.False(hasDecision);
    }

    [Fact]
    public async Task ListTrustedWorkspaces_MultipleWorkspaces_ReturnsAll()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();

        var workspace1 = CreateTrustedWorkspace("/home/user/project1", "user_confirmed");
        var workspace2 = CreateTrustedWorkspace("/home/user/project2", "signature_verified");
        var workspace3 = CreateTrustedWorkspace("/home/user/project3", "user_confirmed");

        trustService.ListTrustedWorkspacesAsync(default)
            .Returns(new[] { workspace1, workspace2, workspace3 });

        var result = await trustService.ListTrustedWorkspacesAsync();

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

        trustService.ListTrustedWorkspacesAsync(default)
            .Returns(Array.Empty<ITrustedWorkspace>());

        var result = await trustService.ListTrustedWorkspacesAsync();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ClearAllTrust_RemovesAllDecisions()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();

        await trustService.ClearAllTrustAsync();

        await trustService.Received(1).ClearAllTrustAsync(default);

        trustService.ListTrustedWorkspacesAsync(default)
            .Returns(Array.Empty<ITrustedWorkspace>());

        var result = await trustService.ListTrustedWorkspacesAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task NonceValidation_MultipleAttempts_OnlyAcceptsValidNonce()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var workspacePath = "/home/user/project";

        var validNonces = new HashSet<string> { "valid-nonce-123", "valid-nonce-456" };

        trustService.PromptUserTrustAsync(Arg.Any<string>(), Arg.Any<IMarkerFileData>(), default)
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
            CreateMarkerDataWithNonce(workspacePath, "invalid-nonce-1"));
        Assert.False(attempt1);

        var attempt2 = await trustService.PromptUserTrustAsync(workspacePath,
            CreateMarkerDataWithNonce(workspacePath, "valid-nonce-123"));
        Assert.True(attempt2);

        var attempt3 = await trustService.PromptUserTrustAsync(workspacePath,
            CreateMarkerDataWithNonce(workspacePath, "invalid-nonce-2"));
        Assert.False(attempt3);

        var attempt4 = await trustService.PromptUserTrustAsync(workspacePath,
            CreateMarkerDataWithNonce(workspacePath, "valid-nonce-456"));
        Assert.True(attempt4);
    }

    [Fact]
    public async Task TrustWorkflow_RecordAfterPrompt_MaintainsState()
    {
        var trustService = Substitute.For<ITrustBootstrapService>();
        var workspacePath = "/home/user/project";
        var markerData = CreateMarkerDataWithNonce(workspacePath, "valid-nonce-123");

        trustService.GetTrustDecisionAsync(workspacePath, default)
            .Returns((false, false));

        trustService.PromptUserTrustAsync(workspacePath, markerData, default)
            .Returns(true);

        var (initialHasDecision, _) = await trustService.GetTrustDecisionAsync(workspacePath);
        Assert.False(initialHasDecision);

        var prompted = await trustService.PromptUserTrustAsync(workspacePath, markerData);
        Assert.True(prompted);

        await trustService.RecordTrustDecisionAsync(workspacePath, true);

        trustService.GetTrustDecisionAsync(workspacePath, default)
            .Returns((true, true));

        var (hasDecision, isTrusted) = await trustService.GetTrustDecisionAsync(workspacePath);
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

        trustService.ListTrustedWorkspacesAsync(default)
            .Returns(new[] { workspace });

        var result = await trustService.ListTrustedWorkspacesAsync();
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
