namespace McpServer.Director.Handlers;

/// <summary>
/// Handles auth-config discovery for the login dialog so the screen only applies UI updates.
/// </summary>
internal sealed class LoginDialogAuthConfigHandler
{
    private readonly Func<CancellationToken, Task<AuthConfigResponse?>> _discoverAuthConfigAsync;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginDialogAuthConfigHandler"/> class.
    /// Uses Director default URL discovery first, then workspace marker fallback.
    /// </summary>
    public LoginDialogAuthConfigHandler()
        : this(static ct => DiscoverFromDefaultUrlOrMarkerAsync(ct))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginDialogAuthConfigHandler"/> class with a custom
    /// discovery delegate for tests.
    /// </summary>
    internal LoginDialogAuthConfigHandler(Func<CancellationToken, Task<AuthConfigResponse?>> discoverAuthConfigAsync)
    {
        _discoverAuthConfigAsync = discoverAuthConfigAsync ?? throw new ArgumentNullException(nameof(discoverAuthConfigAsync));
    }

    public async Task<AuthConfigResponse?> DiscoverAuthConfigAsync(CancellationToken ct = default)
    {
        try
        {
            return await _discoverAuthConfigAsync(ct).ConfigureAwait(true);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<AuthConfigResponse?> DiscoverFromDefaultUrlOrMarkerAsync(CancellationToken ct)
    {
        using var client = McpHttpClient.FromDefaultUrlOrMarker();
        if (client is null)
            return null;

        return await client.GetAuthConfigAsync(ct).ConfigureAwait(true);
    }
}
