using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Identity;

/// <summary>
/// Selects the appropriate <see cref="IOidcProviderStrategy"/> from configuration.
/// Priority: embedded IdentityServer > external authority > disabled.
/// </summary>
internal static class OidcProviderStrategyFactory
{
    /// <summary>
    /// Creates the OIDC provider strategy based on configuration.
    /// </summary>
    public static IOidcProviderStrategy Create(
        IdentityServerOptions identityServerOptions,
        OidcAuthOptions oidcAuthOptions,
        int listenPort,
        ILogger? logger = null)
    {
        logger?.LogInformation(
            "OIDC strategy selection: IdentityServer.Enabled={IdsEnabled}, OidcAuth.Enabled={AuthEnabled}, OidcAuth.Authority={Authority}, ListenPort={Port}",
            identityServerOptions.Enabled, oidcAuthOptions.Enabled, oidcAuthOptions.Authority, listenPort);

        if (identityServerOptions.Enabled)
        {
            logger?.LogInformation("OIDC strategy selected: EmbeddedIdentityServerStrategy");
            return new EmbeddedIdentityServerStrategy(identityServerOptions, listenPort);
        }

        if (oidcAuthOptions.Enabled)
        {
            logger?.LogInformation("OIDC strategy selected: ExternalOidcProviderStrategy (authority={Authority})", oidcAuthOptions.Authority);
            return new ExternalOidcProviderStrategy(oidcAuthOptions);
        }

        logger?.LogInformation("OIDC strategy selected: DisabledOidcProviderStrategy");
        return new DisabledOidcProviderStrategy();
    }
}
