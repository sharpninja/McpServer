using System.Text.Json;
using System.Text.Json.Nodes;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-MEMORY-BENCH-002-03 / TR-MCP-MEMORY-BENCH-002-09 / TR-MCP-MEMORY-BENCH-002-10:
/// Result schema validation. Missing token fields are a hard fail, not a warning.
/// </summary>
public static class MemoryBenchResultSchema
{
    /// <summary>Canonical result schema path relative to the repository root.</summary>
    public const string RelativePath = "docs/benchmarks/schemas/memory-bench-result.schema.json";

    /// <summary>Canonical pack schema path relative to the repository root.</summary>
    public const string PackSchemaRelativePath = "docs/benchmarks/schemas/memory-prompt-pack.schema.json";

    /// <summary>Validates a run before write. Throws <see cref="MemoryBenchTokenSchemaException"/> on missing tokens.</summary>
    public static void ValidateBeforeWrite(MemoryBenchRunResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Cells.Count == 0)
            throw new MemoryBenchTokenSchemaException("Bench run has no cells to finalize.");

        foreach (var cell in result.Cells)
            ValidateCell(cell);
    }

    /// <summary>Validates one cell. Token fields are required integers ≥ 0 with a closed token_source enum.</summary>
    public static void ValidateCell(MemoryBenchCellResult cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        if (cell.TokensIn is null || cell.TokensOut is null || cell.TokensTotal is null
            || string.IsNullOrWhiteSpace(cell.TokenSource))
        {
            throw new MemoryBenchTokenSchemaException(
                $"Cell {cell.Plugin}/{cell.PromptId}/{cell.Condition} lacks required token fields (tokens_in, tokens_out, tokens_total, token_source).");
        }

        if (cell.TokensIn < 0 || cell.TokensOut < 0 || cell.TokensTotal < 0)
        {
            throw new MemoryBenchTokenSchemaException(
                $"Cell {cell.Plugin}/{cell.PromptId}/{cell.Condition} has negative token counts.");
        }

        if (cell.TokensTotal != cell.TokensIn + cell.TokensOut)
        {
            throw new MemoryBenchTokenSchemaException(
                $"Cell {cell.Plugin}/{cell.PromptId}/{cell.Condition} tokens_total must equal tokens_in + tokens_out.");
        }

        if (!MemoryBenchTokenSources.All.Contains(cell.TokenSource))
        {
            throw new MemoryBenchTokenSchemaException(
                $"Cell {cell.Plugin}/{cell.PromptId}/{cell.Condition} token_source must be host|estimator|recorded.");
        }
    }

    /// <summary>Validates a JSON payload against the committed result schema required-token rules.</summary>
    public static void ValidateJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var node = JsonNode.Parse(json) as JsonObject
            ?? throw new MemoryBenchTokenSchemaException("Result JSON must be an object.");
        if (node["cells"] is not JsonArray cells || cells.Count == 0)
            throw new MemoryBenchTokenSchemaException("Result JSON cells[] is required.");

        foreach (var item in cells)
        {
            if (item is not JsonObject cell)
                throw new MemoryBenchTokenSchemaException("Each result cell must be an object.");
            RequireInt(cell, "tokens_in");
            RequireInt(cell, "tokens_out");
            RequireInt(cell, "tokens_total");
            if (cell["token_source"] is not JsonValue source
                || source.GetValueKind() != JsonValueKind.String
                || !MemoryBenchTokenSources.All.Contains(source.GetValue<string>()))
            {
                throw new MemoryBenchTokenSchemaException("token_source must be host|estimator|recorded.");
            }
        }
    }

    private static void RequireInt(JsonObject cell, string name)
    {
        if (cell[name] is not JsonValue value
            || value.GetValueKind() is not (JsonValueKind.Number)
            || !value.TryGetValue<int>(out var number)
            || number < 0)
        {
            throw new MemoryBenchTokenSchemaException(name + " must be an integer >= 0.");
        }
    }
}
