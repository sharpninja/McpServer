namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-QBOPENAI-001: Extracts the workspace token from an OpenAI-style request. OpenAI clients send
/// <c>Authorization: Bearer &lt;token&gt;</c>; the <c>X-Api-Key</c> header is accepted as a fallback so the
/// same workspace token works from either header.
/// </summary>
public static class OpenAiBearerAuth
{
    private const string BearerPrefix = "Bearer ";

    /// <summary>Extracts the bearer token (or X-Api-Key fallback), or null when neither is present.</summary>
    /// <param name="authorizationHeader">The raw <c>Authorization</c> header value.</param>
    /// <param name="apiKeyHeader">The raw <c>X-Api-Key</c> header value.</param>
    /// <returns>The extracted token, or null.</returns>
    public static string? ExtractToken(string? authorizationHeader, string? apiKeyHeader)
    {
        if (!string.IsNullOrWhiteSpace(authorizationHeader))
        {
            var header = authorizationHeader.Trim();
            if (header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var token = header[BearerPrefix.Length..].Trim();
                if (token.Length > 0)
                    return token;
            }
        }

        return string.IsNullOrWhiteSpace(apiKeyHeader) ? null : apiKeyHeader.Trim();
    }
}
