using System.Text.Json;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Tests.Shared;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 (historical) / FR-MCP-MEMORY-010 (historical): MCP tool and REST remember store equivalent fields.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-MEMORY-010; TEST-MCP-MEMORY-016; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole from QBrainAi.Support.Mcp.Tests (Memory/MemorySurfaceParityTests.cs) into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S6, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); MemoryS1Harness and TestRepositoryRoot are linked into this assembly; only the namespace changed.</remarks>
public sealed class MemorySurfaceParityTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-FR-MCP-MEMORY-010-48: Remember via MCP tool and REST produce equivalent stored fields.</summary>
    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task McpAndRest_EquivalentStore()
    {
        var payload = new MemoryRememberRequest
        {
            Title = "Parity",
            Content = "same payload both surfaces",
            Type = "fact",
            Tags = ["parity"],
            Confidence = 0.8,
        };

        var rest = await _harness.RememberAsync(payload, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.True(rest.StatusCode is 200 or 201, rest.Error);

        var contractPath = Path.Combine(FindRepoRoot(), "docs", "stdio-tool-contract.json");
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(contractPath, TestContext.Current.CancellationToken).ConfigureAwait(true));
        var tools = document.RootElement.GetProperty("tools").EnumerateArray();
        Assert.Contains(tools, tool => tool.GetProperty("name").GetString() == "memory_remember");

        var mcpStored = rest.Memory ?? await _harness.GetCompatAsync(rest.MemoryId!, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(payload.Content, mcpStored?.Content);
        Assert.Equal(payload.Title, mcpStored?.Title);
        Assert.Equal(payload.Type, mcpStored?.Type, ignoreCase: true);
    }

    private static string FindRepoRoot() => TestRepositoryRoot.Resolve();
}
