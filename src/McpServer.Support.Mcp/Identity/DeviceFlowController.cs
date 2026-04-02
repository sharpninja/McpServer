using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using IdentityServerOptions = McpServer.Support.Mcp.Identity.IdentityServerOptions;

namespace McpServer.Support.Mcp.Identity;

/// <summary>
/// Minimal device-flow verification UI for the embedded IdentityServer.
/// Handles the browser-side of the OAuth 2.0 Device Authorization Grant:
/// user enters the code, we validate and grant consent automatically.
/// </summary>
[Route("device")]
public sealed class DeviceFlowController : Controller
{
    private readonly IDeviceFlowInteractionService _interaction;
    private readonly SignInManager<McpUser> _signInManager;
    private readonly UserManager<McpUser> _userManager;

    /// <summary>Initializes a new instance.</summary>
    public DeviceFlowController(
        IDeviceFlowInteractionService interaction,
        SignInManager<McpUser> signInManager,
        UserManager<McpUser> userManager)
    {
        _interaction = interaction;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    /// <summary>
    /// GET /device — Shows a minimal HTML form to enter the user code,
    /// or auto-submits if the code is provided via query string.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? userCode,
        [FromServices] IOptions<IdentityServerOptions> idsOptions)
    {
        if (!idsOptions.Value.Enabled)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(userCode))
        {
            // Auto-validate if user code is in query string
            return await ProcessUserCode(userCode);
        }

        // Show code entry form
        return Content(BuildCodeEntryHtml(), "text/html");
    }

    /// <summary>
    /// POST /device — Processes the submitted user code.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Submit(
        [FromForm] string userCode,
        [FromServices] IOptions<IdentityServerOptions> idsOptions)
    {
        if (!idsOptions.Value.Enabled)
            return NotFound();

        if (string.IsNullOrWhiteSpace(userCode))
            return Content(BuildCodeEntryHtml("Please enter the code displayed in your terminal."), "text/html");

        return await ProcessUserCode(userCode.Trim());
    }

    private async Task<IActionResult> ProcessUserCode(string userCode)
    {
        var request = await _interaction.GetAuthorizationContextAsync(userCode);
        if (request is null)
        {
            return Content(BuildCodeEntryHtml("Invalid or expired code. Please try again."), "text/html");
        }

        // If the user is not signed in, show a login form
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return Content(BuildLoginHtml(userCode), "text/html");
        }

        // User is authenticated — grant consent for all requested scopes
        var consent = new Duende.IdentityServer.Models.ConsentResponse
        {
            ScopesValuesConsented = request.ValidatedResources.RawScopeValues,
        };
        await _interaction.HandleRequestAsync(userCode, consent);

        return Content(BuildSuccessHtml(), "text/html");
    }

    /// <summary>
    /// POST /device/login — Handles username/password login during device flow.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromForm] string userCode,
        [FromForm] string username,
        [FromForm] string password,
        [FromServices] IOptions<IdentityServerOptions> idsOptions)
    {
        if (!idsOptions.Value.Enabled)
            return NotFound();

        var request = await _interaction.GetAuthorizationContextAsync(userCode);
        if (request is null)
            return Content(BuildCodeEntryHtml("Invalid or expired code."), "text/html");

        var result = await _signInManager.PasswordSignInAsync(username, password, isPersistent: false, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return Content(BuildLoginHtml(userCode, "Invalid username or password."), "text/html");
        }

        // Grant consent for all requested scopes
        var consent = new Duende.IdentityServer.Models.ConsentResponse
        {
            ScopesValuesConsented = request.ValidatedResources.RawScopeValues,
        };
        await _interaction.HandleRequestAsync(userCode, consent);

        return Content(BuildSuccessHtml(), "text/html");
    }

    // ── HTML templates ──────────────────────────────────────────────────

    private static string BuildCodeEntryHtml(string? error = null)
    {
        var errorBlock = error is not null
            ? $"""<p style="color:#e74c3c;margin-bottom:16px;">{System.Net.WebUtility.HtmlEncode(error)}</p>"""
            : "";

        return $$"""
        <!DOCTYPE html>
        <html><head><title>Device Authorization — MCP Server</title>
        <style>
            body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; display: flex; justify-content: center; align-items: center; min-height: 100vh; margin: 0; background: #1a1a2e; color: #e0e0e0; }
            .card { background: #16213e; padding: 40px; border-radius: 12px; box-shadow: 0 8px 32px rgba(0,0,0,0.3); max-width: 400px; width: 100%; text-align: center; }
            h1 { color: #00d4aa; font-size: 1.4em; margin-bottom: 8px; }
            p { color: #a0a0b0; margin-bottom: 20px; }
            input[type=text] { width: 80%; padding: 12px; font-size: 1.2em; text-align: center; letter-spacing: 4px; border: 2px solid #2a2a4a; border-radius: 8px; background: #0f0f23; color: #fff; margin-bottom: 16px; }
            button { background: #00d4aa; color: #1a1a2e; border: none; padding: 12px 32px; font-size: 1em; font-weight: bold; border-radius: 8px; cursor: pointer; }
            button:hover { background: #00b894; }
        </style>
        </head><body>
        <div class="card">
            <h1>MCP Server — Device Authorization</h1>
            <p>Enter the code displayed in your terminal</p>
            {{errorBlock}}
            <form method="post" action="/device">
                <input type="text" name="userCode" placeholder="Enter code" autofocus /><br/>
                <button type="submit">Verify</button>
            </form>
        </div>
        </body></html>
        """;
    }

    private static string BuildLoginHtml(string userCode, string? error = null)
    {
        var errorBlock = error is not null
            ? $"""<p style="color:#e74c3c;margin-bottom:16px;">{System.Net.WebUtility.HtmlEncode(error)}</p>"""
            : "";

        return $$"""
        <!DOCTYPE html>
        <html><head><title>Sign In — MCP Server</title>
        <style>
            body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; display: flex; justify-content: center; align-items: center; min-height: 100vh; margin: 0; background: #1a1a2e; color: #e0e0e0; }
            .card { background: #16213e; padding: 40px; border-radius: 12px; box-shadow: 0 8px 32px rgba(0,0,0,0.3); max-width: 400px; width: 100%; text-align: center; }
            h1 { color: #00d4aa; font-size: 1.4em; margin-bottom: 8px; }
            p { color: #a0a0b0; margin-bottom: 20px; }
            input[type=text], input[type=password] { width: 80%; padding: 10px; font-size: 1em; border: 2px solid #2a2a4a; border-radius: 8px; background: #0f0f23; color: #fff; margin-bottom: 12px; }
            button { background: #00d4aa; color: #1a1a2e; border: none; padding: 12px 32px; font-size: 1em; font-weight: bold; border-radius: 8px; cursor: pointer; }
            button:hover { background: #00b894; }
        </style>
        </head><body>
        <div class="card">
            <h1>Sign In to MCP Server</h1>
            <p>Authenticate to authorize the device</p>
            {{errorBlock}}
            <form method="post" action="/device/login">
                <input type="hidden" name="userCode" value="{{System.Net.WebUtility.HtmlEncode(userCode)}}" />
                <input type="text" name="username" placeholder="Username" autofocus /><br/>
                <input type="password" name="password" placeholder="Password" /><br/>
                <button type="submit">Sign In</button>
            </form>
        </div>
        </body></html>
        """;
    }

    private static string BuildSuccessHtml()
    {
        return """
        <!DOCTYPE html>
        <html><head><title>Authorized — MCP Server</title>
        <style>
            body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; display: flex; justify-content: center; align-items: center; min-height: 100vh; margin: 0; background: #1a1a2e; color: #e0e0e0; }
            .card { background: #16213e; padding: 40px; border-radius: 12px; box-shadow: 0 8px 32px rgba(0,0,0,0.3); max-width: 400px; width: 100%; text-align: center; }
            h1 { color: #00d4aa; font-size: 1.4em; }
            p { color: #a0a0b0; }
            .check { font-size: 3em; margin-bottom: 16px; }
        </style>
        </head><body>
        <div class="card">
            <div class="check">&#x2705;</div>
            <h1>Device Authorized</h1>
            <p>You can close this window and return to your terminal.</p>
        </div>
        </body></html>
        """;
    }
}
