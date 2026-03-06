using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace McpServer.Web.Pages.Auth;

/// <summary>
/// Non-interactive Razor Page that issues an OIDC challenge redirect.
/// Must be a Razor Page (not a Blazor component) because <c>HttpContext.ChallengeAsync</c> requires
/// a real HTTP context outside of a Blazor SignalR circuit.
/// </summary>
public sealed class LoginModel : PageModel
{
    private readonly IAuthenticationSchemeProvider _schemeProvider;
    private readonly ILogger<LoginModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginModel"/> class.
    /// </summary>
    /// <param name="schemeProvider">Authentication scheme provider used to verify OIDC availability.</param>
    /// <param name="logger">Logger.</param>
    public LoginModel(IAuthenticationSchemeProvider schemeProvider, ILogger<LoginModel> logger)
    {
        _schemeProvider = schemeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Issues an OpenID Connect challenge when configured, redirecting the browser to the OIDC provider's authorization endpoint.
    /// </summary>
    /// <param name="returnUrl">Optional local URL to return to after login. Non-local URLs are ignored.</param>
    public async Task<IActionResult> OnGetAsync(string? returnUrl)
    {
        var oidcScheme = await _schemeProvider.GetSchemeAsync(OpenIdConnectDefaults.AuthenticationScheme).ConfigureAwait(true);
        if (oidcScheme is null)
        {
            _logger.LogWarning("Login requested but OpenID Connect authentication is not configured.");
            return Redirect("/access-denied");
        }

        var redirectUri = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : "/";

        return Challenge(
            new AuthenticationProperties { RedirectUri = redirectUri },
            OpenIdConnectDefaults.AuthenticationScheme);
    }
}
