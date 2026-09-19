namespace McpServer.Support.Mcp.Web;

/// <summary>
/// TR-MCP-MEMORY-UI-002: Hosts <c>/memory/{id}</c> deep links and fail-closes unknown assets.
/// Missing hashed bundles and <c>/memory/unknown-asset</c> return 404, not index.html with 200.
/// </summary>
public static class MemoryUiEndpoints
{
    /// <summary>Maps Memory UI deep-link and unknown-asset routes.</summary>
    /// <param name="app">Endpoint route builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/memory/{id}", (string id, IWebHostEnvironment environment) =>
        {
            if (IsUnknownAsset(id) || !IsMemoryId(id))
                return Results.NotFound();

            var file = Path.Combine(environment.WebRootPath ?? string.Empty, "memory", "index.html");
            return File.Exists(file)
                ? Results.File(file, "text/html")
                : Results.NotFound();
        }).ExcludeFromDescription();

        return app;
    }

    /// <summary>Hashed bundles, extensions, and the unknown-asset probe must 404.</summary>
    public static bool IsUnknownAsset(string id)
        => string.IsNullOrWhiteSpace(id)
            || id.Contains('.', StringComparison.Ordinal)
            || string.Equals(id, "unknown-asset", StringComparison.OrdinalIgnoreCase);

    /// <summary>Deep links only open <c>MEMORY-*</c> ids.</summary>
    public static bool IsMemoryId(string id)
        => id.StartsWith("MEMORY-", StringComparison.OrdinalIgnoreCase);
}
