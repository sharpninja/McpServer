using System.Text.Json;
using System.Text.Json.Nodes;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-MEMORY-BENCH-003: Multi-turn result schema. Missing token fields hard-fail.
/// </summary>
public static class MemoryBenchMultiTurnResultSchema
{
    /// <summary>Canonical multi-turn result schema path relative to the repository root.</summary>
    public const string RelativePath = "docs/benchmarks/schemas/memory-bench-multiturn-result.schema.json";

    /// <summary>Validates a run before write.</summary>
    public static void ValidateBeforeWrite(MemoryBenchMultiTurnRunResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Turns.Count == 0)
            throw new MemoryBenchTokenSchemaException("Multi-turn bench run has no turn cells to finalize.");
        if (result.Jobs.Count == 0)
            throw new MemoryBenchTokenSchemaException("Multi-turn bench run has no job cells to finalize.");

        foreach (var turn in result.Turns)
            ValidateTurn(turn);
        foreach (var job in result.Jobs)
            ValidateJob(job);
    }

    /// <summary>Validates one turn cell.</summary>
    public static void ValidateTurn(MemoryBenchTurnCellResult cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        ValidateTokens(cell.Plugin, cell.JobId + "/" + cell.TurnId, cell.Condition, cell.TokensIn, cell.TokensOut, cell.TokensTotal, cell.TokenSource);
    }

    /// <summary>Validates one job cell.</summary>
    public static void ValidateJob(MemoryBenchJobCellResult cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        ValidateTokens(cell.Plugin, cell.JobId, cell.Condition, cell.TokensIn, cell.TokensOut, cell.TokensTotal, cell.TokenSource);
    }

    /// <summary>Validates a JSON payload against required token fields on turns and jobs.</summary>
    public static void ValidateJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var node = JsonNode.Parse(json) as JsonObject
            ?? throw new MemoryBenchTokenSchemaException("Result JSON must be an object.");
        ValidateArray(node["turns"], "turns");
        ValidateArray(node["jobs"], "jobs");
    }

    private static void ValidateArray(JsonNode? node, string name)
    {
        if (node is not JsonArray array || array.Count == 0)
            throw new MemoryBenchTokenSchemaException("Result JSON " + name + "[] is required.");
        foreach (var item in array)
        {
            if (item is not JsonObject cell)
                throw new MemoryBenchTokenSchemaException("Each " + name + " cell must be an object.");
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

    private static void ValidateTokens(
        string plugin,
        string id,
        string condition,
        int tokensIn,
        int tokensOut,
        int tokensTotal,
        string tokenSource)
    {
        if (tokensIn < 0 || tokensOut < 0 || tokensTotal < 0)
            throw new MemoryBenchTokenSchemaException($"Cell {plugin}/{id}/{condition} has negative token counts.");
        if (tokensTotal != tokensIn + tokensOut)
            throw new MemoryBenchTokenSchemaException($"Cell {plugin}/{id}/{condition} tokens_total must equal tokens_in + tokens_out.");
        if (!MemoryBenchTokenSources.All.Contains(tokenSource))
            throw new MemoryBenchTokenSchemaException($"Cell {plugin}/{id}/{condition} token_source must be host|estimator|recorded.");
    }

    private static void RequireInt(JsonObject cell, string name)
    {
        if (cell[name] is not JsonValue value
            || value.GetValueKind() is not JsonValueKind.Number
            || !value.TryGetValue<int>(out var number)
            || number < 0)
        {
            throw new MemoryBenchTokenSchemaException(name + " must be an integer >= 0.");
        }
    }
}
