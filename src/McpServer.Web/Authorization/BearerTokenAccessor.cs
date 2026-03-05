using Microsoft.AspNetCore.Authentication;

namespace McpServer.Web.Authorization;

/// <summary>
/// Retrieves the OIDC access token saved in the current user's authentication session.
/// Used by <see cref="WebMcpContext"/> to forward the user's bearer token to the McpServer API
/// instead of using a static API key when the user is authenticated.
/// </summary>
internal sealed class BearerTokenAccessor
{
    private readonly IHttpContextAccessor _accessor;

    /// <summary>Initializes a new <see cref="BearerTokenAccessor"/>.</summary>
    /// <param name="accessor">ASP.NET Core HTTP context accessor.</param>
    public BearerTokenAccessor(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    /// <summary>
    /// Returns the OIDC <c>access_token</c> saved in the session, or <c>null</c> if the user
    /// is not authenticated or tokens were not saved (<c>options.SaveTokens</c> must be <c>true</c>).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = _accessor.HttpContext;
        if (httpContext is null)
            return null;

        if (httpContext.User.Identity?.IsAuthenticated != true)
            return null;

        return await httpContext.GetTokenAsync("access_token").ConfigureAwait(false);
    }
}
