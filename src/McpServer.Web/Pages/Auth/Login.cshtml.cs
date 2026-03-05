using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace McpServer.Web.Pages.Auth;

/// <summary>
/// Non-interactive Razor Page that issues an OIDC challenge redirect.
/// Must be a Razor Page (not a Blazor component) because <c>HttpContext.ChallengeAsync</c> requires
/// a real HTTP context outside of a Blazor SignalR circuit.
/// </summary>
public sealed class LoginModel : PageModel
{
    /// <summary>
    /// Issues an OpenID Connect challenge, redirecting the browser to the OIDC provider's authorization endpoint.
    /// </summary>
    /// <param name="returnUrl">Optional local URL to return to after login. Non-local URLs are ignored.</param>
    public IActionResult OnGet(string? returnUrl)
    {
        var redirectUri = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : "/";

        return Challenge(
            new AuthenticationProperties { RedirectUri = redirectUri },
            OpenIdConnectDefaults.AuthenticationScheme);
    }
}
