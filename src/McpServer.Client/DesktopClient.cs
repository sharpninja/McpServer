using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// FR-MCP-047/TR-MCP-DESKTOP-001: Client for desktop-launch endpoints
/// (<c>/mcpserver/desktop</c>).
/// </summary>
/// <seealso cref="McpServerClient.Desktop"/>
public sealed class DesktopClient : McpClientBase
{
    private const string DesktopLaunchTokenHeaderName = "X-Desktop-Launch-Token";

    /// <inheritdoc />
    public DesktopClient(HttpClient http, McpServerClientOptions options)
        : base(http, options)
    {
        DesktopLaunchToken = options.DesktopLaunchToken ?? string.Empty;
    }

    internal DesktopClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder)
    {
        DesktopLaunchToken = options.DesktopLaunchToken ?? string.Empty;
    }

    /// <summary>
    /// Optional privileged token sent only to the desktop-launch endpoint so remote callers can
    /// satisfy the server's desktop-launch authorization tier in addition to workspace auth.
    /// </summary>
    public string DesktopLaunchToken { get; set; } = string.Empty;

    /// <summary>
    /// Launches a local desktop process through the authenticated MCP Server workspace.
    /// </summary>
    /// <param name="request">Structured launch request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The typed launch result returned by the server.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is <see langword="null"/>.</exception>
    public async Task<DesktopLaunchResult> LaunchAsync(
        DesktopLaunchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await PostAsync<DesktopLaunchResult>("mcpserver/desktop/launch", request, cancellationToken);
    }

    /// <inheritdoc />
    protected override void AppendCustomHeaders(HttpRequestMessage request)
    {
        base.AppendCustomHeaders(request);

        if (!string.IsNullOrWhiteSpace(DesktopLaunchToken))
            request.Headers.TryAddWithoutValidation(DesktopLaunchTokenHeaderName, DesktopLaunchToken);
    }
}
