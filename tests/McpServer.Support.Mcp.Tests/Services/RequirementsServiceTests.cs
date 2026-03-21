using McpServer.Common.Copilot;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Unit tests for RequirementsService.ExtractRequirementIds and MergeIds.</summary>
public sealed class RequirementsServiceTests
{
    private readonly RequirementsService _sut;

    public RequirementsServiceTests()
    {
        var todoService = Substitute.For<ITodoService>();
        var accessor = TestWorkspaceAccessorHelper.Create(todoService);
        _sut = new RequirementsService(
            Substitute.For<ICopilotClient>(),
            accessor,
            Substitute.For<Microsoft.Extensions.Options.IOptionsMonitor<McpServer.Support.Mcp.Options.TodoPromptOptions>>(),
            NullLogger<RequirementsService>.Instance);
    }

    [Fact]
    public async Task ExtractRequirementIds_JsonBlock_ExtractsIds()
    {
        var body = """
            Here are the results:

            ```json
            {
              "functionalRequirements": ["FR-LOC-001", "FR-LOC-002"],
              "technicalRequirements": ["TR-LOC-001", "TR-API-003"]
            }
            ```

            Done.
            """;

        var (frs, trs) = _sut.ExtractRequirementIds(body);

        Assert.Equal(2, frs.Count);
        Assert.Contains("FR-LOC-001", frs);
        Assert.Contains("FR-LOC-002", frs);
        Assert.Equal(2, trs.Count);
        Assert.Contains("TR-LOC-001", trs);
        Assert.Contains("TR-API-003", trs);

        await Task.CompletedTask.ConfigureAwait(true);
    }

    [Fact]
    public async Task ExtractRequirementIds_JsonWithoutCodeFence_ExtractsIds()
    {
        var body = """
            {"functionalRequirements": ["FR-BIZ-010"], "technicalRequirements": ["TR-BIZ-005"]}
            """;

        var (frs, trs) = _sut.ExtractRequirementIds(body);

        Assert.Single(frs);
        Assert.Equal("FR-BIZ-010", frs[0]);
        Assert.Single(trs);
        Assert.Equal("TR-BIZ-005", trs[0]);

        await Task.CompletedTask.ConfigureAwait(true);
    }

    [Fact]
    public async Task ExtractRequirementIds_NoJson_FallsBackToRegex()
    {
        var body = """
            I found the following requirements:
            - FR-WF-005 is related to workflows
            - TR-MOBILE-001 covers the mobile implementation
            - FR-WF-005 is mentioned again (should dedup)
            """;

        var (frs, trs) = _sut.ExtractRequirementIds(body);

        Assert.Single(frs);
        Assert.Equal("FR-WF-005", frs[0]);
        Assert.Single(trs);
        Assert.Equal("TR-MOBILE-001", trs[0]);

        await Task.CompletedTask.ConfigureAwait(true);
    }

    [Fact]
    public async Task ExtractRequirementIds_EmptyBody_ReturnsEmptyLists()
    {
        var (frs, trs) = _sut.ExtractRequirementIds("");

        Assert.Empty(frs);
        Assert.Empty(trs);

        await Task.CompletedTask.ConfigureAwait(true);
    }

    [Fact]
    public async Task ExtractRequirementIds_NoRequirements_ReturnsEmptyLists()
    {
        var body = "I analyzed the TODO but found no matching requirements.";

        var (frs, trs) = _sut.ExtractRequirementIds(body);

        Assert.Empty(frs);
        Assert.Empty(trs);

        await Task.CompletedTask.ConfigureAwait(true);
    }

    [Fact]
    public async Task ExtractRequirementIds_MalformedJson_FallsBackToRegex()
    {
        var body = """
            {"functionalRequirements": ["FR-LOC-001" -- bad json
            Also mentioned FR-ARCH-002 and TR-DB-001.
            """;

        var (frs, trs) = _sut.ExtractRequirementIds(body);

        Assert.Contains("FR-LOC-001", frs);
        Assert.Contains("FR-ARCH-002", frs);
        Assert.Contains("TR-DB-001", trs);

        await Task.CompletedTask.ConfigureAwait(true);
    }

    [Fact]
    public async Task ExtractRequirementIds_JsonEmptyArrays_FallsBackToRegex()
    {
        var body = """
            {"functionalRequirements": [], "technicalRequirements": []}
            Also found FR-LOG-001 inline.
            """;

        var (frs, trs) = _sut.ExtractRequirementIds(body);

        // JSON arrays empty → falls through to regex
        Assert.Contains("FR-LOG-001", frs);

        await Task.CompletedTask.ConfigureAwait(true);
    }

    [Fact]
    public async Task ExtractRequirementIds_DuplicateIds_Deduplicates()
    {
        var body = """
            {
              "functionalRequirements": ["FR-LOC-001", "FR-LOC-001", "fr-loc-001"],
              "technicalRequirements": ["TR-API-001", "TR-API-001"]
            }
            """;

        var (frs, trs) = _sut.ExtractRequirementIds(body);

        Assert.Single(frs);
        Assert.Single(trs);

        await Task.CompletedTask.ConfigureAwait(true);
    }

    [Fact]
    public async Task ExtractRequirementIds_OnlyFrs_ReturnsEmptyTrs()
    {
        var body = "Found FR-SOCIAL-001 and FR-SOCIAL-002 but no TRs.";

        var (frs, trs) = _sut.ExtractRequirementIds(body);

        Assert.Equal(2, frs.Count);
        Assert.Empty(trs);

        await Task.CompletedTask.ConfigureAwait(true);
    }

    [Fact]
    public async Task ExtractRequirementIds_OnlyTrs_ReturnsEmptyFrs()
    {
        var body = "Found TR-DB-001 and TR-DB-002 but no FRs.";

        var (frs, trs) = _sut.ExtractRequirementIds(body);

        Assert.Empty(frs);
        Assert.Equal(2, trs.Count);

        await Task.CompletedTask.ConfigureAwait(true);
    }

    [Fact]
    public async Task AnalyzeAsync_UsesInfiniteCopilotTimeout()
    {
        var todo = new TodoFlatItem
        {
            Id = "TODO-1",
            Title = "Analyze me",
            Section = "mvp-app",
            Priority = "high",
            Done = false,
            Description = ["Needs long-running Copilot analysis."],
        };
        var todoService = Substitute.For<ITodoService>();
        todoService.GetByIdAsync("TODO-1", Arg.Any<CancellationToken>()).Returns(todo);
        todoService.UpdateAsync("TODO-1", Arg.Any<TodoUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(true));

        var accessor = TestWorkspaceAccessorHelper.Create(todoService);
        var promptOptions = Substitute.For<IOptionsMonitor<TodoPromptOptions>>();
        promptOptions.CurrentValue.Returns(new TodoPromptOptions());

        CopilotClientOptions? capturedOptions = null;
        var copilotClient = Substitute.For<ICopilotClient>();
        copilotClient.InvokeAsync(Arg.Any<string>(), Arg.Any<CopilotClientOptions?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedOptions = callInfo.ArgAt<CopilotClientOptions?>(1);
                return new CopilotResult
                {
                    State = CopilotResultState.Success,
                    Body = """{"functionalRequirements":["FR-TODO-001"],"technicalRequirements":["TR-TODO-001"]}""",
                };
            });

        var sut = new RequirementsService(
            copilotClient,
            accessor,
            promptOptions,
            NullLogger<RequirementsService>.Instance);

        var result = await sut.AnalyzeAsync("TODO-1").ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.NotNull(capturedOptions);
        Assert.Equal(Timeout.InfiniteTimeSpan, capturedOptions!.Timeout);
    }
}
