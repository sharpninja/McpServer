namespace McpServer.UI.Core.Services;

/// <summary>
/// Host-provided abstraction for JSON parsing and validation used by UI.Core ViewModels.
/// </summary>
public interface IJsonParsingService
{
    /// <summary>Parses JSON text into a tree structure for display.</summary>
    JsonTreeResult ParseToTree(string jsonText);

    /// <summary>Validates JSON syntax. Returns null if valid, or an error message.</summary>
    string? Validate(string jsonText);

    /// <summary>Pretty-prints JSON text with standard indentation.</summary>
    string PrettyPrint(string jsonText);
}

/// <summary>
/// Result of parsing JSON into a navigable tree.
/// </summary>
/// <param name="RootNode">The root object of the parsed JSON tree (implementation-defined type).</param>
/// <param name="NodeCount">Total number of nodes in the tree.</param>
public sealed record JsonTreeResult(object RootNode, int NodeCount);
