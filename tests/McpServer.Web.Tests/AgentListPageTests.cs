using Bunit;
using McpServer.Cqrs;
using McpServer.UI.Core;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using McpServer.UI.Core.ViewModels;
using McpServer.Web.Pages.Agents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Web.Tests;

public sealed class AgentListPageTests
{
    [Fact]
    public void AgentList_ShowsLoadingBranch_WhenDefinitionsInFlight()
    {
        var gate = new TaskCompletionSource<ListAgentDefinitionsResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var ctx = CreateTestContext(
            new WorkspaceApiClientStub(),
            new AgentApiClientStub
            {
                OnListDefinitionsAsync = _ => gate.Task
            });

        ctx.Services.GetRequiredService<WorkspaceContextViewModel>().ActiveWorkspacePath = "E:/repo";
        var cut = ctx.Render<AgentList>();
        cut.WaitForAssertion(() => Assert.Contains("Loading global agent definitions...", cut.Markup, StringComparison.Ordinal));
        gate.SetResult(new ListAgentDefinitionsResult([], 0));
    }

    [Fact]
    public void AgentList_ShowsErrorBranch_WhenDefinitionsFail()
    {
        using var ctx = CreateTestContext(
            new WorkspaceApiClientStub(),
            new AgentApiClientStub
            {
                OnListDefinitionsAsync = _ => Task.FromException<ListAgentDefinitionsResult>(new InvalidOperationException("agent load failure"))
            });

        var cut = ctx.Render<AgentList>();
        cut.WaitForAssertion(() => Assert.Contains("Failed to load global agents", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void AgentList_ShowsEmptyBranch_WhenNoDefinitions()
    {
        using var ctx = CreateTestContext(
            new WorkspaceApiClientStub(),
            new AgentApiClientStub
            {
                OnListDefinitionsAsync = _ => Task.FromResult(new ListAgentDefinitionsResult([], 0)),
                OnListWorkspaceAgentsAsync = (_, _) => Task.FromResult(new ListWorkspaceAgentsResult([], 0))
            });

        var cut = ctx.Render<AgentList>();
        cut.WaitForAssertion(() => Assert.Contains("No global agents found", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void AgentList_ShowsDataAndLoadsDefinitionDetail_OnClick()
    {
        using var ctx = CreateTestContext(
            new WorkspaceApiClientStub
            {
                OnListWorkspacesAsync = _ => Task.FromResult(new ListWorkspacesResult(
                    [new WorkspaceSummary("E:/repo", "Repo", true, true)],
                    1))
            },
            new AgentApiClientStub
            {
                OnListDefinitionsAsync = _ => Task.FromResult(new ListAgentDefinitionsResult(
                    [new AgentDefinitionSummaryItem("assistant", "Assistant", true)],
                    1)),
                OnGetDefinitionAsync = (_, _) => Task.FromResult<AgentDefinitionDetail?>(new AgentDefinitionDetail(
                    "assistant",
                    "Assistant",
                    "run-assistant",
                    "AGENTS.md",
                    ["gpt-5"],
                    "feature/{agent}/{task}",
                    "seed",
                    true)),
                OnListWorkspaceAgentsAsync = (_, _) => Task.FromResult(new ListWorkspaceAgentsResult(
                    [
                        new WorkspaceAgentItem(
                            1,
                            "assistant",
                            "E:/repo",
                            true,
                            false,
                            null,
                            null,
                            "worktree",
                            null,
                            [],
                            null,
                            null,
                            string.Empty,
                            [],
                            DateTime.UtcNow,
                            null)
                    ],
                    1)),
                OnGetWorkspaceAgentAsync = (_, _) => Task.FromResult<WorkspaceAgentDetail?>(new WorkspaceAgentDetail(
                    1,
                    "assistant",
                    "E:/repo",
                    true,
                    false,
                    null,
                    null,
                    "worktree",
                    null,
                    [],
                    null,
                    null,
                    string.Empty,
                    [],
                    DateTime.UtcNow,
                    null))
            });

        var cut = ctx.Render<AgentList>();
        cut.WaitForAssertion(() => Assert.Contains("assistant", cut.Markup, StringComparison.Ordinal));

        var definitionDetailsButton = cut.FindAll("button")
            .First(button => string.Equals(button.TextContent.Trim(), "Details", StringComparison.Ordinal));
        definitionDetailsButton.Click();
        cut.WaitForAssertion(() => Assert.Contains("Definition Detail -", cut.Markup, StringComparison.Ordinal));

    }

    private static Bunit.BunitContext CreateTestContext(IWorkspaceApiClient workspaceApiClient, IAgentApiClient agentApiClient)
    {
        var ctx = new Bunit.BunitContext();
        ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        ctx.Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        ctx.Services.AddSingleton<Dispatcher>();
        ctx.Services.AddUiCore();
        ctx.Services.AddSingleton(workspaceApiClient);
        ctx.Services.AddSingleton(agentApiClient);
        return ctx;
    }

    private sealed class WorkspaceApiClientStub : IWorkspaceApiClient
    {
        public Func<CancellationToken, Task<ListWorkspacesResult>>? OnListWorkspacesAsync { get; init; }

        public Task<ListWorkspacesResult> ListWorkspacesAsync(CancellationToken ct = default)
            => OnListWorkspacesAsync?.Invoke(ct) ?? Task.FromResult(new ListWorkspacesResult([], 0));

        public Task<WorkspaceDetail?> GetWorkspaceAsync(string workspacePath, CancellationToken ct = default) => Task.FromResult<WorkspaceDetail?>(null);
        public Task<bool> UpdateWorkspacePolicyAsync(UpdateWorkspacePolicyCommand command, CancellationToken ct = default) => Task.FromResult(false);
        public Task<WorkspaceMutationOutcome> CreateWorkspaceAsync(CreateWorkspaceCommand command, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<WorkspaceMutationOutcome> UpdateWorkspaceAsync(UpdateWorkspaceCommand command, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<WorkspaceMutationOutcome> DeleteWorkspaceAsync(DeleteWorkspaceCommand command, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<WorkspaceProcessState> GetWorkspaceStatusAsync(string workspacePath, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<WorkspaceProcessState> StartWorkspaceAsync(string workspacePath, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<WorkspaceProcessState> StopWorkspaceAsync(string workspacePath, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<WorkspaceHealthState> CheckWorkspaceHealthAsync(string workspacePath, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<WorkspaceGlobalPromptState> GetWorkspaceGlobalPromptAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public Task<WorkspaceGlobalPromptState> UpdateWorkspaceGlobalPromptAsync(UpdateWorkspaceGlobalPromptCommand command, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<WorkspaceInitInfo> InitWorkspaceAsync(string workspacePath, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class AgentApiClientStub : IAgentApiClient
    {
        public Func<CancellationToken, Task<ListAgentDefinitionsResult>>? OnListDefinitionsAsync { get; init; }
        public Func<string, CancellationToken, Task<AgentDefinitionDetail?>>? OnGetDefinitionAsync { get; init; }
        public Func<ListWorkspaceAgentsQuery, CancellationToken, Task<ListWorkspaceAgentsResult>>? OnListWorkspaceAgentsAsync { get; init; }
        public Func<GetWorkspaceAgentQuery, CancellationToken, Task<WorkspaceAgentDetail?>>? OnGetWorkspaceAgentAsync { get; init; }

        public Task<ListAgentDefinitionsResult> ListDefinitionsAsync(CancellationToken cancellationToken = default)
            => OnListDefinitionsAsync?.Invoke(cancellationToken) ?? Task.FromResult(new ListAgentDefinitionsResult([], 0));

        public Task<AgentDefinitionDetail?> GetDefinitionAsync(string agentType, CancellationToken cancellationToken = default)
            => OnGetDefinitionAsync?.Invoke(agentType, cancellationToken) ?? Task.FromResult<AgentDefinitionDetail?>(null);

        public Task<ListWorkspaceAgentsResult> ListWorkspaceAgentsAsync(ListWorkspaceAgentsQuery query, CancellationToken cancellationToken = default)
            => OnListWorkspaceAgentsAsync?.Invoke(query, cancellationToken) ?? Task.FromResult(new ListWorkspaceAgentsResult([], 0));

        public Task<WorkspaceAgentDetail?> GetWorkspaceAgentAsync(GetWorkspaceAgentQuery query, CancellationToken cancellationToken = default)
            => OnGetWorkspaceAgentAsync?.Invoke(query, cancellationToken) ?? Task.FromResult<WorkspaceAgentDetail?>(null);

        public Task<AgentMutationOutcome> UpsertDefinitionAsync(UpsertAgentDefinitionCommand command, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AgentMutationOutcome> DeleteDefinitionAsync(DeleteAgentDefinitionCommand command, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AgentSeedOutcome> SeedDefaultsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AgentMutationOutcome> UpsertWorkspaceAgentAsync(UpsertWorkspaceAgentCommand command, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AgentMutationOutcome> AssignWorkspaceAgentAsync(AssignWorkspaceAgentCommand command, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AgentMutationOutcome> DeleteWorkspaceAgentAsync(DeleteWorkspaceAgentCommand command, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AgentMutationOutcome> BanAgentAsync(BanAgentCommand command, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AgentMutationOutcome> UnbanAgentAsync(UnbanAgentCommand command, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AgentMutationOutcome> LogEventAsync(LogAgentEventCommand command, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AgentEventsResult> GetEventsAsync(GetAgentEventsQuery query, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AgentValidateOutcome> ValidateAsync(ValidateAgentQuery query, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
