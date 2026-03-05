using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.Director;

/// <summary>Director adapter for <see cref="IAuthConfigApiClient"/>.</summary>
internal sealed class AuthConfigApiClientAdapter : IAuthConfigApiClient
{
    private readonly DirectorMcpContext _context;

    public AuthConfigApiClientAdapter(DirectorMcpContext context)
    {
        _context = context;
    }

    public async Task<AuthConfigSnapshot> GetAuthConfigAsync(CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.AuthConfig.GetConfigAsync(cancellationToken).ConfigureAwait(false);
        return new AuthConfigSnapshot(
            result.Enabled,
            result.Authority,
            result.ClientId,
            result.Scopes,
            result.DeviceAuthorizationEndpoint,
            result.TokenEndpoint,
            DateTimeOffset.UtcNow);
    }
}
