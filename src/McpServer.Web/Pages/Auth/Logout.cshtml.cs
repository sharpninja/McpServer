using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace McpServer.Web.Pages.Auth;

/// <summary>
/// Non-interactive Razor Page that signs the user out of both the local cookie session and the OIDC provider.
/// Must be a Razor Page (not a Blazor component) because <c>HttpContext.SignOutAsync</c> requires
/// a real HTTP context outside of a Blazor SignalR circuit.
/// </summary>
public sealed class LogoutModel : PageModel
{
    /// <summary>
    /// Signs the user out of the cookie scheme, then issues a SignOut to the OIDC provider
    /// which triggers the provider's end_session redirect back to the app root.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)
            .ConfigureAwait(false);

        return SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            OpenIdConnectDefaults.AuthenticationScheme);
    }
}
