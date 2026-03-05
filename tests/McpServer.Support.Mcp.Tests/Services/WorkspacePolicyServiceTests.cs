using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Unit tests for <see cref="WorkspacePolicyService"/>.
/// </summary>
public sealed class WorkspacePolicyServiceTests : IDisposable
{
    private readonly McpDbContext _db;
    private readonly SessionLogService _sessionLogService;
    private readonly IWorkspacePolicyDirectiveParser _parser;
    private readonly IWorkspaceService _workspaceService;
    private readonly WorkspacePolicyService _sut;
    private readonly Dictionary<string, WorkspaceDto> _workspaces;

    public WorkspacePolicyServiceTests()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"WorkspacePolicyTests_{Guid.NewGuid():N}")
            .Options;

        _db = new McpDbContext(options);
        _db.Database.EnsureCreated();

        _sessionLogService = new SessionLogService(
            _db,
            NullLogger<SessionLogService>.Instance,
            Substitute.For<IChangeEventBus>());

        _parser = Substitute.For<IWorkspacePolicyDirectiveParser>();
        _workspaceService = Substitute.For<IWorkspaceService>();
        _workspaces = new Dictionary<string, WorkspaceDto>(StringComparer.OrdinalIgnoreCase);

        var workspacePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), $"policy-ws-{Guid.NewGuid():N}"));
        _workspaces[workspacePath] = new WorkspaceDto
        {
            WorkspacePath = workspacePath,
            Name = "policy-test",
            TodoPath = "docs/todo.yaml",
            IsPrimary = false,
            IsEnabled = true,
            DateTimeCreated = DateTimeOffset.UtcNow,
            DateTimeModified = DateTimeOffset.UtcNow,
            StatusPrompt = TodoPromptDefaults.StatusPrompt,
            ImplementPrompt = TodoPromptDefaults.ImplementPrompt,
            PlanPrompt = TodoPromptDefaults.PlanPrompt,
            BannedLicenses = [],
            BannedCountriesOfOrigin = [],
            BannedOrganizations = [],
            BannedIndividuals = [],
        };

        _workspaceService.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var key = Path.GetFullPath((string)ci[0]!);
                _workspaces.TryGetValue(key, out var dto);
                return dto;
            });

        _workspaceService.ListAsync(Arg.Any<CancellationToken>())
            .Returns(new WorkspaceListResult(_workspaces.Values.ToList(), _workspaces.Count));

        _workspaceService.UpdateAsync(Arg.Any<string>(), Arg.Any<WorkspaceUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var key = Path.GetFullPath((string)ci[0]!);
                var request = (WorkspaceUpdateRequest)ci[1]!;
                if (!_workspaces.TryGetValue(key, out var existing))
                    return new WorkspaceMutationResult(false, $"Workspace not found: {key}");

                var updated = existing with
                {
                    BannedLicenses = request.BannedLicenses ?? existing.BannedLicenses,
                    BannedCountriesOfOrigin = request.BannedCountriesOfOrigin ?? existing.BannedCountriesOfOrigin,
                    BannedOrganizations = request.BannedOrganizations ?? existing.BannedOrganizations,
                    BannedIndividuals = request.BannedIndividuals ?? existing.BannedIndividuals,
                    DateTimeModified = DateTimeOffset.UtcNow,
                };

                _workspaces[key] = updated;
                return new WorkspaceMutationResult(true, Workspace: updated);
            });

        var todoService = Substitute.For<ITodoService>();
        var accessor = TestWorkspaceAccessorHelper.Create(todoService, repoRoot: workspacePath);

        _sut = new WorkspacePolicyService(
            _parser,
            _workspaceService,
            _sessionLogService,
            accessor,
            _db,
            NullLogger<WorkspacePolicyService>.Instance);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task ApplyAsync_ValidDirective_UpdatesWorkspaceAndLogsPolicyChange()
    {
        var workspacePath = _workspaces.Keys.Single();
        _parser.ParseAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new WorkspacePolicyParseResult
            {
                Success = true,
                Directive = new WorkspacePolicyDirective
                {
                    Action = "add",
                    Category = "license",
                    Scope = "current",
                    Values = ["GPL-3.0"],
                    Parser = "fallback",
                }
            });

        var result = await _sut.ApplyAsync(new WorkspacePolicyApplyRequest
        {
            Directive = "Ban GPL-3.0 in this workspace",
            WorkspacePath = workspacePath,
        }).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Single(result.WorkspaceResults);
        Assert.Contains("GPL-3.0", result.WorkspaceResults[0].AfterValues);

        var action = await _db.SessionLogActions.FirstOrDefaultAsync().ConfigureAwait(true);
        Assert.NotNull(action);
        Assert.Equal("policy_change", action!.Type);
    }
}
