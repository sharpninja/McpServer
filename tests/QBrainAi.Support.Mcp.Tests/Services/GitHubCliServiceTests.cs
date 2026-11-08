using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Tests;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>TR-PLANNED-CORE-013 (historical), TR-GH-013-001 (historical): Unit tests for GitHubCliService with mocked IProcessRunner.</summary>
/// <remarks>2026-11-07, FR-MCP-FED-PATH-001 AC5; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class. The real-host binding helper AssertForeignWorkspaceWithoutRepositoryIsRefusedAsync, which only the Windows and Linux Category=Integration classes of this file call, moved with those two classes to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S8, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"), so this unit-owned project holds only unit cases; the unit cases and the other helpers are unchanged.</remarks>
public sealed class GitHubCliServiceTests
{
    private readonly IProcessRunner _processRunner = Substitute.For<IProcessRunner>();
    private readonly GitHubCliService _sut;

    public GitHubCliServiceTests()
    {
        _sut = new GitHubCliService(_processRunner, NullLogger<GitHubCliService>.Instance);
    }

    /// <summary>
    /// FR-MCP-005: when gh exits 0 with a one-issue JSON array, ListIssuesAsync succeeds and returns that one issue
    /// with its number (1) and title (Bug). Data: a substituted IProcessRunner returning the scripted gh output; state
    /// open, limit 10.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-005: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task ListIssuesAsync_WhenGhSucceeds_ReturnsIssues()
    {
        var json = """[{"number":1,"title":"Bug","url":"https://github.com/test/1","state":"open"}]""";
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, json, null));

        var result = await _sut.ListIssuesAsync("open", 10, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Single(result.Issues);
        Assert.Equal(1, result.Issues[0].Number);
        Assert.Equal("Bug", result.Issues[0].Title);
    }

    /// <summary>
    /// FR-MCP-005: when gh exits 1 with the error text not authenticated, ListIssuesAsync fails, returns that text as
    /// the error and returns an empty issue list. Data: a substituted IProcessRunner returning the scripted failure; no
    /// state filter, limit 10.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-005: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task ListIssuesAsync_WhenGhFails_ReturnsError()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(1, null, "not authenticated"));

        var result = await _sut.ListIssuesAsync(null, 10, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal("not authenticated", result.Error);
        Assert.Empty(result.Issues);
    }

    /// <summary>
    /// No requirement id applies: no FR, TR or TEST record in the store names pull-request listing; the case proves
    /// that when gh exits 0 with a one-pull-request JSON array, ListPullsAsync succeeds and returns that one pull
    /// request with number 42. Data: a substituted IProcessRunner returning the scripted gh output; state open, limit
    /// 10.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, TR-MCP-NUKE-019 AC-8: quarantined with the trait Quarantine=NoMatchingCriterion (operator 2026-11-07: "If none match, quarantine them (keep, but descope them) for later manual investigation."): no acceptance criterion in the requirement store matches the behavior. The FR, TR and TEST criteria, titles and descriptions were searched for pull request, pull-request, pull or pulls, PR or PRs, gh pr and ListPulls (uncited-matches.txt, test 21 of 23); the hits are CI triggers, repository renames and provenance notes inside unrelated criteria, and FR-MCP-032 (PR workflows) has no criteria. Test and MigrationIntegrationTests run no quarantined case; TestGroups runs it when this class is named; the test code is unchanged.</para>
    /// </remarks>
    [Fact]
    [Trait("Quarantine", "NoMatchingCriterion")]
    public async Task ListPullsAsync_WhenGhSucceeds_ReturnsPulls()
    {
        var json = """[{"number":42,"title":"Feature","url":"https://github.com/test/pr/42","state":"open"}]""";
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, json, null));

        var result = await _sut.ListPullsAsync("open", 10, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Single(result.Pulls);
        Assert.Equal(42, result.Pulls[0].Number);
    }

    /// <summary>
    /// FR-MCP-005: when gh prints the new issue URL, CreateIssueAsync succeeds, parses issue number 5 from it and
    /// returns a URL containing issues/5. Data: a substituted IProcessRunner returning the scripted URL line; title New
    /// issue, body Body text.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-005: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task CreateIssueAsync_WhenGhSucceeds_ReturnsUrl()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "https://github.com/test/issues/5\n", null));

        var result = await _sut.CreateIssueAsync("New issue", "Body text", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Equal(5, result.Number);
        Assert.Contains("issues/5", result.Url, StringComparison.Ordinal);
    }

    /// <summary>
    /// FR-MCP-005: when gh exits 1 with the error text auth required, CreateIssueAsync fails and returns that text as
    /// the error. Data: a substituted IProcessRunner returning the scripted failure; title New issue, no body.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-005: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task CreateIssueAsync_WhenGhFails_ReturnsError()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(1, null, "auth required"));

        var result = await _sut.CreateIssueAsync("New issue", null, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal("auth required", result.Error);
    }

    /// <summary>
    /// FR-MCP-005: when gh exits 0, CommentOnIssueAsync reports success for a comment on issue 1. Data: a substituted
    /// IProcessRunner returning an empty successful result; comment text test comment.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-005: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task CommentOnIssueAsync_WhenGhSucceeds_ReturnsSuccess()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "", null));

        var result = await _sut.CommentOnIssueAsync("1", "test comment", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
    }

    /// <summary>
    /// TEST-MCP-GH-006 (historical): Verifies that issue comment targets are emitted after an explicit end-of-options
    /// marker so a flag-shaped identifier cannot be reinterpreted as an injected gh CLI option.
    /// </summary>
    [Fact]
    public async Task CommentOnIssueAsync_WithFlagLikeIdentifier_UsesEndOfOptionsMarker()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "", null));

        var result = await _sut.CommentOnIssueAsync("--repo", "test comment", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        await _processRunner.Received(1).RunAsync("gh",
            Arg.Is<string>(a => a != null && a.Contains("issue comment --body \"test comment\" -- --repo", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// No requirement id applies: no FR, TR or TEST record in the store names pull-request comments; the case proves
    /// that CommentOnPullAsync succeeds and runs gh once with <c>pr comment --body "PR comment" -- 42</c>, the pull
    /// request number placed after the end-of-options marker. Data: a substituted IProcessRunner returning an empty
    /// successful result; pull request 42, comment PR comment.
    /// </summary>
    /// <remarks>
    /// <para>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</para>
    /// <para>2026-11-07, TR-MCP-NUKE-019 AC-8: quarantined with the trait Quarantine=NoMatchingCriterion (operator 2026-11-07: "If none match, quarantine them (keep, but descope them) for later manual investigation."): no acceptance criterion in the requirement store matches the behavior. The FR, TR and TEST criteria, titles and descriptions were searched for pull request, pull-request, pull or pulls, PR or PRs, gh pr, ListPulls, end-of-options, comment and CommentOnPull (uncited-matches.txt, test 22 of 23); the comment hits are titles and descriptions of issue-comment records without criteria, and TEST-MCP-GH-006, the historical end-of-options record, is not in the store. Test and MigrationIntegrationTests run no quarantined case; TestGroups runs it when this class is named; the test code is unchanged.</para>
    /// </remarks>
    [Fact]
    [Trait("Quarantine", "NoMatchingCriterion")]
    public async Task CommentOnPullAsync_VerifiesGhArgs()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "", null));

        var result = await _sut.CommentOnPullAsync("42", "PR comment", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        await _processRunner.Received(1).RunAsync("gh",
            Arg.Is<string>(a => a != null && a.Contains("pr comment --body \"PR comment\" -- 42", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// FR-MCP-005: when gh returns one issue JSON object, GetIssueAsync succeeds and maps its number, title, body,
    /// state, its one label (bug), its one assignee (user1), the milestone (v1.0), the author (reporter) and its one
    /// comment (On it). Data: a substituted IProcessRunner returning a scripted issue 42 JSON document.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-005: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task GetIssueAsync_WhenGhSucceeds_ReturnsIssueDetail()
    {
        var json = """
            {
                "number": 42,
                "title": "Bug report",
                "body": "Description here",
                "state": "OPEN",
                "url": "https://github.com/test/issues/42",
                "labels": [{"name": "bug", "color": "d73a4a", "description": "Something broken"}],
                "assignees": [{"login": "user1"}],
                "milestone": {"title": "v1.0"},
                "createdAt": "2026-02-15T00:00:00Z",
                "updatedAt": "2026-02-16T00:00:00Z",
                "closedAt": null,
                "author": {"login": "reporter"},
                "comments": [{"author": {"login": "dev"}, "body": "On it", "createdAt": "2026-02-15T12:00:00Z"}]
            }
            """;
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, json, null));

        var result = await _sut.GetIssueAsync(42, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.NotNull(result.Issue);
        Assert.Equal(42, result.Issue.Number);
        Assert.Equal("Bug report", result.Issue.Title);
        Assert.Equal("Description here", result.Issue.Body);
        Assert.Equal("OPEN", result.Issue.State);
        Assert.Single(result.Issue.Labels);
        Assert.Equal("bug", result.Issue.Labels[0].Name);
        Assert.Single(result.Issue.Assignees);
        Assert.Equal("user1", result.Issue.Assignees[0]);
        Assert.Equal("v1.0", result.Issue.Milestone);
        Assert.Equal("reporter", result.Issue.Author);
        Assert.Single(result.Issue.Comments);
        Assert.Equal("On it", result.Issue.Comments[0].Body);
    }

    /// <summary>
    /// FR-MCP-005: when gh exits 1 with the error text not found, GetIssueAsync fails and returns that text as the
    /// error message. Data: a substituted IProcessRunner returning the scripted failure; issue 999.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-005: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task GetIssueAsync_WhenGhFails_ReturnsError()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(1, null, "not found"));

        var result = await _sut.GetIssueAsync(999, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal("not found", result.ErrorMessage);
    }

    /// <summary>
    /// FR-MCP-005: UpdateIssueAsync succeeds and runs gh once with <c>issue edit 42</c> carrying the --title,
    /// --add-label and --remove-label options. Data: a substituted IProcessRunner returning the issue URL; an update
    /// request with title New title, added label bug and removed label wontfix.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-005: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task UpdateIssueAsync_AssemblesCorrectArgs()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "https://github.com/test/issues/42", null));

        var request = new GitHubIssueUpdateRequest
        {
            Title = "New title",
            AddLabels = new[] { "bug" },
            RemoveLabels = new[] { "wontfix" }
        };
        var result = await _sut.UpdateIssueAsync(42, request, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        await _processRunner.Received(1).RunAsync("gh",
            Arg.Is<string>(a => a != null && a.Contains("issue edit 42", StringComparison.Ordinal)
                && a.Contains("--title", StringComparison.Ordinal)
                && a.Contains("--add-label", StringComparison.Ordinal)
                && a.Contains("--remove-label", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// FR-MCP-005: CloseIssueAsync with the reason not_planned succeeds and runs gh once with <c>issue close 42</c> and
    /// <c>--reason not_planned</c>. Data: a substituted IProcessRunner returning an empty successful result; issue 42.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-005: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task CloseIssueAsync_WithReason_IncludesReasonFlag()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "", null));

        var result = await _sut.CloseIssueAsync(42, "not_planned", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        await _processRunner.Received(1).RunAsync("gh",
            Arg.Is<string>(a => a != null && a.Contains("issue close 42", StringComparison.Ordinal)
                && a.Contains("--reason not_planned", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-GH-006 (historical): Verifies that invalid close reasons are rejected before launching the GitHub CLI so
    /// attacker-controlled query strings cannot append extra flags through the close-issue reason parameter.
    /// </summary>
    [Fact]
    public async Task CloseIssueAsync_WithInvalidReason_DoesNotInvokeGh()
    {
        var result = await _sut.CloseIssueAsync(42, "completed --repo other/repo", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal("Invalid close reason. Allowed values: completed, not_planned.", result.ErrorMessage);
        await _processRunner.DidNotReceiveWithAnyArgs().RunAsync(default!, default!, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    /// <summary>
    /// FR-MCP-005: CloseIssueAsync without a reason succeeds and runs gh once with <c>issue close 42</c> and no
    /// --reason option. Data: a substituted IProcessRunner returning an empty successful result; issue 42.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-005: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task CloseIssueAsync_WithoutReason_NoReasonFlag()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "", null));

        var result = await _sut.CloseIssueAsync(42, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        await _processRunner.Received(1).RunAsync("gh",
            Arg.Is<string>(a => a != null && a.Contains("issue close 42", StringComparison.Ordinal)
                && !a.Contains("--reason", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// FR-MCP-005: ReopenIssueAsync succeeds and runs gh once with <c>issue reopen 42</c>. Data: a substituted
    /// IProcessRunner returning an empty successful result; issue 42.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-005: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task ReopenIssueAsync_WhenGhSucceeds_ReturnsSuccess()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "", null));

        var result = await _sut.ReopenIssueAsync(42, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        await _processRunner.Received(1).RunAsync("gh",
            Arg.Is<string>(a => a != null && a.Contains("issue reopen 42", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-GH-006 (historical): Verifies that invalid list-state values are rejected before process launch so state
    /// query parameters cannot smuggle additional GitHub CLI flags into the issue-list command line.
    /// </summary>
    [Fact]
    public async Task ListIssuesAsync_WithInvalidState_DoesNotInvokeGh()
    {
        var result = await _sut.ListIssuesAsync("open --repo other/repo", 10, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal("Invalid state. Allowed values: open, closed, all.", result.Error);
        await _processRunner.DidNotReceiveWithAnyArgs().RunAsync(default!, default!, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    /// <summary>
    /// FR-MCP-005: when gh returns a two-label JSON array, ListIssueLabelsAsync succeeds and returns both labels in
    /// order (bug, then enhancement). Data: a substituted IProcessRunner returning the scripted label JSON.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-005: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task ListIssueLabelsAsync_WhenGhSucceeds_ReturnsLabels()
    {
        var json = """[{"name":"bug","color":"d73a4a","description":"Something broken"},{"name":"enhancement","color":"a2eeef","description":"New feature"}]""";
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, json, null));

        var result = await _sut.ListIssueLabelsAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.NotNull(result.Labels);
        Assert.Equal(2, result.Labels.Count);
        Assert.Equal("bug", result.Labels[0].Name);
        Assert.Equal("enhancement", result.Labels[1].Name);
    }

    /// <summary>
    /// FR-MCP-005: when gh exits 1 with the error text not authenticated, ListIssueLabelsAsync fails and returns that
    /// text as the error message. Data: a substituted IProcessRunner returning the scripted failure.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-005: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task ListIssueLabelsAsync_WhenGhFails_ReturnsError()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(1, null, "not authenticated"));

        var result = await _sut.ListIssueLabelsAsync(ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal("not authenticated", result.ErrorMessage);
    }

    /// <summary>
    /// FR-MCP-063; TR-MCP-GH-004: when gh returns a one-run JSON array, ListWorkflowRunsAsync succeeds and maps the run
    /// id (101) and the workflow name (CI). Data: a substituted IProcessRunner returning the scripted run JSON; a query
    /// with limit 10.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-063; TR-MCP-GH-004: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task ListWorkflowRunsAsync_WhenGhSucceeds_ReturnsRuns()
    {
        var json = """[{"databaseId":101,"workflowName":"CI","displayTitle":"build","headBranch":"main","status":"completed","conclusion":"success","event":"push","url":"https://github.com/x/actions/runs/101","createdAt":"2026-03-01T00:00:00Z","updatedAt":"2026-03-01T00:05:00Z"}]""";
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, json, null));

        var result = await _sut.ListWorkflowRunsAsync(new GitHubWorkflowRunQuery { Limit = 10 }, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Single(result.Runs);
        Assert.Equal(101, result.Runs[0].RunId);
        Assert.Equal("CI", result.Runs[0].WorkflowName);
    }

    /// <summary>
    /// FR-MCP-063; TR-MCP-GH-004: when gh returns one run JSON object with one job and one step, GetWorkflowRunAsync
    /// succeeds and maps the run id (202), the one job and its one step (checkout). Data: a substituted IProcessRunner
    /// returning a scripted run 202 JSON document.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-063; TR-MCP-GH-004: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task GetWorkflowRunAsync_WhenGhSucceeds_ReturnsRun()
    {
        var json = """
            {
                "databaseId": 202,
                "workflowName": "Deploy",
                "displayTitle": "release",
                "headBranch": "main",
                "headSha": "abc123",
                "status": "completed",
                "conclusion": "success",
                "event": "workflow_dispatch",
                "url": "https://github.com/x/actions/runs/202",
                "attempt": 1,
                "createdAt": "2026-03-01T00:00:00Z",
                "updatedAt": "2026-03-01T00:10:00Z",
                "jobs": [
                    {
                        "name": "build",
                        "status": "completed",
                        "conclusion": "success",
                        "startedAt": "2026-03-01T00:01:00Z",
                        "completedAt": "2026-03-01T00:09:00Z",
                        "url": "https://github.com/x/actions/runs/202/job/1",
                        "steps": [
                            { "name": "checkout", "status": "completed", "conclusion": "success", "number": 1 }
                        ]
                    }
                ]
            }
            """;
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, json, null));

        var result = await _sut.GetWorkflowRunAsync(202, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.NotNull(result.Run);
        Assert.Equal(202, result.Run.RunId);
        Assert.Single(result.Run.Jobs);
        Assert.Single(result.Run.Jobs[0].Steps);
        Assert.Equal("checkout", result.Run.Jobs[0].Steps[0].Name);
    }

    /// <summary>
    /// FR-MCP-063; TR-MCP-GH-004: RerunWorkflowRunAsync succeeds and runs gh once with <c>run rerun 303</c>. Data: a
    /// substituted IProcessRunner returning an empty successful result; run 303.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-063; TR-MCP-GH-004: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task RerunWorkflowRunAsync_UsesRerunCommand()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "", null));

        var result = await _sut.RerunWorkflowRunAsync(303, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        await _processRunner.Received(1).RunAsync("gh",
            Arg.Is<string>(a => a != null && a.Contains("run rerun 303", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// FR-MCP-063; TR-MCP-GH-004: CancelWorkflowRunAsync succeeds and runs gh once with <c>run cancel 404</c>. Data: a
    /// substituted IProcessRunner returning an empty successful result; run 404.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-063; TR-MCP-GH-004: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task CancelWorkflowRunAsync_UsesCancelCommand()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "", null));

        var result = await _sut.CancelWorkflowRunAsync(404, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        await _processRunner.Received(1).RunAsync("gh",
            Arg.Is<string>(a => a != null && a.Contains("run cancel 404", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// TR-MCP-GH-003 with TR-MCP-FED-PATH-001: a stored workspace token is passed as the process-run override, and the
    /// host-native workspace (a temporary directory, so the case holds on every host OS) is the gh working directory.
    /// </summary>
    [Fact]
    public async Task ListIssuesAsync_WithStoredWorkspaceToken_UsesProcessRunRequestOverride()
    {
        var workspacePath = CreateHostWorkspace();
        try
        {
            var tokenStore = Substitute.For<IGitHubWorkspaceTokenStore>();
            tokenStore.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new GitHubWorkspaceTokenRecord(workspacePath, "gho_stored", DateTimeOffset.UtcNow, null));

            var options = Substitute.For<IOptionsMonitor<GitHubIntegrationOptions>>();
            options.CurrentValue.Returns(new GitHubIntegrationOptions
            {
                PreferStoredToken = true,
                AllowCliFallback = true,
            });

            var services = new ServiceCollection();
            services.AddScoped(_ => new WorkspaceContext { WorkspacePath = workspacePath });
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var accessor = Substitute.For<IHttpContextAccessor>();
            accessor.HttpContext.Returns(new DefaultHttpContext { RequestServices = scope.ServiceProvider });

            _processRunner.RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>())
                .Returns(new ProcessRunResult(0, "[]", null));

            var workspaceAccessor = TestWorkspaceAccessorHelper.Create(Substitute.For<ITodoService>(), repoRoot: workspacePath);
            var sut = new GitHubCliService(_processRunner, NullLogger<GitHubCliService>.Instance, tokenStore, accessor, options, workspaceAccessor);
            var result = await sut.ListIssuesAsync("open", 10, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.True(result.Success);
            await _processRunner.Received(1).RunAsync(
                Arg.Is<ProcessRunRequest>(r => UsesHostWorkspace(r, workspacePath) && r!.GitHubTokenOverride == "gho_stored"),
                Arg.Any<CancellationToken>()).ConfigureAwait(true);
        }
        finally
        {
            Directory.Delete(workspacePath, true);
        }
    }

    /// <summary>
    /// TR-MCP-GH-003 with TR-MCP-FED-PATH-001: the host-native workspace from the accessor (a temporary directory, so
    /// the case holds on every host OS) is the gh working directory.
    /// </summary>
    [Fact]
    public async Task ListIssuesAsync_WithWorkspaceAccessor_UsesWorkspaceWorkingDirectory()
    {
        var workspacePath = CreateHostWorkspace();
        try
        {
            _processRunner.RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>())
                .Returns(new ProcessRunResult(0, "[]", null));

            var workspaceAccessor = TestWorkspaceAccessorHelper.Create(Substitute.For<ITodoService>(), repoRoot: workspacePath);
            var sut = new GitHubCliService(_processRunner, NullLogger<GitHubCliService>.Instance, workspaceAccessor: workspaceAccessor);

            var result = await sut.ListIssuesAsync("open", 5, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.True(result.Success);
            await _processRunner.Received(1).RunAsync(
                Arg.Is<ProcessRunRequest>(request => UsesHostWorkspace(request, workspacePath)),
                Arg.Any<CancellationToken>()).ConfigureAwait(true);
        }
        finally
        {
            Directory.Delete(workspacePath, true);
        }
    }

    /// <summary>
    /// TR-MCP-FED-PATH-001, BDPv4 mock gate for the working-directory predicates used above: they accept the
    /// specified requests and reject a missing directory, a different executable, a re-rooted foreign path (the
    /// defect: the foreign path joined under the process directory) and a foreign safe.directory, using synthetic
    /// requests only.
    /// </summary>
    [Fact]
    public void WorkingDirectoryPredicates_DiscriminateSpecifiedRequests()
    {
        var host = Path.Combine(Path.GetTempPath(), "mcp-gh-gate");
        Assert.True(UsesHostWorkspace(new ProcessRunRequest("gh", "issue list", WorkingDirectory: Path.GetFullPath(host)), host));
        Assert.False(UsesHostWorkspace(new ProcessRunRequest("gh", "issue list"), host));
        Assert.False(UsesHostWorkspace(new ProcessRunRequest("git", "status", WorkingDirectory: Path.GetFullPath(host)), host));

        var reRooted = Path.Combine(AppContext.BaseDirectory, "C:/foreign/workspace");
        Assert.True(OpensWorkspaceOnHost(new ProcessRunRequest("gh", "issue list", WorkingDirectory: reRooted)));
        Assert.True(OpensWorkspaceOnHost(new ProcessRunRequest(
            "gh",
            "issue list",
            EnvironmentVariables: new Dictionary<string, string?> { ["GIT_CONFIG_COUNT"] = "1" })));
        Assert.False(OpensWorkspaceOnHost(new ProcessRunRequest("gh", "issue list")));
    }

    private static bool UsesHostWorkspace(ProcessRunRequest? request, string workspacePath)
        => request != null
           && request.FileName == "gh"
           && request.WorkingDirectory == Path.GetFullPath(workspacePath);

    private static bool OpensWorkspaceOnHost(ProcessRunRequest? request)
        => request != null
           && (request.WorkingDirectory is not null
               || (request.EnvironmentVariables?.ContainsKey("GIT_CONFIG_COUNT") ?? false));

    private static string CreateHostWorkspace()
    {
        var workspacePath = Path.Combine(Path.GetTempPath(), $"mcp-gh-ws-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspacePath);
        return workspacePath;
    }

    /// <summary>
    /// BUG-GITHUB-001: Verifies service-account GitHub CLI calls pass a command-scoped
    /// <c>safe.directory</c> override for the active workspace instead of requiring a
    /// global Git configuration mutation under the Windows service account.
    /// </summary>
    [Fact]
    public async Task ListIssuesAsync_WithWorkspaceAccessor_AddsCommandScopedSafeDirectory()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "[]", null));

        var workspacePath = Path.Combine(Path.GetTempPath(), $"mcp-gh-safe-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspacePath);
        try
        {
            var workspaceAccessor = TestWorkspaceAccessorHelper.Create(Substitute.For<ITodoService>(), repoRoot: workspacePath);
            var sut = new GitHubCliService(_processRunner, NullLogger<GitHubCliService>.Instance, workspaceAccessor: workspaceAccessor);

            var result = await sut.ListIssuesAsync("open", 5, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.True(result.Success);
            await _processRunner.Received(1).RunAsync(
                Arg.Is<ProcessRunRequest>(request => HasSafeDirectoryEnvironment(request, workspacePath)),
                Arg.Any<CancellationToken>()).ConfigureAwait(true);
        }
        finally
        {
            Directory.Delete(workspacePath, true);
        }
    }

    /// <summary>
    /// BUG-GITHUB-001: Verifies an explicit GitHub repository selector uses
    /// <c>gh --repo</c> and avoids local repository discovery entirely.
    /// </summary>
    [Fact]
    public async Task CreateIssueAsync_WithConfiguredRepository_AddsRepoOptionAndAvoidsWorkspaceCwd()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "https://github.com/example-org/example-repo/issues/123\n", null));

        var options = Substitute.For<IOptionsMonitor<GitHubIntegrationOptions>>();
        options.CurrentValue.Returns(new GitHubIntegrationOptions
        {
            Repository = "https://github.com/example-org/example-repo.git"
        });
        var workspaceAccessor = TestWorkspaceAccessorHelper.Create(Substitute.For<ITodoService>(), repoRoot: "C:\\repo\\workspace");
        var sut = new GitHubCliService(_processRunner, NullLogger<GitHubCliService>.Instance, githubOptions: options, workspaceAccessor: workspaceAccessor);

        var result = await sut.CreateIssueAsync("New issue", "Body", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Equal(123, result.Number);
        await _processRunner.Received(1).RunAsync(
            "gh",
            Arg.Is<string>(arguments => arguments != null
                && arguments.Contains("issue create", StringComparison.Ordinal)
                && arguments.Contains("--repo example-org/example-repo", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    private static bool HasSafeDirectoryEnvironment(ProcessRunRequest? request, string workspacePath)
        => request != null
           && request.WorkingDirectory == Path.GetFullPath(workspacePath)
           && request.EnvironmentVariables is not null
           && request.EnvironmentVariables.TryGetValue("GIT_CONFIG_COUNT", out var count)
           && count == "1"
           && request.EnvironmentVariables.TryGetValue("GIT_CONFIG_KEY_0", out var key)
           && key == "safe.directory"
           && request.EnvironmentVariables.TryGetValue("GIT_CONFIG_VALUE_0", out var value)
           && value == Path.GetFullPath(workspacePath);
}
