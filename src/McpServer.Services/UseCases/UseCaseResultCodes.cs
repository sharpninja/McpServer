namespace McpServer.Support.Mcp.UseCases;

/// <summary>
/// TR-MCP-USECASE-003: Optional error prefixes for Result failure messages so REST can map status codes.
/// Handlers may omit prefixes; controllers also apply heuristic matching.
/// </summary>
public static class UseCaseResultCodes
{
    /// <summary>Prefix for not-found failures.</summary>
    public const string NotFound = "not_found:";

    /// <summary>Prefix for validation failures.</summary>
    public const string Validation = "validation:";

    /// <summary>Prefix for conflict failures.</summary>
    public const string Conflict = "conflict:";

    /// <summary>Builds a not-found failure message with prefix.</summary>
    public static string NotFoundMsg(string message) => NotFound + message;

    /// <summary>Builds a validation failure message with prefix.</summary>
    public static string ValidationMsg(string message) => Validation + message;

    /// <summary>Builds a conflict failure message with prefix.</summary>
    public static string ConflictMsg(string message) => Conflict + message;
}
