using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using QBrainAi.Client.Models;

namespace QBrainAi.Client;

/// <summary>
/// Client for diagnostic endpoints (<c>/qbrainai/diagnostic</c>).
/// </summary>
public sealed class DiagnosticClient : McpClientBase
{
    /// <inheritdoc />
    public DiagnosticClient(HttpClient http, QBrainAiClientOptions options)
        : base(http, options) { }

    internal DiagnosticClient(HttpClient http, QBrainAiClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Gets execution-path diagnostic details.</summary>
    public async Task<DiagnosticExecutionPathResult> GetExecutionPathAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<DiagnosticExecutionPathResult>(
            "qbrainai/diagnostic/execution-path",
            cancellationToken);
    }

    /// <summary>Gets resolved appsettings-path diagnostic details.</summary>
    public async Task<DiagnosticAppSettingsPathResult> GetAppSettingsPathAsync(CancellationToken cancellationToken = default)
    {
        return await GetAsync<DiagnosticAppSettingsPathResult>(
            "qbrainai/diagnostic/appsettings-path",
            cancellationToken);
    }
}
