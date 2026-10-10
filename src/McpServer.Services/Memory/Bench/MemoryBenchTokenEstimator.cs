using System.Text.RegularExpressions;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-018-34 / TR-MCP-MEMORY-BENCH-002-11:
/// Pure, versioned whitespace tokenizer used when the host does not report usage.
/// </summary>
public static class MemoryBenchTokenEstimator
{
    /// <summary>Stable estimator id recorded on cells.</summary>
    public const string EstimatorId = "memory-bench-whitespace";

    /// <summary>Estimator semver for deterministic stub re-runs.</summary>
    public const string EstimatorVersion = "1.0.0";

    private static readonly Regex Tokenizer = new(@"\S+", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>Counts tokens as non-whitespace runs. Pure and deterministic.</summary>
    public static int Count(string? text)
        => string.IsNullOrWhiteSpace(text) ? 0 : Tokenizer.Matches(text).Count;

    /// <summary>
    /// FR-MCP-MEMORY-018-35: tokens_in = prompt/system/injection/tool-result;
    /// tokens_out = completion; tokens_total = in + out.
    /// </summary>
    public static (int TokensIn, int TokensOut, int TokensTotal) EstimateTurn(
        string? systemAndPrompt,
        string? injection,
        string? toolPayloads,
        string? completion)
    {
        var tokensIn = Count(systemAndPrompt) + Count(injection) + Count(toolPayloads);
        var tokensOut = Count(completion);
        return (tokensIn, tokensOut, tokensIn + tokensOut);
    }
}
