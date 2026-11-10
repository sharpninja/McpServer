using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using QBrainAi.Tests.Shared;

namespace QBrainAi.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-010; TR-MCP-MEMORY-MODEL-002: the per-test state and helpers that both halves of MemoryMigrationTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once in a base class both halves derive from and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-MEMORY-010; TR-MCP-MEMORY-MODEL-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from MemoryMigrationTests (_harness and Dispose) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal; xUnit still creates one instance of a half per case, so each case gets fresh state as before.</remarks>
public abstract class MemoryMigrationTestsBase : IDisposable
{
    internal readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();
}
