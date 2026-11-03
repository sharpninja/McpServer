using QBrainAi.Client.Models;

namespace QBrainAi.Repl.Core.Tests;

/// <summary>
/// FR-MCP-040 ac-1 (PR 87 slice P): binds the unchanged <see cref="RequirementTimestampAdapterContractTests"/> rows and
/// assertions, mock-gated in ug-r9-mockgate-p-1, to the production FR, TR, TEST and mapping adapters over client entries
/// that carry fixed stored times. No clock, store or environment is read.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-02</term><description>FR-MCP-040 ac-1: binds the mock-gated rows to the production adapters (binding ug-r9-binding-p-1).</description></item>
/// </list>
/// </remarks>
public sealed class RequirementTimestampAdapterBindingTests
{
    /// <summary>The production adapters report the stored times, keep the stored offset, and report null when nothing is stored.</summary>
    [Fact]
    public void ProductionAdapters_SatisfyMockGatedRows() =>
        RequirementTimestampAdapterContractTests.AssertAdapters((kind, created, updated) => kind switch
        {
            "fr" => Read(new FrItemAdapter(new FrEntry { Id = "FR-X-001", CreatedAtUtc = created, UpdatedAtUtc = updated })),
            "tr" => Read(new TrItemAdapter(new TrEntry { Id = "TR-X-001", CreatedAtUtc = created, UpdatedAtUtc = updated })),
            "test" => Read(new TestItemAdapter(new TestEntry { Id = "TEST-X-001", CreatedAtUtc = created, UpdatedAtUtc = updated })),
            "mapping" => (new MappingItemAdapter("FR-X-001", "TR-X-001", null, null, created).CreatedAt, null),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        });

    /// <summary>FR-MCP-040 ac-1: the creation and update texts a REPL FR item reports.</summary>
    /// <param name="item">The FR item.</param>
    /// <returns>Its CreatedAt and UpdatedAt.</returns>
    /// <remarks>2026-11-02, FR-MCP-040 ac-1: documented (MEMORY-FACT-008; hostile validation finding R9-F8).</remarks>
    private static (string? Created, string? Updated) Read(IFrItem item) => (item.CreatedAt, item.UpdatedAt);

    /// <summary>FR-MCP-040 ac-1: the creation and update texts a REPL TR item reports.</summary>
    /// <param name="item">The TR item.</param>
    /// <returns>Its CreatedAt and UpdatedAt.</returns>
    /// <remarks>2026-11-02, FR-MCP-040 ac-1: documented (MEMORY-FACT-008; hostile validation finding R9-F8).</remarks>
    private static (string? Created, string? Updated) Read(ITrItem item) => (item.CreatedAt, item.UpdatedAt);

    /// <summary>FR-MCP-040 ac-1: the creation and update texts a REPL TEST item reports.</summary>
    /// <param name="item">The TEST item.</param>
    /// <returns>Its CreatedAt and UpdatedAt.</returns>
    /// <remarks>2026-11-02, FR-MCP-040 ac-1: documented (MEMORY-FACT-008; hostile validation finding R9-F8).</remarks>
    private static (string? Created, string? Updated) Read(ITestItem item) => (item.CreatedAt, item.UpdatedAt);
}
