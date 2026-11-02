using System.Text.Json;
using QBrainAi.Client.Models;

namespace QBrainAi.Client.Tests;

/// <summary>
/// FR-MCP-040 ac-1 (PR 87 slice P): binds the unchanged <see cref="RequirementTimestampJsonContractTests"/> rows and
/// assertions, mock-gated in ug-r9-mockgate-p-1, to the typed client's own deserialization (the options and
/// source-generated <see cref="McpClientJsonContext"/> that RequirementsClient uses) for FR, TR, TEST and mapping payloads.
/// </summary>
public sealed class RequirementTimestampJsonBindingTests
{
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

    private static (DateTimeOffset? Created, DateTimeOffset? Updated) Times(FrEntry entry) => (entry.CreatedAtUtc, entry.UpdatedAtUtc);

    private static (DateTimeOffset? Created, DateTimeOffset? Updated) Times(TrEntry entry) => (entry.CreatedAtUtc, entry.UpdatedAtUtc);

    private static (DateTimeOffset? Created, DateTimeOffset? Updated) Times(TestEntry entry) => (entry.CreatedAtUtc, entry.UpdatedAtUtc);
}
