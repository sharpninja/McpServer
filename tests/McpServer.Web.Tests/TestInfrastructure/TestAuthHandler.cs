using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Web.Tests.TestInfrastructure;

/// <summary>
/// Custom authentication handler that resolves a pre-configured <see cref="ClaimsPrincipal"/>
/// from the DI container for use in integration tests that need a pre-authenticated HTTP request.
/// Register a <see cref="ClaimsPrincipal"/> singleton via <c>services.AddSingleton(user)</c>
/// before adding this handler.
/// </summary>
internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>Authentication scheme name used to register this handler.</summary>
    public const string SchemeName = "TestAuth";

    private readonly ClaimsPrincipal? _user;

    /// <summary>Initializes a new <see cref="TestAuthHandler"/>.</summary>
    public TestAuthHandler(
        IServiceProvider serviceProvider,
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
        // Resolve the pre-configured test user; null means no authentication.
        _user = serviceProvider.GetService<ClaimsPrincipal>();
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (_user is null)
            return Task.FromResult(AuthenticateResult.NoResult());

        var ticket = new AuthenticationTicket(_user, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
