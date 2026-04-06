using McpServer.Support.Mcp.Options;

namespace McpServer.Support.Mcp.Identity;

/// <summary>
/// Selects the appropriate <see cref="IOidcProviderStrategy"/> from configuration.
/// Priority: embedded IdentityServer > external authority > disabled.
/// </summary>
internal static class OidcProviderStrategyFactory
{
    public static IOidcProviderStrategy Create(
        IdentityServerOptions identityServerOptions,
        OidcAuthOptions oidcAuthOptions,
        int listenPort)
    {
        if (identityServerOptions.Enabled)
            return new EmbeddedIdentityServerStrategy(identityServerOptions, listenPort);

        if (oidcAuthOptions.Enabled)
            return new ExternalOidcProviderStrategy(oidcAuthOptions);

        return new DisabledOidcProviderStrategy();
    }
}
