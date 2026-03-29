using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace McpServer.Support.Mcp.IntegrationTests;

/// <summary>
/// Allocates temporary non-standard ports for integration tests so the suite never relies on the
/// developer-facing default MCP service port.
/// </summary>
internal static class IntegrationTestPortAllocator
{
    private const int ReservedServicePort = 7147;

    /// <summary>
    /// Allocates a temporary TCP port for integration-test configuration.
    /// </summary>
    /// <returns>A temporary port that is never the standard MCP service port.</returns>
    internal static int AllocateTemporaryPort()
    {
        for (var attempt = 0; attempt < 16; attempt++)
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            if (port != ReservedServicePort)
                return port;
        }

        throw new InvalidOperationException("Could not allocate a temporary integration-test port that differs from the standard MCP service port.");
    }

    /// <summary>
    /// Builds the hostname-based MCP base URL used by runtime-generated marker content.
    /// </summary>
    /// <param name="port">Temporary integration-test port.</param>
    /// <returns>A hostname-based HTTP base URL string.</returns>
    internal static string BuildHostBaseUrl(int port)
        => string.Create(CultureInfo.InvariantCulture, $"http://{Dns.GetHostName()}:{port}");

    /// <summary>
    /// Builds a loopback MCP base URL for tests that only need a synthetic endpoint value.
    /// </summary>
    /// <param name="port">Temporary integration-test port.</param>
    /// <returns>A loopback HTTP base URL string.</returns>
    internal static string BuildLoopbackBaseUrl(int port)
        => string.Create(CultureInfo.InvariantCulture, $"http://localhost:{port}");
}
