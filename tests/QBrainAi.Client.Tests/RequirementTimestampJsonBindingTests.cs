using System.Text.Json;
using QBrainAi.Client.Models;

namespace QBrainAi.Client.Tests;

/// <summary>
/// FR-MCP-040 ac-1 (PR 87 slice P): binds the unchanged <see cref="RequirementTimestampJsonContractTests"/> rows and
/// assertions, mock-gated in ug-r9-mockgate-p-1, to the typed client's own deserialization (the options and
/// source-generated <see cref="McpClientJsonContext"/> that RequirementsClient uses) for FR, TR, TEST and mapping payloads.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-02</term><description>FR-MCP-040 ac-1: binds the mock-gated rows to the client models (binding ug-r9-binding-p-1).</description></item>
/// </list>
/// </remarks>
public sealed class RequirementTimestampJsonBindingTests
{
    /// <summary>FR-MCP-040 ac-1: the deserialization options RequirementsClient reads with (case-insensitive names and the source-generated <see cref="McpClientJsonContext"/>).</summary>
    /// <remarks>2026-11-05, FR-MCP-040 ac-1: this entry documents the field, added on 2026-11-02 with the class (binding ug-r9-binding-p-1), because the ninth holistic read before hostile validation round 13 (finding e1; MEMORY-FACT-008) found it without XML documentation.</remarks>
    private static readonly JsonSerializerOptions s_clientOptions = new() { PropertyNameCaseInsensitive = true, TypeInfoResolver = McpClientJsonContext.Default };

    /// <summary>The client models read the sent times, keep the sent offset, and read absent or null values as null.</summary>
    [Fact]
    public void ClientModels_SatisfyMockGatedRows() =>
        RequirementTimestampJsonContractTests.AssertReader((kind, json) => kind switch
        {
            "fr" => Times(JsonSerializer.Deserialize<FrEntry>(json, s_clientOptions)!),
            "tr" => Times(JsonSerializer.Deserialize<TrEntry>(json, s_clientOptions)!),
            "test" => Times(JsonSerializer.Deserialize<TestEntry>(json, s_clientOptions)!),
            "mapping" => (JsonSerializer.Deserialize<FrTrMapping>(json, s_clientOptions)!.CreatedAtUtc, null),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        });

    /// <summary>FR-MCP-040 ac-1: the creation and update times a deserialized FR entry carries.</summary>
    /// <param name="entry">The FR entry.</param>
    /// <returns>Its CreatedAtUtc and UpdatedAtUtc.</returns>
    /// <remarks>2026-11-02, FR-MCP-040 ac-1: documented (MEMORY-FACT-008; hostile validation finding R9-F8).</remarks>
    private static (DateTimeOffset? Created, DateTimeOffset? Updated) Times(FrEntry entry) => (entry.CreatedAtUtc, entry.UpdatedAtUtc);

    /// <summary>FR-MCP-040 ac-1: the creation and update times a deserialized TR entry carries.</summary>
    /// <param name="entry">The TR entry.</param>
    /// <returns>Its CreatedAtUtc and UpdatedAtUtc.</returns>
    /// <remarks>2026-11-02, FR-MCP-040 ac-1: documented (MEMORY-FACT-008; hostile validation finding R9-F8).</remarks>
    private static (DateTimeOffset? Created, DateTimeOffset? Updated) Times(TrEntry entry) => (entry.CreatedAtUtc, entry.UpdatedAtUtc);

    /// <summary>FR-MCP-040 ac-1: the creation and update times a deserialized TEST entry carries.</summary>
    /// <param name="entry">The TEST entry.</param>
    /// <returns>Its CreatedAtUtc and UpdatedAtUtc.</returns>
    /// <remarks>2026-11-02, FR-MCP-040 ac-1: documented (MEMORY-FACT-008; hostile validation finding R9-F8).</remarks>
    private static (DateTimeOffset? Created, DateTimeOffset? Updated) Times(TestEntry entry) => (entry.CreatedAtUtc, entry.UpdatedAtUtc);
}
