using System.Reflection;
using System.Text.Json;
using QBrainAi.Cqrs.Mvvm;
using QBrainAi.Support.Mcp.Indexing;
using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.McpStdio;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Requirements;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Services.AgentHelp;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Server;
using NSubstitute;
using QBrainAi.Tests.Shared;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace QBrainAi.Support.Mcp.Tests.McpStdio;

/// <summary>
/// TEST-HANDOFF-006: the per-test state and helpers that both halves of HandoffMcpToolTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once in a base class both halves derive from and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TEST-HANDOFF-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from HandoffMcpToolTests (_db and Dispose) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal; xUnit still creates one instance of a half per case, so each case gets fresh state as before.</remarks>
public abstract class HandoffMcpToolTestsBase : IDisposable
{
    internal readonly McpDbContext _db = new(
        new DbContextOptionsBuilder<McpDbContext>().UseInMemoryDatabase("handoff-tools-" + Guid.NewGuid().ToString("N")).Options,
        new WorkspaceContext { WorkspacePath = @"Q:\__mcp_unit_test__\QBrainAi" });

    /// <inheritdoc />
    public void Dispose() => _db.Dispose();
}
