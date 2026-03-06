using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using McpServer.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using NetEscapades.Configuration.Yaml;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Tokens;

// When running as a dotnet global tool, wwwroot is next to the assembly, not in CWD.
var assemblyDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
var toolWebRoot = Path.Combine(assemblyDir, "wwwroot");
var defaultWebRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

var options = new WebApplicationOptions { Args = args };
if (Directory.Exists(toolWebRoot) && !Directory.Exists(defaultWebRoot))
{
    options = new WebApplicationOptions { Args = args, WebRootPath = toolWebRoot };
}

var builder = WebApplication.CreateBuilder(options);

// Enable RCL static web assets in all environments (needed for BlazorMonaco, etc.).
if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.UseStaticWebAssets();
}

builder.Configuration.AddYamlFile("appsettings.yaml", optional: true, reloadOnChange: true);
builder.Configuration.AddYamlFile($"appsettings.{builder.Environment.EnvironmentName}.yaml", optional: true, reloadOnChange: true);
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
builder.Services.AddRazorPages();
bootstrapLogger.LogInformation("Razor components and pages configured.");

var authSchemesSection = builder.Configuration.GetSection("Authentication:Schemes");
var cookieSection = authSchemesSection.GetSection("Cookie");
var oidcSection = authSchemesSection.GetSection("OpenIdConnect");
var claimMappingSection = oidcSection.GetSection("ClaimMapping");
var authorizationSection = builder.Configuration.GetSection("Authentication:Authorization");
var mcpServerBaseUrl = builder.Configuration["McpServer:BaseUrl"] ?? "http://localhost:7147";
var oidcAuthority = oidcSection["Authority"];
var oidcClientId = oidcSection["ClientId"];
var oidcClientSecret = oidcSection["ClientSecret"];
var discoverOidcAuthorityFromMcpAuthConfig = oidcSection.GetValue<bool?>("DiscoverAuthorityFromMcpAuthConfig") ?? true;
if (discoverOidcAuthorityFromMcpAuthConfig)
{
    var discoveredOidcConfig = await TryDiscoverOidcConfigFromMcpAsync(mcpServerBaseUrl, bootstrapLogger);
    if (discoveredOidcConfig is not null)
    {
        oidcAuthority = discoveredOidcConfig.Authority;
        if (IsPlaceholderOrEmpty(oidcClientId) && !string.IsNullOrWhiteSpace(discoveredOidcConfig.ClientId))
        {
            oidcClientId = discoveredOidcConfig.ClientId;
        }

        bootstrapLogger.LogInformation(
            "Resolved OIDC auth settings from MCP /auth/config discovery. Authority={Authority}, ClientId={ClientId}",
            oidcAuthority,
            oidcClientId ?? "(null)");
    }
}

var oidcEnabled = IsOidcConfigurationUsable(oidcAuthority, oidcClientId);
var requireHttpsMetadataOverride = oidcSection.GetValue<bool?>("RequireHttpsMetadata");
var oidcAuthorityUsesHttps = Uri.TryCreate(oidcAuthority, UriKind.Absolute, out var parsedOidcAuthorityUri)
                             && parsedOidcAuthorityUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
var requireHttpsMetadata = requireHttpsMetadataOverride ?? oidcAuthorityUsesHttps;
var enableHttpsRedirection = builder.Configuration.GetValue<bool?>("Web:EnableHttpsRedirection")
                            ?? ShouldEnableHttpsRedirection(builder.Configuration);

builder.Services.AddCascadingAuthenticationState();
bootstrapLogger.LogInformation("Authentication state cascading configured.");

var authenticationBuilder = builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        if (oidcEnabled)
        {
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        }
    });

authenticationBuilder
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = cookieSection["CookieName"] ?? "McpServer.Web.Auth";
        options.LoginPath = cookieSection["LoginPath"] ?? "/login";
        options.LogoutPath = cookieSection["LogoutPath"] ?? "/logout";
        options.AccessDeniedPath = cookieSection["AccessDeniedPath"] ?? "/access-denied";
    });

if (oidcEnabled)
{
    authenticationBuilder.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.Authority = oidcAuthority!;
        options.ClientId = oidcClientId!;
        options.RequireHttpsMetadata = requireHttpsMetadata;
        if (!string.IsNullOrWhiteSpace(oidcClientSecret))
        {
            options.ClientSecret = oidcClientSecret;
        }
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

    if (!requireHttpsMetadata)
    {
        bootstrapLogger.LogWarning(
            "OpenID Connect RequireHttpsMetadata is disabled for authority {Authority}. This is intended for local/dev non-HTTPS authorities only.",
            oidcAuthority);
    }
}
else
{
    bootstrapLogger.LogWarning(
        "OpenID Connect is disabled because configuration is missing or placeholder. Authority='{Authority}', ClientId='{ClientId}'.",
        oidcAuthority ?? "(null)",
        oidcClientId ?? "(null)");
}

bootstrapLogger.LogInformation("Authentication schemes configured. OIDC enabled: {OidcEnabled}", oidcEnabled);

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

if (enableHttpsRedirection)
{
    app.UseHttpsRedirection();
}
else
{
    app.Logger.LogInformation("HTTPS redirection middleware is disabled because no HTTPS endpoint/port is configured.");
}
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
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

static async Task<OidcDiscoveryConfigResponse?> TryDiscoverOidcConfigFromMcpAsync(
    string? mcpServerBaseUrl,
    ILogger logger,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(mcpServerBaseUrl) ||
        !Uri.TryCreate(mcpServerBaseUrl, UriKind.Absolute, out var baseUri))
    {
        logger.LogWarning(
            "Skipping OIDC discovery because McpServer:BaseUrl is not a valid absolute URL: {BaseUrl}",
            mcpServerBaseUrl ?? "(null)");
        return null;
    }

    var authConfigUri = new Uri(baseUri, "/auth/config");
    try
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        using var response = await httpClient.GetAsync(authConfigUri, cancellationToken).ConfigureAwait(true);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "OIDC discovery call to {AuthConfigUri} returned HTTP {StatusCode}; falling back to local Web UI auth config.",
                authConfigUri,
                (int)response.StatusCode);
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(true);
        var discovered = await JsonSerializer.DeserializeAsync<OidcDiscoveryConfigResponse>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken).ConfigureAwait(true);
        if (discovered is null || !discovered.Enabled || string.IsNullOrWhiteSpace(discovered.Authority))
        {
            return null;
        }

        return discovered;
    }
    catch (Exception ex)
    {
        logger.LogWarning(
            ex,
            "OIDC discovery failed for {AuthConfigUri}; falling back to local Web UI auth config.",
            authConfigUri);
        return null;
    }
}

static bool IsOidcConfigurationUsable(string? authority, string? clientId)
{
    if (IsPlaceholderOrEmpty(authority) || IsPlaceholderOrEmpty(clientId))
    {
        return false;
    }

    if (!Uri.TryCreate(authority, UriKind.Absolute, out var authorityUri))
    {
        return false;
    }

    var isHttpScheme = authorityUri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
        || authorityUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    if (!isHttpScheme)
    {
        return false;
    }

    return !authorityUri.Host.Equals("example.invalid", StringComparison.OrdinalIgnoreCase);
}

static bool IsPlaceholderOrEmpty(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return true;
    }

    var trimmed = value.Trim();
    return trimmed.Contains("example.invalid", StringComparison.OrdinalIgnoreCase)
           || trimmed.Contains("placeholder", StringComparison.OrdinalIgnoreCase)
           || trimmed.Equals("change-me-in-user-secrets", StringComparison.OrdinalIgnoreCase);
}

static bool ShouldEnableHttpsRedirection(IConfiguration configuration)
{
    if (configuration.GetValue<int?>("HttpsRedirection:HttpsPort").HasValue ||
        configuration.GetValue<int?>("ASPNETCORE_HTTPS_PORT").HasValue ||
        configuration.GetValue<int?>("HTTPS_PORT").HasValue)
    {
        return true;
    }

    var urls = configuration["ASPNETCORE_URLS"] ?? configuration["urls"];
    if (string.IsNullOrWhiteSpace(urls))
    {
        return false;
    }

    return urls
        .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Any(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
}

file sealed class OidcDiscoveryConfigResponse
{
    public bool Enabled { get; set; }

    public string? Authority { get; set; }

    public string? ClientId { get; set; }
}
