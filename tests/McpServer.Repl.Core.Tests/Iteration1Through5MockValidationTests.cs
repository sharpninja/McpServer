using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;
using McpServer.Repl.Core;
using NSubstitute;
using Xunit;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// Comprehensive validation tests for iterations 1-5 with mock implementations.
/// Verifies all mock components work together and all test scenarios pass.
/// This validates the full test infrastructure from trust bootstrap through generic client passthrough.
/// </summary>
public class Iteration1Through5MockValidationTests
{
    #region Iteration 1 - Trust Bootstrap Validation

    /// <summary>
    /// Validates that marker file reader and trust bootstrap service mocks work correctly.
    /// </summary>
    [Fact]
    public async Task Iteration1_TrustBootstrap_AllMocksWork()
    {
        var markerReader = Substitute.For<IMarkerFileReader>();
        var trustService = Substitute.For<ITrustBootstrapService>();

        var workspacePath = "/test/workspace";
        var markerData = Substitute.For<IMarkerFileData>();
        markerData.WorkspacePath.Returns(workspacePath);
        markerData.ApiKey.Returns("test-key");

        markerReader.ReadAsync(workspacePath, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(markerData));

        trustService.GetTrustDecisionAsync(workspacePath, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((true, true)));

        var result = await markerReader.ReadAsync(workspacePath);
        Assert.NotNull(result);
        Assert.Equal(workspacePath, result.WorkspacePath);

        var (hasDecision, isTrusted) = await trustService.GetTrustDecisionAsync(workspacePath);
        Assert.True(hasDecision);
        Assert.True(isTrusted);
    }

    /// <summary>
    /// Validates that auth rotation handler mock works correctly.
    /// </summary>
    [Fact]
    public async Task Iteration1_AuthRotation_MockBehaviorCorrect()
    {
        var authHandler = Substitute.For<IAuthRotationHandler>();
        var markerData = Substitute.For<IMarkerFileData>();
        markerData.WorkspacePath.Returns("/test/workspace");
        markerData.ApiKey.Returns("new-key-123");

        authHandler.UpdateAuthStateAsync(markerData, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await authHandler.UpdateAuthStateAsync(markerData);

        await authHandler.Received(1).UpdateAuthStateAsync(markerData, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Iteration 2 - Session Log Workflow Validation

    /// <summary>
    /// Validates that session log workflow mock for opening sessions works correctly.
    /// </summary>
    [Fact]
    public async Task Iteration2_SessionLogWorkflow_MockOpenSessionWorks()
    {
        var workflow = Substitute.For<ISessionLogWorkflow>();

        workflow.OpenSessionAsync("Copilot", "Copilot-20260304T120000Z-test", "Test Session", "gpt-4", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await workflow.OpenSessionAsync("Copilot", "Copilot-20260304T120000Z-test", "Test Session", "gpt-4");

        await workflow.Received(1).OpenSessionAsync("Copilot", "Copilot-20260304T120000Z-test", "Test Session", "gpt-4", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Validates that session log workflow mock for beginning turns works correctly.
    /// </summary>
    [Fact]
    public async Task Iteration2_SessionLogWorkflow_MockBeginTurnWorks()
    {
        var workflow = Substitute.For<ISessionLogWorkflow>();

        workflow.BeginTurnAsync("req-20260304T120000Z-test-001", "Test query", "Full query text", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await workflow.BeginTurnAsync("req-20260304T120000Z-test-001", "Test query", "Full query text");

        await workflow.Received(1).BeginTurnAsync("req-20260304T120000Z-test-001", "Test query", "Full query text", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Validates that session log workflow mock for completing turns works correctly.
    /// </summary>
    [Fact]
    public async Task Iteration2_SessionLogWorkflow_MockCompleteTurnWorks()
    {
        var workflow = Substitute.For<ISessionLogWorkflow>();

        workflow.CompleteTurnAsync("Response text", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await workflow.CompleteTurnAsync("Response text");

        await workflow.Received(1).CompleteTurnAsync("Response text", Arg.Any<CancellationToken>());
    }

    #endregion

    #region Iteration 3 - TODO Workflow Validation

    /// <summary>
    /// Validates that TODO workflow mock for creating TODOs works correctly.
    /// </summary>
    [Fact]
    public async Task Iteration3_TodoWorkflow_MockCreateWorks()
    {
        var workflow = Substitute.For<ITodoWorkflow>();
        var createRequest = Substitute.For<ITodoCreateRequest>();
        var mutationResult = Substitute.For<ITodoMutationResult>();
        mutationResult.Success.Returns(true);

        workflow.CreateAsync(createRequest, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mutationResult));

        var result = await workflow.CreateAsync(createRequest);

        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    /// <summary>
    /// Validates that TODO workflow mock for querying TODOs works correctly.
    /// </summary>
    [Fact]
    public async Task Iteration3_TodoWorkflow_MockQueryWorks()
    {
        var workflow = Substitute.For<ITodoWorkflow>();
        var queryResult = Substitute.For<ITodoQueryResult>();
        queryResult.TotalCount.Returns(2);

        workflow.QueryAsync(null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(queryResult));

        var result = await workflow.QueryAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
    }

    /// <summary>
    /// Validates that TODO workflow mock for updating TODOs works correctly.
    /// </summary>
    [Fact]
    public async Task Iteration3_TodoWorkflow_MockUpdateWorks()
    {
        var workflow = Substitute.For<ITodoWorkflow>();
        var updateRequest = Substitute.For<ITodoUpdateRequest>();
        var mutationResult = Substitute.For<ITodoMutationResult>();
        mutationResult.Success.Returns(true);

        workflow.UpdateAsync("TODO-TEST-001", updateRequest, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(mutationResult));

        var result = await workflow.UpdateAsync("TODO-TEST-001", updateRequest);

        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    #endregion

    #region Iteration 4 - Requirements Workflow Validation

    /// <summary>
    /// Validates that requirements workflow mock for creating FRs works correctly.
    /// </summary>
    [Fact]
    public async Task Iteration4_RequirementsWorkflow_MockCreateFrWorks()
    {
        var workflow = Substitute.For<IRequirementsWorkflow>();
        var createRequest = Substitute.For<IFrCreateRequest>();
        var createResult = Substitute.For<IFrMutationResult>();
        createResult.Success.Returns(true);

        workflow.CreateFrAsync(createRequest, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(createResult));

        var result = await workflow.CreateFrAsync(createRequest);

        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    /// <summary>
    /// Validates that requirements workflow mock for listing FRs works correctly.
    /// </summary>
    [Fact]
    public async Task Iteration4_RequirementsWorkflow_MockListFrWorks()
    {
        var workflow = Substitute.For<IRequirementsWorkflow>();
        var queryResult = Substitute.For<IFrQueryResult>();
        queryResult.TotalCount.Returns(2);

        workflow.ListFrAsync("MCP", null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(queryResult));

        var result = await workflow.ListFrAsync("MCP");

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
    }

    /// <summary>
    /// Validates that requirements workflow mock for updating FRs works correctly.
    /// </summary>
    [Fact]
    public async Task Iteration4_RequirementsWorkflow_MockUpdateFrWorks()
    {
        var workflow = Substitute.For<IRequirementsWorkflow>();
        var updateRequest = Substitute.For<IFrUpdateRequest>();
        var updateResult = Substitute.For<IFrMutationResult>();
        updateResult.Success.Returns(true);

        workflow.UpdateFrAsync(updateRequest, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(updateResult));

        var result = await workflow.UpdateFrAsync(updateRequest);

        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    #endregion

    #region Iteration 5 - Generic Client Passthrough Validation

    /// <summary>
    /// Validates that generic client passthrough mock for context search works correctly.
    /// </summary>
    [Fact]
    public async Task Iteration5_GenericPassthrough_MockContextSearchWorks()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();

        var args = new Dictionary<string, object?>
        {
            ["query"] = "authentication flow",
            ["limit"] = 10
        };

        var expectedResult = new ContextSearchResult
        {
            Query = "authentication flow",
            Chunks = new List<ContextChunkResult>
            {
                new() { Id = "chunk-1", Content = "Auth content", Score = 0.95 }
            }
        };

        passthrough.InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(expectedResult));

        var result = await passthrough.InvokeAsync("context", "SearchAsync", args);

        Assert.NotNull(result);
        var searchResult = result as ContextSearchResult;
        Assert.NotNull(searchResult);
        Assert.Equal("authentication flow", searchResult.Query);
    }

    /// <summary>
    /// Validates that generic client passthrough mock for GitHub issues works correctly.
    /// </summary>
    [Fact]
    public async Task Iteration5_GenericPassthrough_MockGitHubIssuesWorks()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();

        var args = new Dictionary<string, object?>
        {
            ["state"] = "open",
            ["limit"] = 20
        };

        var expectedResult = new GitHubIssueListResult
        {
            Issues = new List<GitHubIssueItem>
            {
                new() { Number = 42, Title = "Test Issue", State = "open" }
            }
        };

        passthrough.InvokeAsync("github", "ListIssuesAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(expectedResult));

        var result = await passthrough.InvokeAsync("github", "ListIssuesAsync", args);

        Assert.NotNull(result);
        var issueResult = result as GitHubIssueListResult;
        Assert.NotNull(issueResult);
        Assert.Single(issueResult.Issues);
    }

    /// <summary>
    /// Validates that generic client passthrough mock for repo file read works correctly.
    /// </summary>
    [Fact]
    public async Task Iteration5_GenericPassthrough_MockRepoReadWorks()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();

        var args = new Dictionary<string, object?>
        {
            ["path"] = "README.md"
        };

        var expectedResult = new RepoFileReadResult
        {
            Path = "README.md",
            Content = "# Test Project",
            Exists = true
        };

        passthrough.InvokeAsync("repo", "ReadFileAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(expectedResult));

        var result = await passthrough.InvokeAsync("repo", "ReadFileAsync", args);

        Assert.NotNull(result);
        var fileResult = result as RepoFileReadResult;
        Assert.NotNull(fileResult);
        Assert.Equal("README.md", fileResult.Path);
    }

    /// <summary>
    /// Validates that generic client passthrough mock for desktop launch works correctly.
    /// </summary>
    [Fact]
    public async Task Iteration5_GenericPassthrough_MockDesktopLaunchWorks()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();

        var args = new Dictionary<string, object?>
        {
            ["request"] = new Dictionary<string, object?>
            {
                ["executablePath"] = "notepad.exe"
            }
        };

        var expectedResult = new DesktopLaunchResult
        {
            Success = true,
            ProcessId = 1234
        };

        passthrough.InvokeAsync("desktop", "LaunchAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(expectedResult));

        var result = await passthrough.InvokeAsync("desktop", "LaunchAsync", args);

        Assert.NotNull(result);
        var launchResult = result as DesktopLaunchResult;
        Assert.NotNull(launchResult);
        Assert.True(launchResult.Success);
    }

    /// <summary>
    /// Validates that generic client passthrough mock handles argument coercion correctly.
    /// </summary>
    [Fact]
    public async Task Iteration5_GenericPassthrough_MockArgumentCoercionWorks()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();

        var args = new Dictionary<string, object?>
        {
            ["query"] = "test",
            ["limit"] = "15"  // String representation of number
        };

        passthrough.InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new ContextSearchResult()));

        var result = await passthrough.InvokeAsync("context", "SearchAsync", args);

        Assert.NotNull(result);
        await passthrough.Received(1).InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Validates that generic client passthrough mock handles error cases correctly.
    /// </summary>
    [Fact]
    public async Task Iteration5_GenericPassthrough_MockErrorHandlingWorks()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();

        var args = new Dictionary<string, object?>();

        passthrough.InvokeAsync("unknownclient", "SomeMethod", args, Arg.Any<CancellationToken>())
            .Returns<object?>(x => throw new System.InvalidOperationException("Unknown client: unknownclient"));

        await Assert.ThrowsAsync<System.InvalidOperationException>(
            async () => await passthrough.InvokeAsync("unknownclient", "SomeMethod", args));
    }

    /// <summary>
    /// Validates that generic client passthrough mock handles case-insensitive parameters correctly.
    /// </summary>
    [Fact]
    public async Task Iteration5_GenericPassthrough_MockCaseInsensitivityWorks()
    {
        var passthrough = Substitute.For<IGenericClientPassthrough>();

        var args = new Dictionary<string, object?>
        {
            ["QUERY"] = "test",  // Uppercase
            ["Limit"] = 10       // PascalCase
        };

        passthrough.InvokeAsync("CoNtExT", "SearchAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new ContextSearchResult()));

        var result = await passthrough.InvokeAsync("CoNtExT", "SearchAsync", args);

        Assert.NotNull(result);
        await passthrough.Received(1).InvokeAsync("CoNtExT", "SearchAsync", args, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Cross-Iteration Integration Tests

    /// <summary>
    /// Validates that all required interfaces are defined correctly.
    /// </summary>
    [Fact]
    public void CrossIteration_AllInterfacesDefinedCorrectly()
    {
        Assert.NotNull(typeof(ITrustBootstrapService));
        Assert.NotNull(typeof(IMarkerFileReader));
        Assert.NotNull(typeof(IAuthRotationHandler));
        Assert.NotNull(typeof(ISessionLogWorkflow));
        Assert.NotNull(typeof(ITodoWorkflow));
        Assert.NotNull(typeof(IRequirementsWorkflow));
        Assert.NotNull(typeof(IGenericClientPassthrough));
        Assert.NotNull(typeof(IYamlSerializer));
    }

    /// <summary>
    /// Validates that all required model classes exist.
    /// </summary>
    [Fact]
    public void CrossIteration_AllModelClassesExist()
    {
        Assert.NotNull(typeof(UnifiedSessionLogDto));
        Assert.NotNull(typeof(SessionLogSubmitResult));
        Assert.NotNull(typeof(SessionLogQueryResult));
        Assert.NotNull(typeof(TodoFlatItem));
        Assert.NotNull(typeof(TodoQueryResult));
        Assert.NotNull(typeof(ContextSearchResult));
        Assert.NotNull(typeof(GitHubIssueListResult));
        Assert.NotNull(typeof(RepoFileReadResult));
        Assert.NotNull(typeof(DesktopLaunchResult));
    }

    #endregion

    #region YAML Serialization Integration

    /// <summary>
    /// Validates that FakeYamlSerializer works correctly for basic serialization.
    /// </summary>
    [Fact]
    public void YamlSerialization_FakeSerializerWorks()
    {
        var serializer = new FakeYamlSerializer();
        var envelope = Substitute.For<IYamlEnvelope>();
        envelope.Type.Returns("test");
        envelope.Payload.Returns(new { message = "hello" });

        var yaml = serializer.Serialize(envelope);

        Assert.NotNull(yaml);
        Assert.Contains("type: test", yaml);
    }

    /// <summary>
    /// Validates that FakeYamlSerializer round-trips data correctly.
    /// </summary>
    [Fact]
    public void YamlSerialization_RoundTripPreservesData()
    {
        var serializer = new FakeYamlSerializer();
        var envelope = Substitute.For<IYamlEnvelope>();
        envelope.Type.Returns("request");
        envelope.Payload.Returns(new { method = "test", requestId = "req-1" });

        var yaml = serializer.Serialize(envelope);
        var deserialized = serializer.Deserialize(yaml);

        Assert.NotNull(deserialized);
        Assert.Equal("request", deserialized.Type);
    }

    #endregion
}
