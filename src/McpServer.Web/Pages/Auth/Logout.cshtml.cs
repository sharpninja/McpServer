using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace McpServer.Web.Pages.Auth;

/// <summary>
/// Non-interactive Razor Page that signs the user out of both the local cookie session and the OIDC provider.
/// Must be a Razor Page (not a Blazor component) because <c>HttpContext.SignOutAsync</c> requires
/// a real HTTP context outside of a Blazor SignalR circuit.
/// </summary>
public sealed class LogoutModel : PageModel
{
    private readonly IAuthenticationSchemeProvider _schemeProvider;
    private readonly ILogger<LogoutModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogoutModel"/> class.
    /// </summary>
    /// <param name="schemeProvider">Authentication scheme provider used to verify OIDC availability.</param>
    /// <param name="logger">Logger.</param>
    public LogoutModel(IAuthenticationSchemeProvider schemeProvider, ILogger<LogoutModel> logger)
    {
        _schemeProvider = schemeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Signs the user out of the cookie scheme, then issues a SignOut to the OIDC provider
    /// which triggers the provider's end_session redirect back to the app root when OIDC is configured.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)
            .ConfigureAwait(false);

        var oidcScheme = await _schemeProvider.GetSchemeAsync(OpenIdConnectDefaults.AuthenticationScheme).ConfigureAwait(false);
        if (oidcScheme is null)
        {
            _logger.LogInformation("Logout requested while OpenID Connect authentication is disabled; completed cookie sign-out only.");
            return Redirect("/");
        }

        return SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            OpenIdConnectDefaults.AuthenticationScheme);
    }
}
