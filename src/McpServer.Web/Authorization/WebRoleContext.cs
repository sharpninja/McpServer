using McpServer.UI.Core.Authorization;

namespace McpServer.Web.Authorization;

/// <summary>
/// Blazor Server implementation of <see cref="IRoleContext"/> that reads authentication state
/// from the current <see cref="IHttpContextAccessor.HttpContext"/>.
/// </summary>
/// <remarks>
/// Role claims are expected under the claim type matching <c>Authentication:Schemes:OpenIdConnect:ClaimMapping:RoleClaimType</c>
/// in appsettings.json (default: <c>role</c>).
/// Register before calling <c>AddUiCore()</c> so this implementation wins over the
/// <see cref="AllowAllRoleContext"/> fallback registered with <c>TryAddSingleton</c>.
/// </remarks>
internal sealed class WebRoleContext : IRoleContext
{
    private const string RoleClaimType = "role";

    private readonly IHttpContextAccessor _accessor;

    /// <summary>Initializes a new <see cref="WebRoleContext"/>.</summary>
    /// <param name="accessor">ASP.NET Core HTTP context accessor.</param>
    public WebRoleContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    /// <inheritdoc />
    public bool IsAuthenticated
        => _accessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc />
    public IReadOnlyList<string> Roles
        => _accessor.HttpContext?.User.Claims
               .Where(static c => string.Equals(c.Type, RoleClaimType, StringComparison.OrdinalIgnoreCase))
               .Select(static c => c.Value)
               .ToList()
           ?? (IReadOnlyList<string>)[];

    /// <inheritdoc />
    public bool HasRole(string role)
        => Roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
}
