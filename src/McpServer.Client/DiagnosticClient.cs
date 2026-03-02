using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// Client for diagnostic endpoints (<c>/mcpserver/diagnostic</c>).
/// </summary>
public sealed class DiagnosticClient : McpClientBase
{
    /// <inheritdoc />
    public DiagnosticClient(HttpClient http, McpServerClientOptions options)
        : base(http, options) { }

    internal DiagnosticClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Gets execution-path diagnostic details.</summary>
    public async Task<DiagnosticExecutionPathResult> GetExecutionPathAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<DiagnosticExecutionPathResult>(
            "mcpserver/diagnostic/execution-path",
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Gets resolved appsettings-path diagnostic details.</summary>
    public async Task<DiagnosticAppSettingsPathResult> GetAppSettingsPathAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<DiagnosticAppSettingsPathResult>(
            "mcpserver/diagnostic/appsettings-path",
            cancellationToken).ConfigureAwait(false);
    }
}
