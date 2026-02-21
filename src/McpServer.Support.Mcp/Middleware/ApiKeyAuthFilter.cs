using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Support.Mcp.Middleware;

/// <summary>
/// Action filter that validates requests contain a valid API key.
/// The key is read from the <c>Mcp:ApiKey</c> configuration section and
/// matched against the <c>X-Api-Key</c> request header (or <c>api_key</c> query parameter).
/// When the configured key is empty or missing, the filter is a no-op (all requests pass).
/// Actions decorated with <see cref="SkipApiKeyAuthAttribute"/> bypass this filter.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class ApiKeyAuthFilter : Attribute, IAsyncActionFilter
{
    /// <summary>The configuration key that holds the expected API key value.</summary>
    public const string ConfigKey = "Mcp:ApiKey";

    /// <summary>The HTTP header used to supply the API key.</summary>
    public const string HeaderName = "X-Api-Key";

    /// <summary>The query-string parameter accepted as a fallback.</summary>
    public const string QueryParam = "api_key";

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Skip if the action or controller is decorated with [SkipApiKeyAuth].
        if (context.ActionDescriptor.EndpointMetadata.OfType<SkipApiKeyAuthAttribute>().Any())
        {
            await next().ConfigureAwait(false);
            return;
        }

        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expectedKey = configuration.GetValue<string>(ConfigKey);

        // When no key is configured, allow all requests (open mode).
        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            await next().ConfigureAwait(false);
            return;
        }

        // Check header first, then query string.
        var providedKey = context.HttpContext.Request.Headers[HeaderName].FirstOrDefault()
                          ?? context.HttpContext.Request.Query[QueryParam].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(providedKey) || !string.Equals(providedKey, expectedKey, StringComparison.Ordinal))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Invalid or missing API key." });
            return;
        }

        await next().ConfigureAwait(false);
    }
}

/// <summary>
/// Marks an action to bypass the <see cref="ApiKeyAuthFilter"/>.
/// Use on read-only endpoints that must be publicly accessible.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SkipApiKeyAuthAttribute : Attribute
{
}
