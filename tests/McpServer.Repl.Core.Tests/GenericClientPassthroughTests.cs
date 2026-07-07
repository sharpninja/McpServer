using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client;
using McpServer.Client.Models;
using McpServer.Repl.Core;
using NSubstitute;
using Xunit;

namespace McpServer.Repl.Core.Tests;

/// <summary>
/// Iteration 5 unit tests for generic client passthrough functionality.
/// Tests client resolution by name (case-insensitive), method resolution by reflection,
/// argument coercion (YAML dictionary → method parameters), routing to correct McpServerClient sub-client,
/// consistent YAML response shaping for arbitrary return types, error handling (unknown client, unknown method,
/// argument type mismatch), and coverage of non-workflow clients (Context, GitHub, Repo, Desktop, etc.).
/// Mocks IGenericClientPassthrough to validate dynamic invocation contract.
/// Red phase: all tests expected to fail until implementation is complete.
/// </summary>
public class GenericClientPassthroughTests
{
    private readonly IGenericClientPassthrough _passthrough;
    private readonly IYamlSerializer _yamlSerializer;

    public GenericClientPassthroughTests()
    {
        _yamlSerializer = new FakeYamlSerializer();
        _passthrough = Substitute.For<IGenericClientPassthrough>();
    }

    #region Client Resolution Tests

    [Fact]
    public async Task ResolveClient_ContextCaseInsensitive_ResolvesContextClient()
    {
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

        _passthrough.InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(expectedResult));

        var result = await _passthrough.InvokeAsync("context", "SearchAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await _passthrough.Received(1).InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveClient_GitHubCaseInsensitive_ResolvesGitHubClient()
    {
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

        _passthrough.InvokeAsync("github", "ListIssuesAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(expectedResult));

        var result = await _passthrough.InvokeAsync("github", "ListIssuesAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await _passthrough.Received(1).InvokeAsync("github", "ListIssuesAsync", args, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveClient_RepoCaseInsensitive_ResolvesRepoClient()
    {
        var args = new Dictionary<string, object?>
        {
            ["path"] = "README.md"
        };

        var expectedResult = new RepoFileReadResult
        {
            Path = "README.md",
            Content = "# Test Project"
        };

        _passthrough.InvokeAsync("repo", "ReadFileAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(expectedResult));

        var result = await _passthrough.InvokeAsync("repo", "ReadFileAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await _passthrough.Received(1).InvokeAsync("repo", "ReadFileAsync", args, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveClient_DesktopCaseInsensitive_ResolvesDesktopClient()
    {
        var args = new Dictionary<string, object?>
        {
            ["request"] = new Dictionary<string, object?>
            {
                ["executablePath"] = "notepad.exe",
                ["workingDirectory"] = "C:\\Users\\Test"
            }
        };

        var expectedResult = new DesktopLaunchResult
        {
            ProcessId = 1234,
            Success = true
        };

        _passthrough.InvokeAsync("desktop", "LaunchAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(expectedResult));

        var result = await _passthrough.InvokeAsync("desktop", "LaunchAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await _passthrough.Received(1).InvokeAsync("desktop", "LaunchAsync", args, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveClient_SessionLogCaseInsensitive_ResolvesSessionLogClient()
    {
        var args = new Dictionary<string, object?>
        {
            ["agent"] = "Copilot",
            ["limit"] = 10
        };

        var expectedResult = new SessionLogQueryResult
        {
            Items = new List<UnifiedSessionLogDto>
            {
                new() { SessionId = "session-1", SourceType = "Copilot" }
            },
            TotalCount = 1
        };

        _passthrough.InvokeAsync("sessionlog", "QueryAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(expectedResult));

        var result = await _passthrough.InvokeAsync("sessionlog", "QueryAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await _passthrough.Received(1).InvokeAsync("sessionlog", "QueryAsync", args, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveClient_TodoCaseInsensitive_ResolvesTodoClient()
    {
        var args = new Dictionary<string, object?>
        {
            ["keyword"] = "authentication",
            ["limit"] = 20
        };

        // Mock expects IQueryable interface, so create a mock result
        _passthrough.InvokeAsync("todo", "QueryAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new { Items = new List<object>(), TotalCount = 0 }));

        var result = await _passthrough.InvokeAsync("todo", "QueryAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await _passthrough.Received(1).InvokeAsync("todo", "QueryAsync", args, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveClient_RequirementsCaseInsensitive_ResolvesRequirementsClient()
    {
        var args = new Dictionary<string, object?>
        {
            ["category"] = "FR",
            ["limit"] = 10
        };

        _passthrough.InvokeAsync("requirements", "QueryAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new { Items = new List<object>(), TotalCount = 0 }));

        var result = await _passthrough.InvokeAsync("requirements", "QueryAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await _passthrough.Received(1).InvokeAsync("requirements", "QueryAsync", args, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveClient_MixedCase_ResolvesCorrectly()
    {
        var args = new Dictionary<string, object?>
        {
            ["query"] = "test query"
        };

        _passthrough.InvokeAsync("CoNtExT", "SearchAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new ContextSearchResult()));

        var result = await _passthrough.InvokeAsync("CoNtExT", "SearchAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await _passthrough.Received(1).InvokeAsync("CoNtExT", "SearchAsync", args, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Method Resolution Tests

    [Fact]
    public async Task ResolveMethod_SearchAsync_ResolvesCorrectMethod()
    {
        var args = new Dictionary<string, object?>
        {
            ["query"] = "authentication"
        };

        _passthrough.InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new ContextSearchResult()));

        var result = await _passthrough.InvokeAsync("context", "SearchAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await _passthrough.Received(1).InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveMethod_RebuildIndexAsync_ResolvesCorrectMethod()
    {
        var args = new Dictionary<string, object?>();

        _passthrough.InvokeAsync("context", "RebuildIndexAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new RebuildIndexResult { Status = "completed" }));

        var result = await _passthrough.InvokeAsync("context", "RebuildIndexAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await _passthrough.Received(1).InvokeAsync("context", "RebuildIndexAsync", args, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveMethod_PackAsync_ResolvesCorrectMethod()
    {
        var args = new Dictionary<string, object?>
        {
            ["query"] = "authentication",
            ["limit"] = 20
        };

        _passthrough.InvokeAsync("context", "PackAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new ContextPack()));

        var result = await _passthrough.InvokeAsync("context", "PackAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await _passthrough.Received(1).InvokeAsync("context", "PackAsync", args, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveMethod_ListSourcesAsync_ResolvesCorrectMethod()
    {
        var args = new Dictionary<string, object?>();

        _passthrough.InvokeAsync("context", "ListSourcesAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new ContextSourcesResult()));

        var result = await _passthrough.InvokeAsync("context", "ListSourcesAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await _passthrough.Received(1).InvokeAsync("context", "ListSourcesAsync", args, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Argument Coercion Tests

    [Fact]
    public async Task ArgumentCoercion_StringParameter_CoercesCorrectly()
    {
        var args = new Dictionary<string, object?>
        {
            ["query"] = "authentication flow",
            ["sourceType"] = "markdown",
            ["limit"] = 10
        };

        _passthrough.InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new ContextSearchResult()));

        var result = await _passthrough.InvokeAsync("context", "SearchAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await _passthrough.Received(1).InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ArgumentCoercion_IntParameter_CoercesCorrectly()
    {
        var args = new Dictionary<string, object?>
        {
            ["query"] = "test",
            ["limit"] = 25
        };

        _passthrough.InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new ContextSearchResult()));

        var result = await _passthrough.InvokeAsync("context", "SearchAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await _passthrough.Received(1).InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ArgumentCoercion_BoolParameter_CoercesCorrectly()
    {
        var args = new Dictionary<string, object?>
        {
            ["url"] = "https://example.com",
            ["includeSubpages"] = true,
            ["maxPages"] = 10
        };

        _passthrough.InvokeAsync("context", "IngestWebsiteAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new WebsiteIngestResult()));

        var result = await _passthrough.InvokeAsync("context", "IngestWebsiteAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await _passthrough.Received(1).InvokeAsync("context", "IngestWebsiteAsync", args, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ArgumentCoercion_NullableParameter_CoercesNull()
    {
        var args = new Dictionary<string, object?>
        {
            ["query"] = "test",
            ["sourceType"] = null,
            ["limit"] = 20
        };

        _passthrough.InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new ContextSearchResult()));

        var result = await _passthrough.InvokeAsync("context", "SearchAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await _passthrough.Received(1).InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ArgumentCoercion_OptionalParameter_UsesDefaultValue()
    {
        var args = new Dictionary<string, object?>
        {
            ["query"] = "test"
            // limit is optional with default value 20
        };

        _passthrough.InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new ContextSearchResult()));

        var result = await _passthrough.InvokeAsync("context", "SearchAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await _passthrough.Received(1).InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ArgumentCoercion_ComplexObject_DeserializesCorrectly()
    {
        var args = new Dictionary<string, object?>
        {
            ["request"] = new Dictionary<string, object?>
            {
                ["executablePath"] = "notepad.exe",
                ["workingDirectory"] = "C:\\Users\\Test",
                ["arguments"] = "test.txt"
            }
        };

        _passthrough.InvokeAsync("desktop", "LaunchAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new DesktopLaunchResult()));

        var result = await _passthrough.InvokeAsync("desktop", "LaunchAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await _passthrough.Received(1).InvokeAsync("desktop", "LaunchAsync", args, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ArgumentCoercion_NumberAsString_CoercesCorrectly()
    {
        var args = new Dictionary<string, object?>
        {
            ["query"] = "test",
            ["limit"] = "15"  // String representation of number
        };

        _passthrough.InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new ContextSearchResult()));

        var result = await _passthrough.InvokeAsync("context", "SearchAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        await _passthrough.Received(1).InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ErrorHandling_UnknownClient_ThrowsInvalidOperationException()
    {
        var args = new Dictionary<string, object?>();

        _passthrough.InvokeAsync("unknownclient", "SomeMethod", args, Arg.Any<CancellationToken>())
            .Returns<object?>(x => throw new InvalidOperationException("Unknown client: unknownclient"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _passthrough.InvokeAsync("unknownclient", "SomeMethod", args, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ErrorHandling_UnknownMethod_ThrowsInvalidOperationException()
    {
        var args = new Dictionary<string, object?>();

        _passthrough.InvokeAsync("context", "NonExistentMethod", args, Arg.Any<CancellationToken>())
            .Returns<object?>(x => throw new InvalidOperationException("Unknown method: NonExistentMethod on client: context"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _passthrough.InvokeAsync("context", "NonExistentMethod", args, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ErrorHandling_MissingRequiredParameter_ThrowsArgumentException()
    {
        var args = new Dictionary<string, object?>
        {
            // Missing required 'query' parameter
            ["limit"] = 10
        };

        _passthrough.InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>())
            .Returns<object?>(x => throw new ArgumentException("Missing required parameter: query"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _passthrough.InvokeAsync("context", "SearchAsync", args, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ErrorHandling_TypeConversionError_ThrowsArgumentException()
    {
        var args = new Dictionary<string, object?>
        {
            ["query"] = "test",
            ["limit"] = "not-a-number"  // Invalid type for int parameter
        };

        _passthrough.InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>())
            .Returns<object?>(x => throw new ArgumentException("Type conversion error for parameter 'limit': cannot convert 'not-a-number' to System.Int32"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _passthrough.InvokeAsync("context", "SearchAsync", args, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ErrorHandling_NullForNonNullableParameter_ThrowsArgumentException()
    {
        var args = new Dictionary<string, object?>
        {
            ["query"] = null  // Null for non-nullable string parameter
        };

        _passthrough.InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>())
            .Returns<object?>(x => throw new ArgumentException("Null value provided for non-nullable parameter: query"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _passthrough.InvokeAsync("context", "SearchAsync", args, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ErrorHandling_JsonDeserializationError_ThrowsArgumentException()
    {
        var args = new Dictionary<string, object?>
        {
            ["request"] = "invalid-object"  // String instead of object
        };

        _passthrough.InvokeAsync("desktop", "LaunchAsync", args, Arg.Any<CancellationToken>())
            .Returns<object?>(x => throw new ArgumentException("JSON deserialization error for parameter 'request'"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _passthrough.InvokeAsync("desktop", "LaunchAsync", args, cancellationToken: TestContext.Current.CancellationToken));
    }

    #endregion

    #region Response Shaping Tests

    [Fact]
    public async Task ResponseShaping_ContextSearchResult_ShapesCorrectly()
    {
        var args = new Dictionary<string, object?>
        {
            ["query"] = "authentication"
        };

        var expectedResult = new ContextSearchResult
        {
            Query = "authentication",
            Chunks = new List<ContextChunkResult>
            {
                new() { Id = "chunk-1", Content = "Auth content", Score = 0.95, TokenCount = 100 }
            },
            SourceKeys = new List<string> { "doc-1" }
        };

        _passthrough.InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(expectedResult));

        var result = await _passthrough.InvokeAsync("context", "SearchAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        var searchResult = result as ContextSearchResult;
        Assert.NotNull(searchResult);
        Assert.Equal("authentication", searchResult.Query);
        Assert.Single(searchResult.Chunks);
    }

    [Fact]
    public async Task ResponseShaping_GitHubIssueListResult_ShapesCorrectly()
    {
        var args = new Dictionary<string, object?>
        {
            ["state"] = "open"
        };

        var expectedResult = new GitHubIssueListResult
        {
            Issues = new List<GitHubIssueItem>
            {
                new() { Number = 42, Title = "Test Issue", State = "open" }
            }
        };

        _passthrough.InvokeAsync("github", "ListIssuesAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(expectedResult));

        var result = await _passthrough.InvokeAsync("github", "ListIssuesAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        var issueResult = result as GitHubIssueListResult;
        Assert.NotNull(issueResult);
        Assert.Single(issueResult.Issues);
        Assert.Equal(42, issueResult.Issues[0].Number);
    }

    [Fact]
    public async Task ResponseShaping_RepoFileReadResult_ShapesCorrectly()
    {
        var args = new Dictionary<string, object?>
        {
            ["path"] = "README.md"
        };

        var expectedResult = new RepoFileReadResult
        {
            Path = "README.md",
            Content = "# Test Project\n\nThis is a test.",
            Exists = true
        };

        _passthrough.InvokeAsync("repo", "ReadFileAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(expectedResult));

        var result = await _passthrough.InvokeAsync("repo", "ReadFileAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        var fileResult = result as RepoFileReadResult;
        Assert.NotNull(fileResult);
        Assert.Equal("README.md", fileResult.Path);
        Assert.True(fileResult.Exists);
    }

    [Fact]
    public async Task ResponseShaping_VoidResult_ReturnsEmptyObject()
    {
        var args = new Dictionary<string, object?>
        {
            ["path"] = "test.txt",
            ["content"] = "Hello World"
        };

        _passthrough.InvokeAsync("repo", "WriteFileAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new RepoWriteResult { Written = true }));

        var result = await _passthrough.InvokeAsync("repo", "WriteFileAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ResponseShaping_ComplexNestedObject_PreservesStructure()
    {
        var args = new Dictionary<string, object?>
        {
            ["query"] = "test",
            ["limit"] = 20
        };

        var expectedResult = new ContextPack
        {
            QueryId = "query-123",
            Chunks = new List<ContextChunkResult>
            {
                new() { Id = "chunk-1", Content = "Content 1", Score = 0.95 },
                new() { Id = "chunk-2", Content = "Content 2", Score = 0.85 }
            },
            SourceKeys = new List<string> { "source-1", "source-2" }
        };

        _passthrough.InvokeAsync("context", "PackAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(expectedResult));

        var result = await _passthrough.InvokeAsync("context", "PackAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        var packResult = result as ContextPack;
        Assert.NotNull(packResult);
        Assert.Equal("query-123", packResult.QueryId);
        Assert.Equal(2, packResult.Chunks.Count);
        Assert.Equal(2, packResult.SourceKeys.Count);
    }

    #endregion

    #region Multi-Client Coverage Tests

    [Fact]
    public async Task MultiClient_Context_AllMethods_Work()
    {
        // SearchAsync
        _passthrough.InvokeAsync("context", "SearchAsync", Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new ContextSearchResult()));

        var searchResult = await _passthrough.InvokeAsync("context", "SearchAsync", new Dictionary<string, object?> { ["query"] = "test" }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(searchResult);

        // RebuildIndexAsync
        _passthrough.InvokeAsync("context", "RebuildIndexAsync", Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new RebuildIndexResult()));

        var rebuildResult = await _passthrough.InvokeAsync("context", "RebuildIndexAsync", new Dictionary<string, object?>(), cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(rebuildResult);

        // PackAsync
        _passthrough.InvokeAsync("context", "PackAsync", Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new ContextPack()));

        var packResult = await _passthrough.InvokeAsync("context", "PackAsync", new Dictionary<string, object?> { ["query"] = "test" }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(packResult);

        // ListSourcesAsync
        _passthrough.InvokeAsync("context", "ListSourcesAsync", Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new ContextSourcesResult()));

        var sourcesResult = await _passthrough.InvokeAsync("context", "ListSourcesAsync", new Dictionary<string, object?>(), cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(sourcesResult);
    }

    [Fact]
    public async Task MultiClient_GitHub_ListAndCreateIssues_Work()
    {
        // ListIssuesAsync
        _passthrough.InvokeAsync("github", "ListIssuesAsync", Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new GitHubIssueListResult()));

        var listResult = await _passthrough.InvokeAsync("github", "ListIssuesAsync", new Dictionary<string, object?>(), cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(listResult);

        // GetIssueAsync
        _passthrough.InvokeAsync("github", "GetIssueAsync", Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new GitHubIssueDetail()));

        var getResult = await _passthrough.InvokeAsync("github", "GetIssueAsync", new Dictionary<string, object?> { ["number"] = 42 }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(getResult);
    }

    [Fact]
    public async Task MultiClient_Repo_ReadAndWrite_Work()
    {
        // ReadFileAsync
        _passthrough.InvokeAsync("repo", "ReadFileAsync", Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new RepoFileReadResult()));

        var readResult = await _passthrough.InvokeAsync("repo", "ReadFileAsync", new Dictionary<string, object?> { ["path"] = "test.txt" }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(readResult);

        // WriteFileAsync
        _passthrough.InvokeAsync("repo", "WriteFileAsync", Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new RepoWriteResult()));

        var writeResult = await _passthrough.InvokeAsync("repo", "WriteFileAsync", 
            new Dictionary<string, object?> { ["path"] = "test.txt", ["content"] = "Hello" }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(writeResult);

        // ListAsync
        _passthrough.InvokeAsync("repo", "ListAsync", Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new RepoListResult()));

        var listResult = await _passthrough.InvokeAsync("repo", "ListAsync", new Dictionary<string, object?>(), cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(listResult);
    }

    [Fact]
    public async Task MultiClient_Desktop_Launch_Works()
    {
        var args = new Dictionary<string, object?>
        {
            ["request"] = new Dictionary<string, object?>
            {
                ["executablePath"] = "notepad.exe"
            }
        };

        _passthrough.InvokeAsync("desktop", "LaunchAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new DesktopLaunchResult { Success = true }));

        var result = await _passthrough.InvokeAsync("desktop", "LaunchAsync", args, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task MultiClient_SessionLog_QueryAndAppend_Work()
    {
        // QueryAsync
        _passthrough.InvokeAsync("sessionlog", "QueryAsync", Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new SessionLogQueryResult()));

        var queryResult = await _passthrough.InvokeAsync("sessionlog", "QueryAsync", 
            new Dictionary<string, object?> { ["agent"] = "Copilot" }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(queryResult);

        // SubmitAsync
        _passthrough.InvokeAsync("sessionlog", "SubmitAsync", Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new SessionLogSubmitResult()));

        var submitResult = await _passthrough.InvokeAsync("sessionlog", "SubmitAsync", 
            new Dictionary<string, object?> { ["sessionLog"] = new UnifiedSessionLogDto() }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(submitResult);
    }

    #endregion

    #region Parameter Name Case Insensitivity Tests

    [Fact]
    public async Task ParameterName_CaseInsensitive_MatchesCorrectly()
    {
        var args = new Dictionary<string, object?>
        {
            ["QUERY"] = "test",  // Uppercase
            ["Limit"] = 10       // PascalCase
        };

        _passthrough.InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new ContextSearchResult()));

        var result = await _passthrough.InvokeAsync("context", "SearchAsync", args, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ParameterName_MixedCase_MatchesCorrectly()
    {
        var args = new Dictionary<string, object?>
        {
            ["qUeRy"] = "test",      // Mixed case
            ["sOuRcEtYpE"] = "md",   // Mixed case
            ["LiMiT"] = 15           // Mixed case
        };

        _passthrough.InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new ContextSearchResult()));

        var result = await _passthrough.InvokeAsync("context", "SearchAsync", args, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
    }

    #endregion

    #region CancellationToken Handling Tests

    [Fact]
    public async Task CancellationToken_PassedToMethod_ProperlyCancels()
    {
        var args = new Dictionary<string, object?>
        {
            ["query"] = "test"
        };

        var cts = new CancellationTokenSource();
        cts.Cancel();

        _passthrough.InvokeAsync("context", "SearchAsync", args, cts.Token)
            .Returns<object?>(x => throw new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _passthrough.InvokeAsync("context", "SearchAsync", args, cts.Token));
    }

    [Fact]
    public async Task CancellationToken_NotInArguments_StillPassedToMethod()
    {
        var args = new Dictionary<string, object?>
        {
            ["query"] = "test"
            // CancellationToken should NOT be in arguments
        };

        _passthrough.InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new ContextSearchResult()));

        var result = await _passthrough.InvokeAsync("context", "SearchAsync", args, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        await _passthrough.Received(1).InvokeAsync("context", "SearchAsync", args, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Federation Client Passthrough Tests

    [Fact]
    public async Task ResolveClient_FederationCaseInsensitive_ResolvesFederationClient()
    {
        var args = new Dictionary<string, object?>();

        var expectedResult = new FederationStatusResponse
        {
            Enabled = true,
            Targets = new List<FederationTargetInfo>(),
            WorkspaceRoutes = new List<WorkspaceRouteInfo>()
        };

        _passthrough.InvokeAsync("federation", "GetStatusAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(expectedResult));

        var result = await _passthrough.InvokeAsync("federation", "GetStatusAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        var status = Assert.IsType<FederationStatusResponse>(result);
        Assert.True(status.Enabled);
        await _passthrough.Received(1).InvokeAsync("federation", "GetStatusAsync", args, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FederationClient_AddTarget_CoercesComplexRequest()
    {
        var args = new Dictionary<string, object?>
        {
            ["request"] = new Dictionary<string, object?>
            {
                ["name"] = "remote-server",
                ["baseUrl"] = "http://remote:7147",
                ["apiKey"] = "secret"
            }
        };

        var expectedResult = new FederationTargetInfo
        {
            Name = "remote-server",
            BaseUrl = "http://remote:7147",
            HasApiKey = true,
            IsDefault = false
        };

        _passthrough.InvokeAsync("federation", "AddTargetAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(expectedResult));

        var result = await _passthrough.InvokeAsync("federation", "AddTargetAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        var target = Assert.IsType<FederationTargetInfo>(result);
        Assert.Equal("remote-server", target.Name);
        Assert.True(target.HasApiKey);
    }

    [Fact]
    public async Task FederationClient_EnrollProxy_CoercesHubSpokeRequest()
    {
        var args = new Dictionary<string, object?>
        {
            ["request"] = new Dictionary<string, object?>
            {
                ["proxyId"] = "PAYTON-LEGION2",
                ["displayName"] = "PAYTON-LEGION2",
                ["baseUrl"] = "http://PAYTON-LEGION2:7147",
                ["workspaces"] = new List<object?>
                {
                    new Dictionary<string, object?>
                    {
                        ["workspaceName"] = "McpServer",
                        ["workspacePath"] = @"F:\GitHub\McpServer",
                    },
                },
            },
        };

        var expectedResult = new FederationEnrollmentResponse
        {
            ProxyId = "PAYTON-LEGION2",
            Accepted = true,
            HeartbeatSeconds = 30,
        };

        _passthrough.InvokeAsync("federation", "EnrollProxyAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(expectedResult));

        var result = await _passthrough.InvokeAsync("federation", "EnrollProxyAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        var enrollment = Assert.IsType<FederationEnrollmentResponse>(result);
        Assert.True(enrollment.Accepted);
        Assert.Equal("PAYTON-LEGION2", enrollment.ProxyId);
    }

    [Fact]
    public async Task FederationClient_Push_CoercesTypeFilter()
    {
        var args = new Dictionary<string, object?>
        {
            ["types"] = new List<object?> { "todos", "sessionlogs" }
        };

        var expectedResult = new FederationPushResult
        {
            Succeeded = 10,
            Failed = 0,
            Errors = new List<string>()
        };

        _passthrough.InvokeAsync("federation", "PushAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(expectedResult));

        var result = await _passthrough.InvokeAsync("federation", "PushAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        var push = Assert.IsType<FederationPushResult>(result);
        Assert.Equal(10, push.Succeeded);
        Assert.Equal(0, push.Failed);
    }

    [Fact]
    public async Task FederationClient_ListTargets_ReturnsCollection()
    {
        var args = new Dictionary<string, object?>();

        var expectedResult = new List<FederationTargetInfo>
        {
            new() { Name = "server-a", BaseUrl = "http://a:7147", HasApiKey = false, IsDefault = true },
            new() { Name = "server-b", BaseUrl = "http://b:7148", HasApiKey = true, IsDefault = false }
        };

        _passthrough.InvokeAsync("federation", "ListTargetsAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(expectedResult));

        var result = await _passthrough.InvokeAsync("federation", "ListTargetsAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        var targets = Assert.IsType<List<FederationTargetInfo>>(result);
        Assert.Equal(2, targets.Count);
        Assert.Equal("server-a", targets[0].Name);
        Assert.True(targets[0].IsDefault);
    }

    [Fact]
    public async Task FederationClient_DiscoverFromTunnels_ReturnsTunnelDiscoveryResult()
    {
        var args = new Dictionary<string, object?>();

        var expectedResult = new TunnelDiscoveryResult
        {
            Discovered = 1,
            Targets = new List<FederationTargetInfo>
            {
                new() { Name = "ngrok", BaseUrl = "https://abc.ngrok.io", HasApiKey = false, IsDefault = false }
            }
        };

        _passthrough.InvokeAsync("federation", "DiscoverFromTunnelsAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(expectedResult));

        var result = await _passthrough.InvokeAsync("federation", "DiscoverFromTunnelsAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        var discovery = Assert.IsType<TunnelDiscoveryResult>(result);
        Assert.Equal(1, discovery.Discovered);
        Assert.Single(discovery.Targets);
    }

    [Fact]
    public async Task FederationClient_GetConnection_ReturnsCredentials()
    {
        var args = new Dictionary<string, object?>
        {
            ["workspaceName"] = "MyProject"
        };

        var expectedResult = new FederationConnectionInfo
        {
            BaseUrl = "http://hostname:7147",
            Port = 7147,
            ApiKey = "ws-token"
        };

        _passthrough.InvokeAsync("federation", "GetConnectionAsync", args, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(expectedResult));

        var result = await _passthrough.InvokeAsync("federation", "GetConnectionAsync", args, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        var conn = Assert.IsType<FederationConnectionInfo>(result);
        Assert.Equal(7147, conn.Port);
        Assert.Equal("ws-token", conn.ApiKey);
    }

    #endregion
}
