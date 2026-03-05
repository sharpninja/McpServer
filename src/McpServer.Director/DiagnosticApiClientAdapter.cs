using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.Director;

/// <summary>Director adapter for <see cref="IDiagnosticApiClient"/>.</summary>
internal sealed class DiagnosticApiClientAdapter : IDiagnosticApiClient
{
    private readonly DirectorMcpContext _context;

    public DiagnosticApiClientAdapter(DirectorMcpContext context)
    {
        _context = context;
    }

    public async Task<DiagnosticExecutionPathSnapshot> GetExecutionPathAsync(CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Diagnostic.GetExecutionPathAsync(cancellationToken).ConfigureAwait(false);
        return new DiagnosticExecutionPathSnapshot(result.ProcessPath, result.BaseDirectory, DateTimeOffset.UtcNow);
    }

    public async Task<DiagnosticAppSettingsSnapshot> GetAppSettingsPathAsync(CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.Diagnostic.GetAppSettingsPathAsync(cancellationToken).ConfigureAwait(false);
        return new DiagnosticAppSettingsSnapshot(
            result.EnvironmentName,
            result.ContentRootPath,
            result.Files.Select(f => new DiagnosticPathFileView(f.Path, f.Exists)).ToList(),
            DateTimeOffset.UtcNow);
    }
}
