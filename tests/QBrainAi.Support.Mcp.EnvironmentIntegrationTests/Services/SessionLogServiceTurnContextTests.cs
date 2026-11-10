using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// AC-FR-MCP-SESSIONLOGCTX-001-001 / 002 / 003 / 005 and AC-TR-MCP-SESSIONLOG-006-002:
/// persist, merge, replace, and query of planFile/todoId through SessionLogService.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-SESSIONLOGCTX-001-001; TR-MCP-SESSIONLOG-006-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/SessionLogServiceTurnContextTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case moved unchanged with the class attributes; the state and helpers both halves use come from the abstract base class SessionLogServiceTurnContextTestsBase in Services/SessionLogServiceTurnContextTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class SessionLogServiceTurnContextTests : SessionLogServiceTurnContextTestsBase, IDisposable
{
    /// <summary>AC-TR-MCP-SESSIONLOG-006-003: import with omitted fields extracts then persists.</summary>
    /// <remarks>Integration: uses the real import extractor and its read-only profile scan without asserting profile contents (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task SubmitAsync_ImportMissingFields_ExtractsFromTurnText()
    {
        var dto = CreateSession("Cursor-20260304T113901Z-ctx-imp");
        dto.Turns!.First().PlanFile = null;
        dto.Turns!.First().TodoId = null;
        dto.Turns!.First().QueryText = "working MCP-IMPORT-001 on docs/plans/imported.md";
        await _sut.SubmitAsync(dto, sourceFilePath: @"E:\imports\session.json", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var stored = await _db.SessionLogTurns.SingleAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal("docs/plans/imported.md", stored.PlanFile);
        Assert.Equal("MCP-IMPORT-001", stored.TodoId);
    }
}
