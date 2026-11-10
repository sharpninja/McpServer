using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Tests.TestSupport;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-011 / TR-MCP-MEMORY-SEARCH-002 / FR-MCP-MEMORY-011 (historical):
/// S2 Green acceptance for the dedicated memory indexer, ANN/FTS fusion,
/// and EmbeddingStatus transitions. The class runs in <see cref="ProcessEnvironmentCollection"/> because one of its
/// tests writes the process environment (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008).
/// </summary>
/// <remarks>
/// The TEST-MCP-MEMORY-011, TR-MCP-MEMORY-SEARCH-002 and FR-MCP-MEMORY-011 citations in this file, its
/// AC-TR-MCP-MEMORY-SEARCH-002 criteria included, are historical: those records are not in the requirements store
/// (hostile validation finding R6-F2), so they make no active coverage claim. The active claims are the
/// TR-MCP-USECASE-018 AC-006 and TR-MCP-USECASE-019 AC-008 classification of the integration test.
/// <para>2026-11-07, TEST-MCP-MEMORY-011; TR-MCP-MEMORY-SEARCH-002; FR-MCP-MEMORY-011; TR-MCP-USECASE-018 AC-006; TR-MCP-USECASE-019 AC-008; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Memory/MemoryIndexerTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 1 Category=Integration case moved unchanged with the class attributes; the state and helpers both halves use come from the abstract base class MemoryIndexerTestsBase in Memory/MemoryIndexerTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</para>
/// </remarks>
[Collection(ProcessEnvironmentCollection.Name)]
public sealed class MemoryIndexerTests : MemoryIndexerTestsBase, IDisposable
{
    /// <summary>
    /// AC-TR-MCP-MEMORY-SEARCH-002-09 (historical): Local ONNX path works without cloud credentials in the test host. The test
    /// clears both cloud keys, so it writes the process environment and is an integration test; it restores both
    /// keys afterwards, which it did not do before (TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008).
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task OnnxLocal_NoCloudRequired()
    {
        var previousOpenAi = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var previousAzure = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
            Environment.SetEnvironmentVariable("AZURE_OPENAI_API_KEY", null);
            var memory = await SeedAsync("onnx local embedding body").ConfigureAwait(true);

            var indexed = await _harness.IndexAsync(
                memory.Id,
                provider: "onnx",
                cloudEnabled: false,
                cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            var status = indexed.EmbeddingStatus
                ?? await _harness.GetEmbeddingStatusAsync(memory.Id).ConfigureAwait(true);

            Assert.True(indexed.StatusCode is 200 or 201, indexed.Error);
            Assert.Equal("ready", status);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", previousOpenAi);
            Environment.SetEnvironmentVariable("AZURE_OPENAI_API_KEY", previousAzure);
        }
    }
}
