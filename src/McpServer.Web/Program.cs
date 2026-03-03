using System.Diagnostics;
using McpServer.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var startupStopwatch = Stopwatch.StartNew();
using var bootstrapLoggerFactory = LoggerFactory.Create(static logging =>
{
    logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss.fff ";
    });
});
var bootstrapLogger = bootstrapLoggerFactory.CreateLogger("McpServer.Web.Bootstrap");
bootstrapLogger.LogInformation("Bootstrap starting for McpServer.Web. PID {ProcessId}", Environment.ProcessId);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();
bootstrapLogger.LogInformation("Razor components configured.");

var authSchemesSection = builder.Configuration.GetSection("Authentication:Schemes");
var cookieSection = authSchemesSection.GetSection("Cookie");
var oidcSection = authSchemesSection.GetSection("OpenIdConnect");
var claimMappingSection = oidcSection.GetSection("ClaimMapping");
var authorizationSection = builder.Configuration.GetSection("Authentication:Authorization");

builder.Services.AddCascadingAuthenticationState();
bootstrapLogger.LogInformation("Authentication state cascading configured.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = cookieSection["CookieName"] ?? "McpServer.Web.Auth";
        options.LoginPath = cookieSection["LoginPath"] ?? "/login";
        options.LogoutPath = cookieSection["LogoutPath"] ?? "/logout";
        options.AccessDeniedPath = cookieSection["AccessDeniedPath"] ?? "/access-denied";
    })
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.Authority = oidcSection["Authority"] ?? "https://example.invalid";
        options.ClientId = oidcSection["ClientId"] ?? "placeholder-client-id";
        options.ClientSecret = oidcSection["ClientSecret"] ?? "placeholder-client-secret";
        options.ResponseType = oidcSection["ResponseType"] ?? "code";
        options.CallbackPath = oidcSection["CallbackPath"] ?? "/signin-oidc";
        options.SignedOutCallbackPath = oidcSection["SignedOutCallbackPath"] ?? "/signout-callback-oidc";
        options.MapInboundClaims = oidcSection.GetValue<bool?>("MapInboundClaims") ?? false;
        options.GetClaimsFromUserInfoEndpoint = oidcSection.GetValue<bool?>("GetClaimsFromUserInfoEndpoint") ?? true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = claimMappingSection["NameClaimType"] ?? "name",
            RoleClaimType = claimMappingSection["RoleClaimType"] ?? "role",
        };
        options.SaveTokens = true;

        var configuredScopes = oidcSection.GetSection("Scope").GetChildren()
            .Select(static scope => scope.Value)
            .Where(static scope => !string.IsNullOrWhiteSpace(scope))
            .ToArray();

        if (configuredScopes.Length > 0)
        {
            options.Scope.Clear();
            foreach (var scope in configuredScopes)
            {
                options.Scope.Add(scope!);
            }
        }
    });
bootstrapLogger.LogInformation("Authentication schemes configured.");

var authorizationBuilder = builder.Services.AddAuthorizationBuilder();
if (authorizationSection.GetValue<bool?>("RequireAuthenticatedUserByDefault") == true)
{
    authorizationBuilder.SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());
}

foreach (var policySection in authorizationSection.GetSection("Policies").GetChildren())
{
    var roles = policySection.GetSection("Roles").GetChildren()
        .Select(static role => role.Value)
        .Where(static role => !string.IsNullOrWhiteSpace(role))
        .ToArray();

    if (roles.Length > 0)
    {
        authorizationBuilder.AddPolicy(policySection.Key, policy => policy.RequireRole(roles!));
    }
}
bootstrapLogger.LogInformation("Authorization policies configured.");

builder.Services.AddWebServices();
bootstrapLogger.LogInformation("Web services registered.");

bootstrapLogger.LogInformation("Building app host.");
var app = builder.Build();
bootstrapLogger.LogInformation("App host built after {ElapsedMs}ms.", startupStopwatch.ElapsedMilliseconds);

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
{
    if (eventArgs.ExceptionObject is Exception ex)
    {
        app.Logger.LogCritical(ex, "Unhandled exception in McpServer.Web. IsTerminating: {IsTerminating}", eventArgs.IsTerminating);
    }
    else
    {
        app.Logger.LogCritical("Unhandled non-exception object in McpServer.Web. IsTerminating: {IsTerminating}", eventArgs.IsTerminating);
    }
};

TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
{
    app.Logger.LogError(eventArgs.Exception, "Unobserved task exception in McpServer.Web.");
};

app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Logger.LogInformation(
        "McpServer.Web started. URLs: {Urls}. StartupElapsedMs: {ElapsedMs}",
        string.Join(", ", app.Urls),
        startupStopwatch.ElapsedMilliseconds);
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    app.Logger.LogWarning("McpServer.Web is stopping.");
});

app.Lifetime.ApplicationStopped.Register(() =>
{
    app.Logger.LogWarning("McpServer.Web stopped.");
});

app.Logger.LogInformation("Calling app.Run for McpServer.Web.");
try
{
    app.Run();
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "McpServer.Web terminated with a startup/runtime exception.");
    throw;
}
finally
{
    app.Logger.LogInformation("app.Run exited after {ElapsedMs}ms.", startupStopwatch.ElapsedMilliseconds);
}
