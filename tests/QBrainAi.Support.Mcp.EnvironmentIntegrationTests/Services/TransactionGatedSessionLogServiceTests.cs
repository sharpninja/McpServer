using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.TransactionSecurity.Models;
using QBrainAi.TransactionSecurity.Options;
using QBrainAi.TransactionSecurity.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// TEST-MCP-161: QBAgent session-log mutations execute through the turn transaction
/// coordinator and restore durable session-log records when post-mutation commit
/// fails.
/// TEST-MCP-221 / FR-MCP-173 / TR-MCP-TXNKEY-001: non-QBAgent session-log mutations
/// persist without calling the coordinator (and therefore without keyserver signing)
/// even when turn transactions are required.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-161; TEST-MCP-221; FR-MCP-173; TR-MCP-TXNKEY-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/TransactionGatedSessionLogServiceTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case and the helpers only it uses (FindRepositoryRoot) moved unchanged with the class attributes; only the namespace changed.</remarks>
public sealed class TransactionGatedSessionLogServiceTests
{
    /// <summary>
    /// TR-MCP-DB-003: rollback code must not bypass tracked soft-delete protection with
    /// bulk physical delete operations.
    /// </summary>
    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void TransactionGatedSessionLogServiceSource_DoesNotUseBulkPhysicalDeletes()
    {
        var sourcePath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "QBrainAi.Support.Mcp",
            "Services",
            "TransactionGatedSessionLogService.cs");
        var source = File.ReadAllText(sourcePath);

        Assert.DoesNotContain("ExecuteDelete", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DELETE FROM", source, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot() => QBrainAi.Tests.Shared.TestRepositoryRoot.Resolve();
}
