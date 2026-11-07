using QBrainAi.Support.Mcp.Indexing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Indexing;

/// <summary>
/// TR-PLANNED-CORE-013; TR-MCP-QUALITY-001 ac-4: the per-test state and helpers that both halves of EmbeddingServiceTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once in a base class both halves derive from and compiled into both projects.
/// </summary>
/// <remarks>
/// <para>2026-11-07, TR-PLANNED-CORE-013; TR-MCP-QUALITY-001 ac-4; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from EmbeddingServiceTests (_sut and Dispose) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal; xUnit still creates one instance of a half per case, so each case gets fresh state as before.</para>
/// <para>2026-11-07, FR-MCP-004; TR-MCP-NUKE-019 AC-7: the service is built from the options each half passes: the parameterless constructor keeps the default options the integration half uses (the model at the default path, when present), and the unit half passes a model path in a fresh temporary folder that holds no model, so its cases do not depend on the host (ClaudeCode review of slice S9).</para>
/// </remarks>
public abstract class EmbeddingServiceTestsBase : IDisposable
{
    /// <summary>FR-MCP-004: the embedding service under test, one per test case.</summary>
    internal readonly EmbeddingService _sut;

    /// <summary>FR-MCP-004: builds the service from default options (the model and vocabulary at the default path).</summary>
    /// <remarks>2026-11-07, FR-MCP-004; TR-MCP-NUKE-019 AC-7: added; the integration half keeps the behavior of the earlier field initializer (ClaudeCode review of slice S9).</remarks>
    protected EmbeddingServiceTestsBase()
        : this(new EmbeddingOptions())
    {
    }

    /// <summary>FR-MCP-004: builds the service from the given options.</summary>
    /// <param name="options">The embedding options the service under test uses.</param>
    /// <remarks>2026-11-07, FR-MCP-004; TR-MCP-NUKE-019 AC-7: added so the unit half can pass a model path that holds no model (ClaudeCode review of slice S9).</remarks>
    protected EmbeddingServiceTestsBase(EmbeddingOptions options)
    {
        _sut = new EmbeddingService(options, NullLogger<EmbeddingService>.Instance);
    }

    /// <summary>FR-MCP-004: disposes the service under test after each case.</summary>
    public void Dispose() => _sut.Dispose();
}
