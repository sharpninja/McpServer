namespace McpServer.Support.Mcp.Products;

/// <summary>
/// TR-MCP-PRODUCT-API-001: Result error prefixes so adapters can map HTTP status codes.
/// Handler tests assert the numeric token is present in <c>Result.Error</c>.
/// </summary>
public static class ProductResultCodes
{
    /// <summary>Invalid input (key format, unknown workspace).</summary>
    public const string BadRequest = "400:";

    /// <summary>Authenticated caller is a member but not allowed to mutate.</summary>
    public const string Forbidden = "403:";

    /// <summary>Product is not visible to the caller (or does not exist).</summary>
    public const string NotFound = "404:";

    /// <summary>Duplicate non-deleted product key.</summary>
    public const string Conflict = "409:";

    /// <summary>Builds a 400 failure message.</summary>
    public static string BadRequestMsg(string message) => BadRequest + " " + message;

    /// <summary>Builds a 403 failure message.</summary>
    public static string ForbiddenMsg(string message) => Forbidden + " " + message;

    /// <summary>Builds a 404 failure message.</summary>
    public static string NotFoundMsg(string message) => NotFound + " " + message;

    /// <summary>Builds a 409 failure message.</summary>
    public static string ConflictMsg(string message) => Conflict + " " + message;
}
