using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// Unit tests for <see cref="FederatedSessionLogService"/>. Validates merge semantics,
/// pass-through when federation is disabled, and graceful degradation on remote failure.
/// FR-MCP-083, TEST-MCP-FED-002 (historical).
/// </summary>
/// <remarks>2026-11-07, FR-MCP-083; TEST-MCP-FED-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/FederatedSessionLogServiceTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case moved unchanged with the class attributes; the state and helpers both halves use come from the abstract base class FederatedSessionLogServiceTestsBase in Services/FederatedSessionLogServiceTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class FederatedSessionLogServiceTests : FederatedSessionLogServiceTestsBase
{
    /// <summary>
    /// AC-FR-MCP-SESSIONLOGCTX-001-007: federation apply/submit with omitted fields
    /// persists a validated pair (None if extraction finds nothing).
    /// </summary>
    /// <remarks>Integration: uses the real import extractor, which can scan the user profile read-only without asserting its contents (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task Apply_OmittedFields_PersistsProperValue()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<QBrainAi.Support.Mcp.Storage.McpDbContext>()
            .UseInMemoryDatabase("fed-omit-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new QBrainAi.Support.Mcp.Storage.McpDbContext(options);
        db.Database.EnsureCreated();
        var workspace = Path.Combine(Path.GetTempPath(), "fed-omit");
        db.OverrideWorkspaceId(workspace);
        var inner = new SessionLogService(
            db,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SessionLogService>.Instance,
            workspaceContext: new WorkspaceContext { WorkspacePath = workspace });
        var sut = new FederatedSessionLogService(
            inner,
            CreateRegistry(enabled: false),
            _client,
            NullLogger<FederatedSessionLogService>.Instance);

        await sut.SubmitAsync(
            new UnifiedSessionLogDto
            {
                SourceType = "Cursor",
                SessionId = "Cursor-20260304T113901Z-fed",
                Title = "fed",
                Turns =
                [
                    new UnifiedRequestEntryDto
                    {
                        RequestId = "req-20260304T113901Z-fed",
                        QueryText = "plain text with no identifiers",
                        Status = "completed",
                    },
                ],
            },
            sourceFilePath: Path.Combine(workspace, "import.json"),
            cancellationToken: TestContext.Current.CancellationToken);

        var stored = Assert.Single(db.SessionLogTurns);
        Assert.Equal("None", stored.PlanFile);
        Assert.Equal("None", stored.TodoId);
    }
}
