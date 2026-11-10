using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using QBrainAi.Support.Mcp.UseCases;
using QBrainAi.Support.Mcp.UseCases.Commands;
using QBrainAi.Support.Mcp.UseCases.Models;
using QBrainAi.Support.Mcp.UseCases.Queries;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// Expanded-scope Use Case tests: PlantUML diagrams, approval versioning, product key hooks,
/// and shared Realizes coverage evaluation for traceability.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-USECASE-008; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/UseCaseExpandedScopeTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case moved unchanged with the class attributes; the state and helpers both halves use come from the abstract base class UseCaseExpandedScopeTestsBase in Services/UseCaseExpandedScopeTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class UseCaseExpandedScopeTests : UseCaseExpandedScopeTestsBase, IDisposable
{
    /// <summary>FR-MCP-USECASE-008: Approving increments version number.</summary>
    /// <remarks>Integration: binds real approval handling that resolves physical workspace identity (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task Approval_Approve_IncrementsVersion()
    {
        using var scope = _provider.CreateScope();
        var ws = scope.ServiceProvider.GetRequiredService<WorkspaceContext>();
        ws.WorkspacePath = _workspace;
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

        var created = await dispatcher.SendAsync(
            new CreateUseCaseCommand(_workspace, new CreateUseCaseRequest { Title = "Approve me", CreateBasicFlow = true }),
            CancellationToken.None).ConfigureAwait(true);
        Assert.True(created.IsSuccess, created.Error);
        Assert.Equal(1, created.Value!.VersionNumber);
        Assert.Equal("Draft", created.Value.ApprovalStatus);

        var submitted = await dispatcher.SendAsync(
            new SetUseCaseApprovalStatusCommand(_workspace, created.Value.UseCaseId, "Submitted"),
            CancellationToken.None).ConfigureAwait(true);
        Assert.True(submitted.IsSuccess, submitted.Error);

        var approved = await dispatcher.SendAsync(
            new SetUseCaseApprovalStatusCommand(_workspace, created.Value.UseCaseId, "Approved"),
            CancellationToken.None).ConfigureAwait(true);
        Assert.True(approved.IsSuccess, approved.Error);
        Assert.Equal("Approved", approved.Value!.ApprovalStatus);
        Assert.Equal(2, approved.Value.VersionNumber);
    }
}
