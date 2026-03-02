namespace McpServer.Director.Handlers;

/// <summary>
/// Handles auth-config discovery for the login dialog so the screen only applies UI updates.
/// </summary>
internal sealed class LoginDialogAuthConfigHandler
{
    public async Task<AuthConfigResponse?> DiscoverAuthConfigAsync(CancellationToken ct = default)
    {
        try
        {
            using var client = McpHttpClient.FromMarkerFile();
            if (client is null)
                return null;

            return await client.GetAuthConfigAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }
}
