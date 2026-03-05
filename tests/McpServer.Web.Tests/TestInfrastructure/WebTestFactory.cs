using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace McpServer.Web.Tests.TestInfrastructure;

/// <summary>
/// Shared factory helpers for <see cref="WebApplicationFactory{TEntryPoint}"/>-based integration tests.
/// Stubs the OIDC discovery metadata so no real network calls are made to the provider during tests,
/// and forces <c>RequireAuthenticatedUserByDefault = false</c> so unauthenticated tests are not redirected.
/// </summary>
internal static class WebTestFactory
{
    /// <summary>
    /// The fake OIDC authorization endpoint injected during tests.
    /// Login tests verify that the challenge redirects to this URL.
    /// </summary>
    public const string FakeAuthorizationEndpoint = "https://test-oidc.invalid/auth";

    /// <summary>The fake OIDC token endpoint.</summary>
    public const string FakeTokenEndpoint = "https://test-oidc.invalid/token";

    /// <summary>The fake OIDC end-session endpoint.</summary>
    public const string FakeEndSessionEndpoint = "https://test-oidc.invalid/logout";

    /// <summary>
    /// Creates a <see cref="McpWebTestFactory"/> with OIDC metadata stubbed out
    /// and, optionally, a pre-authenticated user injected via <see cref="TestAuthHandler"/>.
    /// </summary>
    /// <param name="authenticatedUser">
    /// When non-null, registers a <see cref="TestAuthHandler"/> that returns this principal
    /// as the authenticated user and overrides the default auth scheme accordingly.
    /// </param>
    public static McpWebTestFactory Create(ClaimsPrincipal? authenticatedUser = null)
        => new(authenticatedUser);

    /// <summary>Builds an authenticated <see cref="ClaimsPrincipal"/> with the given roles.</summary>
    public static ClaimsPrincipal BuildUser(string name = "test-user", params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, name) };
        claims.AddRange(roles.Select(static r => new Claim("role", r)));
        var identity = new ClaimsIdentity(claims, TestAuthHandler.SchemeName);
        return new ClaimsPrincipal(identity);
    }
}

/// <summary>
/// <see cref="WebApplicationFactory{Program}"/> subclass that disables DI validation-on-build
/// (since <see cref="McpServer.Web"/> only registers adapters for its own feature area, not the full
/// <c>UI.Core</c> handler surface) and stubs out OIDC metadata.
/// </summary>
internal sealed class McpWebTestFactory : WebApplicationFactory<Program>
{
    private readonly ClaimsPrincipal? _authenticatedUser;

    public McpWebTestFactory(ClaimsPrincipal? authenticatedUser = null)
    {
        _authenticatedUser = authenticatedUser;
    }

    /// <inheritdoc />
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Disable ValidateOnBuild so the missing UI.Core handler API client registrations
        // don't prevent the test host from starting.
        builder.UseServiceProviderFactory(new DefaultServiceProviderFactory(
            new ServiceProviderOptions
            {
                ValidateOnBuild = false,
                ValidateScopes = true,
            }));
        return base.CreateHost(builder);
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        // Override config so we don't require auth by default in tests.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authorization:RequireAuthenticatedUserByDefault"] = "false",
                ["Authentication:Schemes:OpenIdConnect:Authority"] = "https://test-oidc.invalid",
                ["Authentication:Schemes:OpenIdConnect:ClientId"] = "test-web-client",
                ["Authentication:Schemes:OpenIdConnect:DiscoverAuthorityFromMcpAuthConfig"] = "false",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Stub out OIDC discovery so no real HTTP calls are made to the provider.
            services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.RequireHttpsMetadata = false;
                options.Configuration = new OpenIdConnectConfiguration
                {
                    AuthorizationEndpoint = WebTestFactory.FakeAuthorizationEndpoint,
                    TokenEndpoint = WebTestFactory.FakeTokenEndpoint,
                    EndSessionEndpoint = WebTestFactory.FakeEndSessionEndpoint,
                    Issuer = "https://test-oidc.invalid",
                };
                options.BackchannelHttpHandler = new NoOpHttpMessageHandler();
            });

            if (_authenticatedUser is not null)
            {
                // Register the test user so TestAuthHandler can resolve it from DI.
                services.AddSingleton(_authenticatedUser);

                // Replace the default auth scheme with the test handler.
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName,
                        _ => { });

                services.Configure<AuthenticationOptions>(authOptions =>
                {
                    authOptions.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    authOptions.DefaultSignInScheme = TestAuthHandler.SchemeName;
                    // Keep OIDC as the challenge scheme so /login still redirects to the provider.
                    authOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                });
            }
        });
    }
}

/// <summary>HTTP handler that always returns 404 — prevents real outbound calls during tests.</summary>
internal sealed class NoOpHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
}
