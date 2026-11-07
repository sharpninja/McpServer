using QBrainAi.Support.Mcp.Indexing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Indexing;

/// <summary>
/// TR-PLANNED-CORE-013; TR-MCP-QUALITY-001 ac-4: the per-test state and helpers that both halves of EmbeddingServiceTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once in a base class both halves derive from and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TR-PLANNED-CORE-013; TR-MCP-QUALITY-001 ac-4; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from EmbeddingServiceTests (_sut and Dispose) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal; xUnit still creates one instance of a half per case, so each case gets fresh state as before.</remarks>
public abstract class EmbeddingServiceTestsBase : IDisposable
{
    internal readonly EmbeddingService _sut = new(new EmbeddingOptions(), NullLogger<EmbeddingService>.Instance);

    public void Dispose() => _sut.Dispose();
}
